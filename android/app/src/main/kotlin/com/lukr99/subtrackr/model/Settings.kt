package com.lukr99.subtrackr.model

import kotlinx.serialization.Serializable

/** Kotlin mirror of the proto `Settings` message. Settings stay on the device and never sync. */
@Serializable
data class Settings(
    val baseCurrency: String = "EUR",
    val schemaVersion: String = "0.1",
    /** Supabase project URL. Device-local, never pushed and never written to a backup. */
    val syncUrl: String = "",
    /** Supabase publishable key. It identifies the project, not the user. */
    val syncKey: String = "",
    /** Cost-per-use cutoff in the base currency; 0 selects the currency-aware default. */
    val worthThreshold: Double = 0.0,
    /** Monthly spend budget in the base currency; 0 means no budget. */
    val monthlyBudget: Double = 0.0,
    /** Light, dark, or follow the system. Stored per device and included in backups. */
    val themeMode: ThemeMode = ThemeMode.SYSTEM,
)
