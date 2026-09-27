package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.spend.SubscriptionSpend
import com.lukr99.subtrackr.domain.worth.WorthVerdict
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** One subscription in the dashboard list: its icon, name and worth verdict, and costs. */
@Composable
internal fun SubscriptionRow(spend: SubscriptionSpend, base: String, showLogos: Boolean, onEdit: (Subscription) -> Unit) {
    val s = spend.subscription
    Row(
        Modifier.fillMaxWidth().clickable { onEdit(s) }.padding(vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        ServiceIcon(s, showLogos)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(s.name, color = SubTrackrTheme.colors.textPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                if (spend.verdict != WorthVerdict.UNKNOWN) {
                    Spacer(Modifier.width(8.dp))
                    VerdictBadge(spend.verdict)
                }
            }
            Text(s.category.ifBlank { "Uncategorized" }, color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        }
        Column(horizontalAlignment = Alignment.End) {
            Text(
                Format.money(spend.monthlyBase, base),
                color = SubTrackrTheme.colors.textPrimary,
                fontSize = 15.sp,
                fontWeight = FontWeight.SemiBold,
            )
            Text(
                Format.money(s.cost.toBigDecimal(), s.cost.currency) + " / " + cycleShort(s.billingCycle),
                color = SubTrackrTheme.colors.textMuted,
                fontSize = 11.sp,
            )
        }
    }
}

@Composable
private fun VerdictBadge(verdict: WorthVerdict) {
    val (label, color) = when (verdict) {
        WorthVerdict.WORTH -> "Worth" to SubTrackrTheme.colors.positive
        WorthVerdict.NOT_WORTH -> "Not worth" to SubTrackrTheme.colors.negative
        WorthVerdict.ESSENTIAL -> "Essential" to SubTrackrTheme.colors.accent
        else -> "" to SubTrackrTheme.colors.textMuted
    }
    Box(Modifier.background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(6.dp)).padding(horizontal = 6.dp, vertical = 1.dp)) {
        Text(label, color = color, fontSize = 10.sp, fontWeight = FontWeight.SemiBold)
    }
}

private fun cycleShort(c: BillingCycle) = when (c) {
    BillingCycle.WEEKLY -> "wk"
    BillingCycle.MONTHLY -> "mo"
    BillingCycle.QUARTERLY -> "qtr"
    BillingCycle.SEMIANNUAL -> "6mo"
    BillingCycle.ANNUAL -> "yr"
    BillingCycle.CUSTOM_DAYS -> "cyc"
    BillingCycle.BILLING_CYCLE_UNSPECIFIED -> ""
}
