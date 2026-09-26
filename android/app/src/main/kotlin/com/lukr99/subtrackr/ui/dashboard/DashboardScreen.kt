package com.lukr99.subtrackr.ui.dashboard

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
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.domain.spend.SubscriptionSpend
import com.lukr99.subtrackr.domain.worth.WorthVerdict
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.charts.ChartSlice
import com.lukr99.subtrackr.ui.charts.ChartSwitcher
import com.lukr99.subtrackr.ui.charts.ChartType
import com.lukr99.subtrackr.ui.charts.SpendChart
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.components.PickerField
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.components.fieldColors
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
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
    budget: BigDecimal,
    onEdit: (Subscription) -> Unit,
    today: LocalDate,
) {
    val active = summary.perSub.count { it.subscription.status == SubStatus.ACTIVE }
    var chart by remember { mutableStateOf(ChartType.DONUT) }
    var search by remember { mutableStateOf("") }
    var category by remember { mutableStateOf("All categories") }
    var sortLabel by remember { mutableStateOf("Monthly ↓") }

    val categories = listOf("All categories") +
        summary.perSub.map { it.subscription.category.ifBlank { "Uncategorized" } }.distinct().sorted()
    if (category != "All categories" && category !in categories) category = "All categories"

    val rows = summary.perSub
        .filter { category == "All categories" || it.subscription.category.ifBlank { "Uncategorized" } == category }
        .filter {
            search.isBlank() ||
                it.subscription.name.contains(search, true) ||
                it.subscription.category.contains(search, true)
        }
        .let { list ->
            when (sortLabel) {
                "Name A–Z" -> list.sortedBy { it.subscription.name.lowercase() }
                "Renewal" -> list.sortedBy { it.subscription.nextRenewal.ifBlank { "9999" } }
                else -> list.sortedByDescending { it.monthlyBase }
            }
        }

    val alerts = summary.perSub
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

    LazyColumn(
        Modifier.testTag(DashboardTags.ROOT).fillMaxSize().background(SubTrackrTheme.colors.background).padding(horizontal = 16.dp),
    ) {
        item { Spacer(Modifier.height(20.dp)) }
        item {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("SubTrackr", color = SubTrackrTheme.colors.textPrimary, fontSize = 26.sp, fontWeight = FontWeight.Bold)
                Spacer(Modifier.width(10.dp))
                Box(
                    Modifier.background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(10.dp)).padding(horizontal = 9.dp, vertical = 3.dp),
                ) { Text("$active active", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp) }
            }
        }
        item { Spacer(Modifier.height(16.dp)) }
        if (alerts.isNotEmpty()) {
            item { AlertsCard(alerts); Spacer(Modifier.height(12.dp)) }
        }
        item { OverviewCard(summary, baseCurrency, chart, budget) { chart = it } }
        item { Spacer(Modifier.height(12.dp)) }
        item { CurrencyCard(summary, baseCurrency, rates) }
        item { Spacer(Modifier.height(12.dp)) }
        item { RenewalsCard(summary, today) }
        item { Spacer(Modifier.height(16.dp)) }
        item {
            Text("SUBSCRIPTIONS", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(start = 4.dp, bottom = 8.dp))
        }
        item {
            FilterBar(
                search, { search = it }, categories, category, { category = it },
                sortLabel, { sortLabel = it },
            )
            Spacer(Modifier.height(8.dp))
        }
        items(rows) { spend -> SubscriptionRow(spend, baseCurrency, onEdit) }
        item { Spacer(Modifier.height(90.dp)) }
    }
}

@Composable
private fun OverviewCard(summary: SpendSummary, base: String, chart: ChartType, budget: BigDecimal, onChart: (ChartType) -> Unit) {
    SectionCard {
        Text("OVERVIEW", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(8.dp))
        val slices = summary.byCategory
            .filter { it.monthlyBase > BigDecimal.ZERO }
            .mapIndexed { i, c -> ChartSlice(c.category, c.monthlyBase.toDouble(), SubTrackrTheme.colors.chartColor(i)) }

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
        Box(Modifier.fillMaxWidth().height(1.dp).background(SubTrackrTheme.colors.border))
        Spacer(Modifier.height(14.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Column {
                Text("Per month", color = SubTrackrTheme.colors.textSecondary, fontSize = 11.sp)
                Text(
                    Format.money(summary.monthlyBase, base),
                    color = SubTrackrTheme.colors.textPrimary,
                    fontSize = 18.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.testTag(DashboardTags.MONTHLY_TOTAL),
                )
            }
            Column(horizontalAlignment = Alignment.End) {
                Text("Per year", color = SubTrackrTheme.colors.textSecondary, fontSize = 11.sp)
                Text(Format.money(summary.yearlyBase, base), color = SubTrackrTheme.colors.textPrimary, fontSize = 18.sp, fontWeight = FontWeight.SemiBold)
            }
        }
        if (budget > BigDecimal.ZERO) {
            val spent = summary.monthlyBase
            val over = spent > budget
            val color = if (over) SubTrackrTheme.colors.negative else SubTrackrTheme.colors.positive
            val frac = (spent.toDouble() / budget.toDouble()).coerceIn(0.0, 1.0).toFloat()
            val remaining = if (over) Format.money(spent - budget, base) + " over"
            else Format.money(budget - spent, base) + " left"
            Spacer(Modifier.height(16.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Budget", color = SubTrackrTheme.colors.textSecondary, fontSize = 11.sp)
                Text(remaining, color = color, fontSize = 11.sp, fontWeight = FontWeight.SemiBold)
            }
            Spacer(Modifier.height(6.dp))
            Box(Modifier.fillMaxWidth().height(8.dp).background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(4.dp))) {
                Box(Modifier.fillMaxWidth(frac).height(8.dp).background(color, RoundedCornerShape(4.dp)))
            }
            Spacer(Modifier.height(4.dp))
            Text(
                Format.money(spent, base) + " of " + Format.money(budget, base),
                color = SubTrackrTheme.colors.textMuted, fontSize = 11.sp,
            )
        }
    }
}

@Composable
private fun CurrencyCard(summary: SpendSummary, base: String, rates: ExchangeRateTable) {
    if (summary.perCurrency.isEmpty()) return
    SectionCard {
        Text("BY CURRENCY", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        summary.perCurrency.forEach { c ->
            val converted = if (rates.knows(c.currency)) rates.convert(c.monthly, c.currency, base) else c.monthly
            Row(Modifier.fillMaxWidth().padding(vertical = 5.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(Modifier.background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(6.dp)).padding(horizontal = 7.dp, vertical = 2.dp)) {
                    Text(c.currency, color = SubTrackrTheme.colors.textPrimary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                }
                Spacer(Modifier.width(10.dp))
                Text(Format.money(c.monthly, c.currency) + " / mo", color = SubTrackrTheme.colors.textPrimary, fontSize = 13.sp)
                Spacer(Modifier.weight(1f))
                Text("≈ " + Format.money(converted, base), color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
            }
        }
    }
}

@Composable
private fun RenewalsCard(summary: SpendSummary, today: LocalDate) {
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
                Text(Format.money(p.subscription.cost.toBigDecimal(), p.subscription.cost.currency), color = SubTrackrTheme.colors.textPrimary, fontSize = 13.sp)
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
        Box(Modifier.size(24.dp), contentAlignment = Alignment.Center) {
            if (s.website.isNotBlank()) {
                AsyncImage(
                    model = "https://www.google.com/s2/favicons?domain=${s.website}&sz=64",
                    contentDescription = null,
                    modifier = Modifier.size(22.dp),
                )
            } else {
                Text(s.iconRef.ifBlank { "•" }, fontSize = 20.sp)
            }
        }
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(s.name, color = SubTrackrTheme.colors.textPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                if (spend.verdict != WorthVerdict.UNKNOWN) {
                    Spacer(Modifier.width(8.dp))
                    val (label, color) = when (spend.verdict) {
                        WorthVerdict.WORTH -> "Worth" to SubTrackrTheme.colors.positive
                        WorthVerdict.NOT_WORTH -> "Not worth" to SubTrackrTheme.colors.negative
                        WorthVerdict.ESSENTIAL -> "Essential" to SubTrackrTheme.colors.accent
                        else -> "" to SubTrackrTheme.colors.textMuted
                    }
                    Box(Modifier.background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(6.dp)).padding(horizontal = 6.dp, vertical = 1.dp)) {
                        Text(label, color = color, fontSize = 10.sp, fontWeight = FontWeight.SemiBold)
                    }
                }
            }
            Text(s.category.ifBlank { "Uncategorized" }, color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        }
        Column(horizontalAlignment = Alignment.End) {
            Text(Format.money(spend.monthlyBase, base), color = SubTrackrTheme.colors.textPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
            Text(
                Format.money(s.cost.toBigDecimal(), s.cost.currency) + " / " + cycleShort(s.billingCycle),
                color = SubTrackrTheme.colors.textMuted, fontSize = 11.sp,
            )
        }
    }
}

private fun whenText(days: Long) = when {
    days <= 0L -> "today"
    days == 1L -> "tomorrow"
    else -> "in $days days"
}

@Composable
private fun AlertsCard(alerts: List<DashboardAlert>) {
    Column(
        Modifier.fillMaxWidth().background(SubTrackrTheme.colors.surface, RoundedCornerShape(14.dp)).padding(16.dp),
    ) {
        Text("⏰ ALERTS", color = SubTrackrTheme.colors.warning, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        alerts.forEach { a ->
            Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                Text(a.icon, fontSize = 15.sp)
                Spacer(Modifier.width(8.dp))
                Text(a.title, color = SubTrackrTheme.colors.textPrimary, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Text(a.detail, color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
            }
        }
    }
}

@Composable
private fun FilterBar(
    search: String,
    onSearch: (String) -> Unit,
    categories: List<String>,
    category: String,
    onCategory: (String) -> Unit,
    sortLabel: String,
    onSort: (String) -> Unit,
) {
    Column {
        OutlinedTextField(
            value = search,
            onValueChange = onSearch,
            placeholder = { Text("Search subscriptions…", color = SubTrackrTheme.colors.textMuted) },
            singleLine = true,
            colors = fieldColors(),
            shape = RoundedCornerShape(10.dp),
            modifier = Modifier.fillMaxWidth(),
        )
        Spacer(Modifier.height(8.dp))
        Row {
            PickerField("Category", category, categories, onCategory, Modifier.weight(1f))
            Spacer(Modifier.width(10.dp))
            PickerField("Sort", sortLabel, listOf("Monthly ↓", "Name A–Z", "Renewal"), onSort, Modifier.weight(1f))
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
