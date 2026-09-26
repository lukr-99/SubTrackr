package com.lukr99.subtrackr.domain.update

/**
 * Reads a `<name>.sha256` release asset: a hex SHA-256, optionally followed by whitespace and the
 * file name (the `sha256sum` format). Verified by the checksumFiles cases in release-selection.json.
 */
object ChecksumFile {
    private val SHA256 = Regex("^[0-9a-fA-F]{64}$")

    /** The lowercase hex digest, or null when the text does not start with a SHA-256. */
    fun parse(text: String): String? {
        val first = text.trim().split(Regex("\\s+"), limit = 2).first()
        return if (SHA256.matches(first)) first.lowercase() else null
    }
}
