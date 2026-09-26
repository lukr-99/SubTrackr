package com.lukr99.subtrackr.application.sync

/** The outcome of a sign-in step, with failures the UI can explain in plain words. */
sealed interface AuthResult {
    data object Success : AuthResult

    /** Sync is off or the project URL and key are not usable. */
    data object NotConfigured : AuthResult

    data object WrongOrExpiredCode : AuthResult

    data object TooManyRequests : AuthResult

    data object Offline : AuthResult

    data class Failed(val reason: String) : AuthResult
}
