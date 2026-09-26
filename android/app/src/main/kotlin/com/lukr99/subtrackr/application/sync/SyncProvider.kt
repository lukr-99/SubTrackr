package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.model.Subscription

/** Transport for sync. A backend implements it; the merge logic never changes. */
interface SyncProvider {
    suspend fun pull(): List<Subscription>
    suspend fun push(subscriptions: List<Subscription>)
}
