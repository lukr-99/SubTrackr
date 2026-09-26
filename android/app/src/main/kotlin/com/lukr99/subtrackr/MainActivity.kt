package com.lukr99.subtrackr

import android.animation.ValueAnimator
import android.graphics.Color
import android.os.Build
import android.os.Bundle
import android.os.Process
import android.os.SystemClock
import androidx.activity.ComponentActivity
import androidx.activity.SystemBarStyle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.DisposableEffect
import androidx.core.splashscreen.SplashScreen
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.lifecycle.viewmodel.compose.viewModel
import com.lukr99.subtrackr.ui.SubTrackrApp
import com.lukr99.subtrackr.ui.SubTrackrViewModel
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import com.lukr99.subtrackr.ui.theme.isDarkTheme

/** The only activity. It takes its object graph from the application's composition root. */
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        // Before super.onCreate: the splash theme hands over to Theme.SubTrackr here.
        val splash = installSplashScreen()
        super.onCreate(savedInstanceState)
        keepSplashForLaunchAnimation(splash)
        enableEdgeToEdge()
        val container = (application as SubTrackrApplication).container
        setContent {
            val app: SubTrackrViewModel = viewModel(factory = container.viewModelFactory)
            val dark = isDarkTheme(app.themeMode)
            // Status and navigation bar icons follow the app theme, not only the system setting.
            DisposableEffect(dark) {
                enableEdgeToEdge(
                    statusBarStyle = SystemBarStyle.auto(Color.TRANSPARENT, Color.TRANSPARENT) { dark },
                    navigationBarStyle = SystemBarStyle.auto(LIGHT_SCRIM, DARK_SCRIM) { dark },
                )
                onDispose {}
            }
            SubTrackrTheme(darkTheme = dark) {
                SubTrackrApp(viewModelFactory = container.viewModelFactory)
            }
        }
    }

    /**
     * Keeps the Android 12+ splash screen up until the logo's bars have finished growing, counted
     * from process start, so a fast cold start does not cut the animation short. A warm start or
     * animations turned off (reduced motion) never waits.
     */
    private fun keepSplashForLaunchAnimation(splash: SplashScreen) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S || !ValueAnimator.areAnimatorsEnabled()) return
        val animationEnd = Process.getStartUptimeMillis() + resources.getInteger(R.integer.splash_animation_ms)
        splash.setKeepOnScreenCondition { SystemClock.uptimeMillis() < animationEnd }
    }

    private companion object {
        // The scrims androidx.activity uses by default for three-button navigation.
        val LIGHT_SCRIM = Color.argb(0xe6, 0xFF, 0xFF, 0xFF)
        val DARK_SCRIM = Color.argb(0x80, 0x1b, 0x1b, 0x1b)
    }
}
