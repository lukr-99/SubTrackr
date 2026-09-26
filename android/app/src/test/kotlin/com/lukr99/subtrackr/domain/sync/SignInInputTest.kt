package com.lukr99.subtrackr.domain.sync

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test

class SignInInputTest {
    @Test
    fun signInCode_acceptsSixToTenDigits() {
        assertEquals("123456", SignInCode.parse("123 456")?.value)
        assertNotNull(SignInCode.parse("1234567890"))
        assertNull(SignInCode.parse("12345"))
        assertNull(SignInCode.parse("12345678901"))
        assertNull(SignInCode.parse("12345a"))
    }

    @Test
    fun email_needsAnAtAndADomain() {
        assertEquals("user@example.com", EmailAddress.parse("  user@example.com ")?.value)
        assertNull(EmailAddress.parse("user@example"))
        assertNull(EmailAddress.parse("user example.com"))
        assertNull(EmailAddress.parse(""))
    }

    @Test
    fun endpoint_isHttpsOnly_exceptLocalHostsInDebugBuilds() {
        assertEquals("https://project.example", SyncEndpoint.parse("https://project.example/", " key ")?.url)
        assertNull(SyncEndpoint.parse("http://project.example", "key"))
        assertNull(SyncEndpoint.parse("http://10.0.2.2:54621", "key"))
        assertNotNull(SyncEndpoint.parse("http://10.0.2.2:54621", "key", allowLocalHttp = true))
        assertNull(SyncEndpoint.parse("http://project.example", "key", allowLocalHttp = true))
        assertNull(SyncEndpoint.parse("https://project.example", ""))
        assertNull(SyncEndpoint.parse("project.example", "key"))
        assertNull(SyncEndpoint.parse("https://project.example?x=1", "key"))
    }
}
