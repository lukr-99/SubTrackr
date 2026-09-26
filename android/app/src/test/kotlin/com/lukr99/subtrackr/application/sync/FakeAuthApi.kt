package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.domain.sync.SyncEndpoint

/** Records calls; each step's behaviour is a replaceable lambda that may throw RemoteCallException. */
class FakeAuthApi : AuthApi {
    val calls = mutableListOf<String>()
    var onSendCode: (String) -> Unit = {}
    var onVerify: (String, String) -> AuthTokens = { _, _ -> tokens(1) }
    var onRefresh: (String) -> AuthTokens = { tokens(2) }
    var onLogout: (String) -> Unit = {}

    override suspend fun sendCode(endpoint: SyncEndpoint, email: String) {
        calls += "otp $email"
        onSendCode(email)
    }

    override suspend fun verify(endpoint: SyncEndpoint, email: String, code: String): AuthTokens {
        calls += "verify $email $code"
        return onVerify(email, code)
    }

    override suspend fun refresh(endpoint: SyncEndpoint, refreshToken: String): AuthTokens {
        calls += "refresh $refreshToken"
        return onRefresh(refreshToken)
    }

    override suspend fun logout(endpoint: SyncEndpoint, accessToken: String) {
        calls += "logout $accessToken"
        onLogout(accessToken)
    }

    companion object {
        const val USER_ID = "9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b"

        fun tokens(n: Int, expiresIn: Long = 3600) =
            AuthTokens("access-$n", "refresh-$n", expiresIn, USER_ID, "user@example.com")
    }
}
