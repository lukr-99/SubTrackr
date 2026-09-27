package com.lukr99.subtrackr.data.diagnostics

import java.time.Instant

/**
 * Turns an uncaught exception into the text of one crash-log entry: when, which app version and
 * thread, then each exception in the cause chain as its class and stack frames.
 *
 * Exception messages are left out on purpose. They can carry a subscription name, an e-mail
 * address, a sync URL or a token, and nothing personal may reach the log.
 */
object CrashReport {
    /** First line of every entry, so a log can be cut at an entry boundary. */
    const val HEADER = "=== SubTrackr crash"
    private const val MAX_CAUSES = 10
    private const val MAX_FRAMES = 40

    fun format(at: Instant, appVersion: String, threadName: String, error: Throwable): String = buildString {
        append(HEADER).append(' ').append(at).append(" v").append(appVersion)
            .append(" thread ").append(threadName).append('\n')
        val seen = mutableSetOf<Throwable>()
        var current: Throwable? = error
        var depth = 0
        while (current != null && depth < MAX_CAUSES && seen.add(current)) {
            if (depth > 0) append("Caused by: ")
            append(current.javaClass.name).append('\n')
            val frames = current.stackTrace
            frames.take(MAX_FRAMES).forEach { append("    at ").append(it).append('\n') }
            if (frames.size > MAX_FRAMES) append("    ... ").append(frames.size - MAX_FRAMES).append(" more\n")
            current = current.cause
            depth++
        }
        if (current != null) append("Caused by: ... (chain cut)\n")
    }
}
