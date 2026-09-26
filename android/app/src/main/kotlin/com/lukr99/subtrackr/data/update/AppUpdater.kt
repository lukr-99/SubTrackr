package com.lukr99.subtrackr.data.update

import android.content.Context
import android.content.Intent
import androidx.core.content.FileProvider
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.net.HttpURLConnection
import java.net.URL

/** GitHub-Releases updater shared in spirit with Workout Tracker's proven implementation. */
class AppUpdater(
    private val context: Context,
    private val owner: String = UPDATE_OWNER,
    private val repo: String = UPDATE_REPO,
    private val fileProviderAuthority: String = "${context.packageName}.files",
) {
    val currentVersion: String by lazy {
        runCatching {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "0"
        }.getOrDefault("0")
    }

    suspend fun check(): AppRelease? = withContext(Dispatchers.IO) {
        val release = parseRelease(JSONObject(httpGet(
            "https://api.github.com/repos/$owner/$repo/releases/latest",
        ))) ?: return@withContext null
        if (isNewer(release.versionName, currentVersion)) release else null
    }

    suspend fun download(release: AppRelease): File = withContext(Dispatchers.IO) {
        val dir = File(context.cacheDir, "updates").apply { mkdirs() }
        dir.listFiles()?.forEach { it.delete() }
        val out = File(dir, "$repo-${release.tag}.apk")
        val connection = (URL(release.apkUrl).openConnection() as HttpURLConnection).apply {
            connectTimeout = 15_000
            readTimeout = 60_000
            instanceFollowRedirects = true
            setRequestProperty("Accept", "application/octet-stream")
            setRequestProperty("User-Agent", "SubTrackr-Android-updater")
        }
        try {
            if (connection.responseCode !in 200..299) {
                error("APK download HTTP ${connection.responseCode}")
            }
            connection.inputStream.use { input -> out.outputStream().use(input::copyTo) }
        } finally {
            connection.disconnect()
        }
        out
    }

    fun install(apk: File) {
        val uri = FileProvider.getUriForFile(context, fileProviderAuthority, apk)
        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(uri, "application/vnd.android.package-archive")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        context.startActivity(intent)
    }

    private fun parseRelease(obj: JSONObject): AppRelease? {
        val tag = obj.optString("tag_name").ifBlank { return null }
        val assets = obj.optJSONArray("assets") ?: JSONArray()
        val apkUrl = (0 until assets.length())
            .map(assets::getJSONObject)
            .firstOrNull { it.optString("name").endsWith(".apk", ignoreCase = true) }
            ?.optString("browser_download_url")
            .orEmpty()
        if (apkUrl.isBlank()) return null
        return AppRelease(
            versionName = tag.removePrefix("v"),
            tag = tag,
            apkUrl = apkUrl,
            notes = obj.optString("body"),
        )
    }

    private fun httpGet(url: String): String {
        val connection = (URL(url).openConnection() as HttpURLConnection).apply {
            connectTimeout = 15_000
            readTimeout = 15_000
            instanceFollowRedirects = true
            setRequestProperty("Accept", "application/vnd.github+json")
            setRequestProperty("User-Agent", "SubTrackr-Android-updater")
        }
        try {
            if (connection.responseCode !in 200..299) {
                error("Update check HTTP ${connection.responseCode}")
            }
            return connection.inputStream.bufferedReader().use { it.readText() }
        } finally {
            connection.disconnect()
        }
    }

    companion object {
        const val UPDATE_OWNER = "lukr-99"
        const val UPDATE_REPO = "SubTrackr-Releases"

        fun isNewer(candidate: String, current: String): Boolean {
            val candidateParts = parse(candidate)
            val currentParts = parse(current)
            for (i in 0 until maxOf(candidateParts.size, currentParts.size)) {
                val a = candidateParts.getOrElse(i) { 0 }
                val b = currentParts.getOrElse(i) { 0 }
                if (a != b) return a > b
            }
            return false
        }

        private fun parse(version: String): List<Int> =
            version.trim().removePrefix("v").split('.', '-')
                .mapNotNull { part -> part.takeWhile(Char::isDigit).toIntOrNull() }
    }
}
