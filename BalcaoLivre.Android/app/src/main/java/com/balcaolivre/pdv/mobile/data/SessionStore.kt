package com.balcaolivre.pdv.mobile.data

import android.content.Context
import com.balcaolivre.pdv.mobile.DEFAULT_ADMIN_API_URL
import com.balcaolivre.pdv.mobile.model.RestaurantProfile
import com.balcaolivre.pdv.mobile.model.RestaurantSession

class SessionStore(context: Context) {
    private val prefs = context.getSharedPreferences("balcao_livre_mobile_session", Context.MODE_PRIVATE)

    fun load(): RestaurantSession? {
        if (!prefs.getBoolean("active", false)) return null

        val licenseKey = prefs.getString("licenseKey", "").orEmpty()
        val machineHash = prefs.getString("machineHash", "").orEmpty()
        val machineCode = prefs.getString("machineCode", "").orEmpty()
        if (licenseKey.isBlank() || machineHash.isBlank()) return null

        return RestaurantSession(
            adminBaseUrl = prefs.getString("adminBaseUrl", DEFAULT_ADMIN_API_URL).orEmpty(),
            licenseKey = licenseKey,
            machineHash = machineHash,
            machineCode = machineCode,
            appVersion = prefs.getString("appVersion", "").orEmpty(),
            plan = prefs.getString("plan", "").orEmpty(),
            expiresAt = prefs.getString("expiresAt", null),
            profile = RestaurantProfile(
                ownerName = prefs.getString("ownerName", "").orEmpty(),
                businessName = prefs.getString("businessName", "").orEmpty(),
                legalName = prefs.getString("legalName", "").orEmpty(),
                cnpj = prefs.getString("cnpj", "").orEmpty(),
                phone = prefs.getString("phone", "").orEmpty(),
                address = prefs.getString("address", "").orEmpty(),
                city = prefs.getString("city", "").orEmpty(),
                state = prefs.getString("state", "").orEmpty()
            )
        )
    }

    fun save(session: RestaurantSession) {
        prefs.edit()
            .putBoolean("active", true)
            .putString("adminBaseUrl", session.adminBaseUrl)
            .putString("licenseKey", session.licenseKey)
            .putString("machineHash", session.machineHash)
            .putString("machineCode", session.machineCode)
            .putString("appVersion", session.appVersion)
            .putString("plan", session.plan)
            .putString("expiresAt", session.expiresAt)
            .putString("ownerName", session.profile.ownerName)
            .putString("businessName", session.profile.businessName)
            .putString("legalName", session.profile.legalName)
            .putString("cnpj", session.profile.cnpj)
            .putString("phone", session.profile.phone)
            .putString("address", session.profile.address)
            .putString("city", session.profile.city)
            .putString("state", session.profile.state)
            .apply()
    }

    fun clear() {
        prefs.edit().clear().apply()
    }
}
