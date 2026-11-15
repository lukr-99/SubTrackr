package com.lukr99.subtrackr.ui.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.lukr99.subtrackr.ui.components.SectionCard
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** The About page body: hero, author card, facts, credits. No animation, so nothing to reduce. */
@Composable
fun AboutSection(appVersion: String) {
    Column(Modifier.testTag(SettingsTags.ABOUT), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(
            "About",
            color = SubTrackrTheme.colors.textPrimary,
            fontSize = 24.sp,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.semantics { heading() },
        )
        Hero(appVersion)
        AuthorCard()
        FactsCard()
        CreditsCard()
    }
}

@Composable
private fun Hero(appVersion: String) {
    Column(
        Modifier
            .fillMaxWidth()
            .background(
                Brush.verticalGradient(listOf(SubTrackrTheme.colors.accent.copy(alpha = 0.22f), SubTrackrTheme.colors.surface)),
                RoundedCornerShape(14.dp),
            )
            .padding(20.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text("SubTrackr", color = SubTrackrTheme.colors.textPrimary, fontSize = 28.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(2.dp))
        Text("Version $appVersion", color = SubTrackrTheme.colors.accent, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(8.dp))
        Text(
            "A personal subscription tracker. See what you pay each month, in one currency, and whether it is worth it.",
            color = SubTrackrTheme.colors.textSecondary,
            fontSize = 13.sp,
            textAlign = TextAlign.Center,
        )
    }
}

@Composable
private fun AuthorCard() {
    Column(
        Modifier
            .fillMaxWidth()
            .background(
                Brush.linearGradient(listOf(SubTrackrTheme.colors.accent.copy(alpha = 0.18f), SubTrackrTheme.colors.surface)),
                RoundedCornerShape(14.dp),
            )
            .border(1.dp, SubTrackrTheme.colors.accent.copy(alpha = 0.6f), RoundedCornerShape(14.dp))
            .padding(18.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Box(
                Modifier.size(48.dp).background(SubTrackrTheme.colors.accent, CircleShape),
                contentAlignment = Alignment.Center,
            ) {
                Text(
                    AboutInfo.AUTHOR_NAME.first().toString(),
                    color = SubTrackrTheme.colors.onAccent,
                    fontSize = 22.sp,
                    fontWeight = FontWeight.Bold,
                )
            }
            Spacer(Modifier.width(14.dp))
            Column {
                Text("Made by", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
                Text(AboutInfo.AUTHOR_NAME, color = SubTrackrTheme.colors.textPrimary, fontSize = 20.sp, fontWeight = FontWeight.Bold)
            }
        }
        Spacer(Modifier.height(12.dp))
        Text(
            "Feedback is welcome: what works, what does not, what is missing.",
            color = SubTrackrTheme.colors.textPrimary,
            fontSize = 13.sp,
        )
        Spacer(Modifier.height(12.dp))
        LinkButton("Instagram", AboutInfo.INSTAGRAM_HANDLE, AboutInfo.INSTAGRAM_URL)
        Spacer(Modifier.height(8.dp))
        LinkButton("GitHub", AboutInfo.GITHUB_HANDLE, AboutInfo.GITHUB_URL)
        Spacer(Modifier.height(8.dp))
        LinkButton("Report an issue", "SubTrackr on GitHub", AboutInfo.ISSUES_URL)
    }
}

@Composable
private fun LinkButton(title: String, detail: String, url: String) {
    val uri = LocalUriHandler.current
    Row(
        Modifier
            .fillMaxWidth()
            .background(SubTrackrTheme.colors.surfaceAlt, RoundedCornerShape(10.dp))
            .border(1.dp, SubTrackrTheme.colors.border, RoundedCornerShape(10.dp))
            .clickable(role = Role.Button) { runCatching { uri.openUri(url) } }
            .padding(horizontal = 14.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(title, color = SubTrackrTheme.colors.textPrimary, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.weight(1f))
        Text(detail, color = SubTrackrTheme.colors.accent, fontSize = 13.sp)
    }
}

@Composable
private fun FactsCard() {
    SectionCard {
        Text("The app", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        Spacer(Modifier.height(6.dp))
        Text(
            "Multi-currency totals, charts, a worth-it check, free-trial tracking and optional sync across your devices. " +
                "It shares its data contract with the desktop app.",
            color = SubTrackrTheme.colors.textPrimary,
            fontSize = 13.sp,
        )
    }
}

@Composable
private fun CreditsCard() {
    val uri = LocalUriHandler.current
    SectionCard {
        Text("Credits and licences", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        Spacer(Modifier.height(6.dp))
        Text(
            "SubTrackr is licensed under ${AboutInfo.LICENSE_NAME}. Free for noncommercial use.",
            color = SubTrackrTheme.colors.textPrimary,
            fontSize = 13.sp,
            modifier = Modifier.clickable { runCatching { uri.openUri(AboutInfo.LICENSE_URL) } },
        )
        Spacer(Modifier.height(10.dp))
        Text("Libraries", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        AboutInfo.libraries.forEach { (name, licence) -> CreditRow(name, licence) }
        Spacer(Modifier.height(10.dp))
        Text("Services", color = SubTrackrTheme.colors.textSecondary, fontSize = 12.sp)
        AboutInfo.services.forEach { (name, use) -> CreditRow(name, use) }
    }
}

@Composable
private fun CreditRow(name: String, note: String) {
    Row(Modifier.fillMaxWidth().padding(top = 4.dp)) {
        Text(name, color = SubTrackrTheme.colors.textPrimary, fontSize = 12.sp, modifier = Modifier.weight(1f))
        Spacer(Modifier.width(8.dp))
        Text(note, color = SubTrackrTheme.colors.textMuted, fontSize = 12.sp)
    }
}
