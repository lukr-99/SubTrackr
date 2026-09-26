package com.lukr99.subtrackr.application.store

import com.lukr99.subtrackr.model.Database

/** Persistence of the whole [Database]. [save] must replace the stored copy atomically. */
interface DatabaseStore {
    fun load(): Database
    fun save(db: Database)
}
