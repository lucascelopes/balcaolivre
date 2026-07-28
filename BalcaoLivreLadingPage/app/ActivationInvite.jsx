"use client";

import { ArrowRight, CheckCircle, DownloadSimple, ShieldCheck, WindowsLogo } from "@phosphor-icons/react";
import { onlineDownloadUrl } from "./siteLinks";

export default function ActivationInvite({ token }) {
  const deepLink = `balcaolivre://activate?token=${encodeURIComponent(token)}`;

  function openWindows() {
    window.location.href = deepLink;
  }

  return (
    <main className="activationInvite">
      <section className="activationInviteCard">
        <span className="activationInviteIcon"><WindowsLogo size={38} weight="fill" /></span>
        <p className="paymentFlowEyebrow">Convite seguro de dispositivo</p>
        <h1>Ative este computador no Balcão Livre</h1>
        <p>
          Este acesso é descartável, expira rapidamente e ocupa somente a vaga
          desktop que foi reservada para este computador.
        </p>
        <button type="button" className="paymentFlowSubmit" onClick={openWindows}>
          Abrir o Balcão Livre
          <ArrowRight size={20} weight="bold" />
        </button>
        <a className="activationInviteDownload" href={onlineDownloadUrl}>
          <DownloadSimple size={20} />
          Ainda não tenho o aplicativo — baixar Windows
        </a>
        <div className="activationInviteNotes">
          <span><ShieldCheck size={19} /> Nenhuma chave permanente é exibida.</span>
          <span><CheckCircle size={19} /> Links usados, vencidos ou revogados são recusados.</span>
        </div>
      </section>
    </main>
  );
}
