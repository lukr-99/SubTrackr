package com.lukr99.subtrackr.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import com.lukr99.subtrackr.model.ThemeMode

private val LocalSubTrackrColors = staticCompositionLocalOf { DesignTokens.dark }

/** Access to the active palette, like `MaterialTheme.colorScheme`. */
object SubTrackrTheme {
    val colors: SubTrackrColors
        @Composable
        @ReadOnlyComposable
        get() = LocalSubTrackrColors.current
}

/** Whether [mode] shows the dark palette. SYSTEM follows the device and recomposes when it changes. */
@Composable
fun isDarkTheme(mode: ThemeMode): Boolean = when (mode) {
    ThemeMode.SYSTEM -> isSystemInDarkTheme()
    ThemeMode.LIGHT -> false
    ThemeMode.DARK -> true
}

/** Provides the token palette for [darkTheme] and a Material scheme built from the same tokens. */
@Composable
fun SubTrackrTheme(darkTheme: Boolean, content: @Composable () -> Unit) {
    val colors = if (darkTheme) DesignTokens.dark else DesignTokens.light
    CompositionLocalProvider(LocalSubTrackrColors provides colors) {
        MaterialTheme(colorScheme = materialScheme(colors), content = content)
    }
}

private fun materialScheme(c: SubTrackrColors): ColorScheme {
    val base = if (c.isDark) darkColorScheme() else lightColorScheme()
    return base.copy(
        primary = c.accent,
        onPrimary = c.onAccent,
        primaryContainer = c.surfaceAlt,
        onPrimaryContainer = c.textPrimary,
        secondary = c.accent,
        onSecondary = c.onAccent,
        secondaryContainer = c.surfaceAlt,
        onSecondaryContainer = c.textPrimary,
        tertiary = c.warning,
        background = c.background,
        onBackground = c.textPrimary,
        surface = c.surface,
        onSurface = c.textPrimary,
        surfaceVariant = c.surfaceAlt,
        onSurfaceVariant = c.textSecondary,
        surfaceTint = c.accent,
        surfaceBright = c.surface,
        surfaceDim = c.background,
        surfaceContainerLowest = c.surface,
        surfaceContainerLow = c.surface,
        surfaceContainer = c.surface,
        surfaceContainerHigh = c.surface,
        surfaceContainerHighest = c.surfaceAlt,
        outline = c.border,
        outlineVariant = c.border,
        error = c.negative,
        onError = c.onAccent,
    )
}
