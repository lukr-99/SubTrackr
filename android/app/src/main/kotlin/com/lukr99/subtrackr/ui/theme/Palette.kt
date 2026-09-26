package com.lukr99.subtrackr.ui.theme

import androidx.compose.ui.graphics.Color

/** The dark palette every screen draws with. */
object Palette {
    val Bg = Color(0xFF1A1B1E)
    val Surface = Color(0xFF25262B)
    val SurfaceAlt = Color(0xFF2E2F35)
    val Border = Color(0xFF3A3B42)
    val Accent = Color(0xFF4C8DFF)
    val TextPrimary = Color(0xFFF1F3F5)
    val TextSecondary = Color(0xFF9AA0A6)
    val TextMuted = Color(0xFF6C727A)
    val Positive = Color(0xFF3DD68C)
    val Negative = Color(0xFFFF6B6B)

    val categories = listOf(
        Color(0xFF4C8DFF), Color(0xFF3DD68C), Color(0xFFFFC048), Color(0xFFFF6B6B),
        Color(0xFFB084FF), Color(0xFF4CD4E0), Color(0xFFFF9F6B), Color(0xFFE86BC7),
    )

    fun category(i: Int) = categories[((i % categories.size) + categories.size) % categories.size]
}
