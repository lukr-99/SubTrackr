package com.lukr99.subtrackr.domain.update

/**
 * Update policy from SPEC.md section 11, verified by contracts/vectors/release-selection.json:
 * a strictly newer `vX.Y.Z` tag with exactly the platform's installer and its `.sha256` file, both
 * served over HTTPS. A running pre-release build such as `0.3.0-dev` never checks.
 */
object ReleaseSelector {
    private val TAG = Regex("""^v(\d+\.\d+\.\d+)$""")
    private const val HTTPS = "https://"

    /** False for pre-release and unparseable running versions; those must not contact GitHub. */
    fun checksAllowed(currentVersion: String): Boolean =
        AppVersion.parse(currentVersion)?.isPreRelease == false

    fun select(release: ReleaseInfo, currentVersion: String, platform: UpdatePlatform): UpdateOffer? {
        if (!checksAllowed(currentVersion)) return null
        val current = AppVersion.parse(currentVersion) ?: return null
        val tagVersion = TAG.matchEntire(release.tagName)?.groupValues?.get(1) ?: return null
        val candidate = AppVersion.parse(tagVersion) ?: return null
        if (candidate <= current) return null

        val assetName = platform.assetName(candidate.core)
        val asset = release.assets.firstOrNull { it.name == assetName } ?: return null
        val checksum = release.assets.firstOrNull { it.name == "$assetName.sha256" } ?: return null
        if (!asset.downloadUrl.startsWith(HTTPS) || !checksum.downloadUrl.startsWith(HTTPS)) return null
        return UpdateOffer(candidate, asset, checksum, release.notes)
    }
}
