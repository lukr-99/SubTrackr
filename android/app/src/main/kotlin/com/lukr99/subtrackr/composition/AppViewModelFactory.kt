package com.lukr99.subtrackr.composition

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.sync.SyncCoordinator
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.ui.SubTrackrViewModel
import com.lukr99.subtrackr.ui.backup.BackupViewModel
import com.lukr99.subtrackr.ui.sync.SyncViewModel
import com.lukr99.subtrackr.ui.update.UpdateViewModel
import java.time.ZoneId

/** Builds every view model with constructor injection from the composition root's objects. */
class AppViewModelFactory(
    private val repository: AppRepository,
    private val updates: UpdateService,
    private val backups: BackupService,
    private val sync: SyncCoordinator,
    private val zone: ZoneId,
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        val viewModel: ViewModel = when (modelClass) {
            SubTrackrViewModel::class.java -> SubTrackrViewModel(repository)
            UpdateViewModel::class.java -> UpdateViewModel(updates)
            BackupViewModel::class.java -> BackupViewModel(backups)
            SyncViewModel::class.java -> SyncViewModel(sync, repository.syncUrl, repository.syncKey, zone)
            else -> throw IllegalArgumentException("Unknown view model: ${modelClass.name}")
        }
        return modelClass.cast(viewModel)!!
    }
}
