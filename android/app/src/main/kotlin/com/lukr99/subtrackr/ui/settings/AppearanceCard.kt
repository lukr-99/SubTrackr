package com.lukr99.subtrackr.ui.settings

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.components.SegmentedChoice
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Theme picker: follow the system, or always light or dark. */
@Composable
fun AppearanceCard(themeMode: ThemeMode, onSelect: (ThemeMode) -> Unit) {
    SectionCard {
        Text("Appearance", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        Text(
            "System follows your phone's light or dark setting.",
            color = SubTrackrTheme.colors.textMuted,
            fontSize = 11.sp,
        )
        Spacer(Modifier.height(10.dp))
        SegmentedChoice(
            options = ThemeMode.entries,
            selected = themeMode,
            label = {
                when (it) {
                    ThemeMode.SYSTEM -> "System"
                    ThemeMode.LIGHT -> "Light"
                    ThemeMode.DARK -> "Dark"
                }
            },
            onSelect = onSelect,
            tag = { SettingsTags.theme(it) },
        )
    }
}
