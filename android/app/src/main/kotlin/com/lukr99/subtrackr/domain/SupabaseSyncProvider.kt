package com.lukr99.subtrackr.domain

import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import okhttp3.Headers
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody

/**
 * Sync transport backed by Supabase (PostgREST). Stores the whole subscriptions array as a single
 * jsonb row in table `subtrackr_docs`. Mirrors the WPF SupabaseSyncProvider (same JSON shape).
 * See docs/SYNC-SETUP.md.
 */
class SupabaseSyncProvider(
    projectUrl: String,
    private val anonKey: String,
    private val docId: String = "main",
    private val client: OkHttpClient = OkHttpClient(),
) : SyncProvider {

    private val base = projectUrl.trimEnd('/')
    private val json = Json { ignoreUnknownKeys = true; encodeDefaults = true }

    @Serializable private data class Row(val subscriptions: List<Subscription> = emptyList())
    @Serializable private data class Doc(val id: String, val subscriptions: List<Subscription>)

    private fun headers() = Headers.Builder()
        .add("apikey", anonKey)
        .add("Authorization", "Bearer $anonKey")
        .build()

    override suspend fun pull(): List<Subscription> = withContext(Dispatchers.IO) {
        val req = Request.Builder()
            .url("$base/rest/v1/subtrackr_docs?id=eq.$docId&select=subscriptions")
            .headers(headers()).get().build()
        client.newCall(req).execute().use { resp ->
            if (!resp.isSuccessful) throw RuntimeException("pull HTTP ${resp.code}")
            val body = resp.body?.string() ?: "[]"
            json.decodeFromString<List<Row>>(body).firstOrNull()?.subscriptions ?: emptyList()
        }
    }

    override suspend fun push(subscriptions: List<Subscription>) {
        withContext(Dispatchers.IO) {
            val payload = json.encodeToString(Doc.serializer(), Doc(docId, subscriptions))
            val req = Request.Builder()
                .url("$base/rest/v1/subtrackr_docs")
                .headers(headers().newBuilder().add("Prefer", "resolution=merge-duplicates,return=minimal").build())
                .post(payload.toRequestBody("application/json".toMediaType()))
                .build()
            client.newCall(req).execute().use { resp ->
                if (!resp.isSuccessful) throw RuntimeException("push HTTP ${resp.code}")
            }
        }
    }
}
