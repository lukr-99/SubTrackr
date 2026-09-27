package com.lukr99.subtrackr.ui.dashboard

import com.lukr99.subtrackr.domain.spend.SubscriptionSpend
import com.lukr99.subtrackr.domain.worth.WorthVerdict
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import org.junit.Assert.assertEquals
import org.junit.Test
import java.math.BigDecimal
import java.time.LocalDate

class DashboardAlertTest {
    private val today = LocalDate.of(2026, 9, 26)

    private fun spend(subscription: Subscription) = SubscriptionSpend(
        subscription = subscription,
        monthlyOwn = BigDecimal.ONE,
        monthlyBase = BigDecimal.ONE,
        yearlyBase = BigDecimal.TEN,
        costPerUse = BigDecimal.ZERO,
        verdict = WorthVerdict.UNKNOWN,
    )

    @Test
    fun trialWithinAWeek_winsOverARenewal() {
        val alerts = DashboardAlert.dueSoon(
            listOf(spend(Subscription(name = "Music", trialEnd = "2026-10-03", nextRenewal = "2026-09-27"))),
            today,
        )
        assertEquals(listOf(DashboardAlert("🆓", "Music trial ends", "in 7 days")), alerts)
    }

    @Test
    fun renewalWithinThreeDays_usesTheIconOrABell() {
        val alerts = DashboardAlert.dueSoon(
            listOf(
                spend(Subscription(name = "Video", iconRef = "🎬", nextRenewal = "2026-09-26")),
                spend(Subscription(name = "Cloud", nextRenewal = "2026-09-27")),
                spend(Subscription(name = "Later", nextRenewal = "2026-09-30")),
            ),
            today,
        )
        assertEquals(
            listOf(
                DashboardAlert("🎬", "Video renews", "today"),
                DashboardAlert("🔔", "Cloud renews", "tomorrow"),
            ),
            alerts,
        )
    }

    @Test
    fun pausedOrPastOrUnparseable_giveNoAlert() {
        val alerts = DashboardAlert.dueSoon(
            listOf(
                spend(Subscription(name = "Paused", status = SubStatus.PAUSED, nextRenewal = "2026-09-26")),
                spend(Subscription(name = "Past", trialEnd = "2026-09-25")),
                spend(Subscription(name = "Bad", nextRenewal = "soon")),
            ),
            today,
        )
        assertEquals(emptyList<DashboardAlert>(), alerts)
    }
}
