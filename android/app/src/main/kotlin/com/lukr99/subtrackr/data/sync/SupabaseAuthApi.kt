package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.application.sync.AuthApi
import com.lukr99.subtrackr.application.sync.AuthTokens
import com.lukr99.subtrackr.data.http.sendExpectingSuccess
import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.put
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody

/**
 * Supabase Auth's REST API for email codes (SPEC.md section 8.2) over OkHttp. The client sets the
 * 15-second timeouts; cancelling the coroutine cancels the call. Tokens are never logged.
 */
class SupabaseAuthApi(private val client: OkHttpClient) : AuthApi {

    override suspend fun sendCode(endpoint: SyncEndpoint, email: String) {
        post(
            endpoint,
            "/auth/v1/otp",
            buildJsonObject {
                put("email", email)
                put("create_user", true)
            },
        ).close()
    }

    override suspend fun verify(endpoint: SyncEndpoint, email: String, code: String): AuthTokens =
        tokens(
            post(
                endpoint,
                "/auth/v1/verify",
                buildJsonObject {
                    put("type", "email")
                    put("email", email)
                    put("token", code)
                },
            ),
        )

    override suspend fun refresh(endpoint: SyncEndpoint, refreshToken: String): AuthTokens =
        tokens(
            post(
                endpoint,
                "/auth/v1/token?grant_type=refresh_token",
                buildJsonObject {
                    put("refresh_token", refreshToken)
                },
            ),
        )

    override suspend fun logout(endpoint: SyncEndpoint, accessToken: String) {
        val request = Request.Builder()
            .url(endpoint.url + "/auth/v1/logout")
            .header("apikey", endpoint.publishableKey)
            .header("Authorization", "Bearer $accessToken")
            .post(ByteArray(0).toRequestBody(null))
            .build()
        client.sendExpectingSuccess(request).close()
    }

    private suspend fun post(endpoint: SyncEndpoint, path: String, body: JsonObject) =
        client.sendExpectingSuccess(
            Request.Builder()
                .url(endpoint.url + path)
                .header("apikey", endpoint.publishableKey)
                .post(body.toString().toRequestBody(JSON))
                .build(),
        )

    private suspend fun tokens(response: okhttp3.Response): AuthTokens {
        val text = response.use { withContext(Dispatchers.IO) { it.body?.string().orEmpty() } }
        return try {
            val root = Json.parseToJsonElement(text).jsonObject
            val user = root["user"] as? JsonObject
            AuthTokens(
                accessToken = root.required("access_token"),
                refreshToken = root.required("refresh_token"),
                expiresInSeconds = root.required("expires_in").toLong(),
                userId = user?.optional("id").orEmpty(),
                email = user?.optional("email").orEmpty(),
            )
        } catch (_: SerializationException) {
            throw RemoteCallException(RemoteFailure.BadResponse("auth response is not JSON"))
        } catch (_: IllegalArgumentException) {
            throw RemoteCallException(RemoteFailure.BadResponse("auth response lacks a token"))
        }
    }

    private fun JsonObject.optional(key: String): String? = (this[key] as? JsonPrimitive)?.content

    private fun JsonObject.required(key: String): String =
        requireNotNull(optional(key)?.takeIf { it.isNotEmpty() }) { "missing $key" }

    private companion object {
        val JSON = "application/json".toMediaType()
    }
}
