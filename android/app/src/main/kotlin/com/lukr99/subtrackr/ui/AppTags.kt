package com.lukr99.subtrackr.ui

/**
 * Stable test tags for the app shell. With testTagsAsResourceId on the root they surface as
 * resource ids, so Compose tests, `uiautomator dump`, and Maestro `id:` selectors share one name.
 */
object AppTags {
    const val NAV_DASHBOARD = "nav_dashboard"
    const val NAV_WHATIF = "nav_whatif"
    const val NAV_SETTINGS = "nav_settings"
    const val ADD = "add_subscription"
}
