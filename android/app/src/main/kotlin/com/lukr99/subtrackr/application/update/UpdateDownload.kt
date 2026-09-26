package com.lukr99.subtrackr.application.update

import java.io.File

/** Outcome of downloading and verifying an installer. */
sealed interface UpdateDownload {
    /** The file's SHA-256 matched the release's checksum file. */
    data class Verified(val file: File) : UpdateDownload

    data class Failed(val reason: String) : UpdateDownload
}
