package com.lukr99.subtrackr.ui.backup

import com.lukr99.subtrackr.MainDispatcherRule
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.backup.FakeDocuments
import com.lukr99.subtrackr.domain.backup.BackupWriter
import com.lukr99.subtrackr.domain.backup.RestoreMode
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.Dispatchers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import java.time.Clock
import java.time.Instant
import java.time.ZoneOffset

class BackupViewModelTest {
    @get:Rule
    val main = MainDispatcherRule()

    private val sub =
        Subscription(
            id = "11111111-1111-4111-8111-111111111111",
            name = "Alpha",
            cost = Money("EUR", 999, 2),
            updatedAt = "2026-09-01T10:00:00Z",
        )
    private val store = InMemoryDatabaseStore(Database(subscriptions = listOf(sub)))
    private val documents = FakeDocuments()
    private val clock = Clock.fixed(Instant.parse("2026-09-26T10:00:00Z"), ZoneOffset.UTC)
    private val vm = BackupViewModel(
        BackupService(
            AppRepository(store, { OfflineFallback.forAnchor(it) }, clock, { "id" }),
            documents,
            clock,
            "0.3.0",
            work = Dispatchers.Unconfined,
        ),
    )

    @Test
    fun export_cancelledPicker_doesNothing() {
        vm.export(null)

        assertEquals(BackupUiState(), vm.uiState.value)
    }

    @Test
    fun export_reportsTheCountInPlainWords() {
        vm.export("content://out")

        assertEquals("Backup saved with 1 subscription.", vm.uiState.value.message)
        assertFalse(vm.uiState.value.messageIsError)
    }

    @Test
    fun replace_asksForConfirmationBeforeChangingAnything() {
        val other = sub.copy(id = "22222222-2222-4222-8222-222222222222", name = "Bravo")
        documents.files["content://in"] = BackupWriter.write(Database(subscriptions = listOf(other)), clock.instant(), "0.3.0")

        vm.chooseFile("content://in")
        assertEquals(RestoreMode.MERGE, vm.uiState.value.mode)
        vm.selectMode(RestoreMode.REPLACE)
        vm.confirmRestore()

        assertTrue(vm.uiState.value.confirmingReplace)
        assertEquals(0, store.saveCount)

        vm.confirmRestore()

        assertNull(vm.uiState.value.pendingUri)
        assertEquals(listOf(other), store.saved.subscriptions)
        assertEquals("Restored: 1 added, 0 updated, 0 unchanged, 1 in total.", vm.uiState.value.message)
    }

    @Test
    fun restore_invalidFile_explainsAndKeepsData() {
        documents.files["content://bad"] = "[]"

        vm.chooseFile("content://bad")
        vm.confirmRestore()

        assertTrue(vm.uiState.value.messageIsError)
        assertTrue(vm.uiState.value.message.startsWith("That file isn't a SubTrackr backup"))
        assertEquals(listOf(sub), store.saved.subscriptions)
    }
}
