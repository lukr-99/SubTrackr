package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.domain.sync.EmailAddress
import com.lukr99.subtrackr.domain.sync.SignInCode
import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import java.io.IOException
import java.time.Clock

/**
 * Per-user sync (SPEC.md section 8): email-code sign-in, token refresh, and sync passes of pull,
 * merge, save locally, push. A pass retries network errors, timeouts, 429, and 5xx under
 * [retry]; a 401 refreshes the session once. Passes never overlap, and requests made while one
 * runs collapse into one more pass. [pause] is the wait between attempts, injectable for tests.
 */
class SyncCoordinator(
    private val repository: AppRepository,
    private val auth: AuthApi,
    private val remote: SyncRemote,
    private val sessions: SessionStore,
    private val clock: Clock,
    private val scope: CoroutineScope,
    private val allowLocalHttp: Boolean = false,
    private val retry: RetryPolicy = RetryPolicy(),
    private val pause: suspend (Long) -> Unit = { delay(it) },
) {
    private val passLock = Mutex()
    private val requests = Channel<Unit>(Channel.CONFLATED)

    @Volatile
    private var session: AuthSession? = sessions.load()

    private val mutableState = MutableStateFlow(SyncState(idleStatus(), session?.email))
    val state: StateFlow<SyncState> = mutableState.asStateFlow()

    /** The configured project, or null when sync is off or the URL or key is not usable. */
    val endpoint: SyncEndpoint?
        get() = SyncEndpoint.parse(repository.syncUrl, repository.syncKey, allowLocalHttp)

    val isSignedIn: Boolean get() = session != null

    /** Starts automatic sync: once now, and after every change made on this device. */
    fun start() {
        scope.launch { for (request in requests) sync() }
        scope.launch { repository.localChanges.collect { requestSync() } }
        requestSync()
    }

    /** Asks for a pass; does nothing while signed out. */
    fun requestSync() {
        if (endpoint != null && session != null) requests.trySend(Unit)
    }

    /** Runs one pass now (waiting for a running one to finish) and returns the resulting status. */
    suspend fun sync(): SyncStatus = passLock.withLock { pass() }

    /** Whether [url] and [key] can be saved: both empty (sync off) or a usable endpoint. */
    fun isValidConfig(url: String, key: String): Boolean =
        (url.isBlank() && key.isBlank()) || SyncEndpoint.parse(url, key, allowLocalHttp) != null

    /** Stores a new project URL and key. A different project signs the user out first. */
    fun saveConfig(url: String, key: String) {
        val newUrl = url.trim()
        val newKey = key.trim()
        if (newUrl == repository.syncUrl && newKey == repository.syncKey) return
        signOut()
        repository.setSyncConfig(newUrl, newKey)
        publish(idleStatus())
    }

    suspend fun sendCode(email: EmailAddress): AuthResult {
        val endpoint = endpoint ?: return AuthResult.NotConfigured
        return signInStep(verifying = false) { auth.sendCode(endpoint, email.value) }
    }

    suspend fun verify(email: EmailAddress, code: SignInCode): AuthResult {
        val endpoint = endpoint ?: return AuthResult.NotConfigured
        return signInStep(verifying = true) {
            val signedIn = sessionFrom(auth.verify(endpoint, email.value, code.value), previous = null)
                .let { if (it.email.isEmpty()) it.copy(email = email.value) else it }
            sessions.save(signedIn)
            session = signedIn
            publish(SyncStatus.Syncing)
            requestSync()
        }
    }

    /** Local first: the stored session is gone even when the logout call fails or never runs. */
    fun signOut() {
        val old = session
        val endpoint = endpoint
        sessions.clear()
        session = null
        publish(idleStatus())
        if (old != null && endpoint != null) {
            scope.launch {
                try {
                    auth.logout(endpoint, old.accessToken)
                } catch (cancelled: CancellationException) {
                    throw cancelled
                } catch (_: Exception) {
                    // The token expires on its own; there is nothing useful to tell the user.
                }
            }
        }
    }

    private suspend fun pass(): SyncStatus {
        val endpoint = endpoint ?: return publish(SyncStatus.Off)
        if (session == null) return publish(SyncStatus.SignedOut)
        publish(SyncStatus.Syncing)
        var attempt = 1
        while (true) {
            try {
                val rows = authorized(endpoint) { token -> remote.pull(endpoint, token) }
                val merged = repository.mergeRemote(rows)
                authorized(endpoint) { token -> remote.push(endpoint, token, currentSession().userId, merged) }
                return publish(SyncStatus.Synced(clock.instant()))
            } catch (rejected: SessionRejectedException) {
                signOut()
                return publish(SyncStatus.SignedOut)
            } catch (error: RemoteCallException) {
                val wait = if (retry.isRetryable(error.failure)) retry.delayAfter(attempt) else null
                if (wait == null) return publish(SyncStatus.Failed(error.failure.shortReason))
                pause(wait)
                attempt++
            } catch (_: IOException) {
                return publish(SyncStatus.Failed("couldn't save on this device"))
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (_: Exception) {
                // A background pass must never take the app down.
                return publish(SyncStatus.Failed("unexpected error"))
            }
        }
    }

    /** Runs [call] with a fresh access token, refreshing once more after an HTTP 401. */
    private suspend fun <T> authorized(endpoint: SyncEndpoint, call: suspend (String) -> T): T {
        val fresh = freshSession(endpoint)
        return try {
            call(fresh.accessToken)
        } catch (error: RemoteCallException) {
            if ((error.failure as? RemoteFailure.Http)?.status != UNAUTHORIZED) throw error
            call(refresh(endpoint, fresh).accessToken)
        }
    }

    /** The current session, refreshed first when its access token expires within 60 seconds. */
    private suspend fun freshSession(endpoint: SyncEndpoint): AuthSession {
        val current = currentSession()
        return if (current.expiresWithin(clock.instant(), REFRESH_MARGIN_SECONDS)) refresh(endpoint, current) else current
    }

    private suspend fun refresh(endpoint: SyncEndpoint, current: AuthSession): AuthSession {
        val tokens = try {
            auth.refresh(endpoint, current.refreshToken)
        } catch (error: RemoteCallException) {
            val status = (error.failure as? RemoteFailure.Http)?.status
            // A refused refresh token will not start working again: sign out (SPEC.md 8.2).
            if (status != null && status in CLIENT_ERRORS && status != TOO_MANY_REQUESTS) throw SessionRejectedException()
            throw error
        }
        val renewed = sessionFrom(tokens, previous = current)
        if (session == null) throw SessionRejectedException() // signed out while refreshing
        sessions.save(renewed)
        session = renewed
        return renewed
    }

    private fun currentSession(): AuthSession = session ?: throw SessionRejectedException()

    private fun sessionFrom(tokens: AuthTokens, previous: AuthSession?): AuthSession = AuthSession(
        accessToken = tokens.accessToken,
        refreshToken = tokens.refreshToken,
        expiresAt = clock.instant().plusSeconds(tokens.expiresInSeconds),
        userId = tokens.userId.ifEmpty { previous?.userId.orEmpty() },
        email = tokens.email.ifEmpty { previous?.email.orEmpty() },
    )

    private suspend fun signInStep(verifying: Boolean, action: suspend () -> Unit): AuthResult = try {
        action()
        AuthResult.Success
    } catch (cancelled: CancellationException) {
        throw cancelled
    } catch (error: RemoteCallException) {
        when (val failure = error.failure) {
            RemoteFailure.Offline, RemoteFailure.Timeout -> AuthResult.Offline
            is RemoteFailure.Http -> when {
                failure.status == TOO_MANY_REQUESTS -> AuthResult.TooManyRequests
                verifying && failure.status in CLIENT_ERRORS -> AuthResult.WrongOrExpiredCode
                else -> AuthResult.Failed(failure.shortReason)
            }
            is RemoteFailure.BadResponse -> AuthResult.Failed(failure.shortReason)
        }
    } catch (_: IOException) {
        AuthResult.Failed("couldn't store the sign-in")
    }

    private fun idleStatus(): SyncStatus = when {
        endpoint == null -> SyncStatus.Off
        session == null -> SyncStatus.SignedOut
        else -> SyncStatus.Syncing
    }

    /** Publishes [status], unless sync was turned off or the user signed out meanwhile. */
    private fun publish(status: SyncStatus): SyncStatus {
        val shown = when {
            endpoint == null -> SyncStatus.Off
            session == null -> SyncStatus.SignedOut
            else -> status
        }
        mutableState.value = SyncState(shown, session?.email)
        return shown
    }

    private class SessionRejectedException : Exception()

    private companion object {
        const val REFRESH_MARGIN_SECONDS = 60L
        const val UNAUTHORIZED = 401
        const val TOO_MANY_REQUESTS = 429
        val CLIENT_ERRORS = 400..499
    }
}
