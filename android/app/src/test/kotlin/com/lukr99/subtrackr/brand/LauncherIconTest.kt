package com.lukr99.subtrackr.brand

import com.lukr99.subtrackr.brand.DrawableXml.Companion.all
import com.lukr99.subtrackr.brand.DrawableXml.Companion.android
import com.lukr99.subtrackr.brand.DrawableXml.Companion.androidDouble
import com.lukr99.subtrackr.brand.DrawableXml.Companion.single
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.hypot

/** SPEC.md section 10: the adaptive launcher icon takes its geometry from logo.json. */
class LauncherIconTest {
    private val logo = LogoSpec.load()

    // The logo's 256 square fills the inner 72dp of the 108dp adaptive-icon canvas.
    private val mapping = LogoSpec.Mapping(offset = 18.0, size = 72.0, canvas = logo.canvas)

    @Test
    fun foregroundBars_areTheLogoBarsMappedOntoTheInnerSquare() {
        val vector = DrawableXml("drawable/ic_launcher_foreground.xml").root
        assertEquals(108.0, vector.androidDouble("viewportWidth"), 0.0)
        assertEquals(108.0, vector.androidDouble("viewportHeight"), 0.0)

        val paths = vector.all("path")
        val expected = logo.bars().map(mapping::map)
        assertEquals(expected.size, paths.size)
        paths.zip(expected).forEach { (path, bar) ->
            assertEquals(logo.mark, path.android("fillColor").uppercase())
            val parsed = VectorPath.parse(path.android("pathData"))
            assertRect(bar, parsed.bounds)
            assertTrue(parsed.arcRadii.isNotEmpty())
            parsed.arcRadii.forEach { assertEquals(logo.barCornerRadius * mapping.scale, it, EPS) }
        }
    }

    @Test
    fun foregroundBars_areCenteredAndInsideTheSafeZone() {
        val bounds = DrawableXml("drawable/ic_launcher_foreground.xml").root.all("path")
            .map { VectorPath.parse(it.android("pathData")).bounds }
        val group = LogoSpec.Rect(
            bounds.minOf { it.left }, bounds.minOf { it.top }, bounds.maxOf { it.right }, bounds.maxOf { it.bottom },
        )
        assertEquals(54.0, group.centerX, EPS)
        assertEquals(54.0, group.centerY, EPS)
        // Every corner stays inside the 66dp safe-zone circle that no launcher mask crops.
        listOf(group.left to group.top, group.right to group.top, group.left to group.bottom, group.right to group.bottom)
            .forEach { (x, y) -> assertTrue(hypot(x - 54, y - 54) <= 33.0) }
    }

    @Test
    fun background_isTheBrandGradientFromTopLeftToBottomRight() {
        val vector = DrawableXml("drawable/ic_launcher_background.xml").root
        val path = vector.single("path")
        assertEquals(LogoSpec.Rect(0.0, 0.0, 108.0, 108.0), VectorPath.parse(path.android("pathData")).bounds)

        val gradient = vector.single("gradient")
        assertEquals("linear", gradient.android("type"))
        assertEquals(logo.gradientStart, gradient.android("startColor").uppercase())
        assertEquals(logo.gradientEnd, gradient.android("endColor").uppercase())
        assertEquals(mapping.map(0.0), gradient.androidDouble("startX"), EPS)
        assertEquals(mapping.map(0.0), gradient.androidDouble("startY"), EPS)
        assertEquals(mapping.map(logo.canvas.toDouble()), gradient.androidDouble("endX"), EPS)
        assertEquals(mapping.map(logo.canvas.toDouble()), gradient.androidDouble("endY"), EPS)
    }

    @Test
    fun adaptiveIcons_useTheGradientTheBarsAndAThemedMonochromeLayer() {
        listOf("ic_launcher", "ic_launcher_round").forEach { name ->
            val icon = DrawableXml("mipmap-anydpi/$name.xml").root
            assertEquals("@drawable/ic_launcher_background", icon.single("background").android("drawable"))
            assertEquals("@drawable/ic_launcher_foreground", icon.single("foreground").android("drawable"))
            assertEquals("@drawable/ic_launcher_foreground", icon.single("monochrome").android("drawable"))
        }
    }

    private fun assertRect(expected: LogoSpec.Rect, actual: LogoSpec.Rect) {
        assertEquals(expected.left, actual.left, EPS)
        assertEquals(expected.top, actual.top, EPS)
        assertEquals(expected.right, actual.right, EPS)
        assertEquals(expected.bottom, actual.bottom, EPS)
    }

    private companion object {
        const val EPS = 1e-6
    }
}
