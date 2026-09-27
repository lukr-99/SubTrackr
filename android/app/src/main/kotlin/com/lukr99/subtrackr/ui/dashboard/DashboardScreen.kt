package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.charts.ChartType
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import java.math.BigDecimal
import java.time.LocalDate

/** The dashboard: alerts, spend overview, currencies, renewals, and the filterable list. */
@Composable
fun DashboardScreen(
    summary: SpendSummary,
    baseCurrency: String,
    rates: ExchangeRateTable,
    budget: BigDecimal,
    onEdit: (Subscription) -> Unit,
    today: LocalDate,
    showServiceLogos: Boolean,
) {
    val active = summary.perSub.count { it.subscription.status == SubStatus.ACTIVE }
    var chart by remember { mutableStateOf(ChartType.DONUT) }
    var filter by remember { mutableStateOf(SubscriptionFilter()) }

    val categories = SubscriptionFilter.categoriesOf(summary.perSub)
    filter.within(categories).let { if (it != filter) filter = it }
    val rows = filter.apply(summary.perSub)
    val alerts = DashboardAlert.dueSoon(summary.perSub, today)

    LazyColumn(
        Modifier.testTag(DashboardTags.ROOT).fillMaxSize().background(SubTrackrTheme.colors.background).padding(horizontal = 16.dp),
    ) {
        item { Spacer(Modifier.height(20.dp)) }
        item { DashboardHeader(active) }
        item { Spacer(Modifier.height(16.dp)) }
        if (alerts.isNotEmpty()) {
            item {
                AlertsCard(alerts)
                Spacer(Modifier.height(12.dp))
            }
        }
        item { OverviewCard(summary, baseCurrency, chart, budget) { chart = it } }
        item { Spacer(Modifier.height(12.dp)) }
        item { CurrencyCard(summary, baseCurrency, rates) }
        item { Spacer(Modifier.height(12.dp)) }
        item { RenewalsCard(summary, today) }
        item { Spacer(Modifier.height(16.dp)) }
        item {
            Text(
                "SUBSCRIPTIONS",
                color = SubTrackrTheme.colors.textSecondary,
                fontSize = 12.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(start = 4.dp, bottom = 8.dp),
            )
        }
        item {
            FilterBar(filter, categories) { filter = it }
            Spacer(Modifier.height(8.dp))
        }
        items(rows) { spend -> SubscriptionRow(spend, baseCurrency, showServiceLogos, onEdit) }
        item { Spacer(Modifier.height(90.dp)) }
    }
}

@Composable
private fun DashboardHeader(active: Int) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Text("SubTrackr", color = SubTrackrTheme.colors.textPrimary, fontSize = 26.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.width(10.dp))
        Box(
            Modifier.background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(10.dp)).padding(horizontal = 9.dp, vertical = 3.dp),
        ) { Text("$active active", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp) }
    }
}
