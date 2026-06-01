export const siteUrl = (
  process.env.NEXT_PUBLIC_SITE_URL || "https://balcaolivrepdv.com.br"
).replace(/\/$/, "");

export const siteName = "Balcao Livre PDV";

export const defaultTitle =
  "Balcao Livre PDV | Sistema Windows para restaurantes";

export const defaultDescription =
  "PDV Windows online e offline para restaurantes, bares, lanchonetes e delivery. Caixa, mesas, estoque, comandas, cardapio digital, garcom web, iFood, WhatsApp e Mercado Pago.";

export const seoKeywords = [
  "PDV para restaurante",
  "sistema para restaurante",
  "PDV Windows",
  "sistema de caixa para restaurante",
  "PDV para lanchonete",
  "PDV para pizzaria",
  "sistema de comandas",
  "cardapio digital",
  "garcom web",
  "PDV delivery",
  "Balcao Livre PDV"
];

export const openGraphImage = "/brand/pdv-online-screen.png";

export function absoluteUrl(path = "/") {
  return new URL(path, `${siteUrl}/`).toString();
}
