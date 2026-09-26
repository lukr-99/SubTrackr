package com.lukr99.subtrackr.domain.update

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class AppVersionTest {

    @Test
    fun parse_releaseVersion_hasNoSuffix() {
        val version = AppVersion.parse("0.3.1")

        assertEquals(AppVersion(0, 3, 1), version)
        assertFalse(version!!.isPreRelease)
    }

    @Test
    fun parse_devBuild_isPreRelease() {
        val version = AppVersion.parse("0.3.0-dev")

        assertTrue(version!!.isPreRelease)
        assertEquals("0.3.0", version.core)
        assertEquals("0.3.0-dev", version.toString())
    }

    @Test
    fun parse_rejectsTagsPartialVersionsAndLeadingZeros() {
        assertNull(AppVersion.parse("v0.3.0"))
        assertNull(AppVersion.parse("0.3"))
        assertNull(AppVersion.parse("01.2.3"))
        assertNull(AppVersion.parse(""))
    }

    @Test
    fun compareTo_ordersNumericallyNotLexically() {
        assertTrue(AppVersion.parse("0.10.0")!! > AppVersion.parse("0.9.9")!!)
        assertTrue(AppVersion.parse("1.0.0")!! > AppVersion.parse("0.99.99")!!)
        assertEquals(0, AppVersion.parse("0.3.0")!!.compareTo(AppVersion.parse("0.3.0-dev")!!))
    }

    @Test
    fun checksAllowed_onlyForReleaseVersions() {
        assertTrue(ReleaseSelector.checksAllowed("0.3.0"))
        assertFalse(ReleaseSelector.checksAllowed("0.3.0-dev"))
        assertFalse(ReleaseSelector.checksAllowed("unknown"))
    }
}
