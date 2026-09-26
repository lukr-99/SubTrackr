package com.lukr99.subtrackr.ui.sync

import com.lukr99.subtrackr.MainDispatcherRule
import com.lukr99.subtrackr.MutableClock
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.application.sync.FakeAuthApi
import com.lukr99.subtrackr.application.sync.FakeSyncRemote
import com.lukr99.subtrackr.application.sync.InMemorySessionStore
import com.lukr99.subtrackr.application.sync.SyncCoordinator
import com.lukr99.subtrackr.application.sync.SyncStatus
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Settings
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import java.time.Instant
import java.time.ZoneId

@OptIn(ExperimentalCoroutinesApi::class)
class SyncViewModelTest {
    @get:Rule
    val main = MainDispatcherRule()

    private val clock = MutableClock(Instant.parse("2026-09-26T12:05:00Z"))
    private val store = InMemoryDatabaseStore(
        Database(settings = Settings(syncUrl = "https://project.example", syncKey = "publishable-key")),
    )
    private val repository = AppRepository(store, { OfflineFallback.forAnchor(it) }, clock, { "id" })
    private val auth = FakeAuthApi()
    private val sessions = InMemorySessionStore()
    private val coordinator = SyncCoordinator(
        repository, auth, FakeSyncRemote(), sessions, clock, CoroutineScope(UnconfinedTestDispatcher()), pause = {},
    )

    private fun viewModel() = SyncViewModel(coordinator, repository.syncUrl, repository.syncKey, ZoneId.of("Europe/Prague"))

    @Test
    fun signIn_emailThenCode_thenShowsTheAccount() {
        val vm = viewModel()
        assertEquals("Signed out.", vm.uiState.value.statusText)

        vm.onEmailChange("user@example.com")
        vm.sendCode()
        assertEquals(SignInStep.CODE, vm.uiState.value.step)
        assertEquals(listOf("otp user@example.com"), auth.calls)

        vm.onCodeChange("12 34 56")
        vm.verify()

        assertTrue(vm.uiState.value.signedIn)
        assertEquals("user@example.com", vm.uiState.value.signedInEmail)
    }

    @Test
    fun invalidInput_isCaughtBeforeAnyRequest() {
        val vm = viewModel()

        vm.onEmailChange("not-an-email")
        vm.sendCode()
        assertEquals(SyncFormError.INVALID_EMAIL, vm.uiState.value.error)

        vm.onEmailChange("user@example.com")
        vm.sendCode()
        vm.onCodeChange("123")
        vm.verify()
        assertEquals(SyncFormError.INVALID_CODE, vm.uiState.value.error)
        assertEquals(listOf("otp user@example.com"), auth.calls)
    }

    @Test
    fun wrongCode_clearsTheCodeAndExplains() {
        auth.onVerify = { _, _ -> throw RemoteCallException(RemoteFailure.Http(403)) }
        val vm = viewModel()
        vm.onEmailChange("user@example.com")
        vm.sendCode()
        vm.onCodeChange("123456")

        vm.verify()

        assertEquals(SyncFormError.WRONG_CODE, vm.uiState.value.error)
        assertEquals("", vm.uiState.value.code)
        assertFalse(vm.uiState.value.signedIn)
    }

    @Test
    fun saveConfig_rejectsPlainHttp_andAcceptsClearingBoth() {
        val vm = viewModel()

        vm.onUrlChange("http://project.example")
        vm.saveConfig()
        assertEquals(SyncFormError.INVALID_CONFIG, vm.uiState.value.error)

        vm.onUrlChange("")
        vm.onKeyChange("")
        vm.saveConfig()
        assertEquals(null, vm.uiState.value.error)
        assertEquals(SyncStatus.Off, vm.uiState.value.status)
        assertEquals("", store.saved.settings.syncUrl)
    }

    @Test
    fun syncedStatus_showsLocalTime() {
        val vm = viewModel()
        vm.onEmailChange("user@example.com")
        vm.sendCode()
        vm.onCodeChange("123456")
        vm.verify()

        runBlocking { coordinator.sync() }

        assertEquals("Synced at 14:05", vm.uiState.value.statusText)
    }
}
