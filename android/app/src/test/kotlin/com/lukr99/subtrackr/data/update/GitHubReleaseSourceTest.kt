package com.lukr99.subtrackr.data.update

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.domain.update.ReleaseAsset
import kotlinx.coroutines.test.runTest
import okhttp3.OkHttpClient
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.fail
import org.junit.Test

class GitHubReleaseSourceTest {
    private val server = MockWebServer().apply { start() }
    private val source = GitHubReleaseSource(OkHttpClient(), server.url("/").toString(), "owner/repo")

    @After
    fun tearDown() = server.shutdown()

    @Test
    fun latest_readsTagNotesAndAssets() = runTest {
        server.enqueue(
            MockResponse().setBody(
                """
                {"tag_name":"v0.3.1","body":"Notes","draft":false,
                 "assets":[{"name":"SubTrackr-0.3.1.apk","browser_download_url":"https://example.invalid/a.apk","size":1},
                           {"name":"broken"}]}
                """.trimIndent(),
            ),
        )

        val release = source.latest()

        assertEquals("v0.3.1", release.tagName)
        assertEquals("Notes", release.notes)
        assertEquals(listOf(ReleaseAsset("SubTrackr-0.3.1.apk", "https://example.invalid/a.apk")), release.assets)
        val request = server.takeRequest()
        assertEquals("/repos/owner/repo/releases/latest", request.path)
        assertEquals("application/vnd.github+json", request.getHeader("Accept"))
    }

    @Test
    fun latest_httpError_throwsWithStatus() = runTest {
        server.enqueue(MockResponse().setResponseCode(404))

        try {
            source.latest()
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertEquals(RemoteFailure.Http(404), error.failure)
        }
    }

    @Test
    fun latest_notJson_isBadResponse() = runTest {
        server.enqueue(MockResponse().setBody("<html>"))

        try {
            source.latest()
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertEquals(RemoteFailure.BadResponse::class, error.failure::class)
        }
    }
}
