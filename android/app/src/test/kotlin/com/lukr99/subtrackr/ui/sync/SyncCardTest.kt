package com.lukr99.subtrackr.ui.sync

import android.app.Application
import androidx.compose.ui.test.assertIsEnabled
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import com.lukr99.subtrackr.application.sync.SyncStatus
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35], application = Application::class)
class SyncCardTest {
    @get:Rule
    val compose = createComposeRule()

    @Test
    fun syncNow_staysAvailableWhileAPassRuns() {
        var presses = 0
        val syncing = SyncUiState(
            status = SyncStatus.Syncing,
            statusText = "Syncing…",
            signedInEmail = "user@example.com",
            savedUrl = "https://project.example",
            savedKey = "publishable-key",
            url = "https://project.example",
            key = "publishable-key",
        )
        compose.setContent {
            SubTrackrTheme(darkTheme = false) {
                SyncCard(syncing, {}, {}, {}, {}, {}, {}, {}, {}, { presses++ }, {})
            }
        }

        compose.onNodeWithTag(SyncTags.SYNC_NOW).assertIsEnabled().performClick()

        assertEquals(1, presses)
    }
}
