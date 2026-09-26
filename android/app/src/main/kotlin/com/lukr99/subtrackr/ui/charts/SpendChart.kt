package com.lukr99.subtrackr.ui.charts

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.ui.theme.Palette

@Composable
fun SpendChart(
    type: ChartType,
    slices: List<ChartSlice>,
    monthlyBase: Double,
    centerText: String,
    centerSub: String,
    modifier: Modifier = Modifier,
) {
    Box(modifier.fillMaxWidth().height(220.dp), contentAlignment = Alignment.Center) {
        Canvas(Modifier.fillMaxWidth().height(220.dp)) {
            when (type) {
                ChartType.DONUT -> drawDonut(slices)
                ChartType.BARS -> drawBars(slices)
                ChartType.TREND -> drawTrend(monthlyBase)
            }
        }
        if (type == ChartType.DONUT) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(centerText, color = Palette.TextPrimary, fontSize = 28.sp, fontWeight = FontWeight.Bold)
                Text(centerSub, color = Palette.TextMuted, fontSize = 12.sp)
            }
        }
    }
}

private fun DrawScope.drawDonut(slices: List<ChartSlice>) {
    val positive = slices.filter { it.value > 0 }
    val total = positive.sumOf { it.value }
    val stroke = size.minDimension * 0.13f
    val diameter = size.minDimension - stroke
    val topLeft = Offset((size.width - diameter) / 2f, (size.height - diameter) / 2f)
    val arcSize = Size(diameter, diameter)
    drawArc(Palette.Border, 0f, 360f, false, topLeft, arcSize, style = Stroke(stroke))
    if (total <= 0.0) return
    var start = -90f
    for (s in positive) {
        val sweep = (s.value / total * 360.0).toFloat()
        drawArc(s.color, start, sweep, false, topLeft, arcSize, style = Stroke(stroke, cap = StrokeCap.Butt))
        start += sweep
    }
}

private fun DrawScope.drawBars(slices: List<ChartSlice>) {
    val positive = slices.filter { it.value > 0 }
    if (positive.isEmpty()) return
    val max = positive.maxOf { it.value }
    val bottom = size.height - 8f
    val top = 12f
    val chartH = bottom - top
    val slot = size.width / positive.size
    val barW = minOf(slot * 0.55f, 60f)
    positive.forEachIndexed { i, s ->
        val h = (s.value / max * chartH).toFloat()
        val x = i * slot + (slot - barW) / 2f
        drawRoundRect(
            color = s.color,
            topLeft = Offset(x, bottom - h),
            size = Size(barW, h),
            cornerRadius = androidx.compose.ui.geometry.CornerRadius(6f, 6f),
        )
    }
}

private fun DrawScope.drawTrend(monthlyBase: Double) {
    if (monthlyBase <= 0.0) return
    val n = 12
    val points = (1..n).map { it * monthlyBase }
    val max = points.last()
    val left = 8f
    val right = size.width - 8f
    val bottom = size.height - 8f
    val top = 14f
    val chartW = right - left
    val chartH = bottom - top
    fun px(i: Int) = left + chartW * i / (n - 1)
    fun py(i: Int) = bottom - (points[i] / max * chartH).toFloat()

    val area = Path().apply {
        moveTo(left, bottom)
        for (i in 0 until n) lineTo(px(i), py(i))
        lineTo(right, bottom)
        close()
    }
    drawPath(area, Palette.Accent.copy(alpha = 0.18f))
    for (i in 1 until n) {
        drawLine(
            Palette.Accent, Offset(px(i - 1), py(i - 1)), Offset(px(i), py(i)),
            strokeWidth = 5f, cap = StrokeCap.Round,
        )
    }
    for (i in 0 until n) drawCircle(Palette.Accent, 6f, Offset(px(i), py(i)))
}

@Composable
fun ChartSwitcher(selected: ChartType, onSelect: (ChartType) -> Unit) {
    Row(
        Modifier
            .background(Palette.SurfaceAlt, RoundedCornerShape(9.dp))
            .padding(3.dp),
    ) {
        ChartType.entries.forEach { t ->
            val active = t == selected
            Box(
                Modifier
                    .background(
                        if (active) Palette.Accent else Color.Transparent,
                        RoundedCornerShape(7.dp),
                    )
                    .clickable { onSelect(t) }
                    .padding(horizontal = 14.dp, vertical = 6.dp),
            ) {
                Text(
                    t.name.lowercase().replaceFirstChar { it.uppercase() },
                    color = if (active) Color.White else Palette.TextSecondary,
                    fontSize = 12.sp,
                )
            }
        }
    }
}
