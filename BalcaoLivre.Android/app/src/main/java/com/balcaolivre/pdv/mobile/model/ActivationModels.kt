package com.balcaolivre.pdv.mobile.model

import org.json.JSONObject

data class RestaurantProfile(
    val ownerName: String = "",
    val businessName: String = "",
    val legalName: String = "",
    val cnpj: String = "",
    val phone: String = "",
    val address: String = "",
    val city: String = "",
    val state: String = ""
) {
    val displayName: String
        get() = businessName.ifBlank { legalName.ifBlank { ownerName.ifBlank { "Restaurante" } } }

    fun toJson(): JSONObject = JSONObject()
        .put("ownerName", ownerName)
        .put("businessName", businessName)
        .put("legalName", legalName)
        .put("cnpj", cnpj)
        .put("phone", phone)
        .put("address", address)
        .put("city", city)
        .put("state", state)
}

data class MobileAppSettings(
    val adminSyncEnabled: Boolean = true
) {
    fun toJson(): JSONObject = JSONObject()
        .put("windowsNotificationsEnabled", false)
        .put("notificationSoundEnabled", true)
        .put("inAppVibrationEnabled", true)
        .put("notificationSound", "PADRAO")
        .put("autoPrintDelivery", false)
        .put("autoPrintKitchen", false)
        .put("printLayout", "MOBILE")
        .put("preferredPrinterName", "")
        .put("receiptQrEnabled", false)
        .put("receiptQrKind", "")
        .put("autoCheckUpdates", false)
        .put("adminSyncEnabled", adminSyncEnabled)
        .put("supabaseAuthEnabled", false)
        .put("supabaseUrlConfigured", false)
        .put("supabaseUserEmail", "")
}

data class AppMetricsSnapshot(
    val tablesCount: Int = 0,
    val openBoardsCount: Int = 0,
    val deliveryCount: Int = 0,
    val productsCount: Int = 0,
    val usersCount: Int = 0,
    val customersCount: Int = 0,
    val lowStockCount: Int = 0
) {
    fun toJson(): JSONObject = JSONObject()
        .put("tablesCount", tablesCount)
        .put("openBoardsCount", openBoardsCount)
        .put("deliveryCount", deliveryCount)
        .put("productsCount", productsCount)
        .put("usersCount", usersCount)
        .put("customersCount", customersCount)
        .put("lowStockCount", lowStockCount)
}

data class AdminClientPayload(
    val eventName: String,
    val licenseKey: String,
    val machineHash: String,
    val machineCode: String,
    val clientKind: String = "android",
    val appVersion: String,
    val profile: RestaurantProfile,
    val localExpiresAt: String? = null,
    val localPlan: String = "",
    val settings: MobileAppSettings = MobileAppSettings(),
    val metrics: AppMetricsSnapshot = AppMetricsSnapshot()
) {
    fun toJson(): JSONObject = JSONObject()
        .put("eventName", eventName)
        .put("licenseKey", licenseKey)
        .put("machineHash", machineHash)
        .put("machineCode", machineCode)
        .put("clientKind", clientKind)
        .put("appVersion", appVersion)
        .put("localExpiresAt", localExpiresAt ?: JSONObject.NULL)
        .put("localPlan", localPlan)
        .put("profile", profile.toJson())
        .put("settings", settings.toJson())
        .put("metrics", metrics.toJson())
}

data class ActivationResponse(
    val ok: Boolean,
    val message: String,
    val plan: String = "",
    val expiresAt: String? = null
) {
    companion object {
        fun fromJson(json: JSONObject): ActivationResponse = ActivationResponse(
            ok = json.optBoolean("ok", false),
            message = json.optString("message"),
            plan = json.optString("plan"),
            expiresAt = json.optString("expiresAt").ifBlank { null }
        )
    }
}

data class RestaurantSession(
    val adminBaseUrl: String,
    val licenseKey: String,
    val machineHash: String,
    val machineCode: String,
    val appVersion: String,
    val plan: String,
    val expiresAt: String?,
    val profile: RestaurantProfile
)
