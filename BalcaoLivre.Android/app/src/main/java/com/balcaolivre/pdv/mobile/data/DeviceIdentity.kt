package com.balcaolivre.pdv.mobile.data

import android.content.Context
import android.os.Build
import android.provider.Settings
import java.security.MessageDigest
import java.util.Locale

object DeviceIdentity {
    fun machineHash(context: Context): String {
        val androidId = Settings.Secure.getString(
            context.contentResolver,
            Settings.Secure.ANDROID_ID
        ).orEmpty()
        val raw = listOf(
            "android",
            context.packageName,
            androidId,
            Build.MANUFACTURER.orEmpty(),
            Build.MODEL.orEmpty()
        ).joinToString("|")

        return sha256(raw)
    }

    fun machineCode(context: Context): String {
        val hash = machineHash(context)
        val maker = clean(Build.MANUFACTURER.orEmpty(), 4)
        val model = clean(Build.MODEL.orEmpty(), 8)
        return "AND-$maker-$model-${hash.takeLast(6).uppercase(Locale.US)}"
    }

    private fun clean(value: String, maxLength: Int): String {
        return value
            .uppercase(Locale.US)
            .replace(Regex("[^A-Z0-9]+"), "")
            .take(maxLength)
            .ifBlank { "DEVICE" }
    }

    private fun sha256(value: String): String {
        val bytes = MessageDigest.getInstance("SHA-256")
            .digest(value.toByteArray(Charsets.UTF_8))
        return bytes.joinToString("") { "%02x".format(it) }
    }
}
