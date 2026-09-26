package com.lukr99.subtrackr.application.update

import java.io.File

/** Hands a verified installer to the system, which asks the user and checks the signing key. */
fun interface PackageInstaller {
    fun install(file: File)
}
