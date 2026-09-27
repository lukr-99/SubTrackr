package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
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
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Trials ending and renewals due soon, at the top of the dashboard. */
@Composable
internal fun AlertsCard(alerts: List<DashboardAlert>) {
    Column(
        Modifier.fillMaxWidth().background(SubTrackrTheme.colors.surface, RoundedCornerShape(14.dp)).padding(16.dp),
    ) {
        Text("⏰ ALERTS", color = SubTrackrTheme.colors.warning, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        alerts.forEach { a ->
            Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                Text(a.icon, fontSize = 15.sp)
                Spacer(Modifier.width(8.dp))
                Text(
                    a.title,
                    color = SubTrackrTheme.colors.textPrimary,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.weight(1f),
                )
                Text(a.detail, color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
            }
        }
    }
}
