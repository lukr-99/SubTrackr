package com.lukr99.subtrackr.ui.sync

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lukr99.subtrackr.application.sync.AuthResult
import com.lukr99.subtrackr.application.sync.SyncCoordinator
import com.lukr99.subtrackr.application.sync.SyncStatus
import com.lukr99.subtrackr.domain.sync.EmailAddress
import com.lukr99.subtrackr.domain.sync.SignInCode
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.ZoneId
import java.time.format.DateTimeFormatter

/** Drives the Sync card: project settings, the two-step email sign-in, sync now, and sign out. */
class SyncViewModel(
    private val sync: SyncCoordinator,
    savedUrl: String,
    savedKey: String,
    zone: ZoneId,
) : ViewModel() {

    private val time = DateTimeFormatter.ofPattern("HH:mm").withZone(zone)
    private val state = MutableStateFlow(SyncUiState(savedUrl = savedUrl, savedKey = savedKey, url = savedUrl, key = savedKey))
    val uiState: StateFlow<SyncUiState> = state.asStateFlow()

    init {
        viewModelScope.launch {
            sync.state.collect { s ->
                state.update {
                    it.copy(
                        status = s.status,
                        statusText = describe(s.status),
                        signedInEmail = s.email,
                        step = if (s.email != null) SignInStep.EMAIL else it.step,
                    )
                }
            }
        }
    }

    fun onUrlChange(value: String) = state.update { it.copy(url = value, error = null) }

    fun onKeyChange(value: String) = state.update { it.copy(key = value, error = null) }

    fun onEmailChange(value: String) = state.update { it.copy(email = value, error = null) }

    fun onCodeChange(value: String) =
        state.update { it.copy(code = value.filter(Char::isDigit).take(SignInCode.MAX_LENGTH), error = null) }

    /** Saves the project URL and key; a different project signs out. */
    fun saveConfig() {
        val current = state.value
        if (!sync.isValidConfig(current.url, current.key)) {
            state.update { it.copy(error = SyncFormError.INVALID_CONFIG) }
            return
        }
        sync.saveConfig(current.url, current.key)
        state.update {
            it.copy(
                savedUrl = current.url.trim(),
                savedKey = current.key.trim(),
                url = current.url.trim(),
                key = current.key.trim(),
                step = SignInStep.EMAIL,
                code = "",
                error = null,
            )
        }
    }

    fun sendCode() {
        val email = EmailAddress.parse(state.value.email)
            ?: return state.update { it.copy(error = SyncFormError.INVALID_EMAIL) }
        run(onSuccess = { it.copy(step = SignInStep.CODE, code = "") }) { sync.sendCode(email) }
    }

    fun verify() {
        val current = state.value
        val email = EmailAddress.parse(current.email)
            ?: return state.update { it.copy(step = SignInStep.EMAIL, error = SyncFormError.INVALID_EMAIL) }
        val code = SignInCode.parse(current.code)
            ?: return state.update { it.copy(error = SyncFormError.INVALID_CODE) }
        run(onSuccess = { it.copy(step = SignInStep.EMAIL, code = "") }) { sync.verify(email, code) }
    }

    fun useAnotherEmail() = state.update { it.copy(step = SignInStep.EMAIL, code = "", error = null) }

    fun syncNow() = sync.requestSync()

    fun signOut() {
        sync.signOut()
        state.update { it.copy(step = SignInStep.EMAIL, code = "", error = null) }
    }

    private fun run(onSuccess: (SyncUiState) -> SyncUiState, action: suspend () -> AuthResult) {
        if (state.value.busy) return
        state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            val result = action()
            state.update { current ->
                val idle = current.copy(busy = false)
                when (result) {
                    AuthResult.Success -> onSuccess(idle)
                    AuthResult.NotConfigured -> idle.copy(error = SyncFormError.NOT_CONFIGURED)
                    AuthResult.WrongOrExpiredCode -> idle.copy(error = SyncFormError.WRONG_CODE, code = "")
                    AuthResult.TooManyRequests -> idle.copy(error = SyncFormError.TOO_MANY_REQUESTS)
                    AuthResult.Offline -> idle.copy(error = SyncFormError.OFFLINE)
                    is AuthResult.Failed -> idle.copy(error = SyncFormError.OTHER, errorDetail = result.reason)
                }
            }
        }
    }

    private fun describe(status: SyncStatus): String = when (status) {
        SyncStatus.Off -> "Off. Add your project URL and publishable key to sync."
        SyncStatus.SignedOut -> "Signed out."
        SyncStatus.Syncing -> "Syncing…"
        is SyncStatus.Synced -> "Synced at ${time.format(status.at)}"
        is SyncStatus.Failed -> "Failed: ${status.reason}"
    }
}
