package br.com.balcaolivre.agenda_livre

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodCall
import io.flutter.plugin.common.MethodChannel
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

class MainActivity : FlutterActivity() {
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        val storage = KeystoreCredentialStorage(this)
        MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            SECURE_STORAGE_CHANNEL,
        ).setMethodCallHandler { call, result ->
            try {
                when (call.method) {
                    "read" -> result.success(storage.read(call.requiredKey()))
                    "write" -> {
                        val value = call.argument<String>("value")
                            ?: throw IllegalArgumentException("Missing value")
                        storage.write(call.requiredKey(), value)
                        result.success(null)
                    }
                    "delete" -> {
                        storage.delete(call.requiredKey())
                        result.success(null)
                    }
                    else -> result.notImplemented()
                }
            } catch (error: Throwable) {
                result.error(
                    "secure_storage_failed",
                    "Não foi possível acessar a credencial segura deste aparelho.",
                    error.javaClass.simpleName,
                )
            }
        }
    }

    private fun MethodCall.requiredKey(): String {
        val value = argument<String>("key")?.trim().orEmpty()
        require(value.matches(Regex("[a-zA-Z0-9_.-]{1,80}"))) { "Invalid key" }
        return value
    }

    companion object {
        private const val SECURE_STORAGE_CHANNEL = "agenda_livre/secure_storage"
    }
}

private class KeystoreCredentialStorage(context: Context) {
    private val preferences = context.getSharedPreferences(
        "agenda_livre_secure_credentials_v1",
        Context.MODE_PRIVATE,
    )

    fun read(key: String): String? {
        val encodedIv = preferences.getString("$key.iv", null) ?: return null
        val encodedPayload = preferences.getString("$key.payload", null) ?: return null
        return try {
            val cipher = Cipher.getInstance(TRANSFORMATION)
            cipher.init(
                Cipher.DECRYPT_MODE,
                secretKey(),
                GCMParameterSpec(GCM_TAG_LENGTH_BITS, Base64.decode(encodedIv, Base64.NO_WRAP)),
            )
            val clear = cipher.doFinal(Base64.decode(encodedPayload, Base64.NO_WRAP))
            clear.toString(Charsets.UTF_8)
        } catch (_: Throwable) {
            // A restored backup cannot reuse a hardware-bound Keystore key.
            // Remove only the unreadable entry so the app can request a new
            // provisioning package instead of ever falling back to plaintext.
            delete(key)
            null
        }
    }

    fun write(key: String, value: String) {
        require(value.isNotEmpty()) { "Empty values are not allowed" }
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.ENCRYPT_MODE, secretKey())
        val payload = cipher.doFinal(value.toByteArray(Charsets.UTF_8))
        check(
            preferences.edit()
                .putString("$key.iv", Base64.encodeToString(cipher.iv, Base64.NO_WRAP))
                .putString("$key.payload", Base64.encodeToString(payload, Base64.NO_WRAP))
                .commit(),
        ) { "Credential write failed" }
    }

    fun delete(key: String) {
        preferences.edit().remove("$key.iv").remove("$key.payload").commit()
    }

    private fun secretKey(): SecretKey {
        val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE).apply { load(null) }
        (keyStore.getKey(KEY_ALIAS, null) as? SecretKey)?.let { return it }
        val generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEYSTORE)
        generator.init(
            KeyGenParameterSpec.Builder(
                KEY_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT,
            )
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setRandomizedEncryptionRequired(true)
                .build(),
        )
        return generator.generateKey()
    }

    companion object {
        private const val ANDROID_KEYSTORE = "AndroidKeyStore"
        private const val KEY_ALIAS = "agenda_livre_device_credentials_v1"
        private const val TRANSFORMATION = "AES/GCM/NoPadding"
        private const val GCM_TAG_LENGTH_BITS = 128
    }
}
