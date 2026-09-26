package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.domain.sync.SyncEndpoint

/**
 * Supabase Auth's email-code flow (SPEC.md section 8.2). The OkHttp adapter implements it; tests
 * use a fake. Every call throws RemoteCallException on failure.
 */
interface AuthApi {
    suspend fun sendCode(endpoint: SyncEndpoint, email: String)

    suspend fun verify(endpoint: SyncEndpoint, email: String, code: String): AuthTokens

    suspend fun refresh(endpoint: SyncEndpoint, refreshToken: String): AuthTokens

    suspend fun logout(endpoint: SyncEndpoint, accessToken: String)
}
