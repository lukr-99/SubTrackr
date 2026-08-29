package com.lukr99.subtrackr

import com.lukr99.subtrackr.data.SeedData
import kotlinx.serialization.Serializable
import org.junit.Assert.assertEquals
import org.junit.Test

class SeedDataVectorTest {

    @Serializable
    data class SeedSubscription(val name: String, val id: String)

    @Serializable
    data class File(val subscriptions: List<SeedSubscription>)

    @Test
    fun seed_subscriptions_match_shared_stable_ids() {
        val expected = vectorJson.decodeFromString<File>(readVector("seed-data.json")).subscriptions
        val actual = SeedData.createInitialDatabase().subscriptions
            .map { SeedSubscription(it.name, it.id) }

        assertEquals(expected, actual)
    }
}
