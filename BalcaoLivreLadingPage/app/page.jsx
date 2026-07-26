import PaymentSuccess from "./PaymentSuccess";
import LandingExperience from "./LandingExperience";
import { absoluteUrl, defaultDescription, defaultTitle, siteName, siteUrl } from "./seo";
import { readFileSync } from "node:fs";
import path from "node:path";

function readSourceLandingMarkup() {
  const sourceHtml = readFileSync(
    path.join(process.cwd(), "public", "source-clone", "page.html"),
    "utf8"
  );
  const shellStart = sourceHtml.indexOf('<div class="site-shell">');
  const shellEnd = sourceHtml.indexOf('<script id="_R_">');

  if (shellStart < 0 || shellEnd < 0) {
    throw new Error("A estrutura da landing de referência não foi encontrada.");
  }

  return sourceHtml.slice(shellStart, shellEnd);
}

const faq = [
  ["Funciona offline?", "Sim. Caixa, comandas e a operação local continuam funcionando. Recursos online sincronizam quando a conexão voltar."],
  ["Preciso de internet para vender?", "Não. O Balcão Livre mantém a operação local funcionando mesmo sem internet."],
  ["Como funciona a implantação?", "Nossa equipe acompanha instalação, configuração inicial e treinamento da sua equipe."],
  ["O kit já chega pronto para usar?", "O kit é preparado para o restaurante. Confirmamos frete, disponibilidade e configuração antes do envio."],
  ["A impressora é fiscal?", "Não. O kit inclui impressora térmica não fiscal para pedidos, comandas e comprovantes internos."],
  ["A maquininha está inclusa?", "Sim nos produtos identificados com Mercado Pago Point. A ativação depende da conta Mercado Pago do cliente."],
  ["O totem inclui instalação?", "Inclui configuração assistida. Frete, instalação física e condições do local são confirmados com o consultor."],
  ["Como funciona o suporte?", "Você fala diretamente com nossa equipe por WhatsApp e recebe acompanhamento na implantação."]
];

const productJsonLd = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: siteName,
  applicationCategory: "BusinessApplication",
  operatingSystem: "Windows",
  url: siteUrl,
  image: absoluteUrl("/brand/pdv-comandas-vitrine.png"),
  description: defaultDescription,
  offers: [
    { "@type": "Offer", priceCurrency: "BRL", price: "29.90", name: "Balcão Livre PDV Básico Mensal" },
    { "@type": "Offer", priceCurrency: "BRL", price: "99.90", name: "Balcão Livre PDV Completo Mensal" },
    { "@type": "Offer", priceCurrency: "BRL", price: "999.90", name: "Balcão Livre PDV Completo Anual com Point" },
    { "@type": "Offer", priceCurrency: "BRL", price: "2990.00", name: "Kit Loja Completa Balcão Livre" },
    { "@type": "Offer", priceCurrency: "BRL", price: "9990.00", name: "Totem Balcão Livre" }
  ]
};

const faqJsonLd = {
  "@context": "https://schema.org",
  "@type": "FAQPage",
  mainEntity: faq.map(([question, answer]) => ({
    "@type": "Question",
    name: question,
    acceptedAnswer: { "@type": "Answer", text: answer }
  }))
};

export const metadata = {
  title: defaultTitle,
  description: defaultDescription,
  alternates: { canonical: "/" }
};

export default async function Page({ searchParams }) {
  const resolvedSearchParams = await searchParams;
  const checkoutSessionId = resolvedSearchParams?.checkout === "sucesso" ? resolvedSearchParams?.session_id : "";

  if (checkoutSessionId) {
    return <PaymentSuccess sessionId={checkoutSessionId} />;
  }

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify([productJsonLd, faqJsonLd]) }}
      />
      <LandingExperience initialMarkup={readSourceLandingMarkup()} />
    </>
  );
}
