package com.lukr99.subtrackr.domain.sync

import com.lukr99.subtrackr.model.Subscription

/**
 * Last-writer-wins merge of two subscription sets (SPEC.md section 8.1). Transport-agnostic.
 * Verified by contracts/vectors/merge.json, the same file the desktop app runs.
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
