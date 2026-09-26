package com.lukr99.subtrackr.ui

import androidx.compose.runtime.Composable
import androidx.compose.ui.test.junit4.ComposeContentTestRule
import androidx.compose.ui.test.onRoot
import com.github.takahirom.roborazzi.captureRoboImage
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Checked-in reference images; `recordRoborazziDebug` rewrites them. */
const val SCREENSHOT_DIR = "src/test/screenshots"

/** Renders [content] in the light or dark theme and captures the whole window as [name].png. */
fun ComposeContentTestRule.captureScreen(name: String, dark: Boolean, content: @Composable () -> Unit) {
    setContent { SubTrackrTheme(darkTheme = dark, content = content) }
    onRoot().captureRoboImage("$SCREENSHOT_DIR/${name}_${if (dark) "dark" else "light"}.png")
}
