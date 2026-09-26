package com.lukr99.subtrackr.ui.update

import com.lukr99.subtrackr.MainDispatcherRule
import com.lukr99.subtrackr.application.update.FakeArtifactDownloader
import com.lukr99.subtrackr.application.update.ReleaseSource
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.domain.update.ReleaseAsset
import com.lukr99.subtrackr.domain.update.ReleaseInfo
import com.lukr99.subtrackr.domain.update.UpdatePlatform
import kotlinx.coroutines.Dispatchers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File

class UpdateViewModelTest {
    @get:Rule
    val main = MainDispatcherRule()

    @get:Rule
    val temp = TemporaryFolder()

    private val release = ReleaseInfo(
        tagName = "v0.3.1",
        assets = listOf(
            ReleaseAsset("SubTrackr-0.3.1.apk", "https://example.invalid/a.apk"),
            ReleaseAsset("SubTrackr-0.3.1.apk.sha256", "https://example.invalid/a.apk.sha256"),
        ),
    )

    private fun viewModel(current: String, files: Map<String, ByteArray?> = emptyMap()): Pair<UpdateViewModel, MutableList<File>> {
        val installed = mutableListOf<File>()
        val service = UpdateService(
            currentVersion = current,
            platform = UpdatePlatform.ANDROID,
            releases = ReleaseSource { release },
            downloader = FakeArtifactDownloader(files),
            installer = { installed += it },
            directory = File(temp.root, "updates"),
            io = Dispatchers.Unconfined,
        )
        return UpdateViewModel(service) to installed
    }

    @Test
    fun devBuild_neverChecks_andSaysSo() {
        val (vm, _) = viewModel("0.3.0-dev")

        val state = vm.uiState.value
        assertEquals(UpdateStatus.Disabled, state.status)
        assertFalse(state.checksEnabled)
        assertTrue(state.statusText.contains("Development builds"))
    }

    @Test
    fun releaseBuild_checksOnLaunch_andPromptsForANewerVersion() {
        val (vm, _) = viewModel("0.3.0")

        val state = vm.uiState.value
        assertEquals(UpdateStatus.Available("0.3.1"), state.status)
        assertTrue(state.promptVisible)

        vm.dismissPrompt()
        assertFalse(vm.uiState.value.promptVisible)
        assertEquals("0.3.1", vm.uiState.value.offer?.version?.core)
    }

    @Test
    fun downloadAndInstall_checksumMismatch_reportsFailureAndInstallsNothing() {
        val (vm, installed) = viewModel(
            "0.3.0",
            mapOf(
                "https://example.invalid/a.apk" to byteArrayOf(1, 2, 3),
                "https://example.invalid/a.apk.sha256" to "0".repeat(64).toByteArray(),
            ),
        )

        vm.downloadAndInstall()

        assertTrue(vm.uiState.value.status is UpdateStatus.DownloadFailed)
        assertFalse(vm.uiState.value.busy)
        assertEquals(emptyList<File>(), installed)
    }
}
