package com.lukr99.subtrackr.application.sync

/** OS-protected storage for the one sign-in session (SPEC.md section 8.2). */
interface SessionStore {
    /** The stored session, or null when there is none or it can no longer be read. */
    fun load(): AuthSession?

    fun save(session: AuthSession)

    fun clear()
}
