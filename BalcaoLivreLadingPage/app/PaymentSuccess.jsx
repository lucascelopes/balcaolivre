"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  ArrowRight,
  Browser,
  Check,
  CheckCircle,
  Devices,
  Headset,
  LockKey,
  ShieldCheck,
  WindowsLogo
} from "@phosphor-icons/react";
import { handoffFunctionUrl, onlineDownloadUrl, pdvWebUrl } from "./siteLinks";

const previewData = {
  ok: true,
  paid: true,
  purchase: {
    planCode: "completo-anual",
    planName: "Balcão Livre Completo Anual",
    currentPeriodEnd: "2027-07-28T23:59:59.000Z",
    desktopSeats: 1,
    mobileSeats: 1,
    machineIncluded: true
  },
  handoff: {
    claimUrl: "#criar-conta?claim=preview",
    webUrl: pdvWebUrl,
    windowsDeepLink: "balcaolivre://activate?token=preview",
    windowsInstallerUrl: onlineDownloadUrl
  }
};

export default function PaymentSuccess({ sessionId, preview = false }) {
  const [state, setState] = useState(
    preview
      ? { loading: false, error: "", data: previewData }
      : { loading: true, data: null, error: "" }
  );
  const [accountMode, setAccountMode] = useState("signup");
  const [accountLinked, setAccountLinked] = useState(false);
  const [selectedTarget, setSelectedTarget] = useState("");
  const [accountForm, setAccountForm] = useState({ name: "", email: "", password: "" });
  const [accountState, setAccountState] = useState({ loading: false, error: "", notice: "" });
  const firstFieldRef = useRef(null);

  useEffect(() => {
    if (preview) return undefined;
    let active = true;

    async function load() {
      try {
        const response = await fetch(
          `/api/checkout/status?session_id=${encodeURIComponent(sessionId)}`,
          { cache: "no-store" }
        );
        const data = await response.json();
        if (!active) return;
        if (!response.ok || !data.ok) {
          setState({
            loading: false,
            data: null,
            error: data.message || "Pagamento ainda não confirmado."
          });
          return;
        }
        setState({ loading: false, data, error: "" });
      } catch {
        if (active) {
          setState({
            loading: false,
            data: null,
            error: "Não foi possível preparar seu acesso agora."
          });
        }
      }
    }

    load();
    return () => {
      active = false;
    };
  }, [preview, sessionId]);

  const planSummary = useMemo(() => {
    const purchase = state.data?.purchase;
    if (!purchase) return "";
    const validUntil = purchase.currentPeriodEnd
      ? new Date(purchase.currentPeriodEnd).toLocaleDateString("pt-BR")
      : "";
    return `${purchase.planName}${validUntil ? ` • válido até ${validUntil}` : ""}`;
  }, [state.data]);

  function requestTarget(target) {
    if (accountLinked) {
      executeTarget(target, state.data.handoff);
      return;
    }
    setSelectedTarget(target);
    setAccountState({
      loading: false,
      error: "",
      notice: target === "web"
        ? "Crie ou entre na conta para abrir o PDV Web."
        : "Crie ou entre na conta para liberar o download do Windows."
    });
    firstFieldRef.current?.focus();
    window.scrollTo({ top: 110, behavior: "smooth" });
  }

  async function submitAccount(event) {
    event.preventDefault();
    setAccountState({ loading: true, error: "", notice: "" });

    if (preview) {
      window.setTimeout(() => {
        setAccountLinked(true);
        setAccountState({
          loading: false,
          error: "",
          notice: "Conta vinculada. Agora escolha onde entrar."
        });
        if (selectedTarget) executeTarget(selectedTarget, state.data.handoff);
      }, 450);
      return;
    }

    try {
      const configResponse = await fetch("/api/balcao/account/config", { cache: "no-store" });
      const config = await configResponse.json();
      if (!configResponse.ok || !config.supabaseUrl || !config.publishableKey) {
        throw new Error("A criação de conta está temporariamente indisponível.");
      }

      const authUrl = accountMode === "signup"
        ? `${config.supabaseUrl}/auth/v1/signup`
        : `${config.supabaseUrl}/auth/v1/token?grant_type=password`;
      const authResponse = await fetch(authUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          apikey: config.publishableKey,
          Authorization: `Bearer ${config.publishableKey}`
        },
        body: JSON.stringify({
          email: accountForm.email.trim(),
          password: accountForm.password,
          ...(accountMode === "signup"
            ? { data: { full_name: accountForm.name.trim(), product: "balcao_livre" } }
            : {})
        })
      });
      const auth = await authResponse.json().catch(() => ({}));
      if (!authResponse.ok) {
        throw new Error(
          auth.error_description ||
            auth.msg ||
            auth.message ||
            "Não foi possível entrar nessa conta."
        );
      }

      const accessToken = auth.access_token || auth.session?.access_token;
      if (!accessToken) {
        setAccountState({
          loading: false,
          error: "",
          notice: "Confirme o e-mail que enviamos e depois entre por esta mesma tela."
        });
        setAccountMode("signin");
        return;
      }

      const claimToken = tokenFromUrl(state.data.handoff.claimUrl, "claim");
      const claimResponse = await fetch(handoffFunctionUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`
        },
        body: JSON.stringify({ action: "claim_account", token: claimToken })
      });
      const claim = await claimResponse.json().catch(() => ({}));
      if (!claimResponse.ok || !claim.ok) {
        throw new Error(claim.message || "Não foi possível vincular a compra à sua conta.");
      }

      localStorage.setItem(
        "balcao_livre_account",
        JSON.stringify({
          accessToken,
          refreshToken: auth.refresh_token || auth.session?.refresh_token || "",
          email: accountForm.email.trim(),
          storeId: claim.storeId
        })
      );

      setAccountLinked(true);
      setAccountState({
        loading: false,
        error: "",
        notice: "Compra vinculada. Escolha onde entrar."
      });
      if (selectedTarget) executeTarget(selectedTarget, state.data.handoff);
    } catch (error) {
      setAccountState({
        loading: false,
        error: error instanceof Error ? error.message : "Não foi possível criar sua conta.",
        notice: ""
      });
    }
  }

  if (state.loading) {
    return (
      <main className="paymentStatusScreen" aria-live="polite">
        <div className="paymentStatusSpinner" aria-hidden="true" />
        <h1>Confirmando seu pagamento</h1>
        <p>Estamos criando sua assinatura e preparando o acesso automático.</p>
      </main>
    );
  }

  if (state.error) {
    return (
      <main className="paymentStatusScreen paymentStatusError" aria-live="polite">
        <span className="paymentStatusIcon" aria-hidden="true">!</span>
        <h1>A confirmação ainda não chegou</h1>
        <p>{state.error}</p>
        <small>Se o pagamento acabou de ser feito, aguarde alguns segundos e tente novamente.</small>
        <button type="button" className="paymentPrimaryAction" onClick={() => window.location.reload()}>
          Tentar novamente
        </button>
        <a href="/" className="paymentTextLink">Voltar para a página inicial</a>
      </main>
    );
  }

  return (
    <div className="paymentFlowPage" aria-live="polite">
      <header className="paymentFlowHeader">
        <a className="paymentFlowBrand" href="/" aria-label="Balcão Livre PDV — página inicial">
          <img src="/brand/bl-orange-icon.png" alt="" />
          <strong>Balcão Livre PDV</strong>
        </a>
        <div className="paymentFlowHeaderStatus">
          <span className="paymentFlowSecureBadge">
            <ShieldCheck size={18} weight="bold" aria-hidden="true" />
            Compra protegida
          </span>
          <span className="paymentFlowApprovedBadge">
            <CheckCircle size={19} weight="fill" aria-hidden="true" />
            Pagamento aprovado
          </span>
        </div>
      </header>

      <nav className="paymentFlowSteps" aria-label="Etapas de liberação">
        <div className="isDone">
          <span><Check size={16} weight="bold" /></span>
          <b>1. Pagamento</b>
        </div>
        <i aria-hidden="true" />
        <div className={!accountLinked ? "isActive" : "isDone"}>
          <span>{accountLinked ? <Check size={16} weight="bold" /> : "2"}</span>
          <b>2. Criar conta</b>
        </div>
        <i aria-hidden="true" />
        <div className={accountLinked ? "isActive" : ""}>
          <span>3</span>
          <b>3. Entrar no PDV</b>
        </div>
      </nav>

      <main className="paymentFlowMain">
        <section className="paymentFlowAccountPanel" aria-labelledby="payment-flow-title">
          <span className="paymentFlowEyebrow">Sua compra já está aqui</span>
          <h1 id="payment-flow-title">Crie seu acesso ao Balcão Livre</h1>
          <p>
            Seu plano foi liberado e será vinculado automaticamente à sua conta após o cadastro.
          </p>

          <div className="paymentFlowTabs" role="tablist" aria-label="Acesso à conta">
            <button
              type="button"
              className={accountMode === "signup" ? "isActive" : ""}
              onClick={() => setAccountMode("signup")}
            >
              Criar conta
            </button>
            <button
              type="button"
              className={accountMode === "signin" ? "isActive" : ""}
              onClick={() => setAccountMode("signin")}
            >
              Já tenho conta
            </button>
          </div>

          <form className="paymentFlowForm" onSubmit={submitAccount}>
            {accountMode === "signup" ? (
              <label>
                Nome completo
                <input
                  ref={firstFieldRef}
                  required
                  minLength={2}
                  autoComplete="name"
                  placeholder="Digite seu nome completo"
                  value={accountForm.name}
                  onChange={(event) => setAccountForm({ ...accountForm, name: event.target.value })}
                />
              </label>
            ) : null}
            <label>
              E-mail
              <input
                ref={accountMode === "signin" ? firstFieldRef : null}
                required
                type="email"
                autoComplete="email"
                placeholder="seu@email.com"
                value={accountForm.email}
                onChange={(event) => setAccountForm({ ...accountForm, email: event.target.value })}
              />
            </label>
            <label>
              Senha
              <input
                required
                minLength={6}
                type="password"
                autoComplete={accountMode === "signup" ? "new-password" : "current-password"}
                placeholder={accountMode === "signup" ? "Crie uma senha segura" : "Digite sua senha"}
                value={accountForm.password}
                onChange={(event) => setAccountForm({ ...accountForm, password: event.target.value })}
              />
            </label>

            {accountState.error ? <p className="paymentFlowError">{accountState.error}</p> : null}
            {accountState.notice ? <p className="paymentFlowNotice">{accountState.notice}</p> : null}

            <button className="paymentFlowSubmit" disabled={accountState.loading || accountLinked}>
              {accountLinked
                ? "Compra vinculada"
                : accountState.loading
                  ? "Vinculando sua compra..."
                  : accountMode === "signup"
                    ? "Criar conta e continuar"
                    : "Entrar e continuar"}
              {!accountState.loading ? (
                accountLinked
                  ? <Check size={20} weight="bold" />
                  : <ArrowRight size={20} weight="bold" />
              ) : null}
            </button>
          </form>

          <div className="paymentFlowNoKey">
            <LockKey size={19} weight="regular" />
            <span><b>Sem chave de ativação</b> • Tudo é automático e seguro.</span>
          </div>
        </section>

        <section className="paymentFlowDestinations" aria-labelledby="payment-destinations-title">
          <div className="paymentFlowDestinationHeading">
            <h2 id="payment-destinations-title">O que vem a seguir</h2>
            <p>Escolha onde quer usar o Balcão Livre PDV.</p>
          </div>

          <article className={`paymentFlowDestination paymentFlowDestinationWeb${selectedTarget === "web" ? " isSelected" : ""}`}>
            <div className="paymentFlowDestinationCopy">
              <span className="paymentFlowRecommended">Recomendado</span>
              <Browser size={31} weight="regular" />
              <h3>PDV Web —<br />abrir no navegador</h3>
              <p>Acesse de qualquer dispositivo com internet. Sem instalação.</p>
              <button type="button" onClick={() => requestTarget("web")}>
                {accountLinked ? "Abrir PDV Web" : "Usar no navegador"}
                <ArrowRight size={17} weight="bold" />
              </button>
            </div>
            <div className="paymentFlowWebVisual" aria-label="Prévia real do PDV Web e do painel">
              <img className="paymentFlowWebDashboard" src="/brand/pdv-orange-dashboard.png" alt="" />
              <img
                className="paymentFlowWebPhone"
                src="/brand/pdv-flutter-mobile-orange.png"
                alt="Tela real laranja do Balcão Livre PDV Web com mesas e comandas"
              />
            </div>
          </article>

          <article className={`paymentFlowDestination paymentFlowDestinationWindows${selectedTarget === "windows" ? " isSelected" : ""}`}>
            <div className="paymentFlowDestinationCopy">
              <WindowsLogo size={31} weight="fill" />
              <h3>Windows —<br />baixar e instalar</h3>
              <p>Aplicativo completo para vendas e gestão no computador.</p>
              <button type="button" onClick={() => requestTarget("windows")}>
                {accountLinked ? "Abrir ou instalar Windows" : "Usar no computador"}
                <ArrowRight size={17} weight="bold" />
              </button>
            </div>
            <div className="paymentFlowWindowsVisual">
              <img
                src="/brand/pdv-orange-dashboard.png"
                alt="Tela real laranja do Balcão Livre PDV para Windows"
              />
            </div>
          </article>
        </section>
      </main>

      <section className="paymentFlowAssurance" aria-label="Benefícios do acesso">
        <article>
          <span><Devices size={27} weight="regular" /></span>
          <div><strong>Uma conta, todos os dispositivos</strong><p>Web e Windows sempre sincronizados.</p></div>
        </article>
        <article>
          <span><ShieldCheck size={27} weight="regular" /></span>
          <div><strong>Seus dados protegidos</strong><p>Acesso seguro vinculado ao dispositivo.</p></div>
        </article>
        <article>
          <span><Headset size={27} weight="regular" /></span>
          <div><strong>Suporte humano de verdade</strong><p>Conte com nossa equipe quando precisar.</p></div>
        </article>
      </section>

      <p className="paymentFlowPlan">{planSummary}</p>
    </div>
  );
}

function tokenFromUrl(value, name) {
  try {
    return new URL(value, window.location.origin).searchParams.get(name) || "";
  } catch {
    return "";
  }
}

function executeTarget(target, handoff) {
  if (target === "web") {
    window.location.assign(handoff.webUrl || pdvWebUrl);
    return;
  }
  localStorage.setItem("balcao_windows_handoff", handoff.windowsDeepLink || "");
  const installerUrl = handoff.windowsInstallerUrl || onlineDownloadUrl;
  const installerPreviouslyDownloaded =
    localStorage.getItem("balcao_windows_installer_downloaded") === "1";
  if (handoff.windowsDeepLink) {
    window.location.href = handoff.windowsDeepLink;
  }
  if (!installerPreviouslyDownloaded) {
    window.setTimeout(() => {
      if (document.visibilityState !== "visible") return;
      localStorage.setItem("balcao_windows_installer_downloaded", "1");
      const download = document.createElement("a");
      download.href = installerUrl;
      download.download = "";
      download.rel = "noopener";
      document.body.appendChild(download);
      download.click();
      download.remove();
    }, 900);
  }
}
