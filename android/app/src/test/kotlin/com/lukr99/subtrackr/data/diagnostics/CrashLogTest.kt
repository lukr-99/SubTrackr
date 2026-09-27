package com.lukr99.subtrackr.data.diagnostics

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertSame
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File
import java.io.IOException
import java.time.Clock
import java.time.Instant
import java.time.ZoneOffset

/** The crash log writes what went wrong, keeps nothing personal, stays small, and passes the crash on. */
class CrashLogTest {
    @get:Rule
    val temp = TemporaryFolder()

    private val clock = Clock.fixed(Instant.parse("2026-09-27T10:00:00Z"), ZoneOffset.UTC)
    private val thread = Thread("worker-1")

    private fun log(file: File, next: Thread.UncaughtExceptionHandler? = null, maxChars: Int = CrashLog.MAX_CHARS) =
        CrashLog(file, "0.4.1", next, clock, maxChars)

    @Test
    fun crash_isWrittenWithVersionAndCauseChain_thenPassedOn() {
        val file = File(temp.root, "files/crash.log")
        var passed: Throwable? = null
        val boom = IllegalStateException("outer", IOException("inner"))

        log(file, next = { _, error -> passed = error }).uncaughtException(thread, boom)

        val text = file.readText()
        assertTrue(text, text.startsWith("${CrashReport.HEADER} 2026-09-27T10:00:00Z v0.4.1 thread worker-1\n"))
        assertTrue(text, text.contains("java.lang.IllegalStateException\n    at "))
        assertTrue(text, text.contains("Caused by: java.io.IOException\n"))
        assertSame(boom, passed)
    }

    @Test
    fun messages_neverReachTheLog() {
        val file = File(temp.root, "crash.log")
        val secret = "user@example.com sb_publishable_key refresh-token-123"

        log(file).uncaughtException(thread, RuntimeException(secret, IllegalArgumentException(secret)))

        val text = file.readText()
        assertFalse(text, text.contains("user@example.com"))
        assertFalse(text, text.contains("token-123"))
        assertFalse(text, text.contains("sb_publishable"))
    }

    @Test
    fun laterCrashes_areAppended() {
        val file = File(temp.root, "crash.log")
        val crashLog = log(file)

        crashLog.uncaughtException(thread, IllegalStateException())
        crashLog.uncaughtException(thread, IllegalArgumentException())

        val text = file.readText()
        assertEquals(2, Regex(Regex.escape(CrashReport.HEADER)).findAll(text).count())
        assertTrue(text.indexOf("IllegalStateException") < text.indexOf("IllegalArgumentException"))
    }

    @Test
    fun fullLog_dropsTheOldestEntriesAndStartsAtAWholeEntry() {
        val file = File(temp.root, "crash.log")
        val crashLog = log(file, maxChars = 6_000)
        repeat(20) { crashLog.append("${CrashReport.HEADER} old $it\n" + "x".repeat(500) + "\n") }

        crashLog.uncaughtException(thread, UnsupportedOperationException())

        val text = file.readText()
        assertTrue("${text.length}", text.length <= 6_000)
        assertTrue(text, text.startsWith(CrashReport.HEADER))
        assertFalse(text, text.contains("old 0\n"))
        assertTrue(text, text.contains("UnsupportedOperationException"))
    }

    @Test
    fun causeCycle_endsTheChain() {
        val a = IllegalStateException()
        val b = IOException(a)
        a.initCause(b)

        val text = CrashReport.format(clock.instant(), "0.4.1", "main", a)

        assertEquals(1, Regex("Caused by: java.io.IOException").findAll(text).count())
    }

    @Test
    fun unwritableLog_isNotASecondCrash_andStillPassesOn() {
        val folderInTheWay = temp.newFolder("crash.log")
        var passed = false

        log(folderInTheWay, next = { _, _ -> passed = true }).uncaughtException(thread, IllegalStateException())

        assertTrue(passed)
    }

    @Test
    fun install_replacesThePreviousHandlerOnlyOnce() {
        val original = Thread.getDefaultUncaughtExceptionHandler()
        val previous = Thread.UncaughtExceptionHandler { _, _ -> }
        try {
            Thread.setDefaultUncaughtExceptionHandler(previous)
            CrashLog.install(temp.root, "0.4.1")
            val installed = Thread.getDefaultUncaughtExceptionHandler()
            CrashLog.install(temp.root, "0.4.1")

            assertTrue(installed is CrashLog)
            assertSame(installed, Thread.getDefaultUncaughtExceptionHandler())
        } finally {
            Thread.setDefaultUncaughtExceptionHandler(original)
        }
    }
}
