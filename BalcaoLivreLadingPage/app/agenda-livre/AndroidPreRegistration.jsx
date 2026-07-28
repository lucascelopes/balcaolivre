"use client";

import {
  BadgeCheck,
  Building2,
  Check,
  ChevronRight,
  CircleAlert,
  Clock3,
  Download,
  FileCheck2,
  ImagePlus,
  KeyRound,
  LoaderCircle,
  LockKeyhole,
  Mail,
  RefreshCw,
  ShieldCheck,
  Smartphone,
  UserRound,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import styles from "./android-pre-registration.module.css";

const CONFIG_URL = "/api/agenda/account/config";
const REGISTRATION_URL = "/api/agenda/android/pre-register";
const BUILD_STATUS_ROOT = "/api/agenda/android/builds";
const BUILD_SESSION_KEY = "agenda_livre_android_build_session_v1";
const MAX_ICON_BYTES = 4 * 1024 * 1024;
const MAX_COVER_BYTES = 8 * 1024 * 1024;
const ACCEPTED_IMAGES = new Set(["image/png", "image/jpeg"]);

const statusCopy = {
  queued: {
    title: "Pedido recebido",
    detail: "Sua personalização entrou na fila segura de preparação.",
    progress: 12,
  },
  preparing: {
    title: "Preparando sua identidade",
    detail: "Estamos ajustando nome, ícone e foto para o seu estabelecimento.",
    progress: 32,
  },
  building: {
    title: "Montando seu aplicativo",
    detail: "O pacote Android está sendo gerado e assinado.",
    progress: 66,
  },
  signing: {
    title: "Protegendo o arquivo",
    detail: "Estamos concluindo a assinatura e a verificação do pacote.",
    progress: 84,
  },
  ready: {
    title: "Seu aplicativo está pronto",
    detail: "Baixe o arquivo privado e siga as instruções para instalar.",
    progress: 100,
  },
  failed: {
    title: "Não foi possível concluir",
    detail: "Tente novamente. Se o problema continuar, fale com nosso suporte.",
    progress: 100,
  },
};

function messageFromPayload(payload, fallback) {
  return (
    payload?.error?.message ||
    payload?.error_description ||
    payload?.msg ||
    payload?.message ||
    fallback
  );
}

async function jsonResponse(response, fallback) {
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(messageFromPayload(payload, fallback));
  }
  return payload;
}

function buildFromPayload(payload, previous = {}) {
  const source = payload?.build || payload?.registration?.build || payload || {};
  const download = payload?.download || source?.download || {};
  const rawStatus = String(source.status || payload?.status || previous.status || "queued")
    .trim()
    .toLowerCase();
  const status = statusCopy[rawStatus] ? rawStatus : "queued";
  return {
    ...previous,
    id: String(source.id || source.buildId || payload?.buildId || previous.id || ""),
    status,
    businessName: String(
      source.businessName || source.appName || payload?.businessName || previous.businessName || "",
    ),
    fileName: String(
      download.fileName || source.fileName || source.filename || previous.fileName || "",
    ),
    sha256: String(
      download.sha256 || source.sha256 || source.checksumSha256 || previous.sha256 || "",
    ),
    size: Number(download.size || source.size || previous.size || 0),
    downloadUrl: String(download.url || source.downloadUrl || previous.downloadUrl || ""),
    detail: String(
      source.error?.message || source.detail || source.message || previous.detail || "",
    ),
  };
}

function authSessionFromPayload(payload, fallbackRefreshToken = "") {
  const accessToken = String(payload?.access_token || "");
  const refreshToken = String(payload?.refresh_token || fallbackRefreshToken || "");
  const expiresAtSeconds = Number(payload?.expires_at || 0);
  const expiresInSeconds = Number(payload?.expires_in || 3600);
  if (!accessToken || accessToken.length > 8192 || refreshToken.length > 8192) return null;
  return {
    accessToken,
    refreshToken,
    expiresAt:
      expiresAtSeconds > 0
        ? expiresAtSeconds * 1000
        : Date.now() + Math.max(60, expiresInSeconds) * 1000,
  };
}

function safeDownloadName(value) {
  const base = String(value || "agenda-livre")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-zA-Z0-9_-]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 72);
  return `Agenda-Livre-${base || "estabelecimento"}.apk`;
}

function filenameFromDisposition(headerValue) {
  if (!headerValue) return "";
  const encoded = headerValue.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
  if (encoded) {
    try {
      return decodeURIComponent(encoded.replace(/["']/g, ""));
    } catch {
      return "";
    }
  }
  return headerValue.match(/filename="?([^";]+)"?/i)?.[1]?.trim() || "";
}

function useObjectUrl(file) {
  const [url, setUrl] = useState("");
  useEffect(() => {
    if (!file) {
      setUrl("");
      return undefined;
    }
    const nextUrl = URL.createObjectURL(file);
    setUrl(nextUrl);
    return () => URL.revokeObjectURL(nextUrl);
  }, [file]);
  return url;
}

function ImageField({ acceptLabel, file, id, label, maxLabel, onChange, preview, square }) {
  return (
    <label className={styles.imageField} htmlFor={id}>
      <span className={`${styles.imagePreview} ${square ? styles.squarePreview : ""}`}>
        {preview ? (
          // This is a local object URL selected by the user, so next/image is not applicable.
          // eslint-disable-next-line @next/next/no-img-element
          <img src={preview} alt={`Prévia de ${label.toLowerCase()}`} />
        ) : (
          <ImagePlus size={26} aria-hidden="true" />
        )}
      </span>
      <span className={styles.imageFieldCopy}>
        <strong>{file ? file.name : label}</strong>
        <small>{file ? `${(file.size / 1024 / 1024).toFixed(1)} MB` : `${acceptLabel} · ${maxLabel}`}</small>
      </span>
      <span className={styles.imageAction}>{file ? "Trocar" : "Escolher"}</span>
      <input
        id={id}
        type="file"
        accept="image/png,image/jpeg"
        onChange={(event) => onChange(event.target.files?.[0] || null)}
      />
    </label>
  );
}

export default function AndroidPreRegistration() {
  const [accountMode, setAccountMode] = useState("signup");
  const [fullName, setFullName] = useState("");
  const [businessName, setBusinessName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [icon, setIcon] = useState(null);
  const [cover, setCover] = useState(null);
  const [consent, setConsent] = useState(false);
  const [authSession, setAuthSession] = useState(null);
  const [build, setBuild] = useState(null);
  const [sessionRestored, setSessionRestored] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const pollFailures = useRef(0);
  const iconPreview = useObjectUrl(icon);
  const coverPreview = useObjectUrl(cover);
  const isFinished = build?.status === "ready" || build?.status === "failed";
  const currentStatus = statusCopy[build?.status] || statusCopy.queued;
  const displayBusinessName = build?.businessName || businessName || "Seu estabelecimento";

  useEffect(() => {
    try {
      const saved = JSON.parse(window.sessionStorage.getItem(BUILD_SESSION_KEY) || "null");
      const savedSession = saved?.session;
      const savedBuild = buildFromPayload(saved?.build || {});
      if (
        savedSession &&
        typeof savedSession.accessToken === "string" &&
        savedSession.accessToken.length > 0 &&
        savedSession.accessToken.length <= 8192 &&
        typeof savedSession.refreshToken === "string" &&
        savedSession.refreshToken.length <= 8192 &&
        Number.isFinite(Number(savedSession.expiresAt)) &&
        /^[A-Za-z0-9_-]{8,128}$/.test(savedBuild.id)
      ) {
        setAuthSession({
          accessToken: savedSession.accessToken,
          refreshToken: savedSession.refreshToken,
          expiresAt: Number(savedSession.expiresAt),
        });
        setBuild(savedBuild);
        setBusinessName(savedBuild.businessName || "");
        setNotice("Seu pedido foi recuperado. Retomamos o acompanhamento automaticamente.");
      }
    } catch {
      window.sessionStorage.removeItem(BUILD_SESSION_KEY);
    } finally {
      setSessionRestored(true);
    }
  }, []);

  useEffect(() => {
    if (!sessionRestored || !authSession?.accessToken || !build?.id) return;
    try {
      window.sessionStorage.setItem(
        BUILD_SESSION_KEY,
        JSON.stringify({
          session: authSession,
          build: {
            id: build.id,
            status: build.status,
            businessName: build.businessName,
            fileName: build.fileName,
            sha256: build.sha256,
            size: build.size,
            detail: build.detail,
          },
        }),
      );
    } catch {
      // The active in-memory session still works if browser storage is unavailable.
    }
  }, [authSession, build, sessionRestored]);

  const validateImage = useCallback((file, maxBytes, label) => {
    if (!file) throw new Error(`Escolha ${label}.`);
    if (!ACCEPTED_IMAGES.has(file.type)) {
      throw new Error(`${label} precisa ser PNG ou JPG.`);
    }
    if (file.size > maxBytes) {
      throw new Error(`${label} ultrapassa o tamanho máximo permitido.`);
    }
  }, []);

  const authenticate = useCallback(async () => {
    const config = await jsonResponse(
      await fetch(CONFIG_URL, { cache: "no-store" }),
      "Não foi possível preparar o acesso agora.",
    );
    const supabaseUrl = String(config.supabaseUrl || "").replace(/\/+$/, "");
    const publishableKey = String(config.publishableKey || config.supabaseAnonKey || "");
    if (!supabaseUrl || !publishableKey) {
      throw new Error("A criação de conta está temporariamente indisponível.");
    }

    const path = accountMode === "signup" ? "/auth/v1/signup" : "/auth/v1/token?grant_type=password";
    const body = {
      email: email.trim(),
      password,
      ...(accountMode === "signup"
        ? { data: { full_name: fullName.trim(), business_name: businessName.trim() } }
        : {}),
    };
    const payload = await jsonResponse(
      await fetch(`${supabaseUrl}${path}`, {
        method: "POST",
        headers: {
          apikey: publishableKey,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
      }),
      accountMode === "signup"
        ? "Não foi possível criar sua conta. Confira os dados e tente novamente."
        : "E-mail ou senha incorretos.",
    );

    const session = authSessionFromPayload(payload);
    if (!session && accountMode === "signup") {
      setAccountMode("signin");
      setNotice("Conta criada. Confirme o e-mail recebido e depois entre para gerar seu aplicativo.");
      return null;
    }
    if (!session) throw new Error("Não foi possível iniciar sua sessão.");
    return session;
  }, [accountMode, businessName, email, fullName, password]);

  const refreshAccessSession = useCallback(
    async (current = authSession) => {
      if (!current?.refreshToken) {
        throw new Error("Sua sessão expirou. Entre novamente para acompanhar este pedido.");
      }
      const config = await jsonResponse(
        await fetch(CONFIG_URL, { cache: "no-store" }),
        "Não foi possível renovar sua sessão agora.",
      );
      const supabaseUrl = String(config.supabaseUrl || "").replace(/\/+$/, "");
      const publishableKey = String(config.publishableKey || config.supabaseAnonKey || "");
      const payload = await jsonResponse(
        await fetch(`${supabaseUrl}/auth/v1/token?grant_type=refresh_token`, {
          method: "POST",
          headers: { apikey: publishableKey, "Content-Type": "application/json" },
          body: JSON.stringify({ refresh_token: current.refreshToken }),
        }),
        "Sua sessão expirou. Entre novamente para acompanhar este pedido.",
      );
      const renewed = authSessionFromPayload(payload, current.refreshToken);
      if (!renewed) throw new Error("Não foi possível renovar sua sessão.");
      setAuthSession(renewed);
      return renewed;
    },
    [authSession],
  );

  const authorizedFetch = useCallback(
    async (url, init = {}) => {
      let current = authSession;
      if (!current?.accessToken) throw new Error("Entre na sua conta para continuar.");
      if (current.expiresAt <= Date.now() + 60_000 && current.refreshToken) {
        current = await refreshAccessSession(current);
      }
      const request = (session) =>
        fetch(url, {
          ...init,
          headers: {
            ...(init.headers || {}),
            Authorization: `Bearer ${session.accessToken}`,
          },
        });
      let response = await request(current);
      if (response.status === 401 && current.refreshToken) {
        current = await refreshAccessSession(current);
        response = await request(current);
      }
      return response;
    },
    [authSession, refreshAccessSession],
  );

  const submit = async (event) => {
    event.preventDefault();
    setError("");
    setNotice("");

    try {
      if (accountMode === "signup" && fullName.trim().length < 2) {
        throw new Error("Informe seu nome.");
      }
      if (businessName.trim().length < 2 || businessName.trim().length > 80) {
        throw new Error("Informe o nome do estabelecimento com até 80 caracteres.");
      }
      if (!/^\S+@\S+\.\S+$/.test(email.trim())) throw new Error("Informe um e-mail válido.");
      if (password.length < 8) throw new Error("Use uma senha com pelo menos 8 caracteres.");
      validateImage(icon, MAX_ICON_BYTES, "o ícone do aplicativo");
      validateImage(cover, MAX_COVER_BYTES, "a foto do estabelecimento");
      if (!consent) throw new Error("Confirme que você entende como instalar o arquivo Android.");

      setSubmitting(true);
      const activeSession = authSession || (await authenticate());
      if (!activeSession) return;
      setAuthSession(activeSession);

      const form = new FormData();
      form.append("businessName", businessName.trim());
      form.append("icon", icon, icon.name);
      form.append("cover", cover, cover.name);
      form.append("sideloadConsent", "true");

      const payload = await jsonResponse(
        await fetch(REGISTRATION_URL, {
          method: "POST",
          headers: { Authorization: `Bearer ${activeSession.accessToken}` },
          body: form,
        }),
        "Não foi possível iniciar a preparação do aplicativo.",
      );
      const nextBuild = buildFromPayload(payload, { businessName: businessName.trim() });
      if (!nextBuild.id) throw new Error("O pedido foi criado sem identificação. Tente novamente.");
      pollFailures.current = 0;
      setBuild(nextBuild);
      setPassword("");
    } catch (submissionError) {
      setError(submissionError.message || "Não foi possível continuar.");
    } finally {
      setSubmitting(false);
    }
  };

  useEffect(() => {
    if (!build?.id || !authSession?.accessToken || isFinished) return undefined;
    let disposed = false;
    let timeoutId;

    const poll = async () => {
      try {
        const payload = await jsonResponse(
          await authorizedFetch(`${BUILD_STATUS_ROOT}/${encodeURIComponent(build.id)}`, {
            cache: "no-store",
          }),
          "Não foi possível consultar a preparação agora.",
        );
        if (disposed) return;
        pollFailures.current = 0;
        setBuild((current) => buildFromPayload(payload, current));
        timeoutId = window.setTimeout(poll, 5000);
      } catch (pollError) {
        if (disposed) return;
        pollFailures.current += 1;
        if (pollFailures.current >= 3) {
          setNotice("A consulta automática foi pausada. Seu pedido continua salvo; toque em atualizar.");
          return;
        }
        timeoutId = window.setTimeout(poll, 7000);
      }
    };

    timeoutId = window.setTimeout(poll, 1800);
    return () => {
      disposed = true;
      window.clearTimeout(timeoutId);
    };
  }, [authSession?.accessToken, authorizedFetch, build?.id, isFinished]);

  const refreshBuild = async () => {
    if (!build?.id || !authSession?.accessToken) return;
    setError("");
    setNotice("");
    try {
      const payload = await jsonResponse(
        await authorizedFetch(`${BUILD_STATUS_ROOT}/${encodeURIComponent(build.id)}`, {
          cache: "no-store",
        }),
        "Não foi possível atualizar o andamento.",
      );
      pollFailures.current = 0;
      setBuild((current) => buildFromPayload(payload, current));
    } catch (refreshError) {
      setError(refreshError.message || "Não foi possível atualizar o andamento.");
    }
  };

  const downloadApk = async () => {
    if (!build?.id || !authSession?.accessToken) return;
    setError("");
    setDownloading(true);
    try {
      const statusPayload = await jsonResponse(
        await authorizedFetch(`${BUILD_STATUS_ROOT}/${encodeURIComponent(build.id)}`, {
          cache: "no-store",
        }),
        "Não foi possível renovar seu link de download.",
      );
      const freshBuild = buildFromPayload(statusPayload, build);
      setBuild(freshBuild);
      const url =
        freshBuild.downloadUrl || `${BUILD_STATUS_ROOT}/${encodeURIComponent(build.id)}/download`;
      const response = await authorizedFetch(url, {
        cache: "no-store",
      });
      if (!response.ok) {
        const payload = await response.json().catch(() => ({}));
        throw new Error(messageFromPayload(payload, "Não foi possível baixar o arquivo agora."));
      }
      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download =
        filenameFromDisposition(response.headers.get("Content-Disposition")) ||
        freshBuild.fileName ||
        safeDownloadName(displayBusinessName);
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    } catch (downloadError) {
      setError(downloadError.message || "Não foi possível baixar o arquivo agora.");
    } finally {
      setDownloading(false);
    }
  };

  const reset = () => {
    setAuthSession(null);
    setBuild(null);
    setPassword("");
    setNotice("");
    setError("");
    pollFailures.current = 0;
    try {
      window.sessionStorage.removeItem(BUILD_SESSION_KEY);
    } catch {
      // Nothing else is required when browser storage is unavailable.
    }
  };

  const buildSteps = useMemo(
    () => [
      { label: "Pedido", done: Boolean(build) },
      { label: "Personalização", done: ["building", "signing", "ready"].includes(build?.status) },
      { label: "Assinatura", done: ["signing", "ready"].includes(build?.status) },
      { label: "Download", done: build?.status === "ready" },
    ],
    [build],
  );

  return (
    <section className={styles.section} id="android-download" aria-labelledby="android-title">
      <div className={styles.glow} aria-hidden="true" />
      <div className={styles.container}>
        <div className={styles.heading}>
          <div>
            <span className={styles.kicker}>
              <Smartphone size={15} aria-hidden="true" /> Android personalizado
            </span>
            <h2 id="android-title">Baixe o aplicativo com a identidade do seu negócio.</h2>
          </div>
          <p>
            Faça o pré-cadastro, envie seu ícone e sua foto. O arquivo chega conectado à sua
            conta: no primeiro acesso, basta abrir. O teste de 7 dias começa somente nessa
            primeira ativação.
          </p>
        </div>

        <div className={styles.flowGrid}>
          <div className={styles.formPanel}>
            {!build ? (
              <form onSubmit={submit} noValidate>
                <div className={styles.panelTop}>
                  <span className={styles.stepNumber}>01</span>
                  <span>
                    <strong>Conta e personalização</strong>
                    <small>Web, Windows e Android usam a mesma conta.</small>
                  </span>
                </div>

                <div className={styles.modeTabs} role="tablist" aria-label="Tipo de acesso">
                  <button
                    type="button"
                    role="tab"
                    aria-selected={accountMode === "signup"}
                    className={accountMode === "signup" ? styles.activeTab : ""}
                    onClick={() => {
                      setAccountMode("signup");
                      setError("");
                      setNotice("");
                    }}
                  >
                    Criar conta
                  </button>
                  <button
                    type="button"
                    role="tab"
                    aria-selected={accountMode === "signin"}
                    className={accountMode === "signin" ? styles.activeTab : ""}
                    onClick={() => {
                      setAccountMode("signin");
                      setError("");
                      setNotice("");
                    }}
                  >
                    Já tenho conta
                  </button>
                </div>

                <div className={styles.fields}>
                  {accountMode === "signup" ? (
                    <label className={styles.field}>
                      <span>Seu nome</span>
                      <span className={styles.inputShell}>
                        <UserRound size={17} aria-hidden="true" />
                        <input
                          type="text"
                          autoComplete="name"
                          value={fullName}
                          onChange={(event) => setFullName(event.target.value)}
                          placeholder="Como podemos chamar você?"
                          maxLength={100}
                          required
                        />
                      </span>
                    </label>
                  ) : null}

                  <label className={styles.field}>
                    <span>Nome do estabelecimento</span>
                    <span className={styles.inputShell}>
                      <Building2 size={17} aria-hidden="true" />
                      <input
                        type="text"
                        value={businessName}
                        onChange={(event) => setBusinessName(event.target.value)}
                        placeholder="Ex.: Studio Aurora"
                        maxLength={80}
                        required
                      />
                    </span>
                    <small>Esse nome aparece no aplicativo e no arquivo baixado.</small>
                  </label>

                  <div className={styles.fieldRow}>
                    <label className={styles.field}>
                      <span>E-mail</span>
                      <span className={styles.inputShell}>
                        <Mail size={17} aria-hidden="true" />
                        <input
                          type="email"
                          inputMode="email"
                          autoComplete="email"
                          value={email}
                          onChange={(event) => setEmail(event.target.value)}
                          placeholder="voce@negocio.com.br"
                          required
                        />
                      </span>
                    </label>
                    <label className={styles.field}>
                      <span>Senha</span>
                      <span className={styles.inputShell}>
                        <KeyRound size={17} aria-hidden="true" />
                        <input
                          type="password"
                          autoComplete={accountMode === "signup" ? "new-password" : "current-password"}
                          value={password}
                          onChange={(event) => setPassword(event.target.value)}
                          placeholder="Mínimo de 8 caracteres"
                          minLength={8}
                          required
                        />
                      </span>
                    </label>
                  </div>
                </div>

                <div className={styles.brandingFields}>
                  <ImageField
                    id="android-app-icon"
                    label="Ícone do aplicativo"
                    acceptLabel="PNG ou JPG"
                    maxLabel="até 4 MB"
                    file={icon}
                    preview={iconPreview}
                    square
                    onChange={setIcon}
                  />
                  <ImageField
                    id="android-business-cover"
                    label="Foto do estabelecimento"
                    acceptLabel="PNG ou JPG"
                    maxLabel="até 8 MB"
                    file={cover}
                    preview={coverPreview}
                    onChange={setCover}
                  />
                </div>

                <label className={styles.consent}>
                  <input
                    type="checkbox"
                    checked={consent}
                    onChange={(event) => setConsent(event.target.checked)}
                  />
                  <span className={styles.checkbox} aria-hidden="true">
                    <Check size={14} />
                  </span>
                  <span>
                    Entendo que vou baixar um arquivo Android privado e que o aparelho pode pedir
                    autorização para instalar aplicativos recebidos pelo navegador ou gerenciador
                    de arquivos.
                  </span>
                </label>

                {notice ? (
                  <div className={styles.notice} role="status">
                    <Mail size={17} aria-hidden="true" /> {notice}
                  </div>
                ) : null}
                {error ? (
                  <div className={styles.error} role="alert">
                    <CircleAlert size={17} aria-hidden="true" /> {error}
                  </div>
                ) : null}

                <button className={styles.submitButton} type="submit" disabled={submitting}>
                  {submitting ? (
                    <LoaderCircle className={styles.spin} size={19} aria-hidden="true" />
                  ) : (
                    <ShieldCheck size={19} aria-hidden="true" />
                  )}
                  {submitting ? "Enviando com segurança…" : "Preparar meu aplicativo Android"}
                  {!submitting ? <ChevronRight size={18} aria-hidden="true" /> : null}
                </button>
                <p className={styles.formMeta}>
                  A senha nunca vai dentro do arquivo. A conexão automática usa uma ativação única
                  e protegida para vincular este aplicativo à sua conta.
                </p>
              </form>
            ) : (
              <div className={styles.buildPanel}>
                <div className={styles.panelTop}>
                  <span className={styles.stepNumber}>02</span>
                  <span>
                    <strong>Acompanhe a preparação</strong>
                    <small>Pedido {build.id.slice(0, 8).toUpperCase()}</small>
                  </span>
                </div>

                <div className={`${styles.statusHero} ${build.status === "failed" ? styles.statusFailed : ""}`}>
                  <span className={styles.statusIcon}>
                    {build.status === "ready" ? (
                      <BadgeCheck size={27} aria-hidden="true" />
                    ) : build.status === "failed" ? (
                      <CircleAlert size={27} aria-hidden="true" />
                    ) : (
                      <LoaderCircle className={styles.spin} size={27} aria-hidden="true" />
                    )}
                  </span>
                  <span>
                    <strong>{currentStatus.title}</strong>
                    <small>{build.detail || currentStatus.detail}</small>
                  </span>
                </div>

                <div className={styles.progressTrack} aria-label={`${currentStatus.progress}% concluído`}>
                  <span style={{ width: `${currentStatus.progress}%` }} />
                </div>
                <ol className={styles.buildSteps}>
                  {buildSteps.map((step) => (
                    <li className={step.done ? styles.doneStep : ""} key={step.label}>
                      <span>{step.done ? <Check size={12} aria-hidden="true" /> : null}</span>
                      {step.label}
                    </li>
                  ))}
                </ol>

                <div className={styles.buildIdentity}>
                  <span className={styles.builtIcon}>
                    {iconPreview ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={iconPreview} alt="" />
                    ) : (
                      <Smartphone size={25} aria-hidden="true" />
                    )}
                  </span>
                  <span>
                    <small>Aplicativo de</small>
                    <strong>{displayBusinessName}</strong>
                  </span>
                  <span className={styles.connectedBadge}>
                    <LockKeyhole size={13} aria-hidden="true" /> Já conectado
                  </span>
                </div>

                {build.status === "ready" ? (
                  <div className={styles.readyBlock}>
                    <button
                      type="button"
                      className={styles.downloadButton}
                      onClick={downloadApk}
                      disabled={downloading}
                    >
                      {downloading ? (
                        <LoaderCircle className={styles.spin} size={20} aria-hidden="true" />
                      ) : (
                        <Download size={20} aria-hidden="true" />
                      )}
                      {downloading ? "Preparando download…" : "Baixar meu aplicativo Android"}
                    </button>
                    {build.sha256 ? (
                      <div className={styles.checksum}>
                        <FileCheck2 size={16} aria-hidden="true" />
                        <span>
                          <strong>SHA-256 verificado</strong>
                          <code>{build.sha256}</code>
                        </span>
                      </div>
                    ) : null}
                  </div>
                ) : (
                  <button type="button" className={styles.refreshButton} onClick={refreshBuild}>
                    <RefreshCw size={16} aria-hidden="true" /> Atualizar andamento
                  </button>
                )}

                {notice ? <div className={styles.notice}>{notice}</div> : null}
                {error ? (
                  <div className={styles.error} role="alert">
                    <CircleAlert size={17} aria-hidden="true" /> {error}
                  </div>
                ) : null}
                {build.status === "failed" ? (
                  <button type="button" className={styles.resetButton} onClick={reset}>
                    Corrigir os dados e tentar novamente
                  </button>
                ) : null}
                {build.status !== "failed" ? (
                  <button type="button" className={styles.logoutButton} onClick={reset}>
                    Encerrar esta sessão de download
                  </button>
                ) : null}
              </div>
            )}
          </div>

          <aside className={styles.previewPanel} aria-label="Prévia do aplicativo personalizado">
            <div className={styles.previewTopline}>
              <span>
                <Clock3 size={15} aria-hidden="true" /> Teste de 7 dias
              </span>
              <small>Começa na primeira ativação</small>
            </div>
            <div className={styles.phone}>
              <div className={styles.phoneSpeaker} aria-hidden="true" />
              <div className={styles.phoneScreen}>
                <div
                  className={styles.coverPreview}
                  style={coverPreview ? { backgroundImage: `url(${coverPreview})` } : undefined}
                >
                  <span className={styles.coverShade} />
                  <span className={styles.appIdentity}>
                    <span className={styles.appIcon}>
                      {iconPreview ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={iconPreview} alt="" />
                      ) : (
                        <CalendarMark />
                      )}
                    </span>
                    <span>
                      <small>Bem-vindo ao</small>
                      <strong>{businessName.trim() || "Seu estabelecimento"}</strong>
                    </span>
                  </span>
                </div>
                <div className={styles.phoneContent}>
                  <span className={styles.phoneBadge}>
                    <BadgeCheck size={13} aria-hidden="true" /> Conectado automaticamente
                  </span>
                  <strong>Sua agenda já está pronta.</strong>
                  <p>Clientes, equipe e atendimentos ficam sincronizados com sua conta.</p>
                  <div className={styles.fakeAgenda}>
                    <span><i /> 09:00</span>
                    <span><i /> 10:30</span>
                    <span><i /> 13:00</span>
                  </div>
                </div>
              </div>
            </div>

            <div className={styles.previewFacts}>
              <div>
                <ShieldCheck size={18} aria-hidden="true" />
                <span>
                  <strong>Ativação protegida</strong>
                  <small>Sem senha dentro do APK</small>
                </span>
              </div>
              <div>
                <LockKeyhole size={18} aria-hidden="true" />
                <span>
                  <strong>Acesso controlado</strong>
                  <small>Ao fim do teste, é preciso pagamento para continuar</small>
                </span>
              </div>
            </div>
          </aside>
        </div>

        <div className={styles.installGuide}>
          <div>
            <span className={styles.guideNumber}>1</span>
            <span><strong>Baixe o arquivo privado</strong><small>O nome do estabelecimento aparece no arquivo.</small></span>
          </div>
          <ChevronRight size={18} aria-hidden="true" />
          <div>
            <span className={styles.guideNumber}>2</span>
            <span><strong>Autorize e instale</strong><small>Se solicitado, permita a instalação pelo navegador ou Arquivos.</small></span>
          </div>
          <ChevronRight size={18} aria-hidden="true" />
          <div>
            <span className={styles.guideNumber}>3</span>
            <span><strong>Abra e comece</strong><small>Sem login no Android; a primeira abertura ativa os 7 dias.</small></span>
          </div>
        </div>
      </div>
    </section>
  );
}

function CalendarMark() {
  return (
    <span className={styles.calendarMark} aria-hidden="true">
      <span />
      <i />
    </span>
  );
}
