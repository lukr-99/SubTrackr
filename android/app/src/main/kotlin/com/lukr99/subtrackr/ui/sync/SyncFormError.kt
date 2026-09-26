package com.lukr99.subtrackr.ui.sync

/** Why the last Sync card action didn't work, as a sentence. */
enum class SyncFormError(val message: String) {
    INVALID_CONFIG("Enter an https:// project URL and its publishable key, or clear both to turn sync off."),
    NOT_CONFIGURED("Save a project URL and publishable key first."),
    INVALID_EMAIL("Enter a valid email address."),
    INVALID_CODE("The code is 6 to 10 digits."),
    WRONG_CODE("That code is wrong or has expired. Check the latest email or send a new code."),
    TOO_MANY_REQUESTS("Too many attempts. Wait a minute and try again."),
    OFFLINE("No connection to the project. Check your internet and try again."),
    OTHER("Sign-in failed"),
}
