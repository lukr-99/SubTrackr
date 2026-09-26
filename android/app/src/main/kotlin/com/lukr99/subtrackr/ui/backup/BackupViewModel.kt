package com.lukr99.subtrackr.ui.backup

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.backup.ExportReport
import com.lukr99.subtrackr.application.backup.RestoreReport
import com.lukr99.subtrackr.domain.backup.RestoreMode
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/** Export to a user-picked file, and restore with Merge (default) or a confirmed Replace. */
class BackupViewModel(private val service: BackupService) : ViewModel() {

    private val state = MutableStateFlow(BackupUiState())
    val uiState: StateFlow<BackupUiState> = state.asStateFlow()

    fun suggestedFileName(): String = service.suggestedFileName()

    /** [uri] is the document the user created, or null when they cancelled the picker. */
    fun export(uri: String?) {
        if (uri == null || state.value.busy) return
        state.update { it.copy(busy = true) }
        viewModelScope.launch {
            val report = service.exportTo(uri)
            state.update {
                it.copy(busy = false, message = BackupMessages.export(report), messageIsError = report is ExportReport.Failed)
            }
        }
    }

    /** [uri] is the backup the user opened, or null when they cancelled the picker. */
    fun chooseFile(uri: String?) {
        if (uri == null || state.value.busy) return
        state.update { it.copy(pendingUri = uri, mode = RestoreMode.MERGE, confirmingReplace = false) }
    }

    fun selectMode(mode: RestoreMode) = state.update { it.copy(mode = mode) }

    fun cancelRestore() = state.update { it.copy(pendingUri = null, confirmingReplace = false) }

    /** Merge runs at once; Replace first asks for confirmation, then runs on the second call. */
    fun confirmRestore() {
        val current = state.value
        val uri = current.pendingUri ?: return
        if (current.mode == RestoreMode.REPLACE && !current.confirmingReplace) {
            state.update { it.copy(confirmingReplace = true) }
            return
        }
        state.update { it.copy(busy = true, pendingUri = null, confirmingReplace = false) }
        viewModelScope.launch {
            val report = service.restoreFrom(uri, current.mode)
            state.update {
                it.copy(busy = false, message = BackupMessages.restore(report), messageIsError = report !is RestoreReport.Restored)
            }
        }
    }
}
