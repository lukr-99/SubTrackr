package com.lukr99.subtrackr.application.sync

/** What the Auth API returns from verify and refresh (SPEC.md section 8.2). */
data class AuthTokens(
    val accessToken: String,
    val refreshToken: String,
    val expiresInSeconds: Long,
    val userId: String,
    val email: String,
) {
    override fun toString(): String = "AuthTokens(userId=$userId, expiresInSeconds=$expiresInSeconds)"
}
