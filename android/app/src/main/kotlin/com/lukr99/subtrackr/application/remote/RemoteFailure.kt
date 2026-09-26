package com.lukr99.subtrackr.application.remote

private const val UNAUTHORIZED = 401
private const val FORBIDDEN = 403
private const val NOT_FOUND = 404
private const val TOO_MANY_REQUESTS = 429
private const val SERVER_ERROR = 500

/** Why a call to a remote service failed, in terms the retry policy and the UI understand. */
sealed interface RemoteFailure {
    /** A few plain words for a status line, for example "offline" or "server error 503". */
    val shortReason: String

    /** No connection, DNS failure, or a dropped connection. */
    data object Offline : RemoteFailure {
        override val shortReason = "offline"
    }

    data object Timeout : RemoteFailure {
        override val shortReason = "timed out"
    }

    data class Http(val status: Int) : RemoteFailure {
        override val shortReason: String
            get() = when {
                status == UNAUTHORIZED -> "not signed in ($status)"
                status == FORBIDDEN -> "not allowed ($status)"
                status == NOT_FOUND -> "not found ($status)"
                status == TOO_MANY_REQUESTS -> "too many requests ($status)"
                status >= SERVER_ERROR -> "server error $status"
                else -> "rejected ($status)"
            }
    }

    /** The server answered, but not with what the contract promises. */
    data class BadResponse(val detail: String) : RemoteFailure {
        override val shortReason = "unexpected response"
    }
}
