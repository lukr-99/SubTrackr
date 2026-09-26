package com.lukr99.subtrackr.data.update

import com.lukr99.subtrackr.application.remote.RemoteCallException
import kotlinx.coroutines.test.runTest
import okhttp3.OkHttpClient
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import okio.Buffer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File
import java.io.IOException

class OkHttpArtifactDownloaderTest {
    @get:Rule
    val temp = TemporaryFolder()

    private val server = MockWebServer().apply { start() }
    private val downloader = OkHttpArtifactDownloader(OkHttpClient())

    @After
    fun tearDown() = server.shutdown()

    @Test
    fun download_writesTheBody() = runTest {
        val bytes = ByteArray(200_000) { (it % 251).toByte() }
        server.enqueue(MockResponse().setBody(Buffer().write(bytes)))
        val target = File(temp.root, "nested/app.apk")

        downloader.download(server.url("/app.apk").toString(), target)

        assertTrue(target.readBytes().contentEquals(bytes))
    }

    @Test
    fun download_httpError_throws() = runTest {
        server.enqueue(MockResponse().setResponseCode(500))

        try {
            downloader.download(server.url("/app.apk").toString(), File(temp.root, "app.apk"))
            throw AssertionError("expected a failure")
        } catch (_: RemoteCallException) {
        }
    }

    @Test
    fun readText_returnsSmallText_andRefusesLargeBodies() = runTest {
        server.enqueue(MockResponse().setBody("abc  file\n"))
        server.enqueue(MockResponse().setBody("x".repeat(10_000)))

        assertEquals("abc  file\n", downloader.readText(server.url("/a.sha256").toString()))
        try {
            downloader.readText(server.url("/b.sha256").toString())
            throw AssertionError("expected a failure")
        } catch (_: IOException) {
        }
    }
}
