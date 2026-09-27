package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.ui.charts.ChartSlice
import com.lukr99.subtrackr.ui.charts.ChartSwitcher
import com.lukr99.subtrackr.ui.charts.ChartType
import com.lukr99.subtrackr.ui.charts.SpendChart
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import java.math.BigDecimal

/** Spend by category as a chart, the monthly and yearly totals, and the budget bar when one is set. */
@Composable
internal fun OverviewCard(
    summary: SpendSummary,
    base: String,
    chart: ChartType,
    budget: BigDecimal,
    onChart: (ChartType) -> Unit,
) {
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
        Totals(summary, base)
        if (budget > BigDecimal.ZERO) BudgetBar(spent = summary.monthlyBase, budget = budget, base = base)
    }
}

@Composable
private fun Totals(summary: SpendSummary, base: String) {
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
            Text(
                Format.money(summary.yearlyBase, base),
                color = SubTrackrTheme.colors.textPrimary,
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
            )
        }
    }
}

@Composable
private fun BudgetBar(spent: BigDecimal, budget: BigDecimal, base: String) {
    val over = spent > budget
    val color = if (over) SubTrackrTheme.colors.negative else SubTrackrTheme.colors.positive
    val frac = (spent.toDouble() / budget.toDouble()).coerceIn(0.0, 1.0).toFloat()
    val remaining = if (over) Format.money(spent - budget, base) + " over" else Format.money(budget - spent, base) + " left"
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
        color = SubTrackrTheme.colors.textMuted,
        fontSize = 11.sp,
    )
}
