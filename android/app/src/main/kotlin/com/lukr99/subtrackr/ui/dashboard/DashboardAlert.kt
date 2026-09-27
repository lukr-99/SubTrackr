package com.lukr99.subtrackr.ui.dashboard

import com.lukr99.subtrackr.domain.spend.SubscriptionSpend
import com.lukr99.subtrackr.model.SubStatus
import java.time.LocalDate
import java.time.temporal.ChronoUnit

/** A trial ending or a renewal due soon, shown at the top of the dashboard. */
internal data class DashboardAlert(val icon: String, val title: String, val detail: String) {
    companion object {
        /** Active subscriptions whose trial ends within 7 days, else whose renewal is within 3. */
        fun dueSoon(perSub: List<SubscriptionSpend>, today: LocalDate): List<DashboardAlert> = perSub
            .filter { it.subscription.status == SubStatus.ACTIVE }
            .mapNotNull { p ->
                val s = p.subscription
                val trial = runCatching { LocalDate.parse(s.trialEnd) }.getOrNull()
                val renew = runCatching { LocalDate.parse(s.nextRenewal) }.getOrNull()
                when {
                    trial != null && ChronoUnit.DAYS.between(today, trial) in 0..7 ->
                        DashboardAlert("🆓", "${s.name} trial ends", whenText(ChronoUnit.DAYS.between(today, trial)))
                    renew != null && ChronoUnit.DAYS.between(today, renew) in 0..3 ->
                        DashboardAlert(s.iconRef.ifBlank { "🔔" }, "${s.name} renews", whenText(ChronoUnit.DAYS.between(today, renew)))
                    else -> null
                }
            }

        private fun whenText(days: Long) = when {
            days <= 0L -> "today"
            days == 1L -> "tomorrow"
            else -> "in $days days"
        }
    }
}
