package com.lukr99.subtrackr.domain

import com.lukr99.subtrackr.model.Subscription

/**
 * Last-writer-wins merge of two subscription sets (SPEC.md §8). Transport-agnostic.
 * Verified by contracts/vectors/merge.json — the same file the WPF app runs.
 */
object MergeEngine {
    fun merge(local: List<Subscription>, remote: List<Subscription>): List<Subscription> {
        val byId = LinkedHashMap<String, Subscription>()
        for (s in local) byId[s.id] = s
        for (r in remote) {
            val existing = byId[r.id]
            byId[r.id] = if (existing == null) r else pick(existing, r)
        }
        return byId.values.sortedBy { it.id }
    }

    /** [a] is the local-side record, [b] the remote candidate. */
    private fun pick(a: Subscription, b: Subscription): Subscription {
        val cmp = b.updatedAt.compareTo(a.updatedAt)
        if (cmp > 0) return b
        if (cmp < 0) return a
        // Equal updatedAt: a tombstone wins; otherwise remote (b) wins.
        val aDeleted = a.deletedAt.isNotEmpty()
        val bDeleted = b.deletedAt.isNotEmpty()
        return if (aDeleted && !bDeleted) a else b
    }
}

/** Transport for sync — a concrete backend implements this; merge logic never changes. */
interface SyncProvider {
    suspend fun pull(): List<Subscription>
    suspend fun push(subscriptions: List<Subscription>)
}

/** One sync pass: pull → merge(local, remote) → push, returning the merged set. */
object SyncService {
    suspend fun sync(local: List<Subscription>, provider: SyncProvider): List<Subscription> {
        val remote = provider.pull()
        val merged = MergeEngine.merge(local, remote)
        provider.push(merged)
        return merged
    }
}
