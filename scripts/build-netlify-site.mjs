import { cp, mkdir, rm, writeFile, copyFile, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const outputDir = process.argv[2]
  ? path.resolve(root, process.argv[2])
  : path.join(root, "dist", "netlify-site");
const publicApexDomain = process.env.BALCAO_PUBLIC_APEX_DOMAIN || "balcaolivrepdv.com.br";
const publicSiteUrl = `https://${publicApexDomain}`;
const googleSiteVerification =
  process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION ||
  process.env.GOOGLE_SITE_VERIFICATION ||
  "";
const bingSiteVerification =
  process.env.NEXT_PUBLIC_BING_SITE_VERIFICATION ||
  process.env.BING_SITE_VERIFICATION ||
  "";
const gaMeasurementId = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID || "G-CPJ89TNX9Q";
const clarityProjectId = process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID || "";
const publicMenuSupabaseUrl = process.env.BALCAO_SUPABASE_URL || "https://hzvplpotsdzxygkxrgyi.supabase.co";
const publicMenuPublishableKey =
  process.env.BALCAO_SUPABASE_PUBLISHABLE_KEY ||
  "sb_publishable_qNl5_EGAeuhN6PqTzRIeyQ_YQV2MdV6";

const fromRoot = (...parts) => path.join(root, ...parts);
const toOutput = (...parts) => path.join(outputDir, ...parts);

async function copyDirectory(source, target) {
  await cp(source, target, { recursive: true, force: true });
}

async function copyLandingHtml(source, target, options = {}) {
  const html = await readFile(source, "utf8");
  await writeFile(target, enhanceLandingHtml(html, options), "utf8");
}

async function copyLanding() {
  await copyLandingHtml(
    fromRoot("BalcaoLivreLadingPage", "preview.html"),
    toOutput("index.html"),
    {
      canonicalPath: "/",
      title: "Balcao Livre PDV | Sistema Windows para restaurantes",
      description: "PDV Windows online e offline para restaurantes, bares, lanchonetes e delivery. Caixa, mesas, estoque, comandas, cardapio digital, garcom web, iFood, WhatsApp e Mercado Pago.",
      mainPage: true
    }
  );

  await mkdir(toOutput("termos"), { recursive: true });
  await copyLandingHtml(
    fromRoot("BalcaoLivreLadingPage", "termos.html"),
    toOutput("termos", "index.html"),
    {
      canonicalPath: "/termos/",
      title: "Termos e privacidade | Balcao Livre PDV",
      description: "Termos de uso, politica de privacidade, regras de app stores, WhatsApp Cloud, dados e suporte do Balcao Livre PDV."
    }
  );

  await mkdir(toOutput("como-usar"), { recursive: true });
  await copyLandingHtml(
    fromRoot("BalcaoLivreLadingPage", "como-usar.html"),
    toOutput("como-usar", "index.html"),
    {
      canonicalPath: "/como-usar/",
      title: "Como usar o PDV Windows | Balcao Livre PDV",
      description: "Manual para instalar, configurar e operar o Balcao Livre PDV no Windows: caixa, produtos, mesas, delivery, pagamentos, impressao, estoque e fechamento."
    }
  );

  await mkdir(toOutput("app"), { recursive: true });
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "app", "globals.css"),
    toOutput("app", "globals.css")
  );

  await copyDirectory(
    fromRoot("BalcaoLivreLadingPage", "public"),
    toOutput("public")
  );
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "public", "balcao-livre-icon.png"),
    toOutput("balcao-livre-icon.png")
  );
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "public", "balcao-livre-logo.png"),
    toOutput("balcao-livre-logo.png")
  );
}

async function copyPdv() {
  await mkdir(toOutput("pdv"), { recursive: true });
  await Promise.all([
    copyFile(fromRoot("BalcaoLivre.PDV.Web", "index.html"), toOutput("pdv", "index.html")),
    copyFile(fromRoot("BalcaoLivre.PDV.Web", "styles.css"), toOutput("pdv", "styles.css")),
    copyFile(fromRoot("BalcaoLivre.PDV.Web", "sw.js"), toOutput("pdv", "sw.js")),
    copyFile(
      fromRoot("BalcaoLivre.PDV.Web", "manifest.webmanifest"),
      toOutput("pdv", "manifest.webmanifest")
    ),
    copyDirectory(fromRoot("BalcaoLivre.PDV.Web", "src"), toOutput("pdv", "src")),
    copyDirectory(fromRoot("BalcaoLivre.PDV.Web", "assets"), toOutput("pdv", "assets"))
  ]);
}

async function copyAdmin() {
  await copyDirectory(fromRoot("BalcaoLivre.Admin", "wwwroot"), toOutput("admin"));
}

async function copyCardapio() {
  await copyDirectory(fromRoot("BalcaoLivre.Cardapio.Web"), toOutput("cardapio"));
  const config = {
    supabaseUrl: publicMenuSupabaseUrl,
    publishableKey: publicMenuPublishableKey,
    licenseFunctionUrl: `${publicMenuSupabaseUrl.replace(/\/$/, "")}/functions/v1/license`,
    apexDomain: publicApexDomain
  };

  await writeFile(
    toOutput("cardapio", "config.js"),
    `window.BALCAO_CARDAPIO_CONFIG = ${JSON.stringify(config, null, 2)};\n`,
    "utf8"
  );
}

async function writeRedirects() {
  const redirects = [
    `https://admin.${publicApexDomain}/admin-api/* https://balcaolivrepdv.onrender.com/api/:splat 200!`,
    "/admin-api/* https://balcaolivrepdv.onrender.com/api/:splat 200!",
    `https://www.${publicApexDomain}/ /index.html 200!`,
    `https://www.${publicApexDomain}/* /:splat 200`,
    `https://admin.${publicApexDomain}/ /admin/index.html 200!`,
    `https://admin.${publicApexDomain}/* /admin/:splat 200!`,
    `https://pdv.${publicApexDomain}/ /pdv/index.html 200!`,
    `https://pdv.${publicApexDomain}/* /pdv/:splat 200!`,
    `https://cardapio.${publicApexDomain}/app.js /cardapio/app.js 200!`,
    `https://cardapio.${publicApexDomain}/config.js /cardapio/config.js 200!`,
    `https://cardapio.${publicApexDomain}/styles.css /cardapio/styles.css 200!`,
    `https://cardapio.${publicApexDomain}/ /cardapio/index.html 200!`,
    `https://cardapio.${publicApexDomain}/* /cardapio/index.html 200!`,
    "/admin /admin/index.html 200",
    "/admin/ /admin/index.html 200",
    "/pdv /pdv/index.html 200",
    "/pdv/ /pdv/index.html 200",
    "/cardapio /cardapio/index.html 200",
    "/cardapio/ /cardapio/index.html 200",
    "/cardapio/* /cardapio/index.html 200"
  ];

  await writeFile(toOutput("_redirects"), `${redirects.join("\n")}\n`, "utf8");
}

async function writeSeoFiles() {
  await writeFile(
    toOutput("robots.txt"),
    [
      "User-agent: *",
      "Allow: /",
      "Disallow: /admin-api/",
      `Sitemap: ${publicSiteUrl}/sitemap.xml`,
      ""
    ].join("\n"),
    "utf8"
  );

  await writeFile(toOutput("sitemap.xml"), sitemapXml(), "utf8");
}

function enhanceLandingHtml(html, options = {}) {
  const canonicalPath = options.canonicalPath || "/";
  const canonicalUrl = `${publicSiteUrl}${canonicalPath}`;
  const description =
    options.description ||
    "PDV Windows online e offline para restaurantes, bares, lanchonetes e delivery. Caixa, mesas, estoque, comandas, cardapio digital, garcom web, iFood, WhatsApp e Mercado Pago.";
  let nextHtml = html;

  if (options.title) {
    nextHtml = nextHtml.replace(/<title>[\s\S]*?<\/title>/i, `<title>${escapeHtml(options.title)}</title>`);
  }

  nextHtml = nextHtml
    .replace(/<meta\s+name=["']description["'][^>]*>\s*/gi, "")
    .replace(/<meta\s+name=["']keywords["'][^>]*>\s*/gi, "")
    .replace(/<meta\s+name=["']robots["'][^>]*>\s*/gi, "")
    .replace(/<meta\s+name=["']google-site-verification["'][^>]*>\s*/gi, "")
    .replace(/<meta\s+name=["']msvalidate\.01["'][^>]*>\s*/gi, "")
    .replace(/<link\s+rel=["']canonical["'][^>]*>\s*/gi, "")
    .replace(/<meta\s+property=["']og:[^"']+["'][^>]*>\s*/gi, "")
    .replace(/<meta\s+name=["']twitter:[^"']+["'][^>]*>\s*/gi, "");

  const headTags = [
    `<meta name="description" content="${escapeHtml(description)}" />`,
    `<meta name="keywords" content="PDV para restaurante, sistema para restaurante, PDV Windows, sistema de caixa, comandas, cardapio digital, garcom web, delivery, Balcao Livre PDV" />`,
    `<link rel="canonical" href="${escapeHtml(canonicalUrl)}" />`,
    `<meta name="robots" content="index, follow, max-image-preview:large" />`,
    googleSiteVerification
      ? `<meta name="google-site-verification" content="${escapeHtml(googleSiteVerification)}" />`
      : "",
    bingSiteVerification
      ? `<meta name="msvalidate.01" content="${escapeHtml(bingSiteVerification)}" />`
      : "",
    `<meta property="og:type" content="website" />`,
    `<meta property="og:locale" content="pt_BR" />`,
    `<meta property="og:site_name" content="Balcao Livre PDV" />`,
    `<meta property="og:title" content="${escapeHtml(options.title || "Balcao Livre PDV")}" />`,
    `<meta property="og:description" content="${escapeHtml(description)}" />`,
    `<meta property="og:url" content="${escapeHtml(canonicalUrl)}" />`,
    `<meta property="og:image" content="${escapeHtml(`${publicSiteUrl}/public/brand/pdv-online-screen.png`)}" />`,
    `<meta name="twitter:card" content="summary_large_image" />`,
    `<meta name="twitter:title" content="${escapeHtml(options.title || "Balcao Livre PDV")}" />`,
    `<meta name="twitter:description" content="${escapeHtml(description)}" />`,
    clarityProjectId
      ? `<script>(function(c,l,a,r,i,t,y){c[a]=c[a]||function(){(c[a].q=c[a].q||[]).push(arguments)};t=l.createElement(r);t.async=1;t.src="https://www.clarity.ms/tag/"+i;y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y)})(window,document,"clarity","script","${escapeHtml(clarityProjectId)}");</script>`
      : "",
    `<script type="application/ld+json">${JSON.stringify(structuredData(canonicalUrl, description, Boolean(options.mainPage)))}</script>`
  ].filter(Boolean).join("\n    ");

  if (gaMeasurementId) {
    nextHtml = nextHtml.replace("<head>", `<head>\n    ${googleTagScript()}`);
  }

  nextHtml = nextHtml.replace("</head>", `    ${headTags}\n  </head>`);
  return nextHtml.replace("</body>", `    ${staticConversionScript()}\n  </body>`);
}

function structuredData(canonicalUrl, description, mainPage) {
  const base = [
    {
      "@context": "https://schema.org",
      "@type": "Organization",
      name: "Balcao Livre PDV",
      url: publicSiteUrl,
      logo: `${publicSiteUrl}/public/brand/bl-modern-icon.png`,
      contactPoint: [
        { "@type": "ContactPoint", telephone: "+55-27-98126-7551", contactType: "sales", areaServed: "BR", availableLanguage: "Portuguese" },
        { "@type": "ContactPoint", telephone: "+55-33-99960-9457", contactType: "sales", areaServed: "BR", availableLanguage: "Portuguese" }
      ]
    },
    {
      "@context": "https://schema.org",
      "@type": "WebPage",
      name: "Balcao Livre PDV",
      url: canonicalUrl,
      description,
      inLanguage: "pt-BR"
    }
  ];

  if (mainPage) {
    base.push({
      "@context": "https://schema.org",
      "@type": "SoftwareApplication",
      name: "Balcao Livre PDV",
      applicationCategory: "BusinessApplication",
      operatingSystem: "Windows",
      url: publicSiteUrl,
      image: `${publicSiteUrl}/public/brand/pdv-online-screen.png`,
      description,
      offers: [
        offer("Balcao Livre PDV Offline mensal", 17),
        offer("Balcao Livre PDV Offline anual", 200),
        offer("Balcao Livre PDV Hibrido Online mensal", 139),
        offer("Balcao Livre PDV Hibrido Online anual", 1390),
        offer("Balcao Livre PDV Completo mensal", 179),
        offer("Balcao Livre PDV Completo anual", 1790)
      ]
    });
  }

  return base;
}

function offer(name, price) {
  return {
    "@type": "Offer",
    name,
    price,
    priceCurrency: "BRL",
    availability: "https://schema.org/InStock",
    url: `${publicSiteUrl}/#planos`
  };
}

function staticConversionScript() {
  return `<script>
      (function(){
        var planPrices={"offline-mensal":17,"offline-anual":200,"online-mensal":139,"online-anual":1390,"complete-mensal":179,"complete-anual":1790};
        function safeUrl(href){try{return new URL(href,window.location.href)}catch{return null}}
        function splitPlan(plan){var parts=String(plan||"").split("-");return{plan:parts[0]||"unknown",billing:parts[1]||"unknown"}}
        function publish(eventName,params,metaEvent){var payload=Object.assign({event_category:"landing",page_location:window.location.href},params||{});window.dataLayer=window.dataLayer||[];window.dataLayer.push(Object.assign({event:eventName},payload));if(window.gtag)window.gtag("event",eventName,payload);if(window.fbq){if(metaEvent)window.fbq("track",metaEvent,payload);window.fbq("trackCustom",eventName,payload)}}
        document.addEventListener("click",function(event){var target=event.target&&event.target.closest?event.target.closest("a[href]"):null;if(!target)return;var href=target.getAttribute("href")||"";if(href.indexOf("/trial-download")!==-1){var trialUrl=safeUrl(href);var trialPlan=(trialUrl&&trialUrl.searchParams.get("plan"))||"offline";publish("trial_download_click",{content_name:"Teste "+trialPlan+" 7 dias",content_category:"teste_7_dias",content_ids:[trialPlan],plan:trialPlan,trial_days:7,currency:"BRL",value:0},"Lead");return}if(href.indexOf("wa.me/")!==-1){publish("whatsapp_click",{content_name:"WhatsApp comercial",content_category:"contato"},"Contact");return}if(href.indexOf("/checkout")!==-1){var checkoutUrl=safeUrl(href);var checkoutPlan=(checkoutUrl&&checkoutUrl.searchParams.get("plan"))||"";var split=splitPlan(checkoutPlan);publish("plan_checkout_click",{content_name:"Balcao Livre PDV "+split.plan,content_category:"planos",content_ids:[checkoutPlan],plan:split.plan,billing:split.billing,value:planPrices[checkoutPlan]||0,currency:"BRL"});return}if(href.indexOf("#planos")!==-1){publish("plans_view_click",{content_name:"Planos Balcao Livre PDV",content_category:"planos"},"ViewContent")}},true);
      })();
    </script>`;
}

function googleTagScript() {
  return `<!-- Google tag (gtag.js) -->
    <script async src="https://www.googletagmanager.com/gtag/js?id=${escapeHtml(gaMeasurementId)}"></script>
    <script>
      window.dataLayer = window.dataLayer || [];
      function gtag(){dataLayer.push(arguments);}
      gtag('js', new Date());
      gtag('consent', 'default', {
        analytics_storage: 'granted',
        ad_storage: 'granted',
        ad_user_data: 'granted',
        ad_personalization: 'granted'
      });
      gtag('config', '${escapeHtml(gaMeasurementId)}');
    </script>`;
}

function sitemapXml() {
  const today = new Date().toISOString().slice(0, 10);
  const urls = [
    { loc: `${publicSiteUrl}/`, priority: "1.0", changefreq: "weekly" },
    { loc: `${publicSiteUrl}/como-usar/`, priority: "0.85", changefreq: "monthly" },
    { loc: `${publicSiteUrl}/termos/`, priority: "0.65", changefreq: "yearly" }
  ];

  return `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls.map((url) => `  <url>\n    <loc>${xmlEscape(url.loc)}</loc>\n    <lastmod>${today}</lastmod>\n    <changefreq>${url.changefreq}</changefreq>\n    <priority>${url.priority}</priority>\n  </url>`).join("\n")}\n</urlset>\n`;
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function xmlEscape(value) {
  return escapeHtml(value).replaceAll("'", "&apos;");
}

await rm(outputDir, { recursive: true, force: true });
await mkdir(outputDir, { recursive: true });

await copyLanding();
await copyPdv();
await copyAdmin();
await copyCardapio();
await writeSeoFiles();
await writeRedirects();

console.log(`Netlify static site generated at ${path.relative(root, outputDir)}`);
