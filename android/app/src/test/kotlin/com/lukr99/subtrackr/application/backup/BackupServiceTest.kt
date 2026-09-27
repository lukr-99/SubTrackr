package com.lukr99.subtrackr.application.backup

import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.domain.backup.BackupError
import com.lukr99.subtrackr.domain.backup.BackupFormat
import com.lukr99.subtrackr.domain.backup.RestoreMode
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.Clock
import java.time.Instant
import java.time.ZoneOffset

class BackupServiceTest {
    private val clock = Clock.fixed(Instant.parse("2026-09-26T08:30:15Z"), ZoneOffset.ofHours(2))
    private val live = Subscription(
        id = "11111111-1111-4111-8111-111111111111",
        name = "Alpha",
        cost = Money("EUR", 999, 2),
        updatedAt = "2026-09-01T10:00:00Z",
    )
    private val tombstone = live.copy(
        id = "22222222-2222-4222-8222-222222222222",
        deletedAt = "2026-09-02T10:00:00Z",
        updatedAt = "2026-09-02T10:00:00Z",
    )
    private val store = InMemoryDatabaseStore(
        Database(
            settings = Settings(baseCurrency = "EUR", syncUrl = "https://project.example", syncKey = "publishable-key"),
            subscriptions = listOf(live, tombstone),
        ),
    )
    private val documents = FakeDocuments()

    private fun service() = BackupService(
        repository = AppRepository(store, { OfflineFallback.forAnchor(it) }, clock, { "id" }),
        documents = documents,
        clock = clock,
        appVersion = "0.3.0",
        work = Dispatchers.Unconfined,
    )

    @Test
    fun suggestedFileName_usesLocalTime() {
        assertEquals("SubTrackr-backup-20260926-103015.json", service().suggestedFileName())
    }

    @Test
    fun exportThenRestore_roundTripsIntoAnEmptyDevice() = runTest {
        val report = service().exportTo("content://backup")

        assertEquals(ExportReport.Saved(1), report)
        val text = documents.files.getValue("content://backup")
        assertFalse("sync URL must not leave the device", text.contains("project.example"))
        assertFalse("sync key must not leave the device", text.contains("publishable-key"))

        val original = store.saved
        store.saved = Database()
        val restored = service().restoreFrom("content://backup", RestoreMode.MERGE)

        assertEquals(RestoreReport.Restored(added = 2, updated = 0, unchanged = 0, total = 2), restored)
        assertEquals(original.subscriptions.sortedBy { it.id }, store.saved.subscriptions)
    }

    @Test
    fun hiddenServiceLogos_travelThroughExportAndReplace() = runTest {
        store.saved = store.saved.copy(settings = store.saved.settings.copy(hideServiceLogos = true))
        service().exportTo("content://backup")
        assertTrue(Regex("\"hideServiceLogos\"\\s*:\\s*true").containsMatchIn(documents.files.getValue("content://backup")))

        store.saved = Database()
        service().restoreFrom("content://backup", RestoreMode.REPLACE)

        assertTrue(store.saved.settings.hideServiceLogos)
    }

    @Test
    fun export_writeFailure_isReported() = runTest {
        documents.failWrites = true

        assertTrue(service().exportTo("content://backup") is ExportReport.Failed)
    }

    @Test
    fun restore_fileLargerThanTheLimit_isInvalidJson() = runTest {
        documents.files["content://big"] = " ".repeat(BackupFormat.MAX_BYTES + 1)

        assertEquals(RestoreReport.Rejected(BackupError.INVALID_JSON), service().restoreFrom("content://big", RestoreMode.MERGE))
        assertEquals(0, store.saveCount)
    }

    @Test
    fun restore_unreadableFile_changesNothing() = runTest {
        assertTrue(service().restoreFrom("content://missing", RestoreMode.MERGE) is RestoreReport.Failed)
        assertEquals(0, store.saveCount)
    }

    @Test
    fun restore_saveFailure_leavesTheDeviceUntouched() = runTest {
        service().exportTo("content://backup")
        val before = store.saved
        store.saved = before.copy(subscriptions = emptyList())
        store.failSaves = true
        val service = service()

        val report = service.restoreFrom("content://backup", RestoreMode.REPLACE)

        assertTrue(report is RestoreReport.Failed)
        assertEquals(emptyList<Subscription>(), store.saved.subscriptions)
    }
}
