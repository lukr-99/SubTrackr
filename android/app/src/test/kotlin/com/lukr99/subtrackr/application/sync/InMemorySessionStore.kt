package com.lukr99.subtrackr.application.sync

/** A [SessionStore] without a file. */
class InMemorySessionStore(var session: AuthSession? = null) : SessionStore {
    override fun load(): AuthSession? = session

    override fun save(session: AuthSession) {
        this.session = session
    }

    override fun clear() {
        session = null
    }
}
