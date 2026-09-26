package com.lukr99.subtrackr.ui.theme

import androidx.compose.ui.graphics.Color
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

/**
 * The app's palettes, read once from contracts/design/tokens.json. Gradle puts that folder on the
 * main Java resources, so the file in the APK is the shared file itself, not a copy.
 */
object DesignTokens {
    private const val RESOURCE = "/tokens.json"
    private val HEX = Regex("^#[0-9A-Fa-f]{6}$")

    private val root: JsonObject by lazy {
        val text = DesignTokens::class.java.getResourceAsStream(RESOURCE)
            ?.bufferedReader()?.use { it.readText() }
            ?: error("Design tokens $RESOURCE are missing from the app resources.")
        Json.parseToJsonElement(text).jsonObject
    }

    val light: SubTrackrColors by lazy { palette(root, "light") }
    val dark: SubTrackrColors by lazy { palette(root, "dark") }

    private fun palette(root: JsonObject, theme: String): SubTrackrColors {
        val neutral = root.getValue("neutral").jsonObject.getValue(theme).jsonObject
        val accent = root.getValue("accent").jsonObject.getValue(theme).jsonObject
        fun n(key: String) = color(neutral.getValue(key).jsonPrimitive.content)
        fun a(key: String) = color(accent.getValue(key).jsonPrimitive.content)
        return SubTrackrColors(
            isDark = theme == "dark",
            background = n("background"),
            surface = n("surface"),
            surfaceAlt = n("surfaceAlt"),
            border = n("border"),
            textPrimary = n("textPrimary"),
            textSecondary = n("textSecondary"),
            textMuted = n("textMuted"),
            positive = n("positive"),
            negative = n("negative"),
            warning = n("warning"),
            accent = a("accent"),
            accentHover = a("accentHover"),
            onAccent = a("onAccent"),
            chart = root.getValue("chart").jsonObject.getValue(theme).jsonArray.map { color(it.jsonPrimitive.content) },
        )
    }

    /** `#RRGGBB` to an opaque color. */
    fun color(hex: String): Color {
        require(HEX.matches(hex)) { "Design token '$hex' is not #RRGGBB." }
        return Color(0xFF000000L or hex.substring(1).toLong(16))
    }
}
