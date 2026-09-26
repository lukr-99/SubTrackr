package com.lukr99.subtrackr.application.sync

import java.time.Instant

/**
 * A signed-in Supabase Auth session. It lives only in the encrypted session store, never in
 * data.json, backups, or logs; [toString] leaves the tokens out for that reason.
 */
data class AuthSession(
    val accessToken: String,
    val refreshToken: String,
    val expiresAt: Instant,
    val userId: String,
    val email: String,
) {
    /** True when the access token has expired or expires within [marginSeconds]. */
    fun expiresWithin(now: Instant, marginSeconds: Long): Boolean = !now.plusSeconds(marginSeconds).isBefore(expiresAt)

    override fun toString(): String = "AuthSession(userId=$userId, expiresAt=$expiresAt)"
}
