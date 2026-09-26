package com.lukr99.subtrackr.ui.settings

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.platform.LocalUriHandler
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.lukr99.subtrackr.domain.update.UpdateChannel
import com.lukr99.subtrackr.ui.SubTrackrViewModel
import com.lukr99.subtrackr.ui.backup.BackupCard
import com.lukr99.subtrackr.ui.backup.BackupViewModel
import com.lukr99.subtrackr.ui.sync.SyncCard
import com.lukr99.subtrackr.ui.sync.SyncViewModel
import com.lukr99.subtrackr.ui.update.UpdateViewModel

private val BACKUP_TYPES = arrayOf("application/json", "text/plain", "application/octet-stream")

/** Connects the Settings screen to its view models and to the system file pickers. */
@Composable
fun SettingsRoute(app: SubTrackrViewModel, viewModelFactory: ViewModelProvider.Factory) {
    val updates: UpdateViewModel = viewModel(factory = viewModelFactory)
    val backups: BackupViewModel = viewModel(factory = viewModelFactory)
    val syncs: SyncViewModel = viewModel(factory = viewModelFactory)
    val sync by syncs.uiState.collectAsStateWithLifecycle()
    val update by updates.uiState.collectAsStateWithLifecycle()
    val backup by backups.uiState.collectAsStateWithLifecycle()
    val uriHandler = LocalUriHandler.current

    val exportPicker = rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/json")) {
        backups.export(it?.toString())
    }
    val restorePicker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) {
        backups.chooseFile(it?.toString())
    }

    SettingsScreen(
        baseCurrency = app.baseCurrency,
        worthThreshold = app.worthThreshold,
        monthlyBudget = app.monthlyBudget,
        ratesLabel = app.ratesLabel,
        themeMode = app.themeMode,
        appVersion = update.currentVersion,
        onSetThemeMode = app::changeThemeMode,
        onSetBaseCurrency = app::changeBaseCurrency,
        onSetThreshold = app::setWorthThreshold,
        onSetBudget = app::setMonthlyBudget,
        onRefreshRates = { app.refreshRates() },
        syncCard = {
            SyncCard(
                state = sync,
                onUrlChange = syncs::onUrlChange,
                onKeyChange = syncs::onKeyChange,
                onSaveConfig = syncs::saveConfig,
                onEmailChange = syncs::onEmailChange,
                onSendCode = syncs::sendCode,
                onCodeChange = syncs::onCodeChange,
                onVerify = syncs::verify,
                onUseAnotherEmail = syncs::useAnotherEmail,
                onSyncNow = syncs::syncNow,
                onSignOut = syncs::signOut,
            )
        },
        backupCard = {
            BackupCard(
                state = backup,
                onExport = { exportPicker.launch(backups.suggestedFileName()) },
                onRestore = { restorePicker.launch(BACKUP_TYPES) },
                onSelectMode = backups::selectMode,
                onConfirmRestore = backups::confirmRestore,
                onCancelRestore = backups::cancelRestore,
            )
        },
        updatesCard = {
            UpdatesCard(
                state = update,
                onCheck = updates::check,
                onInstall = updates::downloadAndInstall,
                onOpenReleases = { uriHandler.openUri(UpdateChannel.RELEASES_PAGE) },
            )
        },
    )
}
