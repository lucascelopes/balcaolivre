package com.balcaolivre.pdv.mobile

import android.app.Activity
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.text.InputType
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ProgressBar
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast
import com.balcaolivre.pdv.mobile.data.AdminApiClient
import com.balcaolivre.pdv.mobile.data.DeviceIdentity
import com.balcaolivre.pdv.mobile.data.SessionStore
import com.balcaolivre.pdv.mobile.model.AdminClientPayload
import com.balcaolivre.pdv.mobile.model.RestaurantProfile
import com.balcaolivre.pdv.mobile.model.RestaurantSession
import java.util.Locale
import kotlin.math.roundToInt

class MainActivity : Activity() {
    private val sessionStore by lazy { SessionStore(this) }
    private val apiClient by lazy { AdminApiClient() }
    private var loading = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.statusBarColor = Blue
        window.navigationBarColor = PageBg

        val session = sessionStore.load()
        if (session == null) showLogin() else showHome(session)
    }

    private fun showLogin(message: String? = null) {
        loading = false

        val content = pageContent()
        content.addView(header("Balcao Livre Mobile", "Login do restaurante"))

        val adminUrl = input("URL do admin", DEFAULT_ADMIN_API_URL, InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_URI)
        val license = input("Chave da licenca", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_CHARACTERS)
        val email = input("Email da conta", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS)
        val businessName = input("Nome fantasia", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_WORDS)
        val ownerName = input("Responsavel", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_WORDS)
        val legalName = input("Razao social", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_WORDS)
        val cnpj = input("CNPJ", "", InputType.TYPE_CLASS_TEXT)
        val phone = input("Telefone", "", InputType.TYPE_CLASS_PHONE)
        val city = input("Cidade", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_WORDS)
        val state = input("UF", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_CHARACTERS)
        val address = input("Endereco", "", InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_SENTENCES)

        listOf(adminUrl, license, email, businessName, ownerName, legalName, cnpj, phone, city, state, address)
            .forEach { content.addView(fieldBlock(it)) }

        val status = statusText(message.orEmpty(), isError = !message.isNullOrBlank())
        val progress = ProgressBar(this).apply {
            visibility = View.GONE
            isIndeterminate = true
        }
        val login = primaryButton("Entrar no restaurante")

        login.setOnClickListener {
            if (loading) return@setOnClickListener
            val normalizedKey = normalizeLicenseKey(license.textString())
            val profile = RestaurantProfile(
                email = email.textString().lowercase(Locale.US),
                ownerName = ownerName.textString(),
                businessName = businessName.textString(),
                legalName = legalName.textString(),
                cnpj = cnpj.textString(),
                phone = phone.textString(),
                address = address.textString(),
                city = city.textString(),
                state = state.textString().uppercase(Locale.US)
            )

            if (normalizedKey.isBlank()) {
                status.showMessage("Informe a chave da licenca.", true)
                return@setOnClickListener
            }
            if (!isReasonableEmail(profile.email)) {
                status.showMessage("Informe um email valido para vincular a conta.", true)
                return@setOnClickListener
            }
            if (profile.businessName.isBlank() && profile.legalName.isBlank() && profile.ownerName.isBlank()) {
                status.showMessage("Informe o nome da loja.", true)
                return@setOnClickListener
            }
            if (profile.cnpj.isBlank()) {
                status.showMessage("Informe o CNPJ da loja.", true)
                return@setOnClickListener
            }

            setLoading(true, login, progress, status, "Validando restaurante...")
            val baseUrl = adminUrl.textString().ifBlank { DEFAULT_ADMIN_API_URL }
            val machineHash = DeviceIdentity.machineHash(this)
            val machineCode = DeviceIdentity.machineCode(this)
            val appVersion = "android-${BuildConfig.VERSION_NAME}"
            val payload = AdminClientPayload(
                eventName = "android.activate",
                licenseKey = normalizedKey,
                machineHash = machineHash,
                machineCode = machineCode,
                appVersion = appVersion,
                profile = profile
            )

            Thread {
                runCatching { apiClient.activate(baseUrl, payload) }
                    .onSuccess { response ->
                        runOnUiThread {
                            if (!response.ok) {
                                setLoading(false, login, progress, status, response.message.ifBlank { "Ativacao recusada." }, true)
                                return@runOnUiThread
                            }

                            val session = RestaurantSession(
                                adminBaseUrl = baseUrl,
                                licenseKey = normalizedKey,
                                machineHash = machineHash,
                                machineCode = machineCode,
                                appVersion = appVersion,
                                plan = response.plan,
                                expiresAt = response.expiresAt,
                                profile = profile
                            )
                            sessionStore.save(session)
                            showHome(session)
                        }
                    }
                    .onFailure { error ->
                        runOnUiThread {
                            setLoading(false, login, progress, status, error.message ?: "Falha ao conectar no admin.", true)
                        }
                    }
            }.start()
        }

        content.addView(login)
        content.addView(progress, centeredWrap())
        content.addView(status)
        setContentView(scroll(content))
    }

    private fun showHome(session: RestaurantSession) {
        val content = pageContent()
        content.addView(header(session.profile.displayName, "Sessao ativa"))

        val summary = card()
        summary.addView(title("Restaurante"))
        summary.addView(body(session.profile.displayName))
        summary.addView(body("Email: ${session.profile.email.ifBlank { "nao informado" }}"))
        summary.addView(body("Plano: ${session.plan.ifBlank { "admin" }}"))
        summary.addView(body("Validade: ${session.expiresAt ?: "sem data local"}"))
        summary.addView(body("Aparelho: ${session.machineCode}"))
        content.addView(summary)

        val grid = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = matchWrap()
        }
        grid.addView(metricCard("Mesas", "0"))
        grid.addView(metricCard("Produtos", "0"))
        grid.addView(metricCard("Pedidos", "0"))
        grid.addView(metricCard("Caixa", "R$ 0,00"))
        content.addView(grid)

        val status = statusText("")
        val sync = secondaryButton("Sincronizar agora")
        sync.setOnClickListener {
            syncCheckIn(session, status, sync)
        }
        val logout = secondaryButton("Sair deste restaurante")
        logout.setOnClickListener {
            sessionStore.clear()
            showLogin()
        }

        content.addView(sync)
        content.addView(logout)
        content.addView(status)
        setContentView(scroll(content))
        syncCheckIn(session, status, sync, silent = true)
    }

    private fun syncCheckIn(
        session: RestaurantSession,
        status: TextView,
        button: Button,
        silent: Boolean = false
    ) {
        if (loading) return
        loading = true
        button.isEnabled = false
        if (!silent) status.showMessage("Sincronizando...", false)

        val payload = AdminClientPayload(
            eventName = "android.checkin",
            licenseKey = session.licenseKey,
            machineHash = session.machineHash,
            machineCode = session.machineCode,
            appVersion = session.appVersion,
            profile = session.profile,
            localExpiresAt = session.expiresAt,
            localPlan = session.plan
        )

        Thread {
            runCatching { apiClient.checkIn(session.adminBaseUrl, payload) }
                .onSuccess { ok ->
                    runOnUiThread {
                        loading = false
                        button.isEnabled = true
                        if (!silent || !ok) {
                            status.showMessage(if (ok) "Sincronizado." else "Admin nao confirmou o check-in.", !ok)
                        }
                    }
                }
                .onFailure { error ->
                    runOnUiThread {
                        loading = false
                        button.isEnabled = true
                        if (!silent) status.showMessage(error.message ?: "Falha na sincronizacao.", true)
                    }
                }
        }.start()
    }

    private fun setLoading(
        value: Boolean,
        button: Button,
        progress: ProgressBar,
        status: TextView,
        message: String,
        isError: Boolean = false
    ) {
        loading = value
        button.isEnabled = !value
        progress.visibility = if (value) View.VISIBLE else View.GONE
        status.showMessage(message, isError)
    }

    private fun pageContent(): LinearLayout = LinearLayout(this).apply {
        orientation = LinearLayout.VERTICAL
        setPadding(dp(18), dp(18), dp(18), dp(24))
        background = solid(PageBg)
        layoutParams = matchWrap()
    }

    private fun scroll(child: View): ScrollView = ScrollView(this).apply {
        setBackgroundColor(PageBg)
        addView(child)
    }

    private fun header(name: String, subtitle: String): View {
        val box = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            background = round(Blue, dp(0))
            setPadding(dp(18), dp(22), dp(18), dp(22))
            layoutParams = matchWrap(bottom = 14)
        }
        box.addView(TextView(this).apply {
            text = name
            setTextColor(Color.WHITE)
            textSize = 26f
            typeface = Typeface.DEFAULT_BOLD
        })
        box.addView(TextView(this).apply {
            text = subtitle
            setTextColor(Color.rgb(220, 235, 245))
            textSize = 15f
            setPadding(0, dp(5), 0, 0)
        })
        return box
    }

    private fun input(label: String, initial: String, inputTypeValue: Int): EditText = EditText(this).apply {
        hint = label
        setText(initial)
        inputType = inputTypeValue
        imeOptions = EditorInfo.IME_ACTION_NEXT
        setSingleLine(true)
        textSize = 16f
        setTextColor(Ink)
        setHintTextColor(Muted)
        background = round(Color.WHITE, dp(8), Stroke)
        setPadding(dp(12), 0, dp(12), 0)
        minHeight = dp(48)
    }

    private fun fieldBlock(input: EditText): View {
        val box = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = matchWrap(bottom = 10)
        }
        box.addView(TextView(this).apply {
            text = input.hint
            setTextColor(Muted)
            typeface = Typeface.DEFAULT_BOLD
            textSize = 12f
            setPadding(dp(2), 0, 0, dp(4))
        })
        box.addView(input)
        return box
    }

    private fun card(): LinearLayout = LinearLayout(this).apply {
        orientation = LinearLayout.VERTICAL
        background = round(Color.WHITE, dp(8), Stroke)
        setPadding(dp(16), dp(14), dp(16), dp(14))
        layoutParams = matchWrap(bottom = 12)
    }

    private fun metricCard(label: String, value: String): View {
        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
            background = round(Color.WHITE, dp(8), Stroke)
            setPadding(dp(16), dp(14), dp(16), dp(14))
            layoutParams = matchWrap(bottom = 10)
        }
        row.addView(TextView(this).apply {
            text = label
            setTextColor(Ink)
            textSize = 17f
            typeface = Typeface.DEFAULT_BOLD
            layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)
        })
        row.addView(TextView(this).apply {
            text = value
            setTextColor(Green)
            textSize = 20f
            typeface = Typeface.DEFAULT_BOLD
        })
        row.setOnClickListener {
            Toast.makeText(this, "$label sera ligado ao modulo do PDV.", Toast.LENGTH_SHORT).show()
        }
        return row
    }

    private fun primaryButton(textValue: String): Button = Button(this).apply {
        text = textValue
        setAllCaps(false)
        textSize = 16f
        typeface = Typeface.DEFAULT_BOLD
        setTextColor(Color.WHITE)
        background = round(Green, dp(8))
        minHeight = dp(50)
        layoutParams = matchWrap(bottom = 10)
    }

    private fun secondaryButton(textValue: String): Button = Button(this).apply {
        text = textValue
        setAllCaps(false)
        textSize = 15f
        setTextColor(Blue)
        background = round(Color.WHITE, dp(8), Stroke)
        minHeight = dp(48)
        layoutParams = matchWrap(bottom = 10)
    }

    private fun title(value: String): TextView = TextView(this).apply {
        text = value
        setTextColor(Ink)
        textSize = 18f
        typeface = Typeface.DEFAULT_BOLD
    }

    private fun body(value: String): TextView = TextView(this).apply {
        text = value
        setTextColor(Muted)
        textSize = 15f
        setPadding(0, dp(5), 0, 0)
    }

    private fun statusText(value: String, isError: Boolean = false): TextView = TextView(this).apply {
        showMessage(value, isError)
        textSize = 14f
        setPadding(dp(2), dp(4), dp(2), dp(4))
    }

    private fun TextView.showMessage(value: String, isError: Boolean) {
        text = value
        setTextColor(if (isError) Red else Green)
        visibility = if (value.isBlank()) View.GONE else View.VISIBLE
    }

    private fun EditText.textString(): String = text?.toString()?.trim().orEmpty()

    private fun normalizeLicenseKey(value: String): String =
        value.trim().uppercase(Locale.US).replace(" ", "")

    private fun isReasonableEmail(value: String): Boolean {
        val clean = value.trim()
        val at = clean.indexOf('@')
        return at > 0 &&
            at == clean.lastIndexOf('@') &&
            at < clean.length - 3 &&
            clean.substring(at + 1).contains('.') &&
            clean.none { it.isWhitespace() }
    }

    private fun matchWrap(bottom: Int = 0): LinearLayout.LayoutParams =
        LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        ).apply {
            bottomMargin = dp(bottom)
        }

    private fun centeredWrap(): LinearLayout.LayoutParams =
        LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WRAP_CONTENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        ).apply {
            gravity = Gravity.CENTER_HORIZONTAL
            bottomMargin = dp(8)
        }

    private fun round(fill: Int, radius: Int, stroke: Int? = null): GradientDrawable =
        GradientDrawable().apply {
            setColor(fill)
            cornerRadius = radius.toFloat()
            if (stroke != null) setStroke(dp(1), stroke)
        }

    private fun solid(fill: Int): GradientDrawable = GradientDrawable().apply {
        setColor(fill)
    }

    private fun dp(value: Int): Int =
        (value * resources.displayMetrics.density).roundToInt()

    private companion object {
        val Blue: Int = Color.rgb(21, 90, 134)
        val Green: Int = Color.rgb(15, 118, 110)
        val Red: Int = Color.rgb(180, 35, 24)
        val Ink: Int = Color.rgb(24, 34, 43)
        val Muted: Int = Color.rgb(96, 112, 128)
        val Stroke: Int = Color.rgb(210, 222, 232)
        val PageBg: Int = Color.rgb(238, 244, 248)
    }
}
