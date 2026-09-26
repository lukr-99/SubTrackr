package com.lukr99.subtrackr

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.lukr99.subtrackr.ui.SubTrackrApp
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** The only activity. It takes its object graph from the application's composition root. */
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val container = (application as SubTrackrApplication).container
        setContent {
            SubTrackrTheme {
                SubTrackrApp(viewModelFactory = container.viewModelFactory)
            }
        }
    }
}
