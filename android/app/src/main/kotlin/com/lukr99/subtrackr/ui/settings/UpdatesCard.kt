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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.Palette
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
        Text("Updates", color = Palette.TextSecondary, fontSize = 12.sp)
        Text(
            state.statusText,
            color = Palette.TextPrimary,
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
                    colors = ButtonDefaults.buttonColors(containerColor = Palette.Accent),
                ) { Text("Download and install", color = Color.White) }
            }
            Button(
                enabled = state.checksEnabled && !state.busy,
                onClick = onCheck,
                colors = ButtonDefaults.buttonColors(containerColor = Palette.SurfaceAlt),
                modifier = Modifier.testTag(SettingsTags.CHECK_UPDATES),
            ) { Text("Check for updates", color = Palette.TextPrimary) }
            Button(
                onClick = onOpenReleases,
                colors = ButtonDefaults.buttonColors(containerColor = Palette.SurfaceAlt),
            ) { Text("Open releases page", color = Palette.TextPrimary) }
            if (state.busy) {
                CircularProgressIndicator(
                    modifier = Modifier.size(22.dp).align(Alignment.CenterVertically),
                    strokeWidth = 2.dp,
                    color = Palette.Accent,
                )
            }
        }
    }
}
