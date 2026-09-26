package com.lukr99.subtrackr.composition

import android.content.Context
import androidx.lifecycle.ViewModelProvider
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.data.SubscriptionStore
import com.lukr99.subtrackr.data.rates.FrankfurterRateSource
import com.lukr99.subtrackr.data.sync.SupabaseSyncProvider
import com.lukr99.subtrackr.data.update.AppUpdater
import okhttp3.OkHttpClient
import java.io.File
import java.time.Clock
import java.util.UUID
import java.util.concurrent.TimeUnit

/**
 * Composition root: picks the adapters, reads platform configuration, and owns every long-lived
 * object. Nothing below this class looks services up; they arrive through constructors.
 */
class AppContainer(context: Context) {
    private val appContext = context.applicationContext

    private val ratesHttp: OkHttpClient = OkHttpClient.Builder()
        .callTimeout(8, TimeUnit.SECONDS)
        .build()
    private val syncHttp: OkHttpClient = OkHttpClient()

    val repository: AppRepository = AppRepository(
        store = SubscriptionStore(File(appContext.filesDir, "data.json")),
        rateSource = FrankfurterRateSource(ratesHttp),
        syncProviders = { url, key -> SupabaseSyncProvider(url, key, syncHttp) },
        clock = Clock.systemUTC(),
        newId = { UUID.randomUUID().toString() },
    )

    val updater: AppUpdater = AppUpdater(appContext)

    val viewModelFactory: ViewModelProvider.Factory = AppViewModelFactory(repository)
}
