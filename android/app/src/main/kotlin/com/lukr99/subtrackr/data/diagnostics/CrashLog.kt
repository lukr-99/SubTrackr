package com.lukr99.subtrackr.data.diagnostics

import java.io.File
import java.time.Clock

/**
 * Appends a [CrashReport] for every uncaught exception to `files/crash.log`, then hands the
 * exception to the handler Android had before, so the process still ends as it would have.
 *
 * The file stays under [maxChars]: when an entry would push it over, the oldest entries go first.
 * A failure to write never becomes a second crash.
 */
class CrashLog(
    private val file: File,
    private val appVersion: String,
    private val next: Thread.UncaughtExceptionHandler?,
    private val clock: Clock = Clock.systemUTC(),
    private val maxChars: Int = MAX_CHARS,
) : Thread.UncaughtExceptionHandler {
    override fun uncaughtException(thread: Thread, error: Throwable) {
        runCatching { append(CrashReport.format(clock.instant(), appVersion, thread.name, error)) }
        next?.uncaughtException(thread, error)
    }

    /** Adds [entry] to the log, dropping the oldest entries if the file would grow past the cap. */
    fun append(entry: String) {
        file.parentFile?.mkdirs()
        val existing = if (file.exists()) file.readText() else ""
        if (existing.length + entry.length <= maxChars) {
            file.appendText(entry)
            return
        }
        file.writeText(newestWithin(existing + entry))
    }

    private fun newestWithin(text: String): String {
        val tail = text.takeLast(maxChars)
        // Start at the first whole entry; a cut-off entry at the front helps nobody.
        val start = tail.indexOf(CrashReport.HEADER)
        return if (start >= 0) tail.substring(start) else tail
    }

    companion object {
        const val FILE_NAME = "crash.log"
        const val MAX_CHARS = 64 * 1024

        /** Starts logging into [folder]/crash.log, keeping whatever handler was installed before. */
        fun install(folder: File, appVersion: String) {
            val existing = Thread.getDefaultUncaughtExceptionHandler()
            if (existing is CrashLog) return
            Thread.setDefaultUncaughtExceptionHandler(CrashLog(File(folder, FILE_NAME), appVersion, existing))
        }
    }
}
