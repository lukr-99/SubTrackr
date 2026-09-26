package com.lukr99.subtrackr.composition

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.ui.SubTrackrViewModel

/** Builds every view model with constructor injection from the composition root's objects. */
class AppViewModelFactory(
    private val repository: AppRepository,
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        val viewModel: ViewModel = when (modelClass) {
            SubTrackrViewModel::class.java -> SubTrackrViewModel(repository)
            else -> throw IllegalArgumentException("Unknown view model: ${modelClass.name}")
        }
        return modelClass.cast(viewModel)!!
    }
}
