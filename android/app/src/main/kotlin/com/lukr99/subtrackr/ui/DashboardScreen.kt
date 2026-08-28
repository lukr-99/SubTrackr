package com.lukr99.subtrackr.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.ExchangeRateTable
import com.lukr99.subtrackr.domain.SpendSummary
import com.lukr99.subtrackr.domain.SubscriptionSpend
import com.lukr99.subtrackr.domain.WorthVerdict
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import java.math.BigDecimal
import java.time.LocalDate
import java.time.format.TextStyle
import java.time.temporal.ChronoUnit
import java.util.Locale

@Composable
fun DashboardScreen(
    summary: SpendSummary,
    baseCurrency: String,
    rates: ExchangeRateTable,
    onEdit: (Subscription) -> Unit,
) {
    val active = summary.perSub.count { it.subscription.status == SubStatus.ACTIVE }
    var chart by remember { mutableStateOf(ChartType.DONUT) }

    LazyColumn(
        Modifier.fillMaxSize().background(Palette.Bg).padding(horizontal = 16.dp),
    ) {
        item { Spacer(Modifier.height(20.dp)) }
        item {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("SubTrackr", color = Palette.TextPrimary, fontSize = 26.sp, fontWeight = FontWeight.Bold)
                Spacer(Modifier.width(10.dp))
                Box(
                    Modifier.background(Palette.SurfaceAlt, RoundedCornerShape(10.dp)).padding(horizontal = 9.dp, vertical = 3.dp),
                ) { Text("$active active", color = Palette.TextSecondary, fontSize = 12.sp) }
            }
        }
        item { Spacer(Modifier.height(16.dp)) }
        item { OverviewCard(summary, baseCurrency, chart) { chart = it } }
        item { Spacer(Modifier.height(12.dp)) }
        item { CurrencyCard(summary, baseCurrency, rates) }
        item { Spacer(Modifier.height(12.dp)) }
        item { RenewalsCard(summary) }
        item { Spacer(Modifier.height(16.dp)) }
        item {
            Text("SUBSCRIPTIONS", color = Palette.TextSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(start = 4.dp, bottom = 8.dp))
        }
        items(summary.perSub.sortedByDescending { it.monthlyBase }) { spend ->
            SubscriptionRow(spend, baseCurrency, onEdit)
        }
        item { Spacer(Modifier.height(90.dp)) }
    }
}

@Composable
private fun OverviewCard(summary: SpendSummary, base: String, chart: ChartType, onChart: (ChartType) -> Unit) {
    SectionCard {
        Text("OVERVIEW", color = Palette.TextSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(8.dp))
        val slices = summary.byCategory
            .filter { it.monthlyBase > BigDecimal.ZERO }
            .mapIndexed { i, c -> ChartSlice(c.category, c.monthlyBase.toDouble(), Palette.category(i)) }

        SpendChart(
            type = chart,
            slices = slices,
            monthlyBase = summary.monthlyBase.toDouble(),
            centerText = Format.moneyWhole(summary.monthlyBase, base),
            centerSub = "Total per month",
        )
        Spacer(Modifier.height(8.dp))
        Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
            ChartSwitcher(chart, onChart)
        }
        Spacer(Modifier.height(14.dp))
        Box(Modifier.fillMaxWidth().height(1.dp).background(Palette.Border))
        Spacer(Modifier.height(14.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Column {
                Text("Per month", color = Palette.TextSecondary, fontSize = 11.sp)
                Text(Format.money(summary.monthlyBase, base), color = Palette.TextPrimary, fontSize = 18.sp, fontWeight = FontWeight.SemiBold)
            }
            Column(horizontalAlignment = Alignment.End) {
                Text("Per year", color = Palette.TextSecondary, fontSize = 11.sp)
                Text(Format.money(summary.yearlyBase, base), color = Palette.TextPrimary, fontSize = 18.sp, fontWeight = FontWeight.SemiBold)
            }
        }
    }
}

@Composable
private fun CurrencyCard(summary: SpendSummary, base: String, rates: ExchangeRateTable) {
    if (summary.perCurrency.isEmpty()) return
    SectionCard {
        Text("BY CURRENCY", color = Palette.TextSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        summary.perCurrency.forEach { c ->
            val converted = if (rates.knows(c.currency)) rates.convert(c.monthly, c.currency, base) else c.monthly
            Row(Modifier.fillMaxWidth().padding(vertical = 5.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(Modifier.background(Palette.SurfaceAlt, RoundedCornerShape(6.dp)).padding(horizontal = 7.dp, vertical = 2.dp)) {
                    Text(c.currency, color = Palette.TextPrimary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                }
                Spacer(Modifier.width(10.dp))
                Text(Format.money(c.monthly, c.currency) + " / mo", color = Palette.TextPrimary, fontSize = 13.sp)
                Spacer(Modifier.weight(1f))
                Text("≈ " + Format.money(converted, base), color = Palette.TextSecondary, fontSize = 12.sp)
            }
        }
    }
}

@Composable
private fun RenewalsCard(summary: SpendSummary) {
    val today = LocalDate.now()
    val upcoming = summary.perSub
        .filter { it.subscription.status == SubStatus.ACTIVE }
        .mapNotNull { p -> runCatching { LocalDate.parse(p.subscription.nextRenewal) }.getOrNull()?.let { p to it } }
        .sortedBy { it.second }
        .take(5)
    if (upcoming.isEmpty()) return
    SectionCard {
        Text("UPCOMING RENEWALS", color = Palette.TextSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
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
                    Text(p.subscription.name, color = Palette.TextPrimary, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
                    Text(whenText, color = Palette.TextSecondary, fontSize = 11.sp)
                }
                Text(Format.money(p.subscription.cost.toBigDecimal(), p.subscription.cost.currency), color = Palette.TextPrimary, fontSize = 13.sp)
            }
        }
    }
}

@Composable
private fun SubscriptionRow(spend: SubscriptionSpend, base: String, onEdit: (Subscription) -> Unit) {
    val s = spend.subscription
    Row(
        Modifier.fillMaxWidth().clickable { onEdit(s) }.padding(vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(s.iconRef.ifBlank { "•" }, fontSize = 20.sp)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(s.name, color = Palette.TextPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                if (spend.verdict != WorthVerdict.UNKNOWN) {
                    Spacer(Modifier.width(8.dp))
                    val (label, color) = when (spend.verdict) {
                        WorthVerdict.WORTH -> "Worth" to Palette.Positive
                        WorthVerdict.NOT_WORTH -> "Not worth" to Palette.Negative
                        WorthVerdict.ESSENTIAL -> "Essential" to Palette.Accent
                        else -> "" to Palette.TextMuted
                    }
                    Box(Modifier.background(Color(0x22FFFFFF), RoundedCornerShape(6.dp)).padding(horizontal = 6.dp, vertical = 1.dp)) {
                        Text(label, color = color, fontSize = 10.sp, fontWeight = FontWeight.SemiBold)
                    }
                }
            }
            Text(s.category.ifBlank { "Uncategorized" }, color = Palette.TextSecondary, fontSize = 12.sp)
        }
        Column(horizontalAlignment = Alignment.End) {
            Text(Format.money(spend.monthlyBase, base), color = Palette.TextPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
            Text(
                Format.money(s.cost.toBigDecimal(), s.cost.currency) + " / " + cycleShort(s.billingCycle),
                color = Palette.TextMuted, fontSize = 11.sp,
            )
        }
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
