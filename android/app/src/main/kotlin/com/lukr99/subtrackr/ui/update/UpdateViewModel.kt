package com.lukr99.subtrackr.ui.update

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lukr99.subtrackr.application.update.UpdateCheck
import com.lukr99.subtrackr.application.update.UpdateDownload
import com.lukr99.subtrackr.application.update.UpdateService
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/** Drives the Updates card and the update prompt. Release builds check once on launch. */
class UpdateViewModel(private val service: UpdateService) : ViewModel() {

    private val state = MutableStateFlow(
        UpdateUiState(currentVersion = service.currentVersion, checksEnabled = service.checksEnabled),
    )
    val uiState: StateFlow<UpdateUiState> = state.asStateFlow()

    init {
        if (service.checksEnabled) check()
    }

    fun check() {
        if (state.value.busy || !service.checksEnabled) return
        state.update { it.copy(busy = true, status = UpdateStatus.Checking) }
        viewModelScope.launch {
            val result = service.check()
            state.update { current ->
                val idle = current.copy(busy = false)
                when (result) {
                    UpdateCheck.Disabled -> idle.copy(status = UpdateStatus.Disabled)
                    UpdateCheck.UpToDate -> idle.copy(status = UpdateStatus.UpToDate, offer = null)
                    is UpdateCheck.Available -> idle.copy(
                        status = UpdateStatus.Available(result.offer.version.core),
                        offer = result.offer,
                        promptVisible = true,
                    )
                    is UpdateCheck.Failed -> idle.copy(status = UpdateStatus.CheckFailed(result.reason))
                }
            }
        }
    }

    fun dismissPrompt() = state.update { it.copy(promptVisible = false) }

    /** Downloads, verifies, and opens the installer. Only called after the user agreed. */
    fun downloadAndInstall() {
        val offer = state.value.offer ?: return
        if (state.value.busy) return
        val version = offer.version.core
        state.update { it.copy(busy = true, promptVisible = false, status = UpdateStatus.Downloading(version)) }
        viewModelScope.launch {
            val status = when (val result = service.download(offer)) {
                is UpdateDownload.Failed -> UpdateStatus.DownloadFailed(result.reason)
                is UpdateDownload.Verified -> try {
                    service.install(result.file)
                    UpdateStatus.OpeningInstaller
                } catch (cancelled: CancellationException) {
                    throw cancelled
                } catch (_: Exception) {
                    UpdateStatus.DownloadFailed("couldn't open the installer")
                }
            }
            state.update { it.copy(busy = false, status = status) }
        }
    }
}
