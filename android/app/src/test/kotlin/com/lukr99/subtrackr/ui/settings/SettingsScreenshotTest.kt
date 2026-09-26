package com.lukr99.subtrackr.ui.settings

import android.app.Application
import androidx.compose.runtime.Composable
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.ui.SCREENSHOT_DIR
import com.lukr99.subtrackr.ui.SampleData
import com.lukr99.subtrackr.ui.captureScreen
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import com.lukr99.subtrackr.ui.update.UpdateUiState
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.math.BigDecimal

@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = RobolectricDeviceQualifiers.Pixel7, application = Application::class)
class SettingsScreenshotTest {
    @get:Rule
    val compose = createComposeRule()

    @Composable
    private fun Screen() {
        SettingsScreen(
            baseCurrency = SampleData.BASE,
            worthThreshold = SampleData.threshold,
            monthlyBudget = BigDecimal("1500"),
            ratesLabel = "CZK · 2026-09-25",
            syncUrl = "",
            syncKey = "",
            themeMode = ThemeMode.SYSTEM,
            update = UpdateUiState(currentVersion = "0.3.0", checksEnabled = true),
            onSetThemeMode = {},
            onSetBaseCurrency = {},
            onSetThreshold = {},
            onSetBudget = {},
            onRefreshRates = {},
            onSync = { _, _, _ -> },
            onCheckUpdate = {},
            onInstallUpdate = {},
            onOpenReleases = {},
        )
    }

    private fun top(dark: Boolean) = compose.captureScreen("settings_top", dark) { Screen() }

    private fun bottom(dark: Boolean) {
        compose.setContent { SubTrackrTheme(darkTheme = dark) { Screen() } }
        compose.onNodeWithText("Shares its data contract with the desktop app.").performScrollTo()
        compose.onRoot().captureRoboImage("$SCREENSHOT_DIR/settings_bottom_${if (dark) "dark" else "light"}.png")
    }

    @Test
    fun topLight() = top(dark = false)

    @Test
    fun topDark() = top(dark = true)

    @Test
    fun bottomLight() = bottom(dark = false)

    @Test
    fun bottomDark() = bottom(dark = true)
}
