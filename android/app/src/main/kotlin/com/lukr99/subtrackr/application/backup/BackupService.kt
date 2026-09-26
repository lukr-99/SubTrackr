package com.lukr99.subtrackr.application.backup

import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.domain.backup.BackupError
import com.lukr99.subtrackr.domain.backup.BackupFileName
import com.lukr99.subtrackr.domain.backup.BackupFormat
import com.lukr99.subtrackr.domain.backup.BackupReadResult
import com.lukr99.subtrackr.domain.backup.BackupReader
import com.lukr99.subtrackr.domain.backup.BackupRestorer
import com.lukr99.subtrackr.domain.backup.BackupWriter
import com.lukr99.subtrackr.domain.backup.RestoreMode
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.Clock
import java.time.LocalDateTime

/**
 * Backup and restore use cases (SPEC.md section 9). A restore validates the whole file, builds the
 * new database in memory, and only then saves it through the repository's atomic save; any failure
 * leaves data.json as it was.
 */
class BackupService(
    private val repository: AppRepository,
    private val documents: DocumentGateway,
    private val clock: Clock,
    private val appVersion: String,
    private val work: CoroutineDispatcher = Dispatchers.Default,
) {
    fun suggestedFileName(): String = BackupFileName.at(LocalDateTime.now(clock))

    suspend fun exportTo(uri: String): ExportReport {
        val database = repository.db
        return try {
            val text = withContext(work) { BackupWriter.write(database, clock.instant(), appVersion) }
            documents.writeText(uri, text)
            ExportReport.Saved(database.subscriptions.count { it.deletedAt.isEmpty() })
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (_: Exception) {
            ExportReport.Failed("the file could not be written")
        }
    }

    suspend fun restoreFrom(uri: String, mode: RestoreMode): RestoreReport {
        val text = try {
            documents.readText(uri, BackupFormat.MAX_BYTES)
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (_: Exception) {
            return RestoreReport.Failed("the file could not be read")
        } ?: return RestoreReport.Rejected(BackupError.INVALID_JSON)
        return restoreText(text, mode)
    }

    suspend fun restoreText(text: String, mode: RestoreMode): RestoreReport {
        val backup = when (val read = withContext(work) { BackupReader.read(text) }) {
            is BackupReadResult.Invalid -> return RestoreReport.Rejected(read.error)
            is BackupReadResult.Valid -> read.backup.database
        }
        return try {
            withContext(work) {
                repository.update { current ->
                    val outcome = BackupRestorer.restore(current, backup, mode)
                    outcome.database to RestoreReport.Restored(outcome.added, outcome.updated, outcome.unchanged, outcome.total)
                }
            }
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (_: Exception) {
            RestoreReport.Failed("the restored data could not be saved")
        }
    }
}
