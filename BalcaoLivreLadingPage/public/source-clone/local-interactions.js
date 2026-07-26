import { initializeOperationShowcase } from "./operation-showcase.js";
import { initializeIntegrationShowcase } from "./integration-showcase.js";
import { initializeInsightsShowcase } from "./insights-showcase.js?v=insights-motion-replay-v2";
import { initializePricingShowcase } from "./pricing-showcase.js?v=pricing-guided-v1";

const monthlyPrices = ["R$ 49,90", "R$ 99,90"];
const annualPrices = ["12x R$ 49,90", "12x R$ 99,90"];

function initializeLocalLanding() {
  initializeOperationShowcase(document);
  initializeIntegrationShowcase(document);
  initializeInsightsShowcase(document);
  initializePricingShowcase(document);

  const menuButton = document.querySelector(".menu-button");
  const navigation = document.querySelector(".nav-links");
  const navigationLinks = [...document.querySelectorAll(".nav-links a, .logo-link")];
  const billingButtons = [...document.querySelectorAll(".billing-toggle button")];
  const priceCards = [...document.querySelectorAll(".price-card")];
  const annualOffer = document.querySelector(".annual-machine-offer");

  const closeMenu = () => {
    menuButton?.setAttribute("aria-expanded", "false");
    menuButton?.setAttribute("aria-label", "Abrir menu");
    navigation?.classList.remove("is-open");
  };

  menuButton?.addEventListener("click", (event) => {
    event.stopPropagation();
    const nextOpen = menuButton.getAttribute("aria-expanded") !== "true";
    menuButton.setAttribute("aria-expanded", String(nextOpen));
    menuButton.setAttribute("aria-label", nextOpen ? "Fechar menu" : "Abrir menu");
    navigation?.classList.toggle("is-open", nextOpen);
  });

  navigationLinks.forEach((link) => link.addEventListener("click", closeMenu));
  window.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeMenu();
  });

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

  billingButtons.forEach((button, index) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      setBilling(index === 1);
    });
  });

  setBilling(true);
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initializeLocalLanding, { once: true });
} else {
  initializeLocalLanding();
}
