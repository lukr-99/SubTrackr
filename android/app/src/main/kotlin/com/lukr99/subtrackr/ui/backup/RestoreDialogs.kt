package com.lukr99.subtrackr.ui.backup

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.backup.RestoreMode
import com.lukr99.subtrackr.ui.components.SegmentedChoice
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Choose Merge or Replace for the picked file; Replace needs a second, explicit confirmation. */
@Composable
fun RestoreDialogs(
    state: BackupUiState,
    onSelectMode: (RestoreMode) -> Unit,
    onConfirm: () -> Unit,
    onCancel: () -> Unit,
) {
    if (state.pendingUri == null) return
    val colors = SubTrackrTheme.colors
    if (state.confirmingReplace) {
        AlertDialog(
            onDismissRequest = onCancel,
            title = { Text("Replace everything?") },
            text = {
                Text(
                    "The subscriptions and settings on this phone will be replaced by the backup's. " +
                        "Your sync project stays as it is. This can't be undone.",
                )
            },
            confirmButton = {
                TextButton(onClick = onConfirm, modifier = Modifier.testTag(BackupTags.REPLACE_CONFIRM)) {
                    Text("Replace", color = colors.negative, fontWeight = FontWeight.SemiBold)
                }
            },
            dismissButton = {
                TextButton(onClick = onCancel, modifier = Modifier.testTag(BackupTags.CANCEL)) { Text("Cancel") }
            },
        )
        return
    }
    AlertDialog(
        onDismissRequest = onCancel,
        title = { Text("Restore backup") },
        text = {
            Column {
                SegmentedChoice(
                    options = RestoreMode.entries,
                    selected = state.mode,
                    label = { if (it == RestoreMode.MERGE) "Merge" else "Replace" },
                    onSelect = onSelectMode,
                    tag = BackupTags::mode,
                )
                Spacer(Modifier.height(12.dp))
                Text(
                    when (state.mode) {
                        RestoreMode.MERGE ->
                            "Adds what's missing and keeps the newer copy of each subscription. " +
                                "Your settings stay as they are."
                        RestoreMode.REPLACE ->
                            "Makes this phone match the backup: its subscriptions and settings. " +
                                "Your sync project stays as it is."
                    },
                    color = colors.textSecondary,
                    fontSize = 14.sp,
                )
            }
        },
        confirmButton = {
            TextButton(onClick = onConfirm, modifier = Modifier.testTag(BackupTags.CONFIRM)) {
                Text(if (state.mode == RestoreMode.MERGE) "Restore" else "Continue")
            }
        },
        dismissButton = {
            TextButton(onClick = onCancel, modifier = Modifier.testTag(BackupTags.CANCEL)) { Text("Cancel") }
        },
    )
}
