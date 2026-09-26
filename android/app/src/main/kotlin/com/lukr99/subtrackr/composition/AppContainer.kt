package com.lukr99.subtrackr.composition

import android.content.Context
import androidx.lifecycle.ViewModelProvider
import com.lukr99.subtrackr.BuildConfig
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.data.SubscriptionStore
import com.lukr99.subtrackr.data.rates.FrankfurterRateSource
import com.lukr99.subtrackr.data.sync.SupabaseSyncProvider
import com.lukr99.subtrackr.data.update.FileProviderPackageInstaller
import com.lukr99.subtrackr.data.update.GitHubReleaseSource
import com.lukr99.subtrackr.data.update.OkHttpArtifactDownloader
import com.lukr99.subtrackr.domain.update.UpdatePlatform
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

    /** Release downloads may take minutes; they never follow an HTTPS-to-HTTP redirect. */
    private val downloadHttp: OkHttpClient = OkHttpClient.Builder()
        .followSslRedirects(false)
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .callTimeout(10, TimeUnit.MINUTES)
        .build()
    private val releaseApiHttp: OkHttpClient = downloadHttp.newBuilder()
        .callTimeout(15, TimeUnit.SECONDS)
        .build()

    val repository: AppRepository = AppRepository(
        store = SubscriptionStore(File(appContext.filesDir, "data.json")),
        rateSource = FrankfurterRateSource(ratesHttp),
        syncProviders = { url, key -> SupabaseSyncProvider(url, key, syncHttp) },
        clock = Clock.systemUTC(),
        newId = { UUID.randomUUID().toString() },
    )

    /** Debug builds are `X.Y.Z-dev`, so their UpdateService never contacts GitHub. */
    val updates: UpdateService = UpdateService(
        currentVersion = BuildConfig.VERSION_NAME,
        platform = UpdatePlatform.ANDROID,
        releases = GitHubReleaseSource(releaseApiHttp),
        downloader = OkHttpArtifactDownloader(downloadHttp),
        installer = FileProviderPackageInstaller(appContext, "${appContext.packageName}.files"),
        directory = File(appContext.cacheDir, "updates"),
    )

    val viewModelFactory: ViewModelProvider.Factory = AppViewModelFactory(repository, updates)
}
