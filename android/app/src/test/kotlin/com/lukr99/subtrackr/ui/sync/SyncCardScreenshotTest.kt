package com.lukr99.subtrackr.ui.sync

import android.app.Application
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.unit.dp
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.lukr99.subtrackr.application.sync.SyncStatus
import com.lukr99.subtrackr.ui.captureScreen
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** The Sync card while signed out, at the code step with an error, and signed in after a failure. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = RobolectricDeviceQualifiers.Pixel7, application = Application::class)
class SyncCardScreenshotTest {
    @get:Rule
    val compose = createComposeRule()

    private val project = SyncUiState(
        status = SyncStatus.SignedOut,
        statusText = "Signed out.",
        savedUrl = "https://project.example",
        savedKey = "publishable-key",
        url = "https://project.example",
        key = "publishable-key",
    )

    private val emailStep = project.copy(email = "user@example.com")
    private val codeStep = emailStep.copy(step = SignInStep.CODE, code = "123", error = SyncFormError.INVALID_CODE)
    private val failed = project.copy(
        status = SyncStatus.Failed("offline"),
        statusText = "Failed: offline",
        signedInEmail = "user@example.com",
    )

    private fun capture(name: String, dark: Boolean, state: SyncUiState) = compose.captureScreen(name, dark) {
        Column(
            Modifier
                .fillMaxSize()
                .background(SubTrackrTheme.colors.background)
                .padding(16.dp),
        ) {
            SyncCard(state, {}, {}, {}, {}, {}, {}, {}, {}, {}, {})
        }
    }

    @Test
    fun emailLight() = capture("sync_email", dark = false, state = emailStep)

    @Test
    fun emailDark() = capture("sync_email", dark = true, state = emailStep)

    @Test
    fun codeLight() = capture("sync_code", dark = false, state = codeStep)

    @Test
    fun codeDark() = capture("sync_code", dark = true, state = codeStep)

    @Test
    fun failedLight() = capture("sync_failed", dark = false, state = failed)

    @Test
    fun failedDark() = capture("sync_failed", dark = true, state = failed)
}
