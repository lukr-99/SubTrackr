package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.MutableClock
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.domain.sync.EmailAddress
import com.lukr99.subtrackr.domain.sync.SignInCode
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.Instant

@OptIn(ExperimentalCoroutinesApi::class)
class SyncCoordinatorTest {
    private val clock = MutableClock(Instant.parse("2026-09-26T10:00:00Z"))
    private val local = Subscription(
        id = "11111111-1111-4111-8111-111111111111",
        name = "Alpha",
        cost = Money("EUR", 999, 2),
        updatedAt = "2026-09-01T10:00:00Z",
    )
    private val remoteOnly = local.copy(id = "22222222-2222-4222-8222-222222222222", name = "Bravo")
    private val configured = Settings(baseCurrency = "EUR", syncUrl = "https://project.example", syncKey = "publishable-key")
    private val store = InMemoryDatabaseStore(Database(settings = configured, subscriptions = listOf(local)))
    private val repository = AppRepository(store, { OfflineFallback.forAnchor(it) }, clock, { "new-id" })
    private val auth = FakeAuthApi()
    private val remote = FakeSyncRemote()
    private val delays = mutableListOf<Long>()

    private fun session(expiresIn: Long = 3600) = AuthSession(
        accessToken = "access-0",
        refreshToken = "refresh-0",
        expiresAt = clock.now.plusSeconds(expiresIn),
        userId = FakeAuthApi.USER_ID,
        email = "user@example.com",
    )

    private val sessions = InMemorySessionStore(session())

    private fun coordinator(scope: CoroutineScope) = SyncCoordinator(
        repository = repository,
        auth = auth,
        remote = remote,
        sessions = sessions,
        clock = clock,
        scope = scope,
        pause = { delays += it },
    )

    private fun TestScope.coordinator() = coordinator(backgroundScope)

    @Test
    fun noProject_isOff_andNeverCallsTheServer() = runTest {
        store.saved = Database(subscriptions = listOf(local))
        val sync = SyncCoordinator(
            AppRepository(store, { OfflineFallback.forAnchor(it) }, clock, { "id" }),
            auth, remote, sessions, clock, backgroundScope,
        )

        assertEquals(SyncStatus.Off, sync.sync())
        assertEquals(emptyList<String>(), remote.calls)
    }

    @Test
    fun signedOut_doesNotSync() = runTest {
        sessions.session = null
        val sync = coordinator()

        assertEquals(SyncStatus.SignedOut, sync.state.value.status)
        assertEquals(SyncStatus.SignedOut, sync.sync())
        assertEquals(emptyList<String>(), remote.calls)
    }

    @Test
    fun pass_pullsMergesSavesAndPushesRowsOwnedByTheUser() = runTest {
        remote.rows = listOf(remoteOnly)
        val sync = coordinator()

        val status = sync.sync()

        assertEquals(SyncStatus.Synced(clock.now), status)
        assertEquals(listOf(local, remoteOnly), store.saved.subscriptions)
        assertEquals(listOf("pull access-0", "push access-0"), remote.calls)
        assertEquals(listOf(FakeAuthApi.USER_ID), remote.pushedUserIds)
        assertEquals(listOf(local, remoteOnly), remote.rows)
        assertEquals(SyncState(SyncStatus.Synced(clock.now), "user@example.com"), sync.state.value)
    }

    @Test
    fun tokenExpiringWithinSixtySeconds_isRefreshedFirstAndStored() = runTest {
        sessions.session = session(expiresIn = 59)
        val sync = coordinator()

        sync.sync()

        assertEquals(listOf("refresh refresh-0"), auth.calls)
        assertEquals(listOf("pull access-2", "push access-2"), remote.calls)
        assertEquals("access-2", sessions.session?.accessToken)
    }

    @Test
    fun unauthorized_refreshesOnceAndRetriesTheRequest() = runTest {
        remote.pullFailures += RemoteFailure.Http(401)
        val sync = coordinator()

        val status = sync.sync()

        assertTrue(status is SyncStatus.Synced)
        assertEquals(listOf("refresh refresh-0"), auth.calls)
        assertEquals(listOf("pull access-0", "pull access-2", "push access-2"), remote.calls)
    }

    @Test
    fun unauthorizedAfterRefresh_failsWithoutMoreAttempts() = runTest {
        remote.pullFailures += listOf(RemoteFailure.Http(401), RemoteFailure.Http(401))
        val sync = coordinator()

        val status = sync.sync()

        assertEquals(SyncStatus.Failed("not signed in (401)"), status)
        assertEquals(listOf("refresh refresh-0"), auth.calls)
        assertEquals(emptyList<Long>(), delays)
    }

    @Test
    fun rejectedRefresh_signsOutAndDropsTheSession() = runTest {
        sessions.session = session(expiresIn = 10)
        auth.onRefresh = { throw RemoteCallException(RemoteFailure.Http(400)) }
        val sync = coordinator()

        val status = sync.sync()
        runCurrent()

        assertEquals(SyncStatus.SignedOut, status)
        assertNull(sessions.session)
        assertEquals(SyncState(SyncStatus.SignedOut, null), sync.state.value)
    }

    @Test
    fun serverErrors_retryAfterOneThenTwoSeconds() = runTest {
        remote.pullFailures += listOf(RemoteFailure.Http(503), RemoteFailure.Timeout)
        val sync = coordinator()

        val status = sync.sync()

        assertTrue(status is SyncStatus.Synced)
        assertEquals(listOf(1_000L, 2_000L), delays)
    }

    @Test
    fun threeRetryableFailures_giveUpWithTheLastReason() = runTest {
        remote.pullFailures += listOf(RemoteFailure.Offline, RemoteFailure.Http(429), RemoteFailure.Http(503))
        val sync = coordinator()

        val status = sync.sync()

        assertEquals(SyncStatus.Failed("server error 503"), status)
        assertEquals(3, remote.calls.size)
        assertEquals(listOf(1_000L, 2_000L), delays)
    }

    @Test
    fun otherClientErrors_failAtOnce() = runTest {
        remote.pushFailures += RemoteFailure.Http(403)
        val sync = coordinator()

        val status = sync.sync()

        assertEquals(SyncStatus.Failed("not allowed (403)"), status)
        assertEquals(emptyList<Long>(), delays)
    }

    @Test
    fun verify_storesTheSessionFromTheCode() = runTest {
        sessions.session = null
        val sync = coordinator()

        val result = sync.verify(EmailAddress.parse("user@example.com")!!, SignInCode.parse("123456")!!)

        assertEquals(AuthResult.Success, result)
        assertEquals("access-1", sessions.session?.accessToken)
        assertEquals(clock.now.plusSeconds(3600), sessions.session?.expiresAt)
        assertEquals("user@example.com", sync.state.value.email)
    }

    @Test
    fun verify_wrongCode_andRateLimit_areTold() = runTest {
        sessions.session = null
        val sync = coordinator()
        val email = EmailAddress.parse("user@example.com")!!
        val code = SignInCode.parse("123456")!!

        auth.onVerify = { _, _ -> throw RemoteCallException(RemoteFailure.Http(403)) }
        assertEquals(AuthResult.WrongOrExpiredCode, sync.verify(email, code))
        auth.onVerify = { _, _ -> throw RemoteCallException(RemoteFailure.Http(429)) }
        assertEquals(AuthResult.TooManyRequests, sync.verify(email, code))
        auth.onSendCode = { throw RemoteCallException(RemoteFailure.Offline) }
        assertEquals(AuthResult.Offline, sync.sendCode(email))
        assertNull(sessions.session)
    }

    @Test
    fun signOut_isLocalFirstEvenWhenLogoutFails() = runTest {
        auth.onLogout = { throw RemoteCallException(RemoteFailure.Offline) }
        val sync = coordinator()

        sync.signOut()
        runCurrent()

        assertNull(sessions.session)
        assertEquals(SyncState(SyncStatus.SignedOut, null), sync.state.value)
        assertEquals(listOf("logout access-0"), auth.calls)
    }

    @Test
    fun changingTheProject_signsOut() = runTest {
        val sync = coordinator()

        sync.saveConfig("https://other.example", "other-key")
        runCurrent()

        assertNull(sessions.session)
        assertEquals("https://other.example", store.saved.settings.syncUrl)
        assertEquals(SyncStatus.SignedOut, sync.state.value.status)
        assertEquals(listOf("logout access-0"), auth.calls)
    }

    @Test
    fun savingTheSameProject_keepsTheSession() = runTest {
        val sync = coordinator()

        sync.saveConfig(" https://project.example ", "publishable-key")

        assertEquals("access-0", sessions.session?.accessToken)
    }

    @Test
    fun configValidation_requiresHttpsOrBothEmpty() = runTest {
        val sync = coordinator()

        assertTrue(sync.isValidConfig("", ""))
        assertTrue(sync.isValidConfig("https://project.example", "key"))
        assertEquals(false, sync.isValidConfig("http://project.example", "key"))
        assertEquals(false, sync.isValidConfig("http://10.0.2.2:54621", "key"))
        assertEquals(false, sync.isValidConfig("https://project.example", ""))
    }

    @Test
    fun start_syncsOnLaunchAndAfterEachLocalChange() = runTest {
        val sync = coordinator()

        sync.start()
        runCurrent()
        assertEquals(listOf("pull access-0", "push access-0"), remote.calls)

        repository.upsert(local.copy(name = "Alpha renamed"))
        runCurrent()

        assertEquals(4, remote.calls.size)
        assertEquals("Alpha renamed", remote.rows.single { it.id == local.id }.name)
    }

    @Test
    fun start_whileSignedOut_staysQuiet() = runTest {
        sessions.session = null
        val sync = coordinator()

        sync.start()
        repository.upsert(local.copy(name = "Changed"))
        runCurrent()

        assertEquals(emptyList<String>(), remote.calls)
    }
}
