package com.lukr99.subtrackr.ui.settings

import android.app.Application
import androidx.compose.runtime.Composable
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import com.lukr99.subtrackr.application.sync.SyncStatus
import com.lukr99.subtrackr.domain.update.AppVersion
import com.lukr99.subtrackr.domain.update.ReleaseAsset
import com.lukr99.subtrackr.domain.update.UpdateOffer
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.ui.SCREENSHOT_DIR
import com.lukr99.subtrackr.ui.SampleData
import com.lukr99.subtrackr.ui.backup.BackupCard
import com.lukr99.subtrackr.ui.backup.BackupTags
import com.lukr99.subtrackr.ui.backup.BackupUiState
import com.lukr99.subtrackr.ui.captureScreen
import com.lukr99.subtrackr.ui.sync.SyncCard
import com.lukr99.subtrackr.ui.sync.SyncUiState
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import com.lukr99.subtrackr.ui.update.UpdateStatus
import com.lukr99.subtrackr.ui.update.UpdateUiState
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.math.BigDecimal
import java.time.Instant

@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = RobolectricDeviceQualifiers.Pixel7, application = Application::class)
class SettingsScreenshotTest {
    @get:Rule
    val compose = createComposeRule()

    private val offer = UpdateOffer(
        version = AppVersion(0, 3, 1),
        asset = ReleaseAsset("SubTrackr-0.3.1.apk", "https://example.invalid/SubTrackr-0.3.1.apk"),
        checksumAsset = ReleaseAsset("SubTrackr-0.3.1.apk.sha256", "https://example.invalid/SubTrackr-0.3.1.apk.sha256"),
        notes = "",
    )

    @Composable
    private fun Screen() {
        SettingsScreen(
            baseCurrency = SampleData.BASE,
            worthThreshold = SampleData.threshold,
            monthlyBudget = BigDecimal("1500"),
            ratesLabel = "CZK · 2026-09-25",
            themeMode = ThemeMode.SYSTEM,
            appVersion = "0.3.0",
            onSetThemeMode = {},
            onSetBaseCurrency = {},
            onSetThreshold = {},
            onSetBudget = {},
            onRefreshRates = {},
            syncCard = {
                SyncCard(
                    state = SyncUiState(
                        status = SyncStatus.Synced(Instant.parse("2026-09-26T12:05:00Z")),
                        statusText = "Synced at 14:05",
                        signedInEmail = "user@example.com",
                        savedUrl = "https://project.example",
                        savedKey = "publishable-key",
                        url = "https://project.example",
                        key = "publishable-key",
                    ),
                    onUrlChange = {},
                    onKeyChange = {},
                    onSaveConfig = {},
                    onEmailChange = {},
                    onSendCode = {},
                    onCodeChange = {},
                    onVerify = {},
                    onUseAnotherEmail = {},
                    onSyncNow = {},
                    onSignOut = {},
                )
            },
            backupCard = {
                BackupCard(
                    state = BackupUiState(message = "Restored: 1 added, 1 updated, 1 unchanged, 3 in total."),
                    onExport = {},
                    onRestore = {},
                    onSelectMode = {},
                    onConfirmRestore = {},
                    onCancelRestore = {},
                )
            },
            updatesCard = {
                UpdatesCard(
                    state = UpdateUiState(
                        currentVersion = "0.3.0",
                        checksEnabled = true,
                        status = UpdateStatus.Available("0.3.1"),
                        offer = offer,
                    ),
                    onCheck = {},
                    onInstall = {},
                    onOpenReleases = {},
                )
            },
        )
    }

    private fun top(dark: Boolean) = compose.captureScreen("settings_top", dark) { Screen() }

    private fun scrolledTo(name: String, dark: Boolean, scroll: () -> Unit) {
        compose.setContent { SubTrackrTheme(darkTheme = dark) { Screen() } }
        scroll()
        compose.onRoot().captureRoboImage("$SCREENSHOT_DIR/${name}_${if (dark) "dark" else "light"}.png")
    }

    private fun middle(dark: Boolean) = scrolledTo("settings_middle", dark) {
        compose.onNodeWithTag(BackupTags.MESSAGE).performScrollTo()
    }

    private fun bottom(dark: Boolean) = scrolledTo("settings_bottom", dark) {
        compose.onNodeWithText("Shares its data contract with the desktop app.").performScrollTo()
    }

    @Test
    fun topLight() = top(dark = false)

    @Test
    fun topDark() = top(dark = true)

    @Test
    fun middleLight() = middle(dark = false)

    @Test
    fun middleDark() = middle(dark = true)

    @Test
    fun bottomLight() = bottom(dark = false)

    @Test
    fun bottomDark() = bottom(dark = true)
}
