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
import java.util.UUID

/** First-run sample data — a realistic sample set with a CZK/USD/EUR mix. */
object SeedData {
    fun createInitialDatabase(): Database {
        val subs = listOf(
            sub("Netflix", "199", "CZK", BillingCycle.MONTHLY, "Entertainment", "🎬", 8.0, true, 4, website = "netflix.com"),
            sub("ChatGPT", "20", "USD", BillingCycle.MONTHLY, "AI & Productivity", "🤖", 40.0, true, 2, website = "openai.com"),
            sub("Claude", "20", "USD", BillingCycle.MONTHLY, "AI & Productivity", "✳️", 60.0, true, 26, website = "claude.ai"),
            sub("Mobile plan", "450", "CZK", BillingCycle.MONTHLY, "Phone & Internet", "📱", 0.0, true, 6, website = ""),
            sub("Meal kit", "149", "CZK", BillingCycle.MONTHLY, "Food", "🍔", 6.0, true, 19, website = ""),
            sub("Ride pass", "119", "CZK", BillingCycle.MONTHLY, "Transport", "🚕", 3.0, true, 26, website = ""),
            sub("Spotify", "10.99", "EUR", BillingCycle.MONTHLY, "Music", "🎧", 90.0, true, 12, website = "spotify.com"),
            sub("City transit pass", "2400", "CZK", BillingCycle.ANNUAL, "Transport", "🚊", 40.0, false, 10, 6, website = ""),
        )
        return Database(
            schemaVersion = "0.1",
            settings = Settings(baseCurrency = "CZK", schemaVersion = "0.1"),
            subscriptions = subs,
        )
    }

    private fun sub(
        name: String, amount: String, currency: String, cycle: BillingCycle,
        category: String, icon: String, usesPerMonth: Double, autoPay: Boolean,
        day: Int, monthsAhead: Long = 0, website: String = "",
    ): Subscription {
        val now = java.time.Instant.now().toString()
        val minor = BigDecimal(amount).movePointRight(2).setScale(0, RoundingMode.HALF_UP).toLong()
        return Subscription(
            id = UUID.randomUUID().toString(),
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
