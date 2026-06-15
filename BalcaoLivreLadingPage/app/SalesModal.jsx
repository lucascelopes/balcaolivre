"use client";

import { useEffect, useMemo, useState } from "react";
import { sellers } from "./siteLinks";

const modalStorageKey = "balcaoLivreLucasSalesModalSeen";

function hasSeenModal() {
  try {
    return window.sessionStorage?.getItem(modalStorageKey) === "true";
  } catch {
    return false;
  }
}

function markModalSeen() {
  try {
    window.sessionStorage?.setItem(modalStorageKey, "true");
  } catch {
    // Storage can be unavailable in restricted browser contexts.
  }
}

export default function SalesModal() {
  const [isOpen, setIsOpen] = useState(false);
  const seller = useMemo(
    () => sellers.find((item) => item.name === "Lucas") || sellers[0],
    []
  );

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    if (!hasSeenModal()) {
      const timer = window.setTimeout(() => setIsOpen(true), 650);
      return () => window.clearTimeout(timer);
    }
  }, []);

  useEffect(() => {
    if (!isOpen || typeof window === "undefined") {
      return;
    }

    const onKeyDown = (event) => {
      if (event.key === "Escape") {
        closeModal();
      }
    };

    document.body.classList.add("blSalesModalOpen");
    window.addEventListener("keydown", onKeyDown);

    return () => {
      document.body.classList.remove("blSalesModalOpen");
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [isOpen]);

  function closeModal() {
    if (typeof window !== "undefined") {
      markModalSeen();
    }

    setIsOpen(false);
  }

  if (!isOpen || !seller) {
    return null;
  }

  return (
    <div
      className="blSalesModalOverlay"
      role="dialog"
      aria-modal="true"
      aria-labelledby="blSalesModalTitle"
      aria-describedby="blSalesModalDescription"
    >
      <div className="blSalesModal">
        <button
          className="blSalesModalClose"
          type="button"
          aria-label="Fechar conversa com vendedor"
          onClick={closeModal}
        >
          X
        </button>

        <div className="blSalesModalAvatar" aria-hidden="true">
          LC
        </div>

        <span className="blSalesModalKicker">Atendimento direto</span>
        <h2 id="blSalesModalTitle">Converse com o vendedor Lucas</h2>
        <p id="blSalesModalDescription">
          Tire duvidas sobre planos, teste de 7 dias, instalacao e qual versao do Balcao Livre combina com seu restaurante.
        </p>

        <div className="blSalesModalContact">
          <strong>Lucas</strong>
          <span>{seller.phone}</span>
        </div>

        <div className="blSalesModalActions">
          <a
            className="blSalesModalPrimary"
            href={seller.href}
            onClick={closeModal}
            data-analytics-action="whatsapp_click"
            data-analytics-location="entry_sales_modal"
            data-analytics-seller={seller.name}
          >
            Conversar agora
          </a>
          <button className="blSalesModalSecondary" type="button" onClick={closeModal}>
            Continuar no site
          </button>
        </div>
      </div>
    </div>
  );
}
