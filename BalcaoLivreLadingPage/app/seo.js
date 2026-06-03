export const siteUrl = (
  process.env.NEXT_PUBLIC_SITE_URL || "https://balcaolivrepdv.com.br"
).replace(/\/$/, "");

export const siteName = "Balcao Livre PDV";

export const defaultTitle =
  "Balcao Livre PDV | Sistema Windows para restaurantes";

export const defaultDescription =
  "PDV para restaurante com caixa offline, cardapio online, garcom no celular, iFood, Mercado Pago e WhatsApp com IA basica por R$139/mes.";

export const seoKeywords = [
  "PDV para restaurante",
  "sistema para restaurante",
  "PDV Windows",
  "sistema de caixa para restaurante",
  "PDV para lanchonete",
  "PDV para pizzaria",
  "sistema de comandas",
  "cardapio digital",
  "garcom no celular",
  "PDV delivery",
  "PDV com WhatsApp",
  "WhatsApp com IA para restaurante",
  "PDV com Mercado Pago",
  "PDV com iFood",
  "Balcao Livre PDV"
];

export const openGraphImage = "/brand/pdv-online-screen.png";

export function absoluteUrl(path = "/") {
  return new URL(path, `${siteUrl}/`).toString();
}
