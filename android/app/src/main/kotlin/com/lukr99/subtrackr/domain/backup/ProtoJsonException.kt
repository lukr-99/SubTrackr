package com.lukr99.subtrackr.domain.backup

/** A value in proto3 JSON has the wrong type, for example text where a number belongs. */
class ProtoJsonException(message: String) : IllegalArgumentException(message)
