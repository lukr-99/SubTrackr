package com.lukr99.subtrackr.ui.update

import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable

private const val MAX_NOTES = 600

/** Asks before anything is downloaded; the system installer asks again before installing. */
@Composable
fun UpdatePrompt(state: UpdateUiState, onInstall: () -> Unit, onLater: () -> Unit) {
    val offer = state.offer ?: return
    if (!state.promptVisible) return
    AlertDialog(
        onDismissRequest = onLater,
        confirmButton = { TextButton(onClick = onInstall) { Text("Download and install") } },
        dismissButton = { TextButton(onClick = onLater) { Text("Later") } },
        title = { Text("SubTrackr ${offer.version.core} is available") },
        text = {
            Text(
                offer.notes.ifBlank { "A new version is available. You're on ${state.currentVersion}." }
                    .take(MAX_NOTES),
            )
        },
    )
}
