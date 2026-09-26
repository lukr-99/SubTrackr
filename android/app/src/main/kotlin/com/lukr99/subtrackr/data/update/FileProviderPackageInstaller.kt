package com.lukr99.subtrackr.data.update

import android.content.Context
import android.content.Intent
import androidx.core.content.FileProvider
import com.lukr99.subtrackr.application.update.PackageInstaller
import java.io.File

/**
 * Opens a verified APK in the system package installer through the app's FileProvider. The system
 * asks the user and rejects an APK signed with a different key.
 */
class FileProviderPackageInstaller(
    private val context: Context,
    private val authority: String,
) : PackageInstaller {

    override fun install(file: File) {
        val uri = FileProvider.getUriForFile(context, authority, file)
        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(uri, "application/vnd.android.package-archive")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        context.startActivity(intent)
    }
}
