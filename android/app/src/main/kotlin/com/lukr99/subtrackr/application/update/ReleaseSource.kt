package com.lukr99.subtrackr.application.update

import com.lukr99.subtrackr.domain.update.ReleaseInfo

/** Discovers the latest published release. The GitHub adapter implements it; tests use a fake. */
fun interface ReleaseSource {
    /** Throws on network or HTTP failure. */
    suspend fun latest(): ReleaseInfo
}
