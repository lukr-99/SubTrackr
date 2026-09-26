package com.lukr99.subtrackr.domain.backup

/** Why a backup file was refused (SPEC.md section 9.2). Nothing changes when a backup is refused. */
enum class BackupError {
    /** Not a JSON object, or larger than 10 MB. */
    INVALID_JSON,

    /** `format` is not `subtrackr-backup`. */
    UNSUPPORTED_FORMAT,

    /** `formatVersion` is missing, not an integer, or not 1. */
    UNSUPPORTED_VERSION,

    /** `database` is missing, or a subscription has a bad ID, currency, exponent, or `updatedAt`. */
    INVALID_RECORD,
}
