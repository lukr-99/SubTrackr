package com.lukr99.subtrackr.ui

/** Author name and links shown on the About page. Single source: change them here only. */
object AboutInfo {
    const val AUTHOR_NAME = "Lukáš Krejčí"
    const val INSTAGRAM_URL = "https://www.instagram.com/lukas9krejci/"
    const val INSTAGRAM_HANDLE = "@lukas9krejci"
    const val GITHUB_URL = "https://github.com/lukr-99"
    const val GITHUB_HANDLE = "lukr-99"
    const val REPO_URL = "https://github.com/lukr-99/SubTrackr"
    const val ISSUES_URL = "$REPO_URL/issues"
    const val LICENSE_NAME = "PolyForm Noncommercial 1.0.0"
    const val LICENSE_URL = "https://polyformproject.org/licenses/noncommercial/1.0.0/"

    /** Libraries this app ships, as (name, licence). Taken from android/gradle/libs.versions.toml. */
    val libraries: List<Pair<String, String>> = listOf(
        "Jetpack Compose and Material 3" to "Apache 2.0",
        "AndroidX (Core, Activity, Lifecycle, Navigation)" to "Apache 2.0",
        "Coil" to "Apache 2.0",
        "OkHttp" to "Apache 2.0",
        "kotlinx.serialization and coroutines" to "Apache 2.0",
    )

    /** Outside services the app talks to, as (name, what for). */
    val services: List<Pair<String, String>> = listOf(
        "Frankfurter" to "exchange rates",
        "Google favicon service" to "service logos",
        "Supabase" to "optional sync",
    )
}
