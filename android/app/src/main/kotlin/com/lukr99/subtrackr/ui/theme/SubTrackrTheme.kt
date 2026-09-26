package com.lukr99.subtrackr.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

@Composable
fun SubTrackrTheme(content: @Composable () -> Unit) {
    val scheme = darkColorScheme(
        primary = Palette.Accent,
        background = Palette.Bg,
        surface = Palette.Surface,
        onBackground = Palette.TextPrimary,
        onSurface = Palette.TextPrimary,
    )
    MaterialTheme(colorScheme = scheme, content = content)
}
