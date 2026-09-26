package com.lukr99.subtrackr.ui.backup

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
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
import com.lukr99.subtrackr.domain.backup.RestoreMode
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Export and restore buttons, the last result, and the restore dialogs while a file is pending. */
@Composable
fun BackupCard(
    state: BackupUiState,
    onExport: () -> Unit,
    onRestore: () -> Unit,
    onSelectMode: (RestoreMode) -> Unit,
    onConfirmRestore: () -> Unit,
    onCancelRestore: () -> Unit,
) {
    val colors = SubTrackrTheme.colors
    SectionCard {
        Text("Backup", color = colors.textSecondary, fontSize = 12.sp)
        Text(
            "Save all subscriptions and settings to a file, or restore from one. " +
                "Sync settings and your sign-in are never included.",
            color = colors.textMuted,
            fontSize = 11.sp,
        )
        Spacer(Modifier.height(10.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.CenterVertically) {
            Button(
                enabled = !state.busy,
                onClick = onExport,
                colors = ButtonDefaults.buttonColors(containerColor = colors.accent),
                modifier = Modifier.testTag(BackupTags.EXPORT),
            ) { Text("Export backup", color = colors.onAccent) }
            Button(
                enabled = !state.busy,
                onClick = onRestore,
                colors = ButtonDefaults.buttonColors(containerColor = colors.surfaceAlt),
                modifier = Modifier.testTag(BackupTags.RESTORE),
            ) { Text("Restore from file", color = colors.textPrimary) }
            if (state.busy) {
                CircularProgressIndicator(modifier = Modifier.size(22.dp), strokeWidth = 2.dp, color = colors.accent)
            }
        }
        if (state.message.isNotEmpty()) {
            Spacer(Modifier.height(10.dp))
            Text(
                state.message,
                color = if (state.messageIsError) colors.negative else colors.positive,
                fontSize = 13.sp,
                modifier = Modifier.testTag(BackupTags.MESSAGE),
            )
        }
    }
    RestoreDialogs(state, onSelectMode, onConfirmRestore, onCancelRestore)
}
