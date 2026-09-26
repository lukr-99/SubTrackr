package com.lukr99.subtrackr.ui.whatif

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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.spend.Normalization
import com.lukr99.subtrackr.domain.worth.WorthIt
import com.lukr99.subtrackr.domain.worth.WorthVerdict
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.components.LabeledTextField
import com.lukr99.subtrackr.ui.components.PickerField
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import java.math.BigDecimal

private val CYCLES = listOf("Weekly", "Monthly", "Quarterly", "Every 6 months", "Annual", "Custom (days)")

private fun cycle(label: String) = when (label) {
    "Weekly" -> BillingCycle.WEEKLY
    "Quarterly" -> BillingCycle.QUARTERLY
    "Every 6 months" -> BillingCycle.SEMIANNUAL
    "Annual" -> BillingCycle.ANNUAL
    "Custom (days)" -> BillingCycle.CUSTOM_DAYS
    else -> BillingCycle.MONTHLY
}

@Composable
fun WhatIfScreen(
    baseCurrency: String,
    currentMonthly: BigDecimal,
    rates: ExchangeRateTable,
    worthThreshold: BigDecimal,
) {
    var amount by remember { mutableStateOf("9.99") }
    var currency by remember { mutableStateOf("EUR") }
    var cycleLabel by remember { mutableStateOf("Monthly") }
    var uses by remember { mutableStateOf("0") }

    val amt = amount.toBigDecimalOrNull()
    val c = cycle(cycleLabel)
    val monthlyBase = if (amt != null && amt >= BigDecimal.ZERO) {
        val own = Normalization.monthlyEquivalent(amt, c, 30)
        if (rates.knows(currency)) rates.convert(own, currency, baseCurrency) else own
    } else null
    val newMonthly = monthlyBase?.let { currentMonthly + it }
    val usesVal = uses.toDoubleOrNull() ?: 0.0

    Column(
        Modifier
            .fillMaxSize()
            .background(SubTrackrTheme.colors.background)
            .padding(horizontal = 16.dp)
            .verticalScroll(rememberScrollState()),
    ) {
        Spacer(Modifier.height(20.dp))
        Text("What-if", color = SubTrackrTheme.colors.textPrimary, fontSize = 24.sp, fontWeight = FontWeight.Bold)
        Text("See the impact of a new subscription before you commit.", color = SubTrackrTheme.colors.textSecondary, fontSize = 13.sp)
        Spacer(Modifier.height(16.dp))

        SectionCard {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Column {
                    Text("Currently", color = SubTrackrTheme.colors.textSecondary, fontSize = 11.sp)
                    Text(Format.money(currentMonthly, baseCurrency), color = SubTrackrTheme.colors.textPrimary, fontSize = 22.sp, fontWeight = FontWeight.Bold)
                    Text("per month", color = SubTrackrTheme.colors.textMuted, fontSize = 11.sp)
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text(Format.money(currentMonthly.multiply(BigDecimal(12)), baseCurrency), color = SubTrackrTheme.colors.textPrimary, fontSize = 16.sp)
                    Text("per year", color = SubTrackrTheme.colors.textMuted, fontSize = 11.sp)
                }
            }
        }
        Spacer(Modifier.height(12.dp))

        SectionCard {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                LabeledTextField("Amount", amount, { amount = it }, Modifier.weight(1f))
                PickerField("Currency", currency, Format.commonCurrencies, { currency = it }, Modifier.width(120.dp))
            }
            Spacer(Modifier.height(12.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                PickerField("Billing cycle", cycleLabel, CYCLES, { cycleLabel = it }, Modifier.weight(1f))
                LabeledTextField("Uses / month", uses, { uses = it }, Modifier.width(120.dp))
            }
        }
        Spacer(Modifier.height(12.dp))

        Column(
            Modifier.fillMaxWidth().background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(14.dp)).padding(16.dp),
        ) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Column {
                    Text("New total", color = SubTrackrTheme.colors.textSecondary, fontSize = 11.sp)
                    Text(
                        newMonthly?.let { Format.money(it, baseCurrency) } ?: "—",
                        color = SubTrackrTheme.colors.textPrimary, fontSize = 24.sp, fontWeight = FontWeight.Bold,
                    )
                    Text(
                        newMonthly?.let { Format.money(it.multiply(BigDecimal(12)), baseCurrency) + " / yr" } ?: "",
                        color = SubTrackrTheme.colors.textMuted, fontSize = 12.sp,
                    )
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text(
                        monthlyBase?.let { "+ " + Format.money(it, baseCurrency) } ?: "—",
                        color = SubTrackrTheme.colors.warning, fontSize = 16.sp, fontWeight = FontWeight.SemiBold,
                    )
                    Text("added / mo", color = SubTrackrTheme.colors.textMuted, fontSize = 11.sp)
                }
            }
            Spacer(Modifier.height(12.dp))
            Box(Modifier.fillMaxWidth().height(1.dp).background(SubTrackrTheme.colors.border))
            Spacer(Modifier.height(12.dp))
            if (monthlyBase != null && usesVal > 0.0) {
                val cpu = WorthIt.costPerUse(monthlyBase, usesVal)
                val verdict = WorthIt.evaluate(monthlyBase, usesVal, worthThreshold)
                Text(Format.money(cpu, baseCurrency) + " per use", color = SubTrackrTheme.colors.textSecondary, fontSize = 13.sp)
                Text(
                    when (verdict) {
                        WorthVerdict.WORTH -> "Worth it 👍"
                        WorthVerdict.NOT_WORTH -> "Not worth it 👎"
                        WorthVerdict.ESSENTIAL -> "Essential 👍"
                        WorthVerdict.UNKNOWN -> ""
                    },
                    color = if (verdict == WorthVerdict.WORTH) SubTrackrTheme.colors.positive else SubTrackrTheme.colors.negative,
                    fontSize = 17.sp, fontWeight = FontWeight.Bold,
                )
            } else {
                Text("Set uses/month to judge worth", color = SubTrackrTheme.colors.textMuted, fontSize = 13.sp)
            }
        }
        Spacer(Modifier.height(28.dp))
    }
}
