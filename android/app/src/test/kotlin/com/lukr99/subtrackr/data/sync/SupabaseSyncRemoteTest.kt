package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import okhttp3.OkHttpClient
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import okhttp3.mockwebserver.SocketPolicy
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.fail
import org.junit.Test
import java.net.InetAddress
import java.util.concurrent.TimeUnit

class SupabaseSyncRemoteTest {
    private val server = MockWebServer().apply { start(InetAddress.getByName("127.0.0.1"), 0) }
    private val endpoint = SyncEndpoint.parse("http://127.0.0.1:${server.port}", "publishable-key", allowLocalHttp = true)!!
    private val remote = SupabaseSyncRemote(OkHttpClient())
    private val sub = Subscription(id = "11111111-1111-4111-8111-111111111111", name = "Alpha", cost = Money("EUR", 999, 2), updatedAt = "2026-09-01T10:00:00Z")

    @After
    fun tearDown() = server.shutdown()

    @Test
    fun pull_readsRowsWithTheUsersToken() = runTest {
        server.enqueue(MockResponse().setBody("""[{"user_id":"u","id":"${sub.id}","name":"Alpha","cost_currency":"EUR","cost_minor":999,"cost_exponent":2,"updated_at":"2026-09-01T10:00:00Z"}]"""))

        val rows = remote.pull(endpoint, "access-1")

        val request = server.takeRequest()
        assertEquals("GET", request.method)
        assertEquals("/rest/v1/subscriptions?select=*", request.path)
        assertEquals("publishable-key", request.getHeader("apikey"))
        assertEquals("Bearer access-1", request.getHeader("Authorization"))
        assertEquals(listOf(sub), rows)
    }

    @Test
    fun push_upsertsOnUserAndIdWithTheUserId() = runTest {
        server.enqueue(MockResponse().setResponseCode(201))

        remote.push(endpoint, "access-1", "user-1", listOf(sub))

        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/rest/v1/subscriptions?on_conflict=user_id,id", request.path)
        assertEquals("resolution=merge-duplicates,return=minimal", request.getHeader("Prefer"))
        assertEquals("Bearer access-1", request.getHeader("Authorization"))
        val row = Json.parseToJsonElement(request.body.readUtf8()).jsonArray.single().jsonObject
        assertEquals("user-1", row.getValue("user_id").jsonPrimitive.content)
        assertEquals(sub.id, row.getValue("id").jsonPrimitive.content)
    }

    @Test
    fun push_nothing_sendsNoRequest() = runTest {
        remote.push(endpoint, "access-1", "user-1", emptyList())

        assertEquals(0, server.requestCount)
    }

    @Test
    fun serverError_isReportedWithItsStatus() = runTest {
        server.enqueue(MockResponse().setResponseCode(503))

        try {
            remote.pull(endpoint, "access-1")
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertEquals(RemoteFailure.Http(503), error.failure)
        }
    }

    @Test
    fun slowServer_timesOut() = runTest {
        server.enqueue(MockResponse().setBody("[]").setHeadersDelay(2, TimeUnit.SECONDS))
        val impatient = SupabaseSyncRemote(OkHttpClient.Builder().callTimeout(200, TimeUnit.MILLISECONDS).build())

        try {
            impatient.pull(endpoint, "access-1")
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertEquals(RemoteFailure.Timeout, error.failure)
        }
    }

    @Test
    fun droppedConnection_isOffline() = runTest {
        server.enqueue(MockResponse().setSocketPolicy(SocketPolicy.DISCONNECT_AT_START))

        try {
            remote.pull(endpoint, "access-1")
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertEquals(RemoteFailure.Offline, error.failure)
        }
    }
}
