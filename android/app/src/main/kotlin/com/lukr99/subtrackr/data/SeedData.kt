package com.lukr99.subtrackr.data

import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import java.math.BigDecimal
import java.math.RoundingMode
import java.time.LocalDate
import java.time.ZoneOffset

/** First-run sample data — a realistic sample set with a CZK/USD/EUR mix. */
object SeedData {
    /**
     * Re-keys legacy first-run rows that used random per-device IDs. The old IDs are retained as
     * tombstones so a later sync cannot resurrect them. Unrelated subscriptions are untouched.
     */
    fun migrateLegacyIds(
        database: Database,
        timestamp: String = java.time.Instant.now().toString(),
    ): Database {
        val rows = database.subscriptions.toMutableList()
        var changed = false

        for (template in createInitialDatabase().subscriptions) {
            val legacy = rows.filter {
                it.id != template.id && it.deletedAt.isBlank() && matchesSeed(it, template)
            }
            if (legacy.isEmpty()) continue

            val stableLive = rows.firstOrNull { it.id == template.id && it.deletedAt.isBlank() }
            val source = (legacy + listOfNotNull(stableLive)).maxBy { it.updatedAt }

            for (old in legacy) {
                val index = rows.indexOfFirst { it.id == old.id }
                rows[index] = old.copy(deletedAt = timestamp, updatedAt = timestamp)
            }

            val canonical = source.copy(id = template.id, deletedAt = "", updatedAt = timestamp)
            val stableIndex = rows.indexOfFirst { it.id == template.id }
            if (stableIndex >= 0) rows[stableIndex] = canonical else rows.add(canonical)
            changed = true
        }

        return if (changed) database.copy(subscriptions = rows) else database
    }

    private fun matchesSeed(candidate: Subscription, template: Subscription): Boolean =
        candidate.name == template.name &&
            candidate.cost == template.cost &&
            candidate.billingCycle == template.billingCycle &&
            candidate.website == template.website

    fun createInitialDatabase(): Database {
        val subs = listOf(
            sub("eb90294c-78d8-40d1-83a3-0596708b1797", "Netflix", "199", "CZK", BillingCycle.MONTHLY, "Entertainment", "🎬", 8.0, true, 4, website = "netflix.com"),
            sub("b62ad93b-16c8-4825-add9-6aab0e8dcfd2", "ChatGPT", "20", "USD", BillingCycle.MONTHLY, "AI & Productivity", "🤖", 40.0, true, 2, website = "openai.com"),
            sub("8ff64ab3-b632-4c2f-9c32-1ea448c1e792", "Claude", "20", "USD", BillingCycle.MONTHLY, "AI & Productivity", "✳️", 60.0, true, 26, website = "claude.ai"),
            sub("f62d5d85-9c59-4342-b33c-59d3e17cc535", "Mobile plan", "450", "CZK", BillingCycle.MONTHLY, "Phone & Internet", "📱", 0.0, true, 6, website = ""),
            sub("48a9c9b1-daaa-40c5-9eec-5fa18476511a", "Meal kit", "149", "CZK", BillingCycle.MONTHLY, "Food", "🍔", 6.0, true, 19, website = ""),
            sub("510b0f45-bd90-491b-aff9-621d2332c429", "Ride pass", "119", "CZK", BillingCycle.MONTHLY, "Transport", "🚕", 3.0, true, 26, website = ""),
            sub("e84fa49a-7e51-4904-9241-7e0f7ec34d0f", "Spotify", "10.99", "EUR", BillingCycle.MONTHLY, "Music", "🎧", 90.0, true, 12, website = "spotify.com"),
            sub("0280b4d6-4b7c-4b86-afec-044c1be90b49", "City transit pass", "2400", "CZK", BillingCycle.ANNUAL, "Transport", "🚊", 40.0, false, 10, 6, website = ""),
        )
        return Database(
            schemaVersion = "0.1",
            settings = Settings(baseCurrency = "CZK", schemaVersion = "0.1"),
            subscriptions = subs,
        )
    }

    private fun sub(
        id: String, name: String, amount: String, currency: String, cycle: BillingCycle,
        category: String, icon: String, usesPerMonth: Double, autoPay: Boolean,
        day: Int, monthsAhead: Long = 0, website: String = "",
    ): Subscription {
        val now = java.time.Instant.now().toString()
        val minor = BigDecimal(amount).movePointRight(2).setScale(0, RoundingMode.HALF_UP).toLong()
        return Subscription(
            id = id,
            name = name,
            cost = Money(currency, minor, 2),
            billingCycle = cycle,
            nextRenewal = nextRenewalOnDay(day, monthsAhead).toString(),
            category = category,
            iconRef = icon,
            autoPay = autoPay,
            status = SubStatus.ACTIVE,
            usesPerMonth = usesPerMonth,
            website = website,
            createdAt = now,
            updatedAt = now,
        )
    }

    private fun nextRenewalOnDay(day: Int, monthsAhead: Long): LocalDate {
        val today = LocalDate.now(ZoneOffset.UTC)
        var base = today.withDayOfMonth(1).plusMonths(monthsAhead)
        val d = minOf(day, base.lengthOfMonth())
        var candidate = base.withDayOfMonth(d)
        if (monthsAhead == 0L && !candidate.isAfter(today)) candidate = candidate.plusMonths(1)
        return candidate
    }
}
