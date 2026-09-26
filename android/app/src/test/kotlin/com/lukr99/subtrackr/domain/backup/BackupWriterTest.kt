package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.model.WorthMode
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.Instant
import java.time.LocalDateTime

class BackupWriterTest {
    private val database = Database(
        schemaVersion = "0.1",
        settings = Settings(
            baseCurrency = "CZK",
            syncUrl = "https://project.example",
            syncKey = "publishable-key",
            worthThreshold = 2.5,
            monthlyBudget = 1200.0,
            themeMode = ThemeMode.DARK,
        ),
        subscriptions = listOf(
            Subscription(
                id = "0280b4d6-4b7c-4b86-afec-044c1be90b49",
                name = "City transit pass",
                cost = Money("CZK", 9_007_199_254_740_993, 2),
                billingCycle = BillingCycle.CUSTOM_DAYS,
                customDays = 45,
                nextRenewal = "2026-11-10",
                category = "Transport",
                iconRef = "🚊",
                status = SubStatus.PAUSED,
                usesPerMonth = 12.5,
                worthMode = WorthMode.NOT_WORTH,
                trialEnd = "2026-10-01",
                createdAt = "2026-08-28T10:00:00Z",
                updatedAt = "2026-09-02T09:00:00Z",
                deletedAt = "2026-09-02T09:00:00Z",
            ),
            Subscription(
                id = "11111111-1111-4111-8111-111111111111",
                name = "Alpha",
                cost = Money("EUR", 0, 0),
                updatedAt = "2026-09-01T10:00:00Z",
            ),
        ),
    )

    private val text = BackupWriter.write(database, Instant.parse("2026-09-26T10:00:00.123Z"), "0.3.0")

    @Test
    fun write_thenRead_roundTripsEveryFieldExceptSyncConfig() {
        val read = BackupReader.read(text) as BackupReadResult.Valid

        val expected = database.copy(settings = database.settings.copy(syncUrl = "", syncKey = ""))
        assertEquals(expected, read.backup.database)
        assertEquals("2026-09-26T10:00:00Z", read.backup.exportedAt)
        assertEquals("0.3.0", read.backup.appVersion)
        assertEquals("android", read.backup.platform)
    }

    @Test
    fun write_emitsDefaultsAndInt64AsString() {
        val root = Json.parseToJsonElement(text).jsonObject
        assertEquals("subtrackr-backup", root.getValue("format").jsonPrimitive.content)
        assertEquals(JsonPrimitive(1), root.getValue("formatVersion"))
        val subs = root.getValue("database").jsonObject.getValue("subscriptions").jsonArray
        val alpha = subs[1].jsonObject

        val minor = subs[0].jsonObject.getValue("cost").jsonObject.getValue("minorUnits").jsonPrimitive
        assertTrue("minorUnits is a JSON string", minor.isString)
        assertEquals("9007199254740993", minor.content)
        val fields = setOf(
            "id", "name", "cost", "billingCycle", "customDays", "nextRenewal", "category", "iconRef", "autoPay",
            "status", "usesPerMonth", "notes", "worthMode", "trialEnd", "website", "createdAt", "updatedAt", "deletedAt",
        )
        assertEquals(fields, alpha.keys)
        assertEquals("", alpha.getValue("deletedAt").jsonPrimitive.content)
        assertEquals(JsonPrimitive(0), alpha.getValue("customDays"))
        assertEquals("MONTHLY", alpha.getValue("billingCycle").jsonPrimitive.content)
        val settings = root.getValue("database").jsonObject.getValue("settings").jsonObject
        assertEquals("", settings.getValue("syncUrl").jsonPrimitive.content)
        assertEquals("DARK", settings.getValue("themeMode").jsonPrimitive.content)
    }

    @Test
    fun fileName_isLocalTimestamped() {
        assertEquals("SubTrackr-backup-20260926-093005.json", BackupFileName.at(LocalDateTime.of(2026, 9, 26, 9, 30, 5)))
    }
}
