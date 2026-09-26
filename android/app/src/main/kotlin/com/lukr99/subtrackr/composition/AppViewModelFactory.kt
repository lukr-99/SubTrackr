package com.lukr99.subtrackr.composition

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.ui.SubTrackrViewModel
import com.lukr99.subtrackr.ui.backup.BackupViewModel
import com.lukr99.subtrackr.ui.update.UpdateViewModel

/** Builds every view model with constructor injection from the composition root's objects. */
class AppViewModelFactory(
    private val repository: AppRepository,
    private val updates: UpdateService,
    private val backups: BackupService,
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        val viewModel: ViewModel = when (modelClass) {
            SubTrackrViewModel::class.java -> SubTrackrViewModel(repository)
            UpdateViewModel::class.java -> UpdateViewModel(updates)
            BackupViewModel::class.java -> BackupViewModel(backups)
            else -> throw IllegalArgumentException("Unknown view model: ${modelClass.name}")
        }
        return modelClass.cast(viewModel)!!
    }
}
