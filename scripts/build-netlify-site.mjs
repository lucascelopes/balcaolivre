import { cp, mkdir, rm, writeFile, copyFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const outputDir = process.argv[2]
  ? path.resolve(root, process.argv[2])
  : path.join(root, "dist", "netlify-site");
const publicApexDomain = process.env.BALCAO_PUBLIC_APEX_DOMAIN || "balcaolivrepdv.com.br";
const publicMenuSupabaseUrl = process.env.BALCAO_SUPABASE_URL || "https://hzvplpotsdzxygkxrgyi.supabase.co";
const publicMenuPublishableKey =
  process.env.BALCAO_SUPABASE_PUBLISHABLE_KEY ||
  "sb_publishable_qNl5_EGAeuhN6PqTzRIeyQ_YQV2MdV6";

const fromRoot = (...parts) => path.join(root, ...parts);
const toOutput = (...parts) => path.join(outputDir, ...parts);

async function copyDirectory(source, target) {
  await cp(source, target, { recursive: true, force: true });
}

async function copyLanding() {
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "preview.html"),
    toOutput("index.html")
  );

  await mkdir(toOutput("termos"), { recursive: true });
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "termos.html"),
    toOutput("termos", "index.html")
  );

  await mkdir(toOutput("como-usar"), { recursive: true });
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "como-usar.html"),
    toOutput("como-usar", "index.html")
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

await rm(outputDir, { recursive: true, force: true });
await mkdir(outputDir, { recursive: true });

await copyLanding();
await copyPdv();
await copyAdmin();
await copyCardapio();
await writeRedirects();

console.log(`Netlify static site generated at ${path.relative(root, outputDir)}`);
