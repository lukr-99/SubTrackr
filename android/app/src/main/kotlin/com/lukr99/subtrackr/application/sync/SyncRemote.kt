package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import com.lukr99.subtrackr.model.Subscription

/** The `subscriptions` table (SPEC.md section 8.3). Calls throw RemoteCallException on failure. */
interface SyncRemote {
    /** Every row the signed-in user may read; row-level security limits it to their own. */
    suspend fun pull(endpoint: SyncEndpoint, accessToken: String): List<Subscription>

    /** Upserts [subscriptions] as rows owned by [userId]. */
    suspend fun push(endpoint: SyncEndpoint, accessToken: String, userId: String, subscriptions: List<Subscription>)
}
