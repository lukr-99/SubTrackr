package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import java.time.LocalDate
import java.time.format.TextStyle
import java.time.temporal.ChronoUnit
import java.util.Locale

/** The next five renewals of active subscriptions; hidden when none has a renewal date. */
@Composable
internal fun RenewalsCard(summary: SpendSummary, today: LocalDate) {
    val upcoming = summary.perSub
        .filter { it.subscription.status == SubStatus.ACTIVE }
        .mapNotNull { p -> runCatching { LocalDate.parse(p.subscription.nextRenewal) }.getOrNull()?.let { p to it } }
        .sortedBy { it.second }
        .take(5)
    if (upcoming.isEmpty()) return
    SectionCard {
        Text("UPCOMING RENEWALS", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        upcoming.forEach { (p, date) ->
            val days = ChronoUnit.DAYS.between(today, date)
            val whenText = when {
                days <= 0L -> "due"
                days == 1L -> "tomorrow"
                else -> "in $days days"
            } + " · " + date.dayOfMonth + " " + date.month.getDisplayName(TextStyle.SHORT, Locale.US)
            Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
                Text(p.subscription.iconRef.ifBlank { "•" }, fontSize = 16.sp)
                Spacer(Modifier.width(10.dp))
                Column(Modifier.weight(1f)) {
                    Text(p.subscription.name, color = SubTrackrTheme.colors.textPrimary, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
                    Text(whenText, color = SubTrackrTheme.colors.textSecondary, fontSize = 11.sp)
                }
                Text(
                    Format.money(p.subscription.cost.toBigDecimal(), p.subscription.cost.currency),
                    color = SubTrackrTheme.colors.textPrimary,
                    fontSize = 13.sp,
                )
            }
        }
    }
}
