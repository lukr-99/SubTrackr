package com.lukr99.subtrackr.domain.update

/** Which app asks for an update, and the exact release asset it installs (SPEC.md section 11). */
enum class UpdatePlatform(private val assetPrefix: String, private val assetExtension: String) {
    DESKTOP("SubTrackr-Setup-", ".exe"),
    ANDROID("SubTrackr-", ".apk"),
    ;

    /** The installer name for `X.Y.Z`, for example `SubTrackr-0.3.1.apk`. */
    fun assetName(version: String): String = "$assetPrefix$version$assetExtension"
}
