package com.lukr99.subtrackr.ui.sync

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.application.sync.SyncStatus
import com.lukr99.subtrackr.ui.components.LabeledTextField
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Project settings, the sync state line, sign-in, and sync controls. State in, callbacks out. */
@Composable
fun SyncCard(
    state: SyncUiState,
    onUrlChange: (String) -> Unit,
    onKeyChange: (String) -> Unit,
    onSaveConfig: () -> Unit,
    onEmailChange: (String) -> Unit,
    onSendCode: () -> Unit,
    onCodeChange: (String) -> Unit,
    onVerify: () -> Unit,
    onUseAnotherEmail: () -> Unit,
    onSyncNow: () -> Unit,
    onSignOut: () -> Unit,
) {
    val colors = SubTrackrTheme.colors
    SectionCard {
        Text("Sync (Supabase)", color = colors.textSecondary, fontSize = 12.sp)
        Text(
            "Keeps subscriptions in step across your devices through your own Supabase project. " +
                "Settings stay on each device.",
            color = colors.textMuted,
            fontSize = 11.sp,
        )
        Spacer(Modifier.height(8.dp))
        Text(
            state.statusText,
            color = if (state.status is SyncStatus.Failed) colors.negative else colors.textPrimary,
            fontSize = 13.sp,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.testTag(SyncTags.STATUS),
        )
        Spacer(Modifier.height(10.dp))
        LabeledTextField(
            "Project URL",
            state.url,
            onUrlChange,
            Modifier.testTag(SyncTags.URL),
            KeyboardOptions(keyboardType = KeyboardType.Uri, imeAction = ImeAction.Next),
        )
        Spacer(Modifier.height(8.dp))
        LabeledTextField("Publishable key", state.key, onKeyChange, Modifier.testTag(SyncTags.KEY))
        if (state.configChanged) {
            Spacer(Modifier.height(8.dp))
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                Button(
                    onClick = onSaveConfig,
                    colors = ButtonDefaults.buttonColors(containerColor = colors.accent),
                    modifier = Modifier.testTag(SyncTags.SAVE),
                ) { Text("Save", color = colors.onAccent) }
                if (state.signedIn) {
                    Text("Saving a different project signs you out.", color = colors.textMuted, fontSize = 11.sp)
                }
            }
        }

        if (state.configured && !state.configChanged) {
            Spacer(Modifier.height(14.dp))
            when {
                state.signedIn -> SignedIn(state, onSyncNow, onSignOut)
                state.step == SignInStep.EMAIL -> EmailStep(state, onEmailChange, onSendCode)
                else -> CodeStep(state, onCodeChange, onVerify, onUseAnotherEmail)
            }
        }

        state.error?.let { error ->
            Spacer(Modifier.height(8.dp))
            Text(
                if (error == SyncFormError.OTHER) "${error.message}: ${state.errorDetail}." else error.message,
                color = colors.negative,
                fontSize = 13.sp,
                modifier = Modifier.testTag(SyncTags.ERROR),
            )
        }
    }
}

@Composable
private fun SignedIn(state: SyncUiState, onSyncNow: () -> Unit, onSignOut: () -> Unit) {
    val colors = SubTrackrTheme.colors
    Text("Signed in as ${state.signedInEmail}", color = colors.textPrimary, fontSize = 13.sp)
    Spacer(Modifier.height(8.dp))
    Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.CenterVertically) {
        Button(
            enabled = state.status !is SyncStatus.Syncing,
            onClick = onSyncNow,
            colors = ButtonDefaults.buttonColors(containerColor = colors.accent),
            modifier = Modifier.testTag(SyncTags.SYNC_NOW),
        ) { Text("Sync now", color = colors.onAccent) }
        Button(
            onClick = onSignOut,
            colors = ButtonDefaults.buttonColors(containerColor = colors.surfaceAlt),
            modifier = Modifier.testTag(SyncTags.SIGN_OUT),
        ) { Text("Sign out", color = colors.textPrimary) }
        if (state.status is SyncStatus.Syncing) {
            CircularProgressIndicator(modifier = Modifier.size(22.dp), strokeWidth = 2.dp, color = colors.accent)
        }
    }
}

@Composable
private fun EmailStep(state: SyncUiState, onEmailChange: (String) -> Unit, onSendCode: () -> Unit) {
    val colors = SubTrackrTheme.colors
    Text("Sign in with a code sent to your email.", color = colors.textSecondary, fontSize = 12.sp)
    Spacer(Modifier.height(8.dp))
    LabeledTextField(
        "Email",
        state.email,
        onEmailChange,
        Modifier.testTag(SyncTags.EMAIL),
        KeyboardOptions(keyboardType = KeyboardType.Email, imeAction = ImeAction.Send),
        enabled = !state.busy,
    )
    Spacer(Modifier.height(8.dp))
    Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.CenterVertically) {
        Button(
            enabled = !state.busy,
            onClick = onSendCode,
            colors = ButtonDefaults.buttonColors(containerColor = colors.accent),
            modifier = Modifier.testTag(SyncTags.SEND_CODE),
        ) { Text("Send code", color = colors.onAccent) }
        if (state.busy) CircularProgressIndicator(modifier = Modifier.size(22.dp), strokeWidth = 2.dp, color = colors.accent)
    }
}

@Composable
private fun CodeStep(
    state: SyncUiState,
    onCodeChange: (String) -> Unit,
    onVerify: () -> Unit,
    onUseAnotherEmail: () -> Unit,
) {
    val colors = SubTrackrTheme.colors
    Text("Enter the code we sent to ${state.email.trim()}.", color = colors.textSecondary, fontSize = 12.sp)
    Spacer(Modifier.height(8.dp))
    LabeledTextField(
        "Code",
        state.code,
        onCodeChange,
        Modifier.testTag(SyncTags.CODE),
        KeyboardOptions(keyboardType = KeyboardType.NumberPassword, imeAction = ImeAction.Done),
        enabled = !state.busy,
    )
    Spacer(Modifier.height(8.dp))
    Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.CenterVertically) {
        Button(
            enabled = !state.busy,
            onClick = onVerify,
            colors = ButtonDefaults.buttonColors(containerColor = colors.accent),
            modifier = Modifier.testTag(SyncTags.VERIFY),
        ) { Text("Verify", color = colors.onAccent) }
        TextButton(enabled = !state.busy, onClick = onUseAnotherEmail) {
            Text("Use another email", color = colors.accent)
        }
        if (state.busy) CircularProgressIndicator(modifier = Modifier.size(22.dp), strokeWidth = 2.dp, color = colors.accent)
    }
}
