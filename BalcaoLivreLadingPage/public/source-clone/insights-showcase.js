const money = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
  minimumFractionDigits: 2,
});

const values = {
  main: 764.3,
  sold: 382.8,
  week: 1217.1,
  profit: 764.3,
  pending: 15,
};

const easeOutQuart = (progress) => 1 - Math.pow(1 - progress, 4);
const showcaseVersion = "insights-motion-replay-v2";

export function initializeInsightsShowcase(root = document) {
  const section = root.querySelector(".insights-section");
  if (!section || section.dataset.insightsShowcaseVersion === showcaseVersion) return;

  section.id = "gestao";
  section.dataset.insightsShowcaseReady = "true";
  section.dataset.insightsShowcaseVersion = showcaseVersion;
  section.dataset.insightsStage = "0";
  section.innerHTML = `
    <div class="insights-showcase">
      <div class="insights-story">
        <div class="insights-story-inner">
          <div class="insights-kicker"><span>08</span> Gestão que mostra o caminho</div>

          <div class="insights-profit" aria-label="Lucro bruto em sete dias">
            <strong data-insights-value="main">${money.format(0)}</strong>
            <span>lucro bruto em 7 dias</span>
          </div>

          <h2>Decida com<br>o caixa aberto.</h2>
          <p>O Balcão transforma o movimento da operação em uma leitura simples: o que vendeu, o que sobrou e onde agir agora.</p>

          <ol class="insights-steps" aria-label="Leitura da operação em três etapas">
            <li data-insights-step="1">
              <i><img src="/media/icon-bag-check.svg" alt=""></i>
              <div><span>01</span><strong>Vendeu</strong><small>Acompanhe o faturamento diário.</small></div>
            </li>
            <li data-insights-step="2">
              <i><img src="/media/icon-bar-chart-line.svg" alt=""></i>
              <div><span>02</span><strong>Lucrou</strong><small>Veja o lucro bruto em tempo real.</small></div>
            </li>
            <li data-insights-step="3">
              <i><img src="/media/icon-check-lg.svg" alt=""></i>
              <div><span>03</span><strong>Encontrou uma prioridade</strong><small>Resolva pendências e proteja o resultado.</small></div>
            </li>
          </ol>
        </div>
      </div>

      <div class="insights-product">
        <div class="insights-product-label"><i></i> OPERAÇÃO EM TEMPO REAL</div>
        <div class="insights-dashboard-stage">
          <figure class="insights-dashboard-window">
            <div class="insights-window-bar" aria-hidden="true">
              <span><i></i><i></i><i></i></span>
              <b>BALCÃO LIVRE • PAINEL DO CAIXA</b>
              <em>ONLINE</em>
            </div>
            <img src="/media/painel-caixa.png" alt="Painel real do caixa do Balcão Livre com faturamento, lucro e análise da semana">
          </figure>

          <div class="insights-alert-ribbon" data-insights-alert>
            <span data-insights-integer="pending">0</span>
            <strong>pendências abertas</strong>
            <i></i>
            <small>resolver antes do próximo caixa</small>
            <button type="button" class="insights-replay" data-insights-replay>
              <img src="/media/icon-arrow-left-right.svg" alt="">
              Repetir
            </button>
          </div>
        </div>

        <div class="insights-product-caption" data-insights-metrics>
          <article><span>Hoje</span><strong data-insights-value="sold">${money.format(0)}</strong><i></i></article>
          <article><span>7 dias</span><strong data-insights-value="week">${money.format(0)}</strong><i></i></article>
          <article><span>Lucro</span><strong data-insights-value="profit">${money.format(0)}</strong><i></i></article>
        </div>
      </div>
    </div>
  `;

  const mainCounter = section.querySelector('[data-insights-value="main"]');
  const metricCounters = [...section.querySelectorAll("[data-insights-metrics] [data-insights-value]")];
  const pendingCounter = section.querySelector('[data-insights-integer="pending"]');
  const profitBlock = section.querySelector(".insights-profit");
  const steps = [...section.querySelectorAll("[data-insights-step]")];
  const stepsBlock = section.querySelector(".insights-steps");
  const metricsBlock = section.querySelector("[data-insights-metrics]");
  const dashboardStage = section.querySelector(".insights-dashboard-stage");
  const replayButton = section.querySelector("[data-insights-replay]");
  let mainFrame = 0;
  let lowerFrame = 0;
  let stageTimers = [];
  let mainWasVisible = false;
  let metricsWereVisible = false;
  let stepsWereVisible = false;

  const setMoney = (counter, progress) => {
    if (!counter) return;
    const target = values[counter.dataset.insightsValue] ?? 0;
    counter.textContent = money.format(target * progress);
  };

  const clearStageTimers = () => {
    stageTimers.forEach((timer) => window.clearTimeout(timer));
    stageTimers = [];
  };

  const setStage = (stage) => {
    section.dataset.insightsStage = String(stage);
    steps.forEach((step, index) => step.classList.toggle("is-active", index < stage));
  };

  const showFinalState = () => {
    cancelAnimationFrame(mainFrame);
    cancelAnimationFrame(lowerFrame);
    setMoney(mainCounter, 1);
    metricCounters.forEach((counter) => setMoney(counter, 1));
    if (pendingCounter) pendingCounter.textContent = String(values.pending);
    setStage(3);
    section.classList.add("is-insights-running", "is-insights-complete", "is-lower-complete");
  };

  const resetAll = () => {
    cancelAnimationFrame(mainFrame);
    cancelAnimationFrame(lowerFrame);
    clearStageTimers();
    setMoney(mainCounter, 0);
    metricCounters.forEach((counter) => setMoney(counter, 0));
    if (pendingCounter) pendingCounter.textContent = "0";
    setStage(0);
    section.classList.remove("is-insights-running", "is-insights-complete", "is-main-counting", "is-main-complete", "is-lower-counting", "is-lower-complete");
    mainWasVisible = false;
    metricsWereVisible = false;
    stepsWereVisible = false;
  };

  const playMainCounter = () => {
    cancelAnimationFrame(mainFrame);
    setMoney(mainCounter, 0);
    section.classList.remove("is-main-complete");
    section.classList.add("is-main-counting");
    const startedAt = performance.now();
    const duration = 2850;

    const tick = (now) => {
      const linearProgress = Math.min(1, (now - startedAt) / duration);
      const easedProgress = easeOutQuart(linearProgress);
      setMoney(mainCounter, easedProgress);

      if (linearProgress < 1) {
        mainFrame = requestAnimationFrame(tick);
      } else {
        setMoney(mainCounter, 1);
        section.classList.remove("is-main-counting");
        section.classList.add("is-main-complete");
      }
    };

    mainFrame = requestAnimationFrame(tick);
  };

  const playSteps = () => {
    clearStageTimers();
    setStage(0);
    [1, 2, 3].forEach((stage, index) => {
      stageTimers.push(window.setTimeout(() => setStage(stage), 260 + (index * 520)));
    });
  };

  const playLowerCounters = () => {
    cancelAnimationFrame(lowerFrame);
    metricCounters.forEach((counter) => setMoney(counter, 0));
    if (pendingCounter) pendingCounter.textContent = "0";
    section.classList.remove("is-lower-complete");
    section.classList.add("is-lower-counting");
    const startedAt = performance.now();
    const duration = 2500;

    const tick = (now) => {
      const elapsed = now - startedAt;
      metricCounters.forEach((counter, index) => {
        const staggered = Math.max(0, Math.min(1, (elapsed - (index * 210)) / (duration - 420)));
        setMoney(counter, easeOutQuart(staggered));
      });
      const pendingProgress = Math.max(0, Math.min(1, elapsed / 1450));
      if (pendingCounter) pendingCounter.textContent = String(Math.round(values.pending * easeOutQuart(pendingProgress)));

      if (elapsed < duration) {
        lowerFrame = requestAnimationFrame(tick);
      } else {
        metricCounters.forEach((counter) => setMoney(counter, 1));
        if (pendingCounter) pendingCounter.textContent = String(values.pending);
        section.classList.remove("is-lower-counting");
        section.classList.add("is-lower-complete");
      }
    };

    lowerFrame = requestAnimationFrame(tick);
  };

  if (!("IntersectionObserver" in window)) {
    showFinalState();
    return;
  }

  resetAll();

  const sectionObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      section.classList.toggle("is-insights-running", entry.isIntersecting);
      if (!entry.isIntersecting) resetAll();
    });
  }, { threshold: 0 });

  const mainObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting && entry.intersectionRatio >= 0.45 && !mainWasVisible) {
        mainWasVisible = true;
        playMainCounter();
      } else if (!entry.isIntersecting) {
        mainWasVisible = false;
        cancelAnimationFrame(mainFrame);
        setMoney(mainCounter, 0);
        section.classList.remove("is-main-counting", "is-main-complete");
      }
    });
  }, { threshold: [0, 0.45, 0.8], rootMargin: "-6% 0px -12% 0px" });

  const stepsObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting && entry.intersectionRatio >= 0.3 && !stepsWereVisible) {
        stepsWereVisible = true;
        playSteps();
      } else if (!entry.isIntersecting) {
        stepsWereVisible = false;
      }
    });
  }, { threshold: [0, 0.3, 0.7] });

  const metricsObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting && entry.intersectionRatio >= 0.2 && !metricsWereVisible) {
        metricsWereVisible = true;
        playLowerCounters();
      } else if (!entry.isIntersecting) {
        metricsWereVisible = false;
        cancelAnimationFrame(lowerFrame);
        metricCounters.forEach((counter) => setMoney(counter, 0));
        if (pendingCounter) pendingCounter.textContent = "0";
        section.classList.remove("is-lower-counting", "is-lower-complete");
      }
    });
  }, { threshold: [0, 0.2, 0.7], rootMargin: "0px 0px -4% 0px" });

  sectionObserver.observe(section);
  if (profitBlock) mainObserver.observe(profitBlock);
  if (stepsBlock) stepsObserver.observe(stepsBlock);
  if (metricsBlock) metricsObserver.observe(metricsBlock);
  if (dashboardStage) {
    dashboardStage.addEventListener("pointerenter", () => section.classList.add("is-dashboard-hovered"));
    dashboardStage.addEventListener("pointerleave", () => section.classList.remove("is-dashboard-hovered"));
  }

  replayButton?.addEventListener("click", () => {
    section.classList.add("is-motion-forced", "is-insights-running");
    mainWasVisible = true;
    metricsWereVisible = true;
    stepsWereVisible = true;
    playMainCounter();
    playSteps();
    playLowerCounters();
    window.setTimeout(() => section.classList.remove("is-motion-forced"), 5200);
  });
}
