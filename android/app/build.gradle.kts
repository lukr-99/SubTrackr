import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.kotlin.serialization)
    alias(libs.plugins.roborazzi)
}

val keystorePropertiesFile = rootProject.file("keystore.properties")
val keystoreProperties = Properties().apply {
    if (keystorePropertiesFile.exists()) keystorePropertiesFile.inputStream().use(::load)
}
if (gradle.startParameter.taskNames.any { it.contains("release", ignoreCase = true) } &&
    !keystorePropertiesFile.exists()
) {
    throw GradleException("Release signing requires android/keystore.properties and its referenced keystore.")
}

// One version for both apps lives in the root Version.props (contracts/SPEC.md section 7).
val subTrackrVersion: String =
    Regex("""<SubTrackrVersion>(\d+\.\d+\.\d+)</SubTrackrVersion>""")
        .find(rootProject.file("../Version.props").readText())?.groupValues?.get(1)
        ?: throw GradleException("Version.props has no X.Y.Z SubTrackrVersion.")
val subTrackrVersionCode: Int = subTrackrVersion.split('.').map(String::toInt)
    .let { (major, minor, patch) -> major * 10000 + minor * 100 + patch }

android {
    namespace = "com.lukr99.subtrackr"
    compileSdk = 35
    // Match the SDK's installed build-tools (34/35 present).
    buildToolsVersion = "35.0.0"

    defaultConfig {
        applicationId = "com.lukr99.subtrackr"
        minSdk = 26
        targetSdk = 35
        versionCode = subTrackrVersionCode
        versionName = subTrackrVersion
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    signingConfigs {
        if (keystoreProperties.isNotEmpty()) {
            create("release") {
                storeFile = rootProject.file(keystoreProperties.getProperty("storeFile"))
                storePassword = keystoreProperties.getProperty("storePassword")
                keyAlias = keystoreProperties.getProperty("keyAlias")
                keyPassword = keystoreProperties.getProperty("keyPassword")
            }
        }
    }
    buildTypes {
        debug {
            // Debug builds install beside the release and never touch its data or updates.
            applicationIdSuffix = ".dev"
            versionNameSuffix = "-dev"
        }
        release {
            isMinifyEnabled = false
            signingConfig = signingConfigs.findByName("release")
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions {
        jvmTarget = "17"
    }
    buildFeatures {
        compose = true
        // BuildConfig.VERSION_NAME carries the -dev suffix that keeps debug builds off the updater.
        buildConfig = true
    }

    sourceSets {
        // Kotlin sources live under src/main/kotlin. The shared design tokens ship as a Java
        // resource so the app reads contracts/design/tokens.json itself, not a copy. Behaviour
        // golden vectors go on the unit-test classpath so Android runs the same fixtures as WPF.
        getByName("main") {
            java.srcDir("src/main/kotlin")
            resources.srcDir("../../contracts/design")
        }
        getByName("test") {
            java.srcDir("src/test/kotlin")
            resources.srcDir("../../contracts/vectors")
        }
    }
    testOptions {
        // Robolectric and Roborazzi render real resources on the JVM.
        unitTests.isIncludeAndroidResources = true
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)

    val composeBom = platform(libs.compose.bom)
    implementation(composeBom)
    implementation(libs.compose.ui)
    implementation(libs.compose.ui.graphics)
    implementation(libs.compose.ui.tooling.preview)
    implementation(libs.compose.material3)
    implementation(libs.compose.material.icons.extended)
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(libs.androidx.lifecycle.runtime.compose)
    implementation(libs.androidx.navigation.compose)
    debugImplementation(libs.compose.ui.tooling)
    debugImplementation(libs.compose.ui.test.manifest)

    implementation(libs.kotlinx.serialization.json)
    implementation(libs.kotlinx.coroutines.android)
    implementation(libs.okhttp)
    implementation(libs.coil.compose)

    testImplementation(libs.junit)
    testImplementation(libs.kotlinx.serialization.json)
    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(libs.okhttp.mockwebserver)
    testImplementation(composeBom)
    testImplementation(libs.compose.ui.test.junit4)
    testImplementation(libs.robolectric)
    testImplementation(libs.roborazzi)
    testImplementation(libs.roborazzi.compose)
}
