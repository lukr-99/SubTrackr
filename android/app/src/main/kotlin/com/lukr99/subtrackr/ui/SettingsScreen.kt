package com.lukr99.subtrackr.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
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
import java.math.BigDecimal

@Composable
fun SettingsScreen(
    baseCurrency: String,
    worthThreshold: BigDecimal,
    monthlyBudget: BigDecimal,
    ratesLabel: String,
    syncUrl: String,
    syncKey: String,
    appVersion: String,
    updateStatus: String,
    updateBusy: Boolean,
    onSetBaseCurrency: (String) -> Unit,
    onSetThreshold: (BigDecimal) -> Unit,
    onSetBudget: (BigDecimal) -> Unit,
    onRefreshRates: () -> Unit,
    onSync: (String, String, (String) -> Unit) -> Unit,
    onCheckUpdate: () -> Unit,
) {
    var threshold by remember { mutableStateOf(worthThreshold.stripTrailingZeros().toPlainString()) }
    var budget by remember {
        mutableStateOf(if (monthlyBudget > BigDecimal.ZERO) monthlyBudget.stripTrailingZeros().toPlainString() else "")
    }
    var url by remember { mutableStateOf(syncUrl) }
    var key by remember { mutableStateOf(syncKey) }
    var syncStatus by remember { mutableStateOf("") }

    Column(
        Modifier
            .fillMaxSize()
            .background(Palette.Bg)
            .padding(horizontal = 16.dp)
            .verticalScroll(rememberScrollState()),
    ) {
        Spacer(Modifier.height(20.dp))
        Text("Settings", color = Palette.TextPrimary, fontSize = 24.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(16.dp))

        SectionCard {
            Text("Base currency", color = Palette.TextSecondary, fontSize = 12.sp)
            Text("All totals roll up into this currency.", color = Palette.TextMuted, fontSize = 11.sp)
            Spacer(Modifier.height(8.dp))
            PickerField("Base currency", baseCurrency, Format.commonCurrencies, onSetBaseCurrency, Modifier.width(180.dp))

            Spacer(Modifier.height(16.dp))
            Text("Worth-it threshold (cost per use)", color = Palette.TextSecondary, fontSize = 12.sp)
            Text("Worth it when cost per use is at or below this.", color = Palette.TextMuted, fontSize = 11.sp)
            Spacer(Modifier.height(8.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                LabeledTextField("Threshold", threshold, { threshold = it }, Modifier.width(160.dp))
                Spacer(Modifier.width(12.dp))
                Button(
                    onClick = { threshold.toBigDecimalOrNull()?.let(onSetThreshold) },
                    colors = ButtonDefaults.buttonColors(containerColor = Palette.Accent),
                ) { Text("Apply") }
            }

            Spacer(Modifier.height(16.dp))
            Text("Monthly budget (0 = off)", color = Palette.TextSecondary, fontSize = 12.sp)
            Text("Shows a budget bar on the dashboard, red when over.", color = Palette.TextMuted, fontSize = 11.sp)
            Spacer(Modifier.height(8.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                LabeledTextField("Budget", budget, { budget = it }, Modifier.width(160.dp))
                Spacer(Modifier.width(12.dp))
                Button(
                    onClick = { onSetBudget(budget.toBigDecimalOrNull() ?: BigDecimal.ZERO) },
                    colors = ButtonDefaults.buttonColors(containerColor = Palette.Accent),
                ) { Text("Apply") }
            }
        }
        Spacer(Modifier.height(12.dp))

        SectionCard {
            Text("Exchange rates", color = Palette.TextSecondary, fontSize = 12.sp)
            Spacer(Modifier.height(6.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(ratesLabel, color = Palette.TextPrimary, fontSize = 13.sp)
                Spacer(Modifier.width(12.dp))
                Button(
                    onClick = onRefreshRates,
                    colors = ButtonDefaults.buttonColors(containerColor = Palette.SurfaceAlt),
                ) { Text("Refresh", color = Palette.TextPrimary) }
            }
        }
        Spacer(Modifier.height(12.dp))

        SectionCard {
            Text("Sync (Supabase)", color = Palette.TextSecondary, fontSize = 12.sp)
            Text("Syncs subscriptions across your devices. See docs/SYNC-SETUP.md.", color = Palette.TextMuted, fontSize = 11.sp)
            Spacer(Modifier.height(8.dp))
            LabeledTextField("Project URL", url, { url = it })
            Spacer(Modifier.height(10.dp))
            LabeledTextField("Anon key", key, { key = it })
            Spacer(Modifier.height(12.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                Button(
                    onClick = { onSync(url, key) { syncStatus = it } },
                    colors = ButtonDefaults.buttonColors(containerColor = Palette.Accent),
                ) { Text("Sync now") }
                Spacer(Modifier.width(12.dp))
                Text(syncStatus, color = Palette.TextSecondary, fontSize = 12.sp)
            }
        }
        Spacer(Modifier.height(12.dp))

        SectionCard {
            Text("Updates", color = Palette.TextSecondary, fontSize = 12.sp)
            Text(
                updateStatus.ifBlank { "Installed version: v$appVersion" },
                color = Palette.TextMuted,
                fontSize = 11.sp,
            )
            Spacer(Modifier.height(8.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                Button(
                    enabled = !updateBusy,
                    onClick = onCheckUpdate,
                    colors = ButtonDefaults.buttonColors(containerColor = Palette.SurfaceAlt),
                ) { Text("Check latest", color = Palette.TextPrimary) }
                if (updateBusy) {
                    Spacer(Modifier.width(12.dp))
                    CircularProgressIndicator(
                        modifier = Modifier.width(22.dp).height(22.dp),
                        strokeWidth = 2.dp,
                        color = Palette.Accent,
                    )
                }
            }
        }
        Spacer(Modifier.height(12.dp))

        SectionCard {
            Text("About", color = Palette.TextSecondary, fontSize = 12.sp)
            Spacer(Modifier.height(4.dp))
            Text("SubTrackr for Android · v$appVersion", color = Palette.TextPrimary, fontSize = 13.sp)
            Text("Shares its data contract with the desktop app.", color = Palette.TextMuted, fontSize = 11.sp)
        }
        Spacer(Modifier.height(28.dp))
    }
}
