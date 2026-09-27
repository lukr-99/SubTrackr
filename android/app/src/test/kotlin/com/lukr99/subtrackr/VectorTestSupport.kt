package com.lukr99.subtrackr

import kotlinx.serialization.json.Json

/** Shared JSON reader for the contract golden vectors (put on the test classpath via build.gradle.kts). */
val vectorJson = Json {
    ignoreUnknownKeys = true
    isLenient = true
}

fun readVector(name: String): String =
    object {}.javaClass.getResourceAsStream("/$name")?.bufferedReader()?.use { it.readText() }
        ?: error("Vector resource not found on test classpath: $name")
