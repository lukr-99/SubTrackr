package com.lukr99.subtrackr.data.update

/** A GitHub release that carries an APK. */
data class AppRelease(
    val versionName: String,
    val tag: String,
    val apkUrl: String,
    val notes: String,
)
