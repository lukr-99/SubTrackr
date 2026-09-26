package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.domain.sync.MergeEngine
import com.lukr99.subtrackr.model.Subscription

/** One sync pass: pull, merge(local, remote), push, returning the merged set. */
object SyncService {
    suspend fun sync(local: List<Subscription>, provider: SyncProvider): List<Subscription> {
        val remote = provider.pull()
        val merged = MergeEngine.merge(local, remote)
        provider.push(merged)
        return merged
    }
}
