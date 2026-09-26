package com.lukr99.subtrackr.ui.settings

import com.lukr99.subtrackr.model.ThemeMode

/** Stable test tags for the Settings screen, exposed as resource ids for Maestro and uiautomator. */
object SettingsTags {
    const val ROOT = "settings"
    const val UPDATE_STATUS = "settings_update_status"
    const val CHECK_UPDATES = "settings_check_updates"

    fun theme(mode: ThemeMode): String = "settings_theme_${mode.name.lowercase()}"
}
