package com.lukr99.subtrackr

import android.app.Application
import com.lukr99.subtrackr.composition.AppContainer
import com.lukr99.subtrackr.data.diagnostics.CrashLog

/** Process entry point. Owns the one [AppContainer] for the app's lifetime. */
class SubTrackrApplication : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        // First, so a crash while the container is built is written down too.
        CrashLog.install(filesDir, BuildConfig.VERSION_NAME)
        container = AppContainer(this)
    }
}
