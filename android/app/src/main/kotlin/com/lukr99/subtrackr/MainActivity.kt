package com.lukr99.subtrackr

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.lukr99.subtrackr.data.SubscriptionStore
import com.lukr99.subtrackr.domain.OfflineFallback
import com.lukr99.subtrackr.domain.SpendCalculator
import com.lukr99.subtrackr.domain.WorthIt
import com.lukr99.subtrackr.ui.DashboardScreen
import com.lukr99.subtrackr.ui.SubTrackrTheme
import java.io.File

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val store = SubscriptionStore(File(filesDir, "data.json"))
        val db = store.load()
        val base = db.settings.baseCurrency
        val rates = OfflineFallback.forAnchor(base)
        val summary = SpendCalculator.summarize(db.subscriptions, base, rates, WorthIt.DEFAULT_THRESHOLD)

        setContent {
            SubTrackrTheme {
                DashboardScreen(summary)
            }
        }
    }
}
