package com.lukr99.subtrackr.ui.theme

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import org.junit.Assert.assertEquals
import org.junit.Test
import java.io.File

/** SPEC.md section 10: the app's palettes must equal contracts/design/tokens.json. */
class ThemeTokensTest {
    private val tokens: JsonObject = Json.parseToJsonElement(
        javaClass.getResourceAsStream("/tokens.json")!!.bufferedReader().use { it.readText() },
    ).jsonObject

    private fun hex(color: Color): String = "#%06X".format(color.toArgb() and 0xFFFFFF)

    private fun appTokens(c: SubTrackrColors): Map<String, String> = mapOf(
        "background" to hex(c.background),
        "surface" to hex(c.surface),
        "surfaceAlt" to hex(c.surfaceAlt),
        "border" to hex(c.border),
        "textPrimary" to hex(c.textPrimary),
        "textSecondary" to hex(c.textSecondary),
        "textMuted" to hex(c.textMuted),
        "positive" to hex(c.positive),
        "negative" to hex(c.negative),
        "warning" to hex(c.warning),
        "accent" to hex(c.accent),
        "accentHover" to hex(c.accentHover),
        "onAccent" to hex(c.onAccent),
    )

    private fun fileTokens(theme: String): Map<String, String> =
        listOf("neutral", "accent").flatMap { group ->
            tokens.getValue(group).jsonObject.getValue(theme).jsonObject.entries
                .map { (key, value) -> key to value.jsonPrimitive.content.uppercase() }
        }.toMap()

    private fun fileChart(theme: String): List<String> =
        tokens.getValue("chart").jsonObject.getValue(theme).jsonArray.map { it.jsonPrimitive.content.uppercase() }

    @Test
    fun lightPalette_equalsTokensFile() {
        assertEquals(fileTokens("light"), appTokens(DesignTokens.light))
        assertEquals(fileChart("light"), DesignTokens.light.chart.map(::hex))
        assertEquals(false, DesignTokens.light.isDark)
    }

    @Test
    fun darkPalette_equalsTokensFile() {
        assertEquals(fileTokens("dark"), appTokens(DesignTokens.dark))
        assertEquals(fileChart("dark"), DesignTokens.dark.chart.map(::hex))
        assertEquals(true, DesignTokens.dark.isDark)
    }

    @Test
    fun launchWindowBackgrounds_matchTheTokens() {
        // Gradle runs unit tests from the module directory.
        fun bg(folder: String): String =
            Regex("""<color name="bg">#FF([0-9A-Fa-f]{6})</color>""")
                .find(File("src/main/res/$folder/colors.xml").readText())!!.groupValues[1].uppercase()

        assertEquals(fileTokens("light").getValue("background"), "#" + bg("values"))
        assertEquals(fileTokens("dark").getValue("background"), "#" + bg("values-night"))
    }
}
