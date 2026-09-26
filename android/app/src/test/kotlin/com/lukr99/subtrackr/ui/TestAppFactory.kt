package com.lukr99.subtrackr.ui

import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.application.backup.BackupService
import com.lukr99.subtrackr.application.backup.FakeDocuments
import com.lukr99.subtrackr.application.sync.FakeAuthApi
import com.lukr99.subtrackr.application.sync.FakeSyncRemote
import com.lukr99.subtrackr.application.sync.InMemorySessionStore
import com.lukr99.subtrackr.application.sync.SyncCoordinator
import com.lukr99.subtrackr.application.update.FakeArtifactDownloader
import com.lukr99.subtrackr.application.update.ReleaseSource
import com.lukr99.subtrackr.application.update.UpdateService
import com.lukr99.subtrackr.composition.AppViewModelFactory
import com.lukr99.subtrackr.domain.update.UpdatePlatform
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import java.io.File
import java.time.ZoneOffset

/** The real view-model factory over in-memory data, fakes for every network seam, and a dev version. */
fun testViewModelFactory(tempDir: File): AppViewModelFactory {
    val repository = AppRepository(
        store = InMemoryDatabaseStore(SampleData.database),
        rateSource = { SampleData.rates },
        clock = SampleData.clock,
        newId = { "00000000-0000-4000-8000-000000000099" },
    )
    return AppViewModelFactory(
        repository = repository,
        updates = UpdateService(
            currentVersion = "0.3.0-dev",
            platform = UpdatePlatform.ANDROID,
            releases = ReleaseSource { error("dev builds never check") },
            downloader = FakeArtifactDownloader(emptyMap()),
            installer = {},
            directory = tempDir,
        ),
        backups = BackupService(repository, FakeDocuments(), SampleData.clock, "0.3.0-dev"),
        sync = SyncCoordinator(
            repository = repository,
            auth = FakeAuthApi(),
            remote = FakeSyncRemote(),
            sessions = InMemorySessionStore(),
            clock = SampleData.clock,
            scope = CoroutineScope(Dispatchers.Unconfined),
        ),
        zone = ZoneOffset.UTC,
    )
}
