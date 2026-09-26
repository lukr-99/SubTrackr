package com.lukr99.subtrackr

import android.app.Application
import com.lukr99.subtrackr.composition.AppContainer

/** Process entry point. Owns the one [AppContainer] for the app's lifetime. */
class SubTrackrApplication : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        container = AppContainer(this)
    }
}
