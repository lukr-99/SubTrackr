package com.lukr99.subtrackr.ui.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import com.lukr99.subtrackr.ui.update.UpdateUiState

/** Update status, a manual check, the verified install, and the releases page as a manual path. */
@OptIn(ExperimentalLayoutApi::class)
@Composable
fun UpdatesCard(
    state: UpdateUiState,
    onCheck: () -> Unit,
    onInstall: () -> Unit,
    onOpenReleases: () -> Unit,
) {
    SectionCard {
        Text("Updates", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        Text(
            state.statusText,
            color = SubTrackrTheme.colors.textPrimary,
            fontSize = 13.sp,
            modifier = Modifier.testTag(SettingsTags.UPDATE_STATUS),
        )
        Spacer(Modifier.height(10.dp))
        FlowRow(
            horizontalArrangement = Arrangement.spacedBy(10.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            if (state.offer != null && !state.busy) {
                Button(
                    onClick = onInstall,
                    colors = ButtonDefaults.buttonColors(containerColor = SubTrackrTheme.colors.accent),
                ) { Text("Download and install", color = SubTrackrTheme.colors.onAccent) }
            }
            Button(
                enabled = state.checksEnabled && !state.busy,
                onClick = onCheck,
                colors = ButtonDefaults.buttonColors(containerColor = SubTrackrTheme.colors.surfaceAlt),
                modifier = Modifier.testTag(SettingsTags.CHECK_UPDATES),
            ) { Text("Check for updates", color = SubTrackrTheme.colors.textPrimary) }
            Button(
                onClick = onOpenReleases,
                colors = ButtonDefaults.buttonColors(containerColor = SubTrackrTheme.colors.surfaceAlt),
            ) { Text("Open releases page", color = SubTrackrTheme.colors.textPrimary) }
            if (state.busy) {
                CircularProgressIndicator(
                    modifier = Modifier.size(22.dp).align(Alignment.CenterVertically),
                    strokeWidth = 2.dp,
                    color = SubTrackrTheme.colors.accent,
                )
            }
        }
    }
}
