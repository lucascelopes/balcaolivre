import java.util.Base64
import java.util.Properties

plugins {
    id("com.android.application")
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val signingProperties = Properties()
val signingPropertiesFile = rootProject.file("key.properties")
if (signingPropertiesFile.exists()) {
    signingPropertiesFile.inputStream().use(signingProperties::load)
}

fun buildSetting(propertyName: String, environmentName: String): String =
    (signingProperties.getProperty(propertyName)
        ?: providers.environmentVariable(environmentName).orNull)
        ?.trim()
        .orEmpty()

fun dartDefine(name: String): String? {
    val encodedValues = project.findProperty("dart-defines")?.toString().orEmpty()
    return encodedValues
        .split(',')
        .asSequence()
        .mapNotNull { encoded ->
            runCatching { String(Base64.getDecoder().decode(encoded)) }.getOrNull()
        }
        .mapNotNull { definition ->
            val separator = definition.indexOf('=')
            if (separator <= 0) null
            else definition.substring(0, separator) to definition.substring(separator + 1)
        }
        .firstOrNull { it.first == name }
        ?.second
}

val releaseStoreFile = buildSetting("storeFile", "AGENDA_ANDROID_KEYSTORE_PATH")
val releaseStorePassword = buildSetting("storePassword", "AGENDA_ANDROID_STORE_PASSWORD")
val releaseKeyAlias = buildSetting("keyAlias", "AGENDA_ANDROID_KEY_ALIAS")
val releaseKeyPassword = buildSetting("keyPassword", "AGENDA_ANDROID_KEY_PASSWORD")
val hasReleaseSigning = listOf(
    releaseStoreFile,
    releaseStorePassword,
    releaseKeyAlias,
    releaseKeyPassword,
).all(String::isNotEmpty)

val personalizedAppName =
    dartDefine("AGENDA_ANDROID_BUSINESS_NAME")
        ?.trim()
        ?.takeIf(String::isNotEmpty)
        ?: providers.environmentVariable("AGENDA_ANDROID_APP_NAME").orNull?.trim()
        ?: "Agenda Livre"

android {
    namespace = "br.com.balcaolivre.agenda_livre"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_17.toString()
    }

    defaultConfig {
        applicationId = "br.com.balcaolivre.agenda_livre"
        // AES/GCM credentials backed by Android Keystore are required. There
        // is deliberately no plaintext fallback on older devices.
        minSdk = maxOf(flutter.minSdkVersion, 23)
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
        resValue("string", "agenda_app_name", personalizedAppName)
    }

    signingConfigs {
        if (hasReleaseSigning) {
            create("release") {
                storeFile = rootProject.file(releaseStoreFile)
                storePassword = releaseStorePassword
                keyAlias = releaseKeyAlias
                keyPassword = releaseKeyPassword
            }
        }
    }

    buildTypes {
        release {
            // Never fall back to the debug certificate for a distributable APK.
            signingConfig = signingConfigs.findByName("release")
        }
    }
}

gradle.taskGraph.whenReady {
    val requestsRelease = allTasks.any { task ->
        task.path.contains("Release", ignoreCase = true) &&
            (task.name.startsWith("assemble", ignoreCase = true) ||
                task.name.startsWith("bundle", ignoreCase = true) ||
                task.name.startsWith("package", ignoreCase = true))
    }
    if (requestsRelease && !hasReleaseSigning) {
        throw GradleException(
            "Release signing is required. Configure android/key.properties " +
                "or the AGENDA_ANDROID_* signing environment variables.",
        )
    }
}

flutter {
    source = "../.."
}
