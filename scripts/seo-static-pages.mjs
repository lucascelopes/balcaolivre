import { readFile } from "node:fs/promises";
import path from "node:path";

function escapeHtml(value = "") {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function extractSeoPages(source) {
  const pagePattern =
    /slug:\s*"([^"]+)"[\s\S]*?title:\s*"([^"]+)"[\s\S]*?metaTitle:\s*"([^"]+)"[\s\S]*?description:\s*"([^"]+)"[\s\S]*?h1:\s*"([^"]+)"[\s\S]*?lead:\s*"([^"]+)"/g;
  return [...source.matchAll(pagePattern)].map((match) => ({
    slug: match[1],
    title: match[2],
    metaTitle: match[3],
    description: match[4],
    h1: match[5],
    lead: match[6]
  }));
}

export async function loadStaticSeoPages(root) {
  const seoSource = await readFile(
    path.join(root, "BalcaoLivreLadingPage", "app", "seoPages.js"),
    "utf8"
  );
  const insightsPath = path.join(root, "BalcaoLivreLadingPage", "app", "seoPageInsights.json");
  let insights = {};

  try {
    insights = JSON.parse(await readFile(insightsPath, "utf8"))?.pages || {};
  } catch {
    insights = {};
  }

  return extractSeoPages(seoSource).map((page) => ({
    ...page,
    ...(insights[page.slug] || {}),
    metaTitle: insights[page.slug]?.metaTitle || page.metaTitle,
    description: insights[page.slug]?.description || page.description,
    h1: insights[page.slug]?.h1 || page.h1,
    lead: insights[page.slug]?.lead || page.lead
  }));
}

export function staticSeoPageHtml(page, publicSiteUrl) {
  const canonical = `${publicSiteUrl}/${page.slug}/`;
  const title = page.metaTitle || `${page.title} | Balcão Livre PDV`;
  const description = page.description;

  return `<!doctype html>
<html lang="pt-BR">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>${escapeHtml(title)}</title>
    <meta name="description" content="${escapeHtml(description)}" />
    <link rel="canonical" href="${escapeHtml(canonical)}" />
    <meta name="robots" content="index, follow, max-image-preview:large" />
    <meta property="og:type" content="website" />
    <meta property="og:locale" content="pt_BR" />
    <meta property="og:site_name" content="Balcão Livre PDV" />
    <meta property="og:title" content="${escapeHtml(title)}" />
    <meta property="og:description" content="${escapeHtml(description)}" />
    <meta property="og:url" content="${escapeHtml(canonical)}" />
    <style>
      :root { color-scheme: light; font-family: Inter, Arial, sans-serif; color: #081b2e; background: #fff; }
      body { margin: 0; }
      main { width: min(100% - 48px, 1120px); margin: 0 auto; padding: 70px 0; }
      a { color: inherit; }
      .back { color: #08766f; font-size: 13px; font-weight: 900; letter-spacing: .08em; text-transform: uppercase; text-decoration: none; }
      h1 { max-width: 880px; margin: 18px 0; font-size: clamp(46px, 7vw, 84px); line-height: .95; letter-spacing: 0; }
      p { max-width: 760px; color: #51637a; font-size: 21px; line-height: 1.55; }
      .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 30px; }
      .btn { display: inline-flex; align-items: center; justify-content: center; min-height: 54px; padding: 0 24px; border-radius: 8px; font-weight: 900; text-decoration: none; }
      .primary { background: #00a99a; color: #fff; }
      .secondary { border: 1px solid #c9dceb; color: #081b2e; }
      .strip { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-top: 46px; }
      .strip span { display: grid; place-items: center; min-height: 58px; border: 1px solid #c9dceb; border-radius: 12px; background: #f7fbfd; font-weight: 900; text-align: center; }
      @media (max-width: 760px) { main { width: min(100% - 28px, 1120px); padding: 42px 0; } .strip { grid-template-columns: 1fr; } }
    </style>
  </head>
  <body>
    <main>
      <a class="back" href="/">Balcão Livre PDV</a>
      <h1>${escapeHtml(page.h1)}</h1>
      <p>${escapeHtml(page.lead)}</p>
      <div class="actions">
        <a class="btn primary" href="/#planos">Testar grátis por 7 dias</a>
        <a class="btn secondary" href="https://wa.me/5533999609457">Falar no WhatsApp</a>
      </div>
      <div class="strip">
        <span>Windows</span>
        <span>Caixa offline</span>
        <span>Cardápio e WhatsApp</span>
        <span>Suporte na implantação</span>
      </div>
    </main>
  </body>
</html>
`;
}

export function seoPageSitemapEntries(pages, publicSiteUrl) {
  return pages.map((page) => ({
    loc: `${publicSiteUrl}/${page.slug}/`,
    priority: "0.78",
    changefreq: "weekly"
  }));
}

export function homeSalesBoostHtml(pages = []) {
  const featured = pages.slice(0, 12);
  return `
      <section class="lpSection lpSeoLinksSection" id="encontre-o-pdv-certo">
        <div class="lpSectionHead">
          <p class="lpKicker">Encontre o PDV certo</p>
          <h2>Páginas diretas para cada tipo de restaurante.</h2>
          <p>Escolha o cenário mais parecido com sua loja e vá direto para o teste, WhatsApp ou plano indicado.</p>
        </div>
        <div class="lpSeoLinksGrid">
          ${featured.map((page) => `
            <a href="/${escapeHtml(page.slug)}/">
              <span>${escapeHtml(page.title.split(" ")[0] || "PDV")}</span>
              <strong>${escapeHtml(page.title)}</strong>
              <small>${escapeHtml(page.description)}</small>
            </a>
          `).join("")}
        </div>
      </section>

      <section class="lpSection lpStorySection" id="casos-de-uso">
        <div class="lpSectionHead">
          <p class="lpKicker">Casos de uso</p>
          <h2>Exemplos de lojas que o Balcão Livre foi feito para atender.</h2>
          <p>Cenários comerciais por cidade e segmento para o cliente se enxergar na rotina do PDV.</p>
        </div>
        <div class="lpStoryGrid">
          ${[
            ["Lanchonete e delivery", "Wender Soares", "Vila Velha - ES", "O ponto principal foi parar de perder informação entre WhatsApp, balcão e entrega. Agora o pedido nasce mais organizado e o caixa fecha com mais clareza.", "Saiu do papel para caixa, entrega e fechamento no mesmo lugar."],
            ["Pizzaria de bairro", "Marina Almeida", "Vila Velha - ES", "Antes a equipe perguntava toda hora se o pedido já tinha sido pago ou se saiu para entrega. Com o PDV, a rotina fica mais visual.", "Pedidos de entrega com cliente, endereço, taxa e pagamento fáceis de conferir."],
            ["Hamburgueria", "Carlos Duarte", "Governador Valadares - MG", "A loja precisava de um caixa direto, mas sem ficar presa só no computador. O plano conectado ajuda quando entra delivery e WhatsApp.", "Produtos, adicionais, estoque e comprovante no mesmo fluxo."]
          ].map(([segment, name, city, quote, result]) => `
            <article>
              <div><span>${escapeHtml(segment)}</span><strong>${escapeHtml(name)}</strong><small>${escapeHtml(city)}</small></div>
              <p>“${escapeHtml(quote)}”</p>
              <b>${escapeHtml(result)}</b>
            </article>
          `).join("")}
        </div>
      </section>

      <section class="lpSection lpPlanChoiceSection" id="qual-plano">
        <div class="lpSectionHead">
          <p class="lpKicker">Oferta direta</p>
          <h2>Comece com caixa local ou vá direto para restaurante conectado.</h2>
          <p>A mensagem precisa ser simples: testar grátis, pagar pouco para começar e evoluir para online quando a loja precisar.</p>
        </div>
        <div class="lpPlanChoiceGrid">
          <article><span>Comece barato</span><strong>R$17/mês</strong><p>Para caixa Windows, produto, estoque, venda, comprovante e fechamento local.</p></article>
          <article><span>Conecte a operação</span><strong>R$139/mês</strong><p>Para cardápio online, garçom no celular, WhatsApp, equipe, entregadores, NFC-e configurável, iFood e Mercado Pago.</p></article>
          <article><span>Cresça sem trocar sistema</span><strong>Mesmo PDV</strong><p>A loja começa simples e ativa recursos online quando a rotina exigir.</p></article>
        </div>
      </section>

      <section class="lpSection lpFunnelSection" id="depois-do-download">
        <div class="lpSectionHead">
          <p class="lpKicker">Depois do download</p>
          <h2>O teste vira venda quando o cliente recebe ajuda no momento certo.</h2>
          <p>O funil acompanha o caminho do visitante até a primeira venda para saber quem precisa de suporte e quem está pronto para assinar.</p>
        </div>
        <div class="lpFunnelGrid">
          ${[
            ["1", "Baixou", "O clique no instalador fica medido para saber qual página trouxe o lead."],
            ["2", "Instalou", "O app identifica versão, chave e primeiro acesso quando sincroniza."],
            ["3", "Cadastrou produto", "Produto cadastrado mostra que o teste virou uso real."],
            ["4", "Fez primeira venda", "A primeira venda separa curioso de cliente com intenção de compra."],
            ["5", "Travou no caminho", "Suporte entra pelo WhatsApp/admin para ajudar antes do teste esfriar."]
          ].map(([number, title, text]) => `<article><b>${number}</b><strong>${escapeHtml(title)}</strong><p>${escapeHtml(text)}</p></article>`).join("")}
        </div>
      </section>
`;
}

export function injectHomeSalesBoost(html, pages) {
  if (html.includes('id="encontre-o-pdv-certo"')) return html;
  const marker = '<section class="lpSection lpPlansSection" id="planos">';
  if (!html.includes(marker)) return html;
  return html.replace(marker, `${homeSalesBoostHtml(pages)}\n      ${marker}`);
}
