package com.lukr99.subtrackr.data

import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.ThemeMode
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File

class SubscriptionStoreTest {
    @get:Rule
    val temp = TemporaryFolder()

    @Test
    fun save_replacesTheFileAndLeavesNoTempFile() {
        val file = File(temp.root, "data.json")
        val store = SubscriptionStore(file)
        val first = Database(settings = Settings(baseCurrency = "EUR"), subscriptions = listOf(Subscription(id = "a", updatedAt = "1")))
        val second = first.copy(settings = Settings(baseCurrency = "CZK", themeMode = ThemeMode.DARK))

        store.save(first)
        store.save(second)

        assertEquals(second, SubscriptionStore(file).load())
        assertFalse(File(temp.root, "data.json.tmp").exists())
    }

    @Test
    fun load_fileWithoutThemeMode_readsSystem() {
        val file = File(temp.root, "data.json")
        file.writeText("""{"schemaVersion":"0.1","settings":{"baseCurrency":"CZK"},"subscriptions":[]}""")

        val loaded = SubscriptionStore(file).load()

        assertEquals(ThemeMode.SYSTEM, loaded.settings.themeMode)
        assertEquals("CZK", loaded.settings.baseCurrency)
    }
}
