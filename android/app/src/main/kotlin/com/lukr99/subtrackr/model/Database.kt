package com.lukr99.subtrackr.model

import kotlinx.serialization.Serializable

/** Root document: exactly what data.json holds. */
@Serializable
data class Database(
    val schemaVersion: String = "0.1",
    val settings: Settings = Settings(),
    val subscriptions: List<Subscription> = emptyList(),
)
