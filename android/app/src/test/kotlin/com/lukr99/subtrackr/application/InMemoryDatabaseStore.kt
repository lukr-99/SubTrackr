package com.lukr99.subtrackr.application

import com.lukr99.subtrackr.application.store.DatabaseStore
import com.lukr99.subtrackr.model.Database

/** A [DatabaseStore] held in memory; [failSaves] makes every save throw like a full disk. */
class InMemoryDatabaseStore(var saved: Database = Database()) : DatabaseStore {
    var saveCount = 0
        private set
    var failSaves = false

    override fun load(): Database = saved

    override fun save(db: Database) {
        if (failSaves) throw java.io.IOException("disk full")
        saved = db
        saveCount++
    }
}
