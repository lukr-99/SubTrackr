package com.lukr99.subtrackr.ui

import android.app.Application
import androidx.compose.ui.test.junit4.createComposeRule
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.backup.FakeDocuments
import com.lukr99.subtrackr.application.update.FakeArtifactDownloader
import com.lukr99.subtrackr.application.update.ReleaseSource
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.composition.AppViewModelFactory
import com.lukr99.subtrackr.domain.update.UpdatePlatform
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** The whole shell (navigation bar, add button) built through the real view-model factory. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = RobolectricDeviceQualifiers.Pixel7, application = Application::class)
class AppShellScreenshotTest {
    @get:Rule
    val compose = createComposeRule()

    @get:Rule
    val temp = TemporaryFolder()

    private fun factory(): AppViewModelFactory {
        val repository = AppRepository(
            store = InMemoryDatabaseStore(SampleData.database),
            rateSource = { SampleData.rates },
            syncProviders = { _, _ -> error("sync is off in screenshots") },
            clock = SampleData.clock,
            newId = { "00000000-0000-4000-8000-000000000099" },
        )
        return AppViewModelFactory(
            repository = repository,
            updates = UpdateService(
                currentVersion = "0.3.0-dev",
                platform = UpdatePlatform.ANDROID,
                releases = ReleaseSource { error("dev builds never check") },
                downloader = FakeArtifactDownloader(emptyMap()),
                installer = {},
                directory = temp.root,
            ),
            backups = BackupService(repository, FakeDocuments(), SampleData.clock, "0.3.0-dev"),
        )
    }

    private fun capture(dark: Boolean) = compose.captureScreen("app_shell", dark) { SubTrackrApp(factory()) }

    @Test
    fun light() = capture(dark = false)

    @Test
    fun dark() = capture(dark = true)
}
