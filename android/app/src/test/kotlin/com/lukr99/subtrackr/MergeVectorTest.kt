package com.lukr99.subtrackr

import com.lukr99.subtrackr.domain.MergeEngine
import com.lukr99.subtrackr.model.Subscription
import kotlinx.serialization.Serializable
import org.junit.Assert.assertEquals
import org.junit.Test

class MergeVectorTest {

    @Serializable
    data class Rec(val id: String, val updatedAt: String, val deletedAt: String)

    @Serializable
    data class Case(val id: String, val local: List<Rec>, val remote: List<Rec>, val expected: List<Rec>)

    @Serializable
    data class File(val cases: List<Case>)

    private fun toSub(r: Rec) = Subscription(id = r.id, updatedAt = r.updatedAt, deletedAt = r.deletedAt)

    @Test
    fun merge_matches_vectors() {
        val doc = vectorJson.decodeFromString<File>(readVector("merge.json"))
        for (c in doc.cases) {
            val merged = MergeEngine.merge(c.local.map(::toSub), c.remote.map(::toSub))
            val actual = merged.map { Rec(it.id, it.updatedAt, it.deletedAt) }
            assertEquals("case ${c.id}", c.expected, actual)
        }
    }
}
