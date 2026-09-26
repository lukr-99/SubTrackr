package com.lukr99.subtrackr.domain.backup

/** How a backup meets the data already on the device (SPEC.md section 9.3). */
enum class RestoreMode {
    /** Last-writer-wins per subscription; settings stay as they are. The default. */
    MERGE,

    /** The backup's subscriptions and settings, keeping this device's sync URL and key. */
    REPLACE,
}
