package com.lukr99.subtrackr.ui.dashboard

import android.app.Application
import androidx.compose.ui.test.junit4.createComposeRule
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.lukr99.subtrackr.ui.SampleData
import com.lukr99.subtrackr.ui.captureScreen
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = RobolectricDeviceQualifiers.Pixel7, application = Application::class)
class DashboardScreenshotTest {
    @get:Rule
    val compose = createComposeRule()

    private fun capture(dark: Boolean) = compose.captureScreen("dashboard", dark) {
        DashboardScreen(
            summary = SampleData.summary,
            baseCurrency = SampleData.BASE,
            rates = SampleData.rates,
            budget = SampleData.budget,
            onEdit = {},
            today = SampleData.today,
            showServiceLogos = true,
        )
    }

    @Test
    fun light() = capture(dark = false)

    @Test
    fun dark() = capture(dark = true)
}
