package com.lukr99.subtrackr.data.http

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import kotlinx.coroutines.suspendCancellableCoroutine
import okhttp3.Call
import okhttp3.Callback
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import java.io.IOException
import java.io.InterruptedIOException
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * Sends [request] without blocking a thread. Cancelling the coroutine cancels the HTTP call. I/O
 * failures become [RemoteCallException]; the caller must close the returned response.
 */
suspend fun OkHttpClient.send(request: Request): Response {
    val call = newCall(request)
    return try {
        call.await()
    } catch (error: RemoteCallException) {
        throw error
    } catch (error: IOException) {
        throw RemoteCallException(error.toRemoteFailure())
    }
}

/** Sends [request] and throws [RemoteCallException] unless the status is 2xx. */
suspend fun OkHttpClient.sendExpectingSuccess(request: Request): Response {
    val response = send(request)
    if (!response.isSuccessful) {
        response.close()
        throw RemoteCallException(RemoteFailure.Http(response.code))
    }
    return response
}

/** Timeouts (including OkHttp's whole-call timeout) versus every other transport failure. */
fun IOException.toRemoteFailure(): RemoteFailure =
    if (this is InterruptedIOException) RemoteFailure.Timeout else RemoteFailure.Offline

private suspend fun Call.await(): Response = suspendCancellableCoroutine { continuation ->
    continuation.invokeOnCancellation { cancel() }
    enqueue(
        object : Callback {
            override fun onResponse(call: Call, response: Response) {
                if (continuation.isActive) continuation.resume(response) else response.close()
            }

            override fun onFailure(call: Call, e: IOException) {
                if (continuation.isActive) continuation.resumeWithException(e)
            }
        },
    )
}
