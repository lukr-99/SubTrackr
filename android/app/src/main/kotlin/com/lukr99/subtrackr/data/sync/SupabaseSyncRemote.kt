package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.application.sync.SyncRemote
import com.lukr99.subtrackr.data.http.sendExpectingSuccess
import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody

/**
 * The `subscriptions` table through PostgREST (SPEC.md section 8.3). Every request carries the
 * publishable key and the user's access token; row-level security scopes it to that user.
 */
class SupabaseSyncRemote(private val client: OkHttpClient) : SyncRemote {

    override suspend fun pull(endpoint: SyncEndpoint, accessToken: String): List<Subscription> {
        val request = authorized(endpoint, accessToken)
            .url(endpoint.url + "/rest/v1/subscriptions?select=*")
            .get()
            .build()
        val text = client.sendExpectingSuccess(request).use { withContext(Dispatchers.IO) { it.body?.string().orEmpty() } }
        return try {
            (Json.parseToJsonElement(text) as JsonArray).map { SubscriptionRowMapper.fromRow(it as JsonObject) }
        } catch (_: SerializationException) {
            throw RemoteCallException(RemoteFailure.BadResponse("rows are not JSON"))
        } catch (_: ClassCastException) {
            throw RemoteCallException(RemoteFailure.BadResponse("rows are not a list of objects"))
        } catch (error: IllegalArgumentException) {
            throw RemoteCallException(RemoteFailure.BadResponse(error.message.orEmpty()))
        }
    }

    override suspend fun push(endpoint: SyncEndpoint, accessToken: String, userId: String, subscriptions: List<Subscription>) {
        if (subscriptions.isEmpty()) return
        val body = JsonArray(subscriptions.map { SubscriptionRowMapper.toRow(it, userId) }).toString()
        val request = authorized(endpoint, accessToken)
            .url(endpoint.url + "/rest/v1/subscriptions?on_conflict=user_id,id")
            .header("Prefer", "resolution=merge-duplicates,return=minimal")
            .post(body.toRequestBody(JSON))
            .build()
        client.sendExpectingSuccess(request).close()
    }

    private fun authorized(endpoint: SyncEndpoint, accessToken: String) = Request.Builder()
        .header("apikey", endpoint.publishableKey)
        .header("Authorization", "Bearer $accessToken")

    private companion object {
        val JSON = "application/json".toMediaType()
    }
}
