package com.lukr99.subtrackr.ui

import android.app.Application
import androidx.compose.ui.test.junit4.createComposeRule
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
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

    private fun capture(dark: Boolean) =
        compose.captureScreen("app_shell", dark) { SubTrackrApp(testViewModelFactory(temp.root)) }

    @Test
    fun light() = capture(dark = false)

    @Test
    fun dark() = capture(dark = true)
}
