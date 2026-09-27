package com.lukr99.subtrackr.application.update

import com.lukr99.subtrackr.domain.update.ReleaseAsset
import com.lukr99.subtrackr.domain.update.ReleaseInfo
import com.lukr99.subtrackr.domain.update.UpdatePlatform
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File
import java.security.MessageDigest

class UpdateServiceTest {
    @get:Rule
    val temp = TemporaryFolder()

    private val apkUrl = "https://example.invalid/SubTrackr-0.3.1.apk"
    private val shaUrl = "https://example.invalid/SubTrackr-0.3.1.apk.sha256"
    private val apkBytes = "not really an apk".toByteArray()
    private val release = ReleaseInfo(
        tagName = "v0.3.1",
        assets = listOf(ReleaseAsset("SubTrackr-0.3.1.apk", apkUrl), ReleaseAsset("SubTrackr-0.3.1.apk.sha256", shaUrl)),
        notes = "Fixes",
    )
    private val installed = mutableListOf<File>()

    private fun service(current: String, downloader: ArtifactDownloader, source: ReleaseSource = ReleaseSource { release }) =
        UpdateService(
            currentVersion = current,
            platform = UpdatePlatform.ANDROID,
            releases = source,
            downloader = downloader,
            installer = { installed += it },
            directory = File(temp.root, "updates"),
            io = Dispatchers.Unconfined,
        )

    private fun sha256(bytes: ByteArray): String =
        MessageDigest.getInstance("SHA-256").digest(bytes).joinToString("") { "%02x".format(it) }

    @Test
    fun check_devBuild_isDisabledAndNeverContactsTheSource() = runTest {
        var contacted = false
        val service = service(
            "0.3.0-dev",
            FakeArtifactDownloader(emptyMap()),
            ReleaseSource {
                contacted = true
                release
            },
        )

        assertEquals(UpdateCheck.Disabled, service.check())
        assertFalse(contacted)
        assertFalse(service.checksEnabled)
    }

    @Test
    fun check_newerRelease_offersTheExactApk() = runTest {
        val result = service("0.3.0", FakeArtifactDownloader(emptyMap())).check()

        val offer = (result as UpdateCheck.Available).offer
        assertEquals("0.3.1", offer.version.core)
        assertEquals(apkUrl, offer.asset.downloadUrl)
    }

    @Test
    fun check_sourceFailure_reportsFailed() = runTest {
        val result = service("0.3.0", FakeArtifactDownloader(emptyMap()), ReleaseSource { throw java.io.IOException("x") }).check()

        assertTrue(result is UpdateCheck.Failed)
    }

    @Test
    fun download_matchingChecksum_isVerified() = runTest {
        val downloader = FakeArtifactDownloader(
            mapOf(apkUrl to apkBytes, shaUrl to "${sha256(apkBytes).uppercase()}  SubTrackr-0.3.1.apk\n".toByteArray()),
        )
        val service = service("0.3.0", downloader)
        val offer = (service.check() as UpdateCheck.Available).offer

        val result = service.download(offer)

        val file = (result as UpdateDownload.Verified).file
        assertEquals("SubTrackr-0.3.1.apk", file.name)
        assertTrue(file.readBytes().contentEquals(apkBytes))
        service.install(file)
        assertEquals(listOf(file), installed)
    }

    @Test
    fun download_checksumMismatch_deletesTheFileAndFails() = runTest {
        val downloader = FakeArtifactDownloader(mapOf(apkUrl to apkBytes, shaUrl to sha256("other".toByteArray()).toByteArray()))
        val service = service("0.3.0", downloader)
        val offer = (service.check() as UpdateCheck.Available).offer

        val result = service.download(offer)

        assertTrue(result is UpdateDownload.Failed)
        assertEquals(emptyList<String>(), File(temp.root, "updates").list()!!.toList())
        assertEquals(emptyList<File>(), installed)
    }

    @Test
    fun download_invalidChecksumFile_failsBeforeDownloading() = runTest {
        val downloader = FakeArtifactDownloader(mapOf(apkUrl to apkBytes, shaUrl to "not a hash".toByteArray()))
        val service = service("0.3.0", downloader)
        val offer = (service.check() as UpdateCheck.Available).offer

        val result = service.download(offer)

        assertTrue(result is UpdateDownload.Failed)
        assertEquals(listOf(shaUrl), downloader.requested)
    }

    @Test
    fun download_transferFailure_leavesNoPartialFile() = runTest {
        val downloader = FakeArtifactDownloader(mapOf(apkUrl to null, shaUrl to sha256(apkBytes).toByteArray()))
        val service = service("0.3.0", downloader)
        val offer = (service.check() as UpdateCheck.Available).offer

        val result = service.download(offer)

        assertTrue(result is UpdateDownload.Failed)
        assertFalse(File(temp.root, "updates/SubTrackr-0.3.1.apk.part").exists())
    }
}
