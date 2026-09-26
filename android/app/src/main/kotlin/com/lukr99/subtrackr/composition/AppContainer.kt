package com.lukr99.subtrackr.composition

import android.content.Context
import androidx.lifecycle.ViewModelProvider
import com.lukr99.subtrackr.BuildConfig
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.sync.SyncCoordinator
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.data.SubscriptionStore
import com.lukr99.subtrackr.data.backup.ContentResolverDocuments
import com.lukr99.subtrackr.data.rates.FrankfurterRateSource
import com.lukr99.subtrackr.data.sync.AesGcmSessionCipher
import com.lukr99.subtrackr.data.sync.AndroidKeystoreKey
import com.lukr99.subtrackr.data.sync.EncryptedSessionStore
import com.lukr99.subtrackr.data.sync.SupabaseAuthApi
import com.lukr99.subtrackr.data.sync.SupabaseSyncRemote
import com.lukr99.subtrackr.data.update.FileProviderPackageInstaller
import com.lukr99.subtrackr.data.update.GitHubReleaseSource
import com.lukr99.subtrackr.data.update.OkHttpArtifactDownloader
import com.lukr99.subtrackr.domain.update.UpdatePlatform
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
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
    private val clock: Clock = Clock.systemDefaultZone()

    /** Lives as long as the process; sync passes and logout calls run here. */
    private val appScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private val ratesHttp: OkHttpClient = OkHttpClient.Builder()
        .callTimeout(8, TimeUnit.SECONDS)
        .build()

    /** Every sync and auth request times out after 15 seconds (SPEC.md section 8.4). */
    private val syncHttp: OkHttpClient = OkHttpClient.Builder()
        .followSslRedirects(false)
        .connectTimeout(SYNC_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .readTimeout(SYNC_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .writeTimeout(SYNC_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .callTimeout(SYNC_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .build()

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
        clock = clock,
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

    val backups: BackupService = BackupService(
        repository = repository,
        documents = ContentResolverDocuments(appContext.contentResolver),
        clock = clock,
        appVersion = BuildConfig.VERSION_NAME,
    )

    /**
     * The session file sits in noBackupFilesDir, apart from data.json, sealed with a Keystore key.
     * Only debug builds accept plain HTTP to a local Supabase stack.
     */
    val sync: SyncCoordinator = SyncCoordinator(
        repository = repository,
        auth = SupabaseAuthApi(syncHttp),
        remote = SupabaseSyncRemote(syncHttp),
        sessions = EncryptedSessionStore(
            File(appContext.noBackupFilesDir, "sync-session.bin"),
            AesGcmSessionCipher { AndroidKeystoreKey.aesGcm(SESSION_KEY_ALIAS) },
        ),
        clock = clock,
        scope = appScope,
        allowLocalHttp = BuildConfig.DEBUG,
    ).also { it.start() }

    val viewModelFactory: ViewModelProvider.Factory =
        AppViewModelFactory(repository, updates, backups, sync, clock.zone)

    private companion object {
        const val SYNC_TIMEOUT_SECONDS = 15L
        const val SESSION_KEY_ALIAS = "subtrackr-sync-session"
    }
}
