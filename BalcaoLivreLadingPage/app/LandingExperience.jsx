"use client";

import { useEffect, useRef, useState } from "react";

const monthlyPrices = ["R$ 49,90", "R$ 99,90"];
const annualPrices = ["12x R$ 49,90", "12x R$ 99,90"];

export default function LandingExperience({ initialMarkup = "" }) {
  const hostRef = useRef(null);
  const [markup, setMarkup] = useState(initialMarkup);

  useEffect(() => {
    if (initialMarkup) return undefined;

    let active = true;

    fetch("/source-clone/page.html")
      .then((response) => {
        if (!response.ok) throw new Error(`Falha ao carregar a landing: ${response.status}`);
        return response.text();
      })
      .then((html) => {
        if (!active) return;
        const documentClone = new DOMParser().parseFromString(html, "text/html");
        const shell = documentClone.querySelector(".site-shell");
        setMarkup(shell?.outerHTML || "");
      })
      .catch(() => {
        if (active) setMarkup("");
      });

    return () => {
      active = false;
    };
  }, [initialMarkup]);

  useEffect(() => {
    if (!markup || !hostRef.current) return undefined;

    const host = hostRef.current;
    let showcaseActive = true;
    Promise.all([
      import(/* webpackIgnore: true */ "/source-clone/operation-showcase.js"),
      import(/* webpackIgnore: true */ "/source-clone/integration-showcase.js"),
      import(/* webpackIgnore: true */ "/source-clone/insights-showcase.js?v=insights-motion-replay-v2"),
      import(/* webpackIgnore: true */ "/source-clone/pricing-showcase.js?v=pricing-guided-v1"),
    ]).then(([operationModule, integrationModule, insightsModule, pricingModule]) => {
      if (!showcaseActive) return;
      operationModule.initializeOperationShowcase(host);
      integrationModule.initializeIntegrationShowcase(host);
      insightsModule.initializeInsightsShowcase(host);
      pricingModule.initializePricingShowcase(host);
    });
    const menuButton = host.querySelector(".menu-button");
    const navigation = host.querySelector(".nav-links");
    const navigationLinks = [...host.querySelectorAll(".nav-links a, .logo-link")];
    const billingButtons = [...host.querySelectorAll(".billing-toggle button")];
    const priceCards = [...host.querySelectorAll(".price-card")];
    const annualOffer = host.querySelector(".annual-machine-offer");

    const closeMenu = () => {
      menuButton?.setAttribute("aria-expanded", "false");
      menuButton?.setAttribute("aria-label", "Abrir menu");
      navigation?.classList.remove("is-open");
    };

    const toggleMenu = () => {
      const nextOpen = menuButton?.getAttribute("aria-expanded") !== "true";
      menuButton?.setAttribute("aria-expanded", String(nextOpen));
      menuButton?.setAttribute("aria-label", nextOpen ? "Fechar menu" : "Abrir menu");
      navigation?.classList.toggle("is-open", nextOpen);
    };

    const handleEscape = (event) => {
      if (event.key === "Escape") closeMenu();
    };

    const setBilling = (annual) => {
      billingButtons.forEach((button, index) => {
        button.classList.toggle("active", annual ? index === 1 : index === 0);
      });

      priceCards.forEach((card, index) => {
        const value = card.querySelector(".price strong");
        const label = card.querySelector(".price span");
        if (value) value.textContent = annual ? annualPrices[index] : monthlyPrices[index];
        if (label) label.textContent = annual ? "no plano anual" : "por mês";
      });

      if (annualOffer) annualOffer.hidden = !annual;
    };

    const billingHandlers = billingButtons.map((button, index) => {
      const handler = () => setBilling(index === 1);
      button.addEventListener("click", handler);
      return [button, handler];
    });

    menuButton?.addEventListener("click", toggleMenu);
    navigationLinks.forEach((link) => link.addEventListener("click", closeMenu));
    window.addEventListener("keydown", handleEscape);
    setBilling(true);

    return () => {
      showcaseActive = false;
      menuButton?.removeEventListener("click", toggleMenu);
      navigationLinks.forEach((link) => link.removeEventListener("click", closeMenu));
      billingHandlers.forEach(([button, handler]) => button.removeEventListener("click", handler));
      window.removeEventListener("keydown", handleEscape);
    };
  }, [markup]);

  return (
    <>
      <link rel="stylesheet" href="/source-clone/fonts.css" />
      <link rel="stylesheet" href="/source-clone/source.css" />
      <link rel="stylesheet" href="/source-clone/effects.css?v=pricing-guided-v1" />
      <div
        ref={hostRef}
        className="source-landing-root"
        dangerouslySetInnerHTML={{ __html: markup }}
      />
    </>
  );
}
