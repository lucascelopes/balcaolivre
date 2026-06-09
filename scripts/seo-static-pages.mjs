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

function stripHomepageSection(html, id) {
  return html.replace(
    new RegExp(`\\s*<section\\b[^>]*\\bid=["']${id}["'][\\s\\S]*?<\\/section>`, "i"),
    ""
  );
}

export function injectHomeSalesBoost(html) {
  let nextHtml = html;
  for (const id of ["casos-de-uso", "qual-plano", "depois-do-download", "encontre-o-pdv-certo"]) {
    nextHtml = stripHomepageSection(nextHtml, id);
  }
  return nextHtml;
}
