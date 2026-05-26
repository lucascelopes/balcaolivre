package com.balcaolivre.pdv.mobile.data

import com.balcaolivre.pdv.mobile.model.ActivationResponse
import com.balcaolivre.pdv.mobile.model.AdminClientPayload
import org.json.JSONObject
import java.io.IOException
import java.net.HttpURLConnection
import java.net.URL

class AdminApiClient {
    fun activate(baseUrl: String, payload: AdminClientPayload): ActivationResponse {
        val json = postJson(baseUrl, "/api/app/activate", payload.toJson())
        return ActivationResponse.fromJson(json)
    }

    fun checkIn(baseUrl: String, payload: AdminClientPayload): Boolean {
        val json = postJson(baseUrl, "/api/app/checkin", payload.toJson())
        return json.optBoolean("ok", false)
    }

    private fun postJson(baseUrl: String, path: String, body: JSONObject): JSONObject {
        val url = URL(normalizeBaseUrl(baseUrl) + path)
        val connection = (url.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            connectTimeout = 15000
            readTimeout = 20000
            doOutput = true
            setRequestProperty("Content-Type", "application/json; charset=utf-8")
            setRequestProperty("Accept", "application/json")
        }

        return try {
            connection.outputStream.use { stream ->
                stream.write(body.toString().toByteArray(Charsets.UTF_8))
            }

            val code = connection.responseCode
            val responseText = readResponse(connection, code)
            if (code !in 200..299) {
                throw IOException(extractErrorMessage(responseText, code))
            }

            if (responseText.isBlank()) JSONObject() else JSONObject(responseText)
        } finally {
            connection.disconnect()
        }
    }

    private fun normalizeBaseUrl(value: String): String {
        val trimmed = value.trim().trimEnd('/')
        if (trimmed.isBlank()) {
            throw IOException("Informe a URL do admin.")
        }

        return if (trimmed.startsWith("http://") || trimmed.startsWith("https://")) {
            trimmed
        } else {
            "https://$trimmed"
        }
    }

    private fun readResponse(connection: HttpURLConnection, code: Int): String {
        val stream = if (code in 200..299) connection.inputStream else connection.errorStream
        return stream?.bufferedReader(Charsets.UTF_8)?.use { it.readText() }.orEmpty()
    }

    private fun extractErrorMessage(responseText: String, code: Int): String {
        val message = runCatching {
            JSONObject(responseText).optString("message")
        }.getOrNull()

        return message?.takeIf { it.isNotBlank() } ?: "Admin retornou HTTP $code."
    }
}
