package com.lukr99.subtrackr.ui.editor

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.WorthMode
import com.lukr99.subtrackr.ui.components.Format
import com.lukr99.subtrackr.ui.components.LabeledTextField
import com.lukr99.subtrackr.ui.components.PickerField
import com.lukr99.subtrackr.ui.theme.Palette
import java.math.BigDecimal

private val ICONS = listOf(
    "🎬", "📺", "🎵", "🎧", "🎮", "🤖", "✳️", "🧠", "💻", "🌐",
    "📱", "☁️", "💳", "🛒", "📦", "🍔", "🍕", "☕", "🚕", "🚊",
    "🚗", "✈️", "🏋️", "📰", "📚", "🎓", "🔒", "📸", "🎨", "⚡",
)

private val CYCLE_LABELS = listOf("Weekly", "Monthly", "Quarterly", "Every 6 months", "Annual", "Custom (days)")

private fun labelFor(cycle: BillingCycle) = when (cycle) {
    BillingCycle.WEEKLY -> "Weekly"
    BillingCycle.QUARTERLY -> "Quarterly"
    BillingCycle.SEMIANNUAL -> "Every 6 months"
    BillingCycle.ANNUAL -> "Annual"
    BillingCycle.CUSTOM_DAYS -> "Custom (days)"
    else -> "Monthly"
}

private fun cycleFor(label: String) = when (label) {
    "Weekly" -> BillingCycle.WEEKLY
    "Quarterly" -> BillingCycle.QUARTERLY
    "Every 6 months" -> BillingCycle.SEMIANNUAL
    "Annual" -> BillingCycle.ANNUAL
    "Custom (days)" -> BillingCycle.CUSTOM_DAYS
    else -> BillingCycle.MONTHLY
}

private val WORTH_LABELS = listOf("Auto (by usage)", "Essential", "Always worth", "Not worth")

private fun worthLabel(mode: WorthMode) = when (mode) {
    WorthMode.ESSENTIAL -> "Essential"
    WorthMode.WORTH -> "Always worth"
    WorthMode.NOT_WORTH -> "Not worth"
    WorthMode.AUTO -> "Auto (by usage)"
}

private fun worthFor(label: String) = when (label) {
    "Essential" -> WorthMode.ESSENTIAL
    "Always worth" -> WorthMode.WORTH
    "Not worth" -> WorthMode.NOT_WORTH
    else -> WorthMode.AUTO
}

@Composable
fun EditSubscriptionScreen(
    initial: Subscription?,
    onSave: (Subscription) -> Unit,
    onCancel: () -> Unit,
    onDelete: (String) -> Unit,
) {
    val isNew = initial == null
    var icon by remember { mutableStateOf(initial?.iconRef?.ifBlank { "💳" } ?: "💳") }
    var name by remember { mutableStateOf(initial?.name ?: "") }
    var amount by remember {
        mutableStateOf(initial?.cost?.toBigDecimal()?.stripTrailingZeros()?.toPlainString() ?: "")
    }
    var currency by remember { mutableStateOf(initial?.cost?.currency ?: "EUR") }
    var cycleLabel by remember { mutableStateOf(labelFor(initial?.billingCycle ?: BillingCycle.MONTHLY)) }
    var customDays by remember { mutableStateOf(if ((initial?.customDays ?: 0) > 0) initial!!.customDays.toString() else "30") }
    var category by remember { mutableStateOf(initial?.category ?: "") }
    var uses by remember { mutableStateOf((initial?.usesPerMonth ?: 0.0).let { if (it == 0.0) "0" else it.toString() }) }
    var nextRenewal by remember { mutableStateOf(initial?.nextRenewal ?: "") }
    var autoPay by remember { mutableStateOf(initial?.autoPay ?: true) }
    var paused by remember { mutableStateOf(initial?.status == SubStatus.PAUSED) }
    var worth by remember { mutableStateOf(worthLabel(initial?.worthMode ?: WorthMode.AUTO)) }
    var website by remember { mutableStateOf(initial?.website ?: "") }
    var isTrial by remember { mutableStateOf((initial?.trialEnd ?: "").isNotBlank()) }
    var trialEnd by remember { mutableStateOf(initial?.trialEnd ?: "") }
    var error by remember { mutableStateOf<String?>(null) }

    fun build(): Subscription? {
        if (name.isBlank()) { error = "Name is required."; return null }
        val amt = amount.toBigDecimalOrNull()
        if (amt == null || amt < BigDecimal.ZERO) { error = "Enter a valid amount."; return null }
        val cycle = cycleFor(cycleLabel)
        val days = if (cycle == BillingCycle.CUSTOM_DAYS) (customDays.toIntOrNull() ?: 0) else 0
        if (cycle == BillingCycle.CUSTOM_DAYS && days <= 0) { error = "Interval must be > 0 days."; return null }
        return Subscription(
            id = initial?.id ?: "",
            name = name.trim(),
            cost = Money.of(amt, currency),
            billingCycle = cycle,
            customDays = days,
            nextRenewal = nextRenewal.trim(),
            category = category.trim(),
            iconRef = icon,
            autoPay = autoPay,
            status = if (paused) SubStatus.PAUSED else SubStatus.ACTIVE,
            usesPerMonth = uses.toDoubleOrNull() ?: 0.0,
            worthMode = worthFor(worth),
            website = website.trim(),
            trialEnd = if (isTrial) trialEnd.trim() else "",
            createdAt = initial?.createdAt ?: "",
        )
    }

    Column(
        Modifier
            .fillMaxSize()
            .background(Palette.Bg)
            .statusBarsPadding()
            .padding(horizontal = 16.dp)
            .verticalScroll(rememberScrollState()),
    ) {
        Spacer(Modifier.height(10.dp))
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            TextButton(onClick = onCancel) { Text("Cancel", color = Palette.TextSecondary, fontSize = 15.sp) }
            Spacer(Modifier.weight(1f))
            Text(if (isNew) "Add subscription" else "Edit", color = Palette.TextPrimary, fontSize = 18.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.weight(1f))
            Button(
                onClick = { build()?.let(onSave) },
                colors = ButtonDefaults.buttonColors(containerColor = Palette.Accent),
            ) { Text("Save", color = Color.White, fontWeight = FontWeight.Bold) }
        }
        Spacer(Modifier.height(12.dp))

        Text("Icon", color = Palette.TextSecondary, fontSize = 12.sp)
        Spacer(Modifier.height(6.dp))
        LazyRow(horizontalArrangement = Arrangement.spacedBy(6.dp), contentPadding = PaddingValues(vertical = 2.dp)) {
            items(ICONS) { e ->
                val selected = e == icon
                Box(
                    Modifier
                        .size(44.dp)
                        .background(if (selected) Palette.Accent.copy(alpha = 0.25f) else Palette.SurfaceAlt, RoundedCornerShape(10.dp))
                        .then(if (selected) Modifier.border(1.dp, Palette.Accent, RoundedCornerShape(10.dp)) else Modifier)
                        .clickable { icon = e },
                    contentAlignment = Alignment.Center,
                ) { Text(e, fontSize = 20.sp) }
            }
        }
        Spacer(Modifier.height(12.dp))

        LabeledTextField("Name", name, { name = it })
        Spacer(Modifier.height(12.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            LabeledTextField("Amount", amount, { amount = it }, Modifier.weight(1f))
            PickerField("Currency", currency, Format.commonCurrencies, { currency = it }, Modifier.width(130.dp))
        }
        Spacer(Modifier.height(12.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            PickerField("Billing cycle", cycleLabel, CYCLE_LABELS, { cycleLabel = it }, Modifier.weight(1f))
            LabeledTextField("Uses / month", uses, { uses = it }, Modifier.width(130.dp))
        }
        if (cycleFor(cycleLabel) == BillingCycle.CUSTOM_DAYS) {
            Spacer(Modifier.height(12.dp))
            LabeledTextField("Interval (days)", customDays, { customDays = it })
        }
        Spacer(Modifier.height(12.dp))
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            LabeledTextField("Category", category, { category = it }, Modifier.weight(1f))
            LabeledTextField("Renews (YYYY-MM-DD)", nextRenewal, { nextRenewal = it }, Modifier.weight(1f))
        }
        Spacer(Modifier.height(12.dp))
        PickerField("Worth-it", worth, WORTH_LABELS, { worth = it })
        Spacer(Modifier.height(12.dp))
        LabeledTextField("Website (for logo, e.g. netflix.com)", website, { website = it })
        Spacer(Modifier.height(16.dp))

        ToggleRow("Auto-paid", autoPay) { autoPay = it }
        ToggleRow("Paused (kept, excluded from totals)", paused) { paused = it }
        ToggleRow("Free trial", isTrial) { isTrial = it }
        if (isTrial) {
            Spacer(Modifier.height(6.dp))
            LabeledTextField("Trial ends (YYYY-MM-DD)", trialEnd, { trialEnd = it })
        }

        error?.let {
            Spacer(Modifier.height(12.dp))
            Text(it, color = Palette.Negative, fontSize = 13.sp)
        }

        if (!isNew) {
            Spacer(Modifier.height(20.dp))
            Button(
                onClick = { onDelete(initial!!.id) },
                colors = ButtonDefaults.buttonColors(containerColor = Palette.SurfaceAlt),
                modifier = Modifier.fillMaxWidth(),
            ) { Text("Delete subscription", color = Palette.Negative) }
        }
        Spacer(Modifier.height(28.dp))
    }
}

@Composable
private fun ToggleRow(label: String, checked: Boolean, onChange: (Boolean) -> Unit) {
    Row(
        Modifier.fillMaxWidth().clickable { onChange(!checked) }.padding(vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Checkbox(
            checked = checked,
            onCheckedChange = onChange,
            colors = CheckboxDefaults.colors(
                checkedColor = Palette.Accent,
                uncheckedColor = Palette.Border,
                checkmarkColor = Color.White,
            ),
        )
        Spacer(Modifier.width(6.dp))
        Text(label, color = Palette.TextPrimary, fontSize = 14.sp)
    }
}
