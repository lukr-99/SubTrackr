package com.lukr99.subtrackr.application.update

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.domain.update.ChecksumFile
import com.lukr99.subtrackr.domain.update.ReleaseSelector
import com.lukr99.subtrackr.domain.update.UpdateOffer
import com.lukr99.subtrackr.domain.update.UpdatePlatform
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import java.io.IOException
import java.security.MessageDigest

/**
 * The updater seam from SPEC.md section 11, kept apart from the UI: release discovery, version
 * policy and asset choice (ReleaseSelector), a verified download into [directory], and the
 * hand-off to the system installer. Pre-release builds never contact the release source.
 */
class UpdateService(
    val currentVersion: String,
    private val platform: UpdatePlatform,
    private val releases: ReleaseSource,
    private val downloader: ArtifactDownloader,
    private val installer: PackageInstaller,
    private val directory: File,
    private val io: CoroutineDispatcher = Dispatchers.IO,
) {
    val checksEnabled: Boolean = ReleaseSelector.checksAllowed(currentVersion)

    suspend fun check(): UpdateCheck {
        if (!checksEnabled) return UpdateCheck.Disabled
        val release = try {
            releases.latest()
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (error: Exception) {
            return UpdateCheck.Failed(reasonFor(error))
        }
        val offer = ReleaseSelector.select(release, currentVersion, platform) ?: return UpdateCheck.UpToDate
        return UpdateCheck.Available(offer)
    }

    /** Downloads the installer, verifies its SHA-256, and deletes it again on any mismatch. */
    suspend fun download(offer: UpdateOffer): UpdateDownload {
        if (!checksEnabled) return UpdateDownload.Failed("updates are off in development builds")
        val expected = try {
            ChecksumFile.parse(downloader.readText(offer.checksumAsset.downloadUrl))
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (error: Exception) {
            return UpdateDownload.Failed(reasonFor(error))
        } ?: return UpdateDownload.Failed("the checksum file is not a SHA-256")

        val partial = File(directory, offer.asset.name + ".part")
        return try {
            withContext(io) { prepareDirectory() }
            downloader.download(offer.asset.downloadUrl, partial)
            val actual = withContext(io) { sha256(partial) }
            if (actual != expected) {
                partial.delete()
                return UpdateDownload.Failed("the download did not match its checksum and was deleted")
            }
            val target = File(directory, offer.asset.name)
            withContext(io) {
                target.delete()
                if (!partial.renameTo(target)) throw IOException("could not store the download")
            }
            UpdateDownload.Verified(target)
        } catch (cancelled: CancellationException) {
            partial.delete()
            throw cancelled
        } catch (error: Exception) {
            partial.delete()
            UpdateDownload.Failed(reasonFor(error))
        }
    }

    fun install(file: File) = installer.install(file)

    /** Keeps only one download at a time in app-private storage. */
    private fun prepareDirectory() {
        directory.mkdirs()
        directory.listFiles()?.forEach { it.delete() }
    }

    private fun reasonFor(error: Exception): String = when (error) {
        is RemoteCallException -> error.failure.shortReason
        is IOException -> "storage error"
        else -> "unexpected error"
    }

    private fun sha256(file: File): String {
        val digest = MessageDigest.getInstance("SHA-256")
        file.inputStream().use { input ->
            val buffer = ByteArray(BUFFER_SIZE)
            while (true) {
                val read = input.read(buffer)
                if (read < 0) break
                digest.update(buffer, 0, read)
            }
        }
        return digest.digest().joinToString("") { "%02x".format(it) }
    }

    private companion object {
        const val BUFFER_SIZE = 64 * 1024
    }
}
