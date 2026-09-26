package com.lukr99.subtrackr.ui.theme

import androidx.compose.runtime.Immutable
import androidx.compose.ui.graphics.Color

/**
 * One theme's semantic colors, loaded from contracts/design/tokens.json. Neutral tokens (surfaces,
 * text, borders, status) and accent tokens come from separate groups; charts have their own list.
 */
@Immutable
data class SubTrackrColors(
    val isDark: Boolean,
    val background: Color,
    val surface: Color,
    val surfaceAlt: Color,
    val border: Color,
    val textPrimary: Color,
    val textSecondary: Color,
    val textMuted: Color,
    val positive: Color,
    val negative: Color,
    val warning: Color,
    val accent: Color,
    val accentHover: Color,
    val onAccent: Color,
    val chart: List<Color>,
) {
    /** The chart color for series [index], wrapping around the list. */
    fun chartColor(index: Int): Color = chart[Math.floorMod(index, chart.size)]
}
