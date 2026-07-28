const integrationMarkup = `
  <div class="integration-control-room" aria-label="Canais conectados à operação do Balcão Livre">
    <div class="integration-control-copy">
      <div class="section-kicker integration-control-kicker"><span>07</span>Tudo conversa</div>
      <h2>Quatro canais lá fora.<br />Uma operação aqui dentro.</h2>
      <p>Pedidos, pagamentos e clientes de todos os canais integrados em tempo real no Balcão Livre PDV.</p>

      <div class="integration-control-brands" aria-label="Integrações do Balcão Livre">
        <div class="integration-control-brand integration-control-ifood" data-flow-brand="ifood">
          <img src="/media/ifood-logo.svg" alt="iFood" />
          <i aria-hidden="true"></i>
        </div>
        <div class="integration-control-brand integration-control-whatsapp" data-flow-brand="whatsapp">
          <img src="/media/whatsapp-logo.svg" alt="" />
          <strong>WhatsApp</strong>
          <i aria-hidden="true"></i>
        </div>
        <div class="integration-control-brand integration-control-mercado" data-flow-brand="mercado">
          <img src="/media/mercado-pago-logo.svg" alt="Mercado Pago" />
          <i aria-hidden="true"></i>
        </div>
        <div class="integration-control-brand integration-control-ai" data-flow-brand="ai">
          <img src="/media/icon-robot.svg" alt="" />
          <strong>IA do Balcão</strong>
          <i aria-hidden="true"></i>
        </div>
      </div>
    </div>

    <div class="integration-control-layout">
      <div class="integration-control-product" aria-label="Telas reais do Balcão Livre">
        <span class="integration-energy-line integration-energy-line-one" aria-hidden="true"></span>
        <span class="integration-energy-line integration-energy-line-two" aria-hidden="true"></span>

        <figure class="integration-control-screen integration-control-dashboard">
          <img
            src="/media/painel-caixa.png"
            alt="Painel real do Balcão Livre com vendas, lucro e pendências"
            loading="eager"
          />
        </figure>

        <figure class="integration-control-screen integration-control-command">
          <img
            src="/media/comandas.jpeg"
            alt="Tela real de mesas e comandas do Balcão Livre"
            loading="eager"
          />
        </figure>

        <div class="integration-live-toast" data-flow-toast>
          <span class="integration-live-toast-icon"><img src="/media/icon-bag-check.svg" alt="" data-flow-toast-icon /></span>
          <div>
            <small data-flow-toast-detail>Recebido do iFood</small>
            <strong data-flow-toast-title>Novo pedido #1058</strong>
          </div>
        </div>
      </div>

      <aside class="integration-control-flow" aria-label="Fluxo sincronizado da operação">
        <div class="integration-flow-items">
          <article data-flow-step="0">
            <span class="integration-flow-icon"><img src="/media/icon-bag-check.svg" alt="" /></span>
            <div><time>12:41</time><strong>pedido iFood recebido</strong></div>
          </article>
          <article data-flow-step="1">
            <span class="integration-flow-icon"><img src="/media/whatsapp-logo.svg" alt="" /></span>
            <div><time>12:42</time><strong>pedido WhatsApp recebido</strong></div>
          </article>
          <article data-flow-step="2">
            <span class="integration-flow-icon"><img src="/media/icon-fire.svg" alt="" /></span>
            <div><time>12:44</time><strong>enviado à cozinha</strong></div>
          </article>
          <article data-flow-step="3">
            <span class="integration-flow-icon"><img src="/media/icon-credit-card.svg" alt="" /></span>
            <div><time>12:45</time><strong>pago no balcão</strong></div>
          </article>
        </div>

        <div class="integration-flow-complete" data-flow-complete>
          <span><img src="/media/icon-check-lg.svg" alt="" /></span>
          <strong>Tudo<br />sincronizado</strong>
        </div>
      </aside>
    </div>

    <p class="integration-control-tagline"><span aria-hidden="true"></span>Menos troca de tela. Menos erro. Mais restaurante rodando.<span aria-hidden="true"></span></p>
  </div>
`;

const flowStates = [
  {
    brand: "ifood",
    title: "Novo pedido #1058",
    detail: "Recebido do iFood",
    icon: "/media/icon-bag-check.svg",
    duration: 1850,
  },
  {
    brand: "whatsapp",
    title: "Novo pedido #1059",
    detail: "Recebido do WhatsApp",
    icon: "/media/whatsapp-logo.svg",
    duration: 1850,
  },
  {
    brand: "none",
    title: "Pedido enviado",
    detail: "Cozinha avisada agora",
    icon: "/media/icon-fire.svg",
    duration: 1750,
  },
  {
    brand: "mercado",
    title: "Venda recebida",
    detail: "Pago no balcão com a Point",
    icon: "/media/icon-credit-card.svg",
    duration: 1950,
  },
  {
    brand: "all",
    title: "Operação sincronizada",
    detail: "Pedidos e caixa atualizados",
    icon: "/media/icon-check-lg.svg",
    duration: 2400,
  },
];

function setFlowStage(section, stageIndex) {
  const state = flowStates[stageIndex];
  const brands = [...section.querySelectorAll("[data-flow-brand]")];
  const steps = [...section.querySelectorAll("[data-flow-step]")];
  const product = section.querySelector(".integration-control-product");
  const complete = section.querySelector("[data-flow-complete]");
  const toast = section.querySelector("[data-flow-toast]");
  const toastIcon = section.querySelector("[data-flow-toast-icon]");
  const toastTitle = section.querySelector("[data-flow-toast-title]");
  const toastDetail = section.querySelector("[data-flow-toast-detail]");

  section.dataset.flowStage = String(stageIndex);
  product?.setAttribute("data-flow-stage", String(stageIndex));

  brands.forEach((brand) => {
    const active = state.brand === "all" || brand.dataset.flowBrand === state.brand;
    brand.dataset.flowState = active ? "active" : "idle";
  });

  steps.forEach((step, index) => {
    step.dataset.flowState = index === stageIndex ? "active" : index < stageIndex ? "past" : "idle";
  });

  if (complete) complete.dataset.flowState = stageIndex === flowStates.length - 1 ? "active" : "idle";
  if (toastIcon) toastIcon.src = state.icon;
  if (toastTitle) toastTitle.textContent = state.title;
  if (toastDetail) toastDetail.textContent = state.detail;

  if (toast) {
    toast.classList.remove("is-changing");
    void toast.offsetWidth;
    toast.classList.add("is-changing");
    toast.style.setProperty("--flow-stage-duration", `${state.duration}ms`);
  }
}

function initializeIntegrationMotion(section) {
  if (section.dataset.integrationMotionReady === "true") return;
  section.dataset.integrationMotionReady = "true";

  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  let stageIndex = reducedMotion ? flowStates.length - 1 : 0;
  let timeoutId;
  let isVisible = false;

  const stop = () => {
    window.clearTimeout(timeoutId);
    timeoutId = undefined;
    section.classList.remove("is-motion-running");
  };

  const scheduleNext = () => {
    stop();
    if (!isVisible || document.hidden || reducedMotion) return;
    section.classList.add("is-motion-running");
    timeoutId = window.setTimeout(() => {
      stageIndex = (stageIndex + 1) % flowStates.length;
      setFlowStage(section, stageIndex);
      scheduleNext();
    }, flowStates[stageIndex].duration);
  };

  setFlowStage(section, stageIndex);

  if (reducedMotion || !("IntersectionObserver" in window)) {
    if (!reducedMotion) {
      isVisible = true;
      scheduleNext();
    }
    return;
  }

  const observer = new IntersectionObserver(([entry]) => {
    isVisible = entry.isIntersecting;
    if (isVisible) scheduleNext();
    else stop();
  }, { threshold: 0.18 });

  observer.observe(section);
  document.addEventListener("visibilitychange", () => {
    if (document.hidden) stop();
    else if (isVisible) scheduleNext();
  });
}

export function initializeIntegrationShowcase(root = document) {
  const section = root.querySelector(".integrations-section");
  if (!section || section.dataset.integrationShowcaseReady === "true") return;

  const template = document.createElement("template");
  template.innerHTML = integrationMarkup.trim();
  section.replaceChildren(template.content);
  section.dataset.integrationShowcaseReady = "true";
  initializeIntegrationMotion(section);
}
