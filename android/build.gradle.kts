// Root build file — plugin versions come from gradle/libs.versions.toml, applied in :app.
plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.kotlin.compose) apply false
    alias(libs.plugins.kotlin.serialization) apply false
    alias(libs.plugins.roborazzi) apply false
    alias(libs.plugins.spotless)
}

// Kotlin formatting: ktlint with the settings in android/.editorconfig. CI runs spotlessCheck;
// spotlessApply fixes whatever ktlint can fix on its own. Spotless applies ktlint's rule switches
// (ktlint_standard_<rule> = disabled) only as overrides, so they are read from the same file.
val ktlintRuleSwitches: Map<String, String> = rootProject.file(".editorconfig").readLines()
    .map(String::trim)
    .filter { it.startsWith("ktlint_standard_") }
    .associate { it.substringBefore('=').trim() to it.substringAfter('=').trim() }

spotless {
    kotlin {
        target("app/src/**/*.kt")
        ktlint(libs.versions.ktlint.get()).editorConfigOverride(ktlintRuleSwitches)
    }
    kotlinGradle {
        target("*.gradle.kts", "app/*.gradle.kts")
        ktlint(libs.versions.ktlint.get()).editorConfigOverride(ktlintRuleSwitches)
    }
}
