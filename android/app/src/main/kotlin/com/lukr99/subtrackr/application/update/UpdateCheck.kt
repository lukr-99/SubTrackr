package com.lukr99.subtrackr.application.update

import com.lukr99.subtrackr.domain.update.UpdateOffer

/** Outcome of asking for the latest release. */
sealed interface UpdateCheck {
    /** A pre-release build; nothing was contacted. */
    data object Disabled : UpdateCheck

    data object UpToDate : UpdateCheck

    data class Available(val offer: UpdateOffer) : UpdateCheck

    data class Failed(val reason: String) : UpdateCheck
}
