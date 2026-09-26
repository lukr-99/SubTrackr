package com.lukr99.subtrackr.domain.update

/**
 * A SubTrackr version, `X.Y.Z` with an optional pre-release suffix such as `-dev` (SPEC.md section 7).
 * Ordering compares major, minor, and patch as numbers and ignores the suffix.
 */
data class AppVersion(
    val major: Int,
    val minor: Int,
    val patch: Int,
    val preRelease: String = "",
) : Comparable<AppVersion> {

    val isPreRelease: Boolean get() = preRelease.isNotEmpty()

    /** `X.Y.Z` without any suffix. */
    val core: String get() = "$major.$minor.$patch"

    override fun compareTo(other: AppVersion): Int =
        compareValuesBy(this, other, AppVersion::major, AppVersion::minor, AppVersion::patch)

    override fun toString(): String = if (isPreRelease) "$core-$preRelease" else core

    companion object {
        private val PATTERN = Regex("""^(0|[1-9]\d{0,8})\.(0|[1-9]\d{0,8})\.(0|[1-9]\d{0,8})(?:-([0-9A-Za-z.-]+))?$""")

        /** Parses `X.Y.Z` or `X.Y.Z-suffix`; anything else, including a leading `v`, is null. */
        fun parse(text: String): AppVersion? {
            val match = PATTERN.matchEntire(text) ?: return null
            val (major, minor, patch, suffix) = match.destructured
            return AppVersion(major.toInt(), minor.toInt(), patch.toInt(), suffix)
        }
    }
}
