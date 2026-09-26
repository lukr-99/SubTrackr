package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.application.remote.RemoteFailure

/**
 * Bounded retry for one sync pass (SPEC.md section 8.4): at most [maxAttempts], waiting
 * [delaysMillis] between them, and only for failures that may pass on their own.
 */
class RetryPolicy(
    val maxAttempts: Int = 3,
    private val delaysMillis: List<Long> = listOf(1_000, 2_000),
) {
    /** Network errors, timeouts, HTTP 429, and HTTP 5xx. Other 4xx responses fail at once. */
    fun isRetryable(failure: RemoteFailure): Boolean = when (failure) {
        RemoteFailure.Offline, RemoteFailure.Timeout -> true
        is RemoteFailure.Http -> failure.status == TOO_MANY_REQUESTS || failure.status >= SERVER_ERROR
        is RemoteFailure.BadResponse -> false
    }

    /** The wait after failed attempt number [attempt] (1-based), or null when no attempt is left. */
    fun delayAfter(attempt: Int): Long? =
        if (attempt >= maxAttempts) null else delaysMillis.getOrElse(attempt - 1) { delaysMillis.last() }

    private companion object {
        const val TOO_MANY_REQUESTS = 429
        const val SERVER_ERROR = 500
    }
}
