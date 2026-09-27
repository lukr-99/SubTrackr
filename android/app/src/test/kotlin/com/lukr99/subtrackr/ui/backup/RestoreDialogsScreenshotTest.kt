package com.lukr99.subtrackr.ui.backup

import android.app.Application
import androidx.compose.ui.test.junit4.createComposeRule
import com.github.takahirom.roborazzi.ExperimentalRoborazziApi
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureScreenRoboImage
import com.lukr99.subtrackr.domain.backup.RestoreMode
import com.lukr99.subtrackr.ui.SCREENSHOT_DIR
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

@OptIn(ExperimentalRoborazziApi::class)
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = RobolectricDeviceQualifiers.Pixel7, application = Application::class)
class RestoreDialogsScreenshotTest {
    @get:Rule
    val compose = createComposeRule()

    private fun capture(name: String, dark: Boolean, state: BackupUiState) {
        compose.setContent {
            SubTrackrTheme(darkTheme = dark) { RestoreDialogs(state, onSelectMode = {}, onConfirm = {}, onCancel = {}) }
        }
        compose.waitForIdle()
        // Dialogs live in their own window, so capture the whole screen rather than one root.
        captureScreenRoboImage("$SCREENSHOT_DIR/${name}_${if (dark) "dark" else "light"}.png")
    }

    private val choosing = BackupUiState(pendingUri = "content://backup", mode = RestoreMode.MERGE)
    private val confirming = choosing.copy(mode = RestoreMode.REPLACE, confirmingReplace = true)

    @Test
    fun chooseLight() = capture("restore_choose", dark = false, state = choosing)

    @Test
    fun chooseDark() = capture("restore_choose", dark = true, state = choosing)

    @Test
    fun confirmReplaceLight() = capture("restore_confirm_replace", dark = false, state = confirming)

    @Test
    fun confirmReplaceDark() = capture("restore_confirm_replace", dark = true, state = confirming)
}
