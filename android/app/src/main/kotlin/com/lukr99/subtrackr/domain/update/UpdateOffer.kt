package com.lukr99.subtrackr.domain.update

/** A newer release with the exact installer and its checksum file, both over HTTPS. */
data class UpdateOffer(
    val version: AppVersion,
    val asset: ReleaseAsset,
    val checksumAsset: ReleaseAsset,
    val notes: String,
)
