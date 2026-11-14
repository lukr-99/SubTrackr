package com.lukr99.subtrackr

import com.lukr99.subtrackr.ui.AboutInfo
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class AboutInfoTest {
    @Test fun linksAreHttps() {
        listOf(AboutInfo.INSTAGRAM_URL, AboutInfo.GITHUB_URL, AboutInfo.ISSUES_URL, AboutInfo.LICENSE_URL)
            .forEach { assertTrue(it, it.startsWith("https://")) }
    }

    @Test fun handlesMatchUrls() {
        assertTrue(AboutInfo.INSTAGRAM_URL.contains(AboutInfo.INSTAGRAM_HANDLE.removePrefix("@")))
        assertTrue(AboutInfo.GITHUB_URL.endsWith(AboutInfo.GITHUB_HANDLE))
        assertEquals("https://github.com/lukr-99/SubTrackr/issues", AboutInfo.ISSUES_URL)
    }

    @Test fun creditsAreNotEmpty() {
        assertTrue(AboutInfo.libraries.isNotEmpty())
        assertTrue(AboutInfo.services.isNotEmpty())
    }
}
