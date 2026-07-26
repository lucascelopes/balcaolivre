const showcaseVersion = "pricing-guided-v1";
const money = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const profileMap = {
  simple: { plan: "essential", note: "Essencial é o melhor ponto de partida para o seu caixa." },
  room: { plan: "complete", note: "Completo conecta salão, caixa e gestão em um só fluxo." },
  kitchen: { plan: "complete", note: "Completo organiza cozinha, delivery e integrações." },
};

const prices = {
  essential: { monthly: 49.9, annual: 49.9 },
  complete: { monthly: 99.9, annual: 99.9 },
};

export function initializePricingShowcase(root = document) {
  const section = root.querySelector(".pricing-section");
  if (!section || section.dataset.pricingShowcaseVersion === showcaseVersion) return;

  section.dataset.pricingShowcaseReady = "true";
  section.dataset.pricingShowcaseVersion = showcaseVersion;
  section.innerHTML = `
    <div class="pricing-guided">
      <aside class="pricing-guide">
        <div class="pricing-guide-kicker"><span>09</span> PLANOS SEM COMPLICAÇÃO</div>
        <h2>Qual operação<br>combina com você?</h2>
        <p>Responda rápido e veja o plano ideal para o seu restaurante.</p>

        <div class="pricing-profile-list" role="list" aria-label="Escolha o perfil da sua operação">
          <button type="button" class="pricing-profile is-active" data-pricing-profile="simple" aria-pressed="true">
            <i><img src="/media/icon-credit-card.svg" alt=""></i>
            <span><strong>Caixa simples</strong><small>Preciso de um caixa rápido e controle do básico.</small></span>
            <b>Essencial</b>
          </button>
          <button type="button" class="pricing-profile" data-pricing-profile="room" aria-pressed="false">
            <i><img src="/media/icon-person-check.svg" alt=""></i>
            <span><strong>Salão conectado</strong><small>Tenho salão e preciso de gestão e pedidos integrados.</small></span>
            <b>Completo</b>
          </button>
          <button type="button" class="pricing-profile" data-pricing-profile="kitchen" aria-pressed="false">
            <i><img src="/media/icon-fire.svg" alt=""></i>
            <span><strong>Cozinha e delivery</strong><small>Trabalho com cozinha, delivery e múltiplos canais.</small></span>
            <b>Completo</b>
          </button>
        </div>

        <div class="pricing-guide-tip">
          <i><img src="/media/icon-arrow-left-right.svg" alt=""></i>
          <p><strong>Uma escolha flexível.</strong><span data-pricing-guidance>Essencial é o melhor ponto de partida para o seu caixa.</span></p>
        </div>
      </aside>

      <div class="pricing-decision">
        <div class="pricing-decision-top">
          <div class="billing-toggle pricing-billing-toggle" role="group" aria-label="Forma de contratação">
            <button type="button" data-pricing-billing="monthly">Mensal</button>
            <button type="button" class="active" data-pricing-billing="annual">Anual <span>+ Point*</span></button>
          </div>
          <p><img src="/media/icon-arrow-left-right.svg" alt=""> Melhor custo-benefício<br>e Point Pro 3 incluso.</p>
        </div>

        <div class="pricing-plan-list">
          <article class="pricing-plan-row is-recommended" data-pricing-plan="essential">
            <div class="pricing-plan-index"><span>01</span></div>
            <div class="pricing-plan-copy">
              <span class="pricing-recommendation">INDICADO PARA VOCÊ</span>
              <h3>Essencial</h3>
              <p>Para quem quer vender, controlar o estoque e acompanhar o caixa sem complicação.</p>
            </div>
            <ul>
              <li><img src="/media/icon-check-lg.svg" alt=""> Frente de caixa e catálogo</li>
              <li><img src="/media/icon-check-lg.svg" alt=""> Produtos, estoque e clientes</li>
              <li><img src="/media/icon-check-lg.svg" alt=""> Vendas, lucro e fechamento</li>
              <li><img src="/media/icon-check-lg.svg" alt=""> Suporte humano no WhatsApp</li>
            </ul>
            <div class="pricing-plan-buy">
              <small data-pricing-period>no plano anual</small>
              <strong><span class="pricing-installments" data-pricing-installments>12x </span>R$ <span data-pricing-value="essential">49,90</span></strong>
              <a href="https://wa.me/5533991314120?text=Ol%C3%A1%21%20Quero%20conhecer%20o%20Balc%C3%A3o%20Livre%20PDV." target="_blank" rel="noreferrer">Escolher Essencial <span>↗</span></a>
            </div>
          </article>

          <article class="pricing-plan-row" data-pricing-plan="complete">
            <div class="pricing-plan-index"><span>02</span></div>
            <div class="pricing-plan-copy">
              <span class="pricing-recommendation">INDICADO PARA VOCÊ</span>
              <h3>Completo</h3>
              <p>Para quem atende no salão, produz na cozinha e vende também pelo delivery.</p>
            </div>
            <ul>
              <li><img src="/media/icon-check-lg.svg" alt=""> Tudo do plano Essencial</li>
              <li><img src="/media/icon-check-lg.svg" alt=""> Mesas, comandas, taxas e equipe</li>
              <li><img src="/media/icon-check-lg.svg" alt=""> Cozinha por praça, voz e atrasos</li>
              <li><img src="/media/icon-check-lg.svg" alt=""> Delivery, WhatsApp e iFood</li>
            </ul>
            <div class="pricing-plan-buy">
              <small data-pricing-period>no plano anual</small>
              <strong><span class="pricing-installments" data-pricing-installments>12x </span>R$ <span data-pricing-value="complete">99,90</span></strong>
              <a href="https://wa.me/5533991314120?text=Ol%C3%A1%21%20Quero%20conhecer%20o%20Balc%C3%A3o%20Livre%20PDV." target="_blank" rel="noreferrer">Escolher Completo <span>↗</span></a>
            </div>
          </article>
        </div>

        <div class="pricing-machine-stage" data-pricing-machine-stage>
          <div class="pricing-machine-copy">
            <span>MAIS ESCOLHIDO</span>
            <small data-pricing-bonus-label>BÔNUS NO ANUAL:</small>
            <strong>Point Pro 3 incluso</strong>
            <p>Plano Completo por 1 ano, implantação acompanhada e Point Pro 3 para receber no balcão.</p>
          </div>
          <ul class="pricing-machine-benefits">
            <li><img src="/media/icon-credit-card.svg" alt=""><span>Aceite por aproximação,<br>chip e tarja</span></li>
            <li><img src="/media/icon-bag-check.svg" alt=""><span>Impressão rápida<br>de comprovantes</span></li>
            <li><img src="/media/icon-arrow-left-right.svg" alt=""><span>Conexão Wi-Fi e chip<br>com autonomia</span></li>
            <li><img src="/media/icon-check-lg.svg" alt=""><span>Bateria de longa duração<br>para o dia todo</span></li>
          </ul>
          <div class="pricing-machine-visual">
            <img src="/media/point-pro-3-cutout.webp" alt="Maquininha Point Pro 3 amarela incluída no plano anual">
          </div>
        </div>
        <p class="pricing-guided-note">*Condição da maquininha vinculada ao plano anual Completo, sujeita à disponibilidade e às condições comerciais apresentadas no atendimento.</p>
      </div>
    </div>
  `;

  const profileButtons = [...section.querySelectorAll("[data-pricing-profile]")];
  const billingButtons = [...section.querySelectorAll("[data-pricing-billing]")];
  const planRows = [...section.querySelectorAll("[data-pricing-plan]")];
  const valueNodes = [...section.querySelectorAll("[data-pricing-value]")];
  const periodNodes = [...section.querySelectorAll("[data-pricing-period]")];
  const installmentNodes = [...section.querySelectorAll("[data-pricing-installments]")];
  const guidance = section.querySelector("[data-pricing-guidance]");
  const bonusLabel = section.querySelector("[data-pricing-bonus-label]");
  let billing = "annual";
  let motionTimer = 0;

  const setRecommendation = (profileKey) => {
    const profile = profileMap[profileKey] || profileMap.simple;
    profileButtons.forEach((button) => {
      const active = button.dataset.pricingProfile === profileKey;
      button.classList.toggle("is-active", active);
      button.setAttribute("aria-pressed", String(active));
    });
    planRows.forEach((row) => row.classList.toggle("is-recommended", row.dataset.pricingPlan === profile.plan));
    if (guidance) guidance.textContent = profile.note;
    section.dataset.pricingRecommendedPlan = profile.plan;
  };

  const setBilling = (nextBilling) => {
    billing = nextBilling;
    section.dataset.pricingBilling = billing;
    billingButtons.forEach((button) => button.classList.toggle("active", button.dataset.pricingBilling === billing));
    periodNodes.forEach((node) => { node.textContent = billing === "annual" ? "no plano anual" : "por mês"; });
    installmentNodes.forEach((node) => { node.hidden = billing !== "annual"; });
    valueNodes.forEach((node) => {
      const plan = node.dataset.pricingValue;
      node.textContent = money.format(prices[plan]?.[billing] || 0);
    });
    if (bonusLabel) bonusLabel.textContent = billing === "annual" ? "BÔNUS NO ANUAL:" : "DISPONÍVEL NO PLANO ANUAL:";
    section.classList.toggle("is-pricing-monthly", billing === "monthly");
  };

  const playMotion = () => {
    window.clearTimeout(motionTimer);
    section.classList.remove("is-pricing-running", "is-pricing-complete");
    void section.offsetWidth;
    section.classList.add("is-pricing-running", "is-pricing-motion-forced");
    motionTimer = window.setTimeout(() => {
      section.classList.add("is-pricing-complete");
      section.classList.remove("is-pricing-motion-forced");
    }, 4200);
  };

  profileButtons.forEach((button) => {
    button.addEventListener("click", () => {
      setRecommendation(button.dataset.pricingProfile);
      section.classList.remove("is-profile-switching");
      void section.offsetWidth;
      section.classList.add("is-profile-switching");
      window.setTimeout(() => section.classList.remove("is-profile-switching"), 720);
    });
  });

  billingButtons.forEach((button) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      setBilling(button.dataset.pricingBilling);
      section.classList.remove("is-machine-reacting");
      void section.offsetWidth;
      section.classList.add("is-machine-reacting");
      window.setTimeout(() => section.classList.remove("is-machine-reacting"), 850);
    });
  });

  setRecommendation("room");
  setBilling("annual");

  if (!("IntersectionObserver" in window)) {
    section.classList.add("is-pricing-running", "is-pricing-complete");
    return;
  }

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting && entry.intersectionRatio > .15 && !section.classList.contains("is-pricing-complete")) {
        playMotion();
      }
    });
  }, { threshold: [0, .15, .45], rootMargin: "0px 0px -8% 0px" });
  observer.observe(section);
}
