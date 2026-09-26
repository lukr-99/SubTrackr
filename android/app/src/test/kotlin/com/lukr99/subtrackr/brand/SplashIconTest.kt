package com.lukr99.subtrackr.brand

import com.lukr99.subtrackr.brand.DrawableXml.Companion.all
import com.lukr99.subtrackr.brand.DrawableXml.Companion.android
import com.lukr99.subtrackr.brand.DrawableXml.Companion.androidDouble
import com.lukr99.subtrackr.brand.DrawableXml.Companion.single
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.w3c.dom.Element
import kotlin.math.sqrt

/** SPEC.md section 10: the splash icon is the logo.json mark and grows its bars with its timing. */
class SplashIconTest {
    private val logo = LogoSpec.load()

    // The logo's 256 square takes 56 of the 108 units, centered.
    private val mapping = LogoSpec.Mapping(offset = 26.0, size = 56.0, canvas = logo.canvas)

    private val still = DrawableXml("drawable/splash_icon.xml").root
    private val animated = DrawableXml("drawable-v31/splash_icon.xml").root

    @Test
    fun stillIcon_isTheWholeMark() = assertMark(still)

    @Test
    fun animatedIcon_isTheWholeMark() = assertMark(animated.single("vector"))

    @Test
    fun mark_staysInsideTheVisibleSplashCircle() {
        // Android 12+ shows an icon without a background inside a circle of 2/3 of its size.
        val visibleRadius = 108.0 / 3
        val halfSide = mapping.size / 2
        val corner = logo.cornerRadius * mapping.scale
        val farthest = (halfSide - corner) * sqrt(2.0) + corner
        assertTrue("mark reaches $farthest of $visibleRadius", farthest <= visibleRadius)
    }

    @Test
    fun animatedBars_growFromTheBaselineInOrder() {
        val groups = animated.all("group")
        val targets = animated.all("target")
        val bars = logo.bars().map(mapping::map)
        assertEquals(bars.size, groups.size)
        assertEquals(bars.size, targets.size)

        bars.forEachIndexed { i, bar ->
            val group = groups[i]
            assertEquals(bar.centerX, group.androidDouble("pivotX"), EPS)
            assertEquals(mapping.map(logo.baseline.toDouble()), group.androidDouble("pivotY"), EPS)
            assertEquals(0.0, group.androidDouble("scaleY"), 0.0)
            assertRect(bar, VectorPath.parse(group.single("path").android("pathData")).bounds)

            val target = targets[i]
            assertEquals(group.android("name"), target.android("name"))
            val animator = target.single("objectAnimator")
            assertEquals("scaleY", animator.android("propertyName"))
            assertEquals(0.0, animator.androidDouble("valueFrom"), 0.0)
            assertEquals(1.0, animator.androidDouble("valueTo"), 0.0)
            assertEquals(logo.barDurationMs, animator.android("duration").toInt())
            assertEquals(i * logo.staggerMs, animator.android("startOffset").toInt())
            assertEquals(INTERPOLATORS.getValue(logo.easing), animator.android("interpolator"))
        }
    }

    @Test
    fun splashTheme_usesTheIconTheThemeBackgroundAndTheAnimationLength() {
        val integer = DrawableXml("values/integers.xml").root.all("integer")
            .single { it.getAttribute("name") == "splash_animation_ms" }
        assertEquals(logo.totalAnimationMs, integer.textContent.trim().toInt())

        val style = DrawableXml("values/themes.xml").root.all("style")
            .single { it.getAttribute("name") == "Theme.SubTrackr.Starting" }
        val items = style.all("item").associate { it.getAttribute("name") to it.textContent.trim() }
        assertEquals("Theme.SplashScreen", style.getAttribute("parent"))
        assertEquals("@color/bg", items["windowSplashScreenBackground"])
        assertEquals("@drawable/splash_icon", items["windowSplashScreenAnimatedIcon"])
        assertEquals("@integer/splash_animation_ms", items["windowSplashScreenAnimationDuration"])
        assertEquals("@style/Theme.SubTrackr", items["postSplashScreenTheme"])
    }

    private fun assertMark(vector: Element) {
        assertEquals(108.0, vector.androidDouble("viewportWidth"), 0.0)
        assertEquals(108.0, vector.androidDouble("viewportHeight"), 0.0)
        val paths = vector.all("path")
        assertEquals(1 + logo.barHeights.size, paths.size)

        val square = VectorPath.parse(paths.first().android("pathData"))
        assertRect(mapping.map(LogoSpec.Rect(0.0, 0.0, logo.canvas.toDouble(), logo.canvas.toDouble())), square.bounds)
        square.arcRadii.forEach { assertEquals(logo.cornerRadius * mapping.scale, it, EPS) }
        val gradient = paths.first().single("gradient")
        assertEquals(logo.gradientStart, gradient.android("startColor").uppercase())
        assertEquals(logo.gradientEnd, gradient.android("endColor").uppercase())
        assertEquals(mapping.map(0.0), gradient.androidDouble("startX"), EPS)
        assertEquals(mapping.map(0.0), gradient.androidDouble("startY"), EPS)
        assertEquals(mapping.map(logo.canvas.toDouble()), gradient.androidDouble("endX"), EPS)
        assertEquals(mapping.map(logo.canvas.toDouble()), gradient.androidDouble("endY"), EPS)

        paths.drop(1).zip(logo.bars().map(mapping::map)).forEach { (path, bar) ->
            assertEquals(logo.mark, path.android("fillColor").uppercase())
            val parsed = VectorPath.parse(path.android("pathData"))
            assertRect(bar, parsed.bounds)
            parsed.arcRadii.forEach { assertEquals(logo.barCornerRadius * mapping.scale, it, EPS) }
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
        val INTERPOLATORS = mapOf("decelerate" to "@android:anim/decelerate_interpolator")
    }
}
