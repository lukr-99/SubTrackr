package com.lukr99.subtrackr.application.remote

import java.io.IOException

/** Thrown by network adapters so callers can decide on retries without knowing the HTTP stack. */
class RemoteCallException(val failure: RemoteFailure) : IOException(failure.shortReason)
