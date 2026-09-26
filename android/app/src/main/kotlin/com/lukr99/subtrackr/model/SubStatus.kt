package com.lukr99.subtrackr.model

/** Mirror of the proto `SubStatus` enum. PAUSED stays listed but leaves active spend. */
enum class SubStatus {
    SUB_STATUS_UNSPECIFIED,
    ACTIVE,
    PAUSED,
}
