package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
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
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Monthly spend per original currency with its base-currency equivalent; hidden when empty. */
@Composable
internal fun CurrencyCard(summary: SpendSummary, base: String, rates: ExchangeRateTable) {
    if (summary.perCurrency.isEmpty()) return
    SectionCard {
        Text("BY CURRENCY", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        summary.perCurrency.forEach { c ->
            val converted = if (rates.knows(c.currency)) rates.convert(c.monthly, c.currency, base) else c.monthly
            Row(Modifier.fillMaxWidth().padding(vertical = 5.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(
                    Modifier.background(
                        SubTrackrTheme.colors.surfaceAlt,
                        RoundedCornerShape(6.dp),
                    ).padding(horizontal = 7.dp, vertical = 2.dp),
                ) {
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
