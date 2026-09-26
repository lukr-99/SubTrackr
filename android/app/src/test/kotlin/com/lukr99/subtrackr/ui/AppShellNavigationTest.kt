package com.lukr99.subtrackr.ui

import android.app.Application
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertTextEquals
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import com.lukr99.subtrackr.ui.dashboard.DashboardTags
import com.lukr99.subtrackr.ui.editor.EditorTags
import com.lukr99.subtrackr.ui.settings.SettingsTags
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import com.lukr99.subtrackr.ui.whatif.WhatIfTags
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** Navigation through the tags that Maestro and uiautomator also use. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35], application = Application::class)
class AppShellNavigationTest {
    @get:Rule
    val compose = createComposeRule()

    @get:Rule
    val temp = TemporaryFolder()

    @Test
    fun tabsAndEditor_areReachableByTag() {
        compose.setContent { SubTrackrTheme(darkTheme = false) { SubTrackrApp(testViewModelFactory(temp.root)) } }

        compose.onNodeWithTag(DashboardTags.ROOT).assertIsDisplayed()
        compose.onNodeWithTag(DashboardTags.MONTHLY_TOTAL).assertTextEquals("1,734.40 Kč")

        compose.onNodeWithTag(AppTags.NAV_WHATIF).performClick()
        compose.onNodeWithTag(WhatIfTags.ROOT).assertIsDisplayed()

        compose.onNodeWithTag(AppTags.NAV_SETTINGS).performClick()
        compose.onNodeWithTag(SettingsTags.ROOT).assertIsDisplayed()

        compose.onNodeWithTag(AppTags.NAV_DASHBOARD).performClick()
        compose.onNodeWithTag(AppTags.ADD).performClick()
        compose.onNodeWithTag(EditorTags.ROOT).assertIsDisplayed()
        compose.onNodeWithTag(EditorTags.CANCEL).performClick()
        compose.onNodeWithTag(DashboardTags.ROOT).assertIsDisplayed()
    }
}
