package com.lukr99.subtrackr.ui.settings

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.components.SegmentedChoice
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Theme picker (follow the system, or always light or dark) and the service-logo switch. */
@Composable
fun AppearanceCard(
    themeMode: ThemeMode,
    onSelect: (ThemeMode) -> Unit,
    showServiceLogos: Boolean,
    onShowServiceLogos: (Boolean) -> Unit,
) {
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
        Spacer(Modifier.height(14.dp))
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            Column(Modifier.weight(1f)) {
                Text("Show service logos", color = SubTrackrTheme.colors.textPrimary, fontSize = 13.sp)
                Text(
                    "Loads each website's icon from Google, which learns the site. Off shows the emoji.",
                    color = SubTrackrTheme.colors.textMuted,
                    fontSize = 11.sp,
                )
            }
            Spacer(Modifier.width(12.dp))
            Switch(
                checked = showServiceLogos,
                onCheckedChange = onShowServiceLogos,
                colors = SwitchDefaults.colors(
                    checkedThumbColor = SubTrackrTheme.colors.onAccent,
                    checkedTrackColor = SubTrackrTheme.colors.accent,
                    uncheckedThumbColor = SubTrackrTheme.colors.textMuted,
                    uncheckedTrackColor = SubTrackrTheme.colors.surfaceAlt,
                    uncheckedBorderColor = SubTrackrTheme.colors.border,
                ),
                modifier = Modifier.testTag(SettingsTags.SHOW_SERVICE_LOGOS),
            )
        }
    }
}
