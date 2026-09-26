package com.lukr99.subtrackr.domain.update

/** What the release source reports for the latest published release. */
data class ReleaseInfo(
    val tagName: String,
    val assets: List<ReleaseAsset>,
    val notes: String = "",
)
