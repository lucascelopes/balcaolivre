"use client";

import { useEffect, useState } from "react";
import { checkoutFunctionUrl } from "./siteLinks";

export default function PaymentSuccess({ sessionId }) {
  const [state, setState] = useState({ loading: true, data: null, error: "" });

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const response = await fetch(`${checkoutFunctionUrl}/status?session_id=${encodeURIComponent(sessionId)}`, {
          cache: "no-store"
        });
        const data = await response.json();

        if (!active) return;

        if (!response.ok || !data.ok) {
          setState({ loading: false, data: null, error: data.message || "Pagamento ainda nao confirmado." });
          return;
        }

        setState({ loading: false, data, error: "" });
      } catch {
        if (active) {
          setState({ loading: false, data: null, error: "Nao foi possivel carregar a chave agora." });
        }
      }
    }

    load();
    return () => {
      active = false;
    };
  }, [sessionId]);

  return (
    <section className="paymentSuccessShell" aria-live="polite">
      <article className="paymentSuccessCard">
        <span className="paymentSuccessMark">Pago</span>
        <h1>Pagamento confirmado</h1>
        {state.loading ? (
          <p>Gerando sua chave e salvando a compra no sistema...</p>
        ) : state.error ? (
          <>
            <p>{state.error}</p>
            <small>Se o pagamento acabou de ser feito, aguarde alguns segundos e atualize esta pagina.</small>
          </>
        ) : (
          <>
            <p>Sua compra foi registrada e a chave abaixo ja ficou salva no banco de dados.</p>
            <div className="paymentLicenseBox">
              <span>Chave de ativacao</span>
              <strong>{state.data.license.key}</strong>
            </div>
            <dl className="paymentLicenseMeta">
              <div>
                <dt>Plano</dt>
                <dd>{state.data.license.plan}</dd>
              </div>
              <div>
                <dt>Validade</dt>
                <dd>{new Date(state.data.license.expiresAt).toLocaleDateString("pt-BR")}</dd>
              </div>
            </dl>
            {state.data.license.installerUrl ? (
              <a className="lpPlanButton paymentSuccessBack" href={state.data.license.installerUrl}>
                Baixar instalador
              </a>
            ) : null}
          </>
        )}
        <a href="/" className="lpPlanButton paymentSuccessBack">Voltar para a pagina inicial</a>
      </article>
    </section>
  );
}
