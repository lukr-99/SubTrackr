package com.lukr99.subtrackr.data.update

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.application.update.ReleaseSource
import com.lukr99.subtrackr.data.http.sendExpectingSuccess
import com.lukr99.subtrackr.domain.update.ReleaseAsset
import com.lukr99.subtrackr.domain.update.ReleaseInfo
import com.lukr99.subtrackr.domain.update.UpdateChannel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.jsonObject
import okhttp3.OkHttpClient
import okhttp3.Request

/**
 * Reads `GET /repos/{owner}/{repo}/releases/latest` from the GitHub REST API. GitHub never returns
 * drafts or pre-releases from that endpoint.
 */
class GitHubReleaseSource(
    private val client: OkHttpClient,
    private val apiBaseUrl: String = "https://api.github.com",
    private val repository: String = UpdateChannel.REPOSITORY,
) : ReleaseSource {

    override suspend fun latest(): ReleaseInfo {
        val request = Request.Builder()
            .url("${apiBaseUrl.trimEnd('/')}/repos/$repository/releases/latest")
            .header("Accept", "application/vnd.github+json")
            .header("User-Agent", "SubTrackr-Android-updater")
            .build()
        val body = client.sendExpectingSuccess(request).use { response ->
            withContext(Dispatchers.IO) { response.body?.string().orEmpty() }
        }
        return try {
            parse(Json.parseToJsonElement(body).jsonObject)
        } catch (error: SerializationException) {
            throw RemoteCallException(RemoteFailure.BadResponse("release JSON: ${error.message}"))
        } catch (error: IllegalArgumentException) {
            throw RemoteCallException(RemoteFailure.BadResponse("release JSON: ${error.message}"))
        }
    }

    private fun parse(release: JsonObject): ReleaseInfo {
        val assets = (release["assets"] as? JsonArray).orEmpty().mapNotNull { element ->
            val asset = element as? JsonObject ?: return@mapNotNull null
            val name = asset.text("name") ?: return@mapNotNull null
            val url = asset.text("browser_download_url") ?: return@mapNotNull null
            ReleaseAsset(name, url)
        }
        return ReleaseInfo(
            tagName = release.text("tag_name").orEmpty(),
            assets = assets,
            notes = release.text("body").orEmpty(),
        )
    }

    private fun JsonObject.text(key: String): String? =
        (this[key] as? JsonPrimitive)?.takeIf { it.isString }?.content
}
