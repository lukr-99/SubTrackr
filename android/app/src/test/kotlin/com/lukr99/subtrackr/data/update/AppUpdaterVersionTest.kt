package com.lukr99.subtrackr.data.update

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class AppUpdaterVersionTest {

    @Test
    fun detects_strictly_newer_dotted_versions() {
        assertTrue(AppUpdater.isNewer("0.2.2", "0.2.1"))
        assertTrue(AppUpdater.isNewer("1.0.0", "0.9.9"))
        assertFalse(AppUpdater.isNewer("0.2.1", "0.2.1"))
        assertFalse(AppUpdater.isNewer("0.2.0", "0.2.1"))
    }

    @Test
    fun accepts_a_leading_v_and_missing_version_parts() {
        assertTrue(AppUpdater.isNewer("v0.3", "0.2.9"))
        assertFalse(AppUpdater.isNewer("v0.2.1", "0.2.1.0"))
    }
}
