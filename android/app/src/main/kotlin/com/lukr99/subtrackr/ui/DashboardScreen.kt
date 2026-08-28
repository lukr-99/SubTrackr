package com.lukr99.subtrackr.ui

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.SpendSummary
import com.lukr99.subtrackr.domain.SubscriptionSpend
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.SubStatus
import java.math.BigDecimal

@Composable
fun DashboardScreen(summary: SpendSummary) {
    val active = summary.perSub.count { it.subscription.status == SubStatus.ACTIVE }
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .background(Palette.Bg)
            .padding(horizontal = 16.dp),
    ) {
        item { Spacer(Modifier.height(24.dp)) }
        item {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("SubTrackr", color = Palette.TextPrimary, fontSize = 26.sp, fontWeight = FontWeight.Bold)
                Spacer(Modifier.width(10.dp))
                Chip("$active active")
            }
        }
        item { Spacer(Modifier.height(16.dp)) }
        item { OverviewCard(summary) }
        item { Spacer(Modifier.height(16.dp)) }
        item {
            Text(
                "SUBSCRIPTIONS",
                color = Palette.TextSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(start = 4.dp, bottom = 8.dp),
            )
        }
        items(summary.perSub.sortedByDescending { it.monthlyBase }) { spend ->
            SubscriptionRow(spend, summary.baseCurrency)
        }
        item { Spacer(Modifier.height(24.dp)) }
    }
}

@Composable
private fun Chip(text: String) {
    Box(
        Modifier
            .background(Palette.SurfaceAlt, RoundedCornerShape(10.dp))
            .padding(horizontal = 9.dp, vertical = 3.dp),
    ) { Text(text, color = Palette.TextSecondary, fontSize = 12.sp) }
}

@Composable
private fun OverviewCard(summary: SpendSummary) {
    Column(
        Modifier
            .fillMaxWidth()
            .background(Palette.Surface, RoundedCornerShape(14.dp))
            .padding(18.dp),
    ) {
        Text("OVERVIEW", color = Palette.TextSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(12.dp))

        val slices = summary.byCategory
            .filter { it.monthlyBase > BigDecimal.ZERO }
            .mapIndexed { i, c -> c.monthlyBase.toDouble() to Palette.category(i) }

        Box(Modifier.fillMaxWidth().height(220.dp), contentAlignment = Alignment.Center) {
            Donut(slices)
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    Format.moneyWhole(summary.monthlyBase, summary.baseCurrency),
                    color = Palette.TextPrimary, fontSize = 30.sp, fontWeight = FontWeight.Bold,
                )
                Text("Total per month", color = Palette.TextMuted, fontSize = 12.sp)
            }
        }

        Spacer(Modifier.height(14.dp))
        Box(Modifier.fillMaxWidth().height(1.dp).background(Palette.Border))
        Spacer(Modifier.height(14.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Column {
                Text("Per month", color = Palette.TextSecondary, fontSize = 11.sp)
                Text(
                    Format.money(summary.monthlyBase, summary.baseCurrency),
                    color = Palette.TextPrimary, fontSize = 18.sp, fontWeight = FontWeight.SemiBold,
                )
            }
            Column(horizontalAlignment = Alignment.End) {
                Text("Per year", color = Palette.TextSecondary, fontSize = 11.sp)
                Text(
                    Format.money(summary.yearlyBase, summary.baseCurrency),
                    color = Palette.TextPrimary, fontSize = 18.sp, fontWeight = FontWeight.SemiBold,
                )
            }
        }
    }
}

@Composable
private fun Donut(slices: List<Pair<Double, androidx.compose.ui.graphics.Color>>) {
    val total = slices.sumOf { it.first }
    Canvas(Modifier.size(200.dp)) {
        val stroke = size.minDimension * 0.13f
        val inset = stroke / 2
        val arcSize = Size(size.minDimension - stroke, size.minDimension - stroke)
        val topLeft = Offset(inset, inset)
        // track
        drawArc(
            color = Palette.Border, startAngle = 0f, sweepAngle = 360f, useCenter = false,
            topLeft = topLeft, size = arcSize, style = Stroke(width = stroke),
        )
        if (total <= 0.0) return@Canvas
        var start = -90f
        for ((value, color) in slices) {
            val sweep = (value / total * 360.0).toFloat()
            drawArc(
                color = color, startAngle = start, sweepAngle = sweep, useCenter = false,
                topLeft = topLeft, size = arcSize, style = Stroke(width = stroke),
            )
            start += sweep
        }
    }
}

@Composable
private fun SubscriptionRow(spend: SubscriptionSpend, base: String) {
    val s = spend.subscription
    Row(
        Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(s.iconRef.ifBlank { "•" }, fontSize = 20.sp)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Text(s.name, color = Palette.TextPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
            Text(
                s.category.ifBlank { "Uncategorized" },
                color = Palette.TextSecondary, fontSize = 12.sp,
            )
        }
        Column(horizontalAlignment = Alignment.End) {
            Text(
                Format.money(spend.monthlyBase, base),
                color = Palette.TextPrimary, fontSize = 15.sp, fontWeight = FontWeight.SemiBold,
            )
            Text(Format.money(s.cost.toBigDecimal(), s.cost.currency) + " / " + cycleShort(s.billingCycle),
                color = Palette.TextMuted, fontSize = 11.sp)
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
