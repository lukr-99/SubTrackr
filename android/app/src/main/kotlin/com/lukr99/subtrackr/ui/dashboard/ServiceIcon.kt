package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.lukr99.subtrackr.model.Subscription

/**
 * A subscription's service logo, the favicon of its website (SPEC.md section 12), or its emoji.
 * With [showLogos] off, or without a website, Coil is never asked for an image. The emoji also
 * shows while the logo loads or when it fails.
 */
@Composable
internal fun ServiceIcon(subscription: Subscription, showLogos: Boolean) {
    val url = if (showLogos) faviconUrl(subscription.website) else null
    var logoShown by remember(url) { mutableStateOf(false) }
    Box(Modifier.size(24.dp), contentAlignment = Alignment.Center) {
        if (!logoShown) {
            Text(subscription.iconRef.ifBlank { "•" }, fontSize = 20.sp, modifier = Modifier.testTag(DashboardTags.SERVICE_EMOJI))
        }
        if (url != null) {
            AsyncImage(
                model = url,
                contentDescription = null,
                onSuccess = { logoShown = true },
                onError = { logoShown = false },
                modifier = Modifier.size(22.dp).testTag(DashboardTags.SERVICE_LOGO),
            )
        }
    }
}

/** Google's favicon service for [website]; null when there is no website to ask about. */
internal fun faviconUrl(website: String): String? =
    website.trim().takeIf { it.isNotEmpty() }?.let { "https://www.google.com/s2/favicons?domain=$it&sz=64" }
