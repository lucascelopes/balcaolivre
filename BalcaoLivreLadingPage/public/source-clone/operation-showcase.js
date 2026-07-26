const showcaseMarkup = `
  <div class="operation-showcase" aria-label="Balcão Livre em operação">
    <div class="operation-showcase-stage">
      <div class="operation-showcase-disc" aria-hidden="true"></div>

      <figure class="operation-screen operation-screen-dashboard">
        <img
          src="/media/painel-caixa.png"
          alt="Painel real do Balcão Livre com faturamento, lucro e análise da operação"
          loading="eager"
        />
      </figure>

      <figure class="operation-screen operation-screen-command">
        <img
          src="/media/comandas.jpeg"
          alt="Tela real de mesas e comandas do Balcão Livre"
          loading="eager"
        />
      </figure>

      <aside class="operation-touch-callout">
        <strong>3</strong>
        <div>
          <h3>toques para<br />lançar um pedido</h3>
          <p>Do salão à cozinha em 3 cliques: selecione a mesa, inclua os itens e envie para produção.</p>
        </div>
      </aside>
    </div>

    <div class="operation-proof-rail">
      <article>
        <span class="operation-proof-index"><img src="/media/icon-arrow-left-right.svg" alt="" aria-hidden="true" /></span>
        <div>
          <h3>Um fluxo só</h3>
          <p>Pedidos do salão e do delivery chegam organizados para a mesma operação.</p>
        </div>
      </article>
      <article>
        <span class="operation-proof-index"><img src="/media/icon-bar-chart-line.svg" alt="" aria-hidden="true" /></span>
        <div>
          <h3>Decisão com números</h3>
          <p>Faturamento, lucro, ticket médio e pendências em uma visão clara.</p>
        </div>
      </article>
    </div>
  </div>
`;

export function initializeOperationShowcase(root = document) {
  const section = root.querySelector(".overview-section");
  if (!section || section.dataset.showcaseReady === "true") return;

  const currentGrid = section.querySelector(".overview-grid");
  if (!currentGrid) return;

  const heading = section.querySelector(".section-heading h2");
  if (heading) heading.innerHTML = "Menos improviso.<br>Mais restaurante rodando.";

  const template = document.createElement("template");
  template.innerHTML = showcaseMarkup.trim();
  currentGrid.replaceWith(template.content);
  section.dataset.showcaseReady = "true";
}
