package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import okhttp3.OkHttpClient
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import java.net.InetAddress

class SupabaseAuthApiTest {
    private val server = MockWebServer().apply { start(InetAddress.getByName("127.0.0.1"), 0) }
    private val endpoint = SyncEndpoint.parse("http://127.0.0.1:${server.port}", "publishable-key", allowLocalHttp = true)!!
    private val api = SupabaseAuthApi(OkHttpClient())

    private val tokenJson = """
        {"access_token":"access-1","token_type":"bearer","expires_in":3600,"refresh_token":"refresh-1",
         "user":{"id":"9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b","email":"user@example.com"}}
    """.trimIndent()

    @After
    fun tearDown() = server.shutdown()

    private fun body(text: String) = Json.parseToJsonElement(text).jsonObject

    @Test
    fun sendCode_postsTheEmailAndCreatesUsers() = runTest {
        server.enqueue(MockResponse().setBody("{}"))

        api.sendCode(endpoint, "user@example.com")

        val request = server.takeRequest()
        assertEquals("POST", request.method)
        assertEquals("/auth/v1/otp", request.path)
        assertEquals("publishable-key", request.getHeader("apikey"))
        assertTrue(request.getHeader("Content-Type")!!.startsWith("application/json"))
        val json = body(request.body.readUtf8())
        assertEquals("user@example.com", json.getValue("email").jsonPrimitive.content)
        assertEquals("true", json.getValue("create_user").jsonPrimitive.content)
    }

    @Test
    fun verify_sendsTheCodeAndReadsTheSession() = runTest {
        server.enqueue(MockResponse().setBody(tokenJson))

        val tokens = api.verify(endpoint, "user@example.com", "123456")

        val request = server.takeRequest()
        assertEquals("/auth/v1/verify", request.path)
        val json = body(request.body.readUtf8())
        assertEquals("email", json.getValue("type").jsonPrimitive.content)
        assertEquals("123456", json.getValue("token").jsonPrimitive.content)
        assertEquals("access-1", tokens.accessToken)
        assertEquals("refresh-1", tokens.refreshToken)
        assertEquals(3600L, tokens.expiresInSeconds)
        assertEquals("9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b", tokens.userId)
    }

    @Test
    fun refresh_usesTheRefreshTokenGrant() = runTest {
        server.enqueue(MockResponse().setBody(tokenJson))

        api.refresh(endpoint, "refresh-0")

        val request = server.takeRequest()
        assertEquals("/auth/v1/token?grant_type=refresh_token", request.path)
        assertEquals("refresh-0", body(request.body.readUtf8()).getValue("refresh_token").jsonPrimitive.content)
    }

    @Test
    fun logout_sendsTheBearerToken() = runTest {
        server.enqueue(MockResponse().setResponseCode(204))

        api.logout(endpoint, "access-1")

        val request = server.takeRequest()
        assertEquals("/auth/v1/logout", request.path)
        assertEquals("Bearer access-1", request.getHeader("Authorization"))
    }

    @Test
    fun verify_rejectedCode_reportsTheStatus() = runTest {
        server.enqueue(MockResponse().setResponseCode(403).setBody("""{"error_code":"otp_expired"}"""))

        try {
            api.verify(endpoint, "user@example.com", "000000")
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertEquals(RemoteFailure.Http(403), error.failure)
        }
    }

    @Test
    fun verify_responseWithoutTokens_isBadResponse() = runTest {
        server.enqueue(MockResponse().setBody("""{"user":{}}"""))

        try {
            api.verify(endpoint, "user@example.com", "123456")
            fail("expected a failure")
        } catch (error: RemoteCallException) {
            assertTrue(error.failure is RemoteFailure.BadResponse)
        }
    }
}
