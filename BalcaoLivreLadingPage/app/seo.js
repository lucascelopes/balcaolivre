export const siteUrl = (
  process.env.NEXT_PUBLIC_SITE_URL || "https://balcaolivrepdv.com.br"
).replace(/\/$/, "");

export const siteName = "Balcão Livre PDV";

export const defaultTitle =
  "Balcão Livre PDV | Sistema Windows para restaurantes";

export const defaultDescription =
  "PDV Online para restaurante com teste de 7 dias, plano Básico de R$29,99/mês e Completo de R$99,99/mês com cardápio, garçom, delivery, Mercado Pago e iFood em manutenção.";

export const seoKeywords = [
  "PDV para restaurante",
  "sistema para restaurante",
  "PDV Windows",
  "sistema de caixa para restaurante",
  "PDV para lanchonete",
  "PDV para pizzaria",
  "sistema de comandas",
  "cardápio digital",
  "garçom no celular",
  "PDV delivery",
  "PDV com WhatsApp",
  "WhatsApp com IA para restaurante",
  "PDV com Mercado Pago",
  "PDV com iFood",
  "PDV com NFC-e",
  "sistema para restaurante com NFC-e",
  "gestão de equipe restaurante",
  "sistema para entregadores delivery",
  "Balcão Livre PDV"
];

export const openGraphImage = "/brand/pdv-online-screen.png";

export function absoluteUrl(path = "/") {
  return new URL(path, `${siteUrl}/`).toString();
}
