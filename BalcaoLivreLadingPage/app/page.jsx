import PaymentSuccess from "./PaymentSuccess";
import SalesModal from "./SalesModal";
import SiteHeader from "./SiteHeader";
import { checkoutFunctionUrl, downloadUrl, sellers } from "./siteLinks";
import { absoluteUrl, defaultDescription, defaultTitle, siteName, siteUrl } from "./seo";

const whatsappHref = sellers[0]?.href || "https://wa.me/5527981267551";

function WhatsAppLogo() {
  return (
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <path fill="#25D366" d="M16 3.4c-6.93 0-12.56 5.58-12.56 12.45 0 2.2.58 4.35 1.68 6.24L3.3 28.6l6.7-1.74a12.72 12.72 0 0 0 6 1.5c6.93 0 12.56-5.58 12.56-12.46S22.93 3.4 16 3.4Z" />
      <path fill="#fff" d="M23.1 19.07c-.32-.16-1.9-.93-2.2-1.04-.3-.1-.52-.16-.74.16-.22.32-.85 1.04-1.04 1.25-.19.22-.38.24-.7.08-.32-.16-1.36-.5-2.6-1.6-.96-.85-1.6-1.9-1.8-2.23-.18-.32-.02-.5.14-.65.14-.14.32-.38.48-.57.16-.2.22-.33.33-.55.11-.22.05-.41-.03-.57-.08-.16-.74-1.77-1.01-2.42-.27-.64-.54-.55-.74-.56h-.63c-.22 0-.57.08-.87.4-.3.33-1.14 1.1-1.14 2.68s1.17 3.12 1.33 3.33c.16.22 2.3 3.48 5.58 4.88.78.33 1.38.53 1.85.68.78.25 1.49.21 2.05.13.63-.09 1.9-.77 2.17-1.51.27-.74.27-1.38.19-1.51-.08-.14-.3-.22-.62-.38Z" />
    </svg>
  );
}

function MercadoPagoLogo() {
  return (
    <svg viewBox="0 0 86 36" aria-hidden="true" focusable="false">
      <rect width="86" height="36" rx="18" fill="#009EE3" />
      <ellipse cx="43" cy="18" rx="31" ry="12" fill="#fff" opacity=".96" />
      <path fill="#009EE3" d="M24 18.2c3.1-4.7 6.3-5.9 9.4-3.6 1.1.8 2.1 1.7 3.2 2.5 1.7 1.3 3.1 1.3 4.9 0 1.1-.8 2.1-1.7 3.2-2.5 3.1-2.3 6.4-1 9.4 3.6-4 5.1-8.2 6.1-12.7 3.2-1.7-1.1-3-1.1-4.7 0-4.5 2.9-8.7 1.9-12.7-3.2Z" />
      <path fill="#0074AE" d="M31.4 17.7c1.1.8 2.2 1.6 3.2 2.4 3.3 2.5 5.4 2.5 8.7 0 1-.8 2.1-1.6 3.2-2.4.8-.6 1.8-.4 2.4.4.5.8.3 1.8-.5 2.4-1 .8-2.1 1.5-3.1 2.3-4.5 3.3-7.8 3.3-12.4 0-1-.8-2.1-1.5-3.1-2.3-.8-.6-1-1.6-.5-2.4.5-.8 1.5-1 2.1-.4Z" opacity=".72" />
    </svg>
  );
}

function QrLogo() {
  return (
    <svg viewBox="0 0 34 34" aria-hidden="true" focusable="false">
      <rect width="34" height="34" rx="10" fill="#EAF2FF" />
      <path fill="#1264E2" d="M8 8h8v8H8V8Zm3 3v2h2v-2h-2Zm7-3h8v8h-8V8Zm3 3v2h2v-2h-2ZM8 18h8v8H8v-8Zm3 3v2h2v-2h-2Zm11-2h4v3h-3v2h-3v-4h2v-1Zm-4 0h2v3h-2v-3Zm7 5h1v2h-5v-2h4Zm-7 0h2v2h-2v-2Z" />
    </svg>
  );
}

function TableLogo() {
  return (
    <svg viewBox="0 0 34 34" aria-hidden="true" focusable="false">
      <rect width="34" height="34" rx="10" fill="#ECFDF5" />
      <path fill="#0B7A5A" d="M9 12.5c0-1.38 1.12-2.5 2.5-2.5h11c1.38 0 2.5 1.12 2.5 2.5V17H9v-4.5Zm1.5 6.5h13v2H21v4h-2v-4h-4v4h-2v-4h-2.5v-2Z" />
    </svg>
  );
}

const featureCards = [
  ["PDV rápido e intuitivo", "Interface simples para caixa, balcão, mesas e delivery venderem sem confusão."],
  ["Controle de mesas e comandas", "Abra, transfira, feche e acompanhe consumo por mesa ou comanda."],
  ["Cardápio personalizado", "Publique produtos, preços, adicionais, fotos e disponibilidade no cardápio online."],
  ["Integração com delivery", "Organize pedidos de retirada, entrega e canais externos em uma fila única."],
  ["Relatórios completos", "Acompanhe vendas, caixa, produtos, margem, estoque e fechamento do dia."],
  ["Cadastro de clientes", "Histórico de pedidos, telefones, endereços e atendimento pelo WhatsApp."],
  ["Controle de estoque", "Veja estoque baixo, margem, preço de compra e produtos mais vendidos."],
  ["Comprovantes e impressão", "Impressão de pedido, conta, fechamento e comprovante em 58/80mm."],
  ["Formas de pagamento", "Dinheiro, Pix, cartão, troco e integração configurável com Mercado Pago."]
];

const segments = [
  {
    title: "Restaurantes",
    text: "Mesas, comandas, consumo aberto, fechamento e relatórios.",
    image: "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=900&q=80"
  },
  {
    title: "Lanchonetes",
    text: "Venda rápida, combos, adicionais, balcão e delivery.",
    image: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=900&q=80"
  },
  {
    title: "Pizzarias",
    text: "Sabores, tamanhos, bordas, entrega, retirada e taxa.",
    image: "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=900&q=80"
  },
  {
    title: "Bares",
    text: "Comandas, mesas, consumo aberto e fechamento por cliente.",
    image: "https://images.unsplash.com/photo-1572116469696-31de0f17cc34?auto=format&fit=crop&w=900&q=80"
  },
  {
    title: "Cafeterias",
    text: "Balcão, combos, adicionais e atendimento rápido.",
    image: "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=900&q=80"
  },
  {
    title: "Food trucks",
    text: "Venda direta, cardápio online, Pix e pedidos por WhatsApp.",
    image: "https://images.unsplash.com/photo-1565123409695-7b5ef63a2efb?auto=format&fit=crop&w=900&q=80"
  }
];

const plans = [
  {
    badge: "Começo simples",
    name: "Básico",
    price: "R$ 29,90",
    note: "por mês",
    description: "Para caixa Windows local, vendas, estoque e fechamento.",
    href: `${checkoutFunctionUrl}?plan=offline-mensal`,
    cta: "Comprar mensal",
    features: ["1 caixa Windows", "Mesas e comandas", "Estoque e fechamento", "Comprovante não fiscal"]
  },
  {
    badge: "Mais escolhido",
    name: "Profissional",
    price: "R$ 149,00",
    note: "por mês",
    description: "Para restaurante com cardápio online, equipe, WhatsApp e operação conectada.",
    href: `${checkoutFunctionUrl}?plan=online-mensal`,
    cta: "Comprar mensal",
    featured: true,
    features: ["Tudo do Básico", "Cardápio online", "Garçom no celular", "WhatsApp e atendimento", "NFC-e configurável"]
  },
  {
    badge: "Sob medida",
    name: "Premium",
    price: "Consultar",
    note: "implantação avançada",
    description: "Para operação com personalização, migração, iFood e treinamento assistido.",
    href: whatsappHref,
    cta: "Falar no WhatsApp",
    features: ["Implantação acompanhada", "Múltiplos computadores", "Integrações avançadas", "Suporte prioritário"]
  }
];

const testimonials = [
  {
    place: "Lanchonete e delivery",
    name: "Wender Soares",
    city: "Vila Velha - ES",
    quote: "O ponto principal foi parar de perder informação entre WhatsApp, balcão e entrega. Agora o pedido nasce mais organizado."
  },
  {
    place: "Pizzaria de bairro",
    name: "Marina Almeida",
    city: "Vila Velha - ES",
    quote: "Antes a equipe perguntava toda hora se o pedido já tinha sido pago. Com o PDV, a rotina ficou mais visual."
  },
  {
    place: "Hamburgueria",
    name: "Carlos Duarte",
    city: "Governador Valadares - MG",
    quote: "A loja precisava de um caixa direto, mas sem ficar presa só no computador. O plano conectado ajudou no delivery."
  }
];

const helpCards = [
  ["Primeiros passos", "Instalação, ativação e primeiro caixa."],
  ["PDV e vendas", "Caixa, balcão, mesas, comandas e pagamentos."],
  ["Cardápio e produtos", "Produtos, adicionais, preços, estoque e QR Code."],
  ["Relatórios", "Fechamento, margem, CMV, vendas e indicadores."],
  ["Integrações", "WhatsApp, Mercado Pago, iFood e cardápio online."],
  ["Suporte", "Ajuda para implantação e dúvidas da equipe."]
];

const faq = [
  ["Funciona sem internet?", "Sim. O caixa Windows continua vendendo localmente. Recursos online dependem do plano e da conexão."],
  ["Tem teste grátis?", "Sim. Você pode testar por 7 dias antes de contratar."],
  ["Imprime em impressora térmica?", "Sim. O app Windows foi pensado para impressoras 58/80mm e comprovante de venda."],
  ["O comprovante é fiscal?", "Não. O comprovante não substitui documento fiscal. A NFC-e é configurável quando a loja tem dados fiscais e certificado."]
];

const productJsonLd = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: siteName,
  applicationCategory: "BusinessApplication",
  operatingSystem: "Windows",
  url: siteUrl,
  image: absoluteUrl("/brand/pdv-real-screen.png"),
  description: defaultDescription,
  offers: [
    {
      "@type": "Offer",
      priceCurrency: "BRL",
      price: "29.90",
      name: "PDV Caixa Local"
    },
    {
      "@type": "Offer",
      priceCurrency: "BRL",
      price: "149.00",
      name: "Restaurante Profissional"
    }
  ]
};

const faqJsonLd = {
  "@context": "https://schema.org",
  "@type": "FAQPage",
  mainEntity: faq.map(([question, answer]) => ({
    "@type": "Question",
    name: question,
    acceptedAnswer: {
      "@type": "Answer",
      text: answer
    }
  }))
};

export const metadata = {
  title: defaultTitle,
  description: defaultDescription,
  alternates: {
    canonical: "/"
  }
};

export default function Page({ searchParams }) {
  const checkoutSessionId = searchParams?.checkout === "sucesso" ? searchParams?.session_id : "";

  if (checkoutSessionId) {
    return <PaymentSuccess sessionId={checkoutSessionId} />;
  }

  return (
    <main className="blLandingPage">
      <SalesModal />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify([productJsonLd, faqJsonLd]) }}
      />
      <SiteHeader id="inicio" />

      <section className="blHero">
        <div className="blHeroText">
          <div className="blHeroKicker">
            <span>Feito para restaurantes. Funciona mesmo sem internet.</span>
          </div>
          <h1>
            O PDV para restaurante que vende <span className="blAccent">no caixa, no cardápio digital e no WhatsApp</span>
          </h1>
          <p className="blLead">
            Caixa Windows, mesas, comandas, delivery, estoque, maquininha Mercado Pago integrada, cardápio digital e WhatsApp. Tudo que seu restaurante precisa.
          </p>

          <div className="blHeroBullets">
            <span>Pedidos e comandas em segundos</span>
            <span>Integração com iFood, delivery e WhatsApp</span>
            <span>Estoque, produtos e relatórios completos</span>
            <span>Funciona online e offline</span>
          </div>

          <div className="blHeroActions">
            <a
              className="blPrimaryButton"
              href={downloadUrl}
              data-analytics-action="trial_download"
              data-analytics-location="hero"
              data-analytics-plan="offline"
            >
              Testar grátis por 7 dias
            </a>
            <a
              className="blWhatsButton"
              href={whatsappHref}
              data-analytics-action="whatsapp_click"
              data-analytics-location="hero"
              data-analytics-seller={sellers[0]?.name}
            >
              Falar no WhatsApp
            </a>
          </div>

          <div className="blTrustInline">
            <span>Sem cartão de crédito</span>
            <span>Sem instalação complicada</span>
            <span>Suporte na implantação</span>
          </div>
        </div>

        <div className="blHeroVisual blHeroConsole" aria-label="Prévia do Balcão Livre PDV em operação">
          <div className="blHeroProductScene">
            <img
              className="blHeroProductImage"
              src="/brand/hero-pdv-restaurante.png"
              alt="Balcão Livre PDV com caixa Windows, maquininha e cardápio digital no celular"
            />
            <div className="blHeroStatusStack" aria-hidden="true">
              <article>
                <span className="blStatusLogo blStatusLogoWhatsapp"><WhatsAppLogo /></span>
                <strong>Pedido no WhatsApp recebido</strong>
                <small>Agora mesmo</small>
              </article>
              <article>
                <span className="blStatusLogo blStatusLogoQr"><QrLogo /></span>
                <strong>Cardápio digital ativo</strong>
                <small>Online</small>
              </article>
              <article>
                <span className="blStatusLogo blStatusLogoMercadoPago"><MercadoPagoLogo /></span>
                <strong>Mercado Pago conectado</strong>
                <small>Online</small>
              </article>
              <article>
                <span className="blStatusLogo blStatusLogoTable"><TableLogo /></span>
                <strong>Mesa 08 aberta</strong>
                <small>00:12:45</small>
              </article>
            </div>
          </div>

          <div className="blConsoleWindow">
            <div className="blConsoleBar">
              <span className="blDeviceDots" aria-hidden="true"><i /><i /><i /></span>
              <strong>Balcão Livre PDV Online</strong>
              <small>Caixa, mesas e delivery</small>
            </div>

            <div className="blConsoleBody">
              <div className="blConsolePanel">
                <span>Comandas</span>
                <strong>Mesa 12</strong>
                <b>R$ 148,00</b>
                <small>ocupada</small>
              </div>
              <div className="blConsolePanel">
                <span>Delivery</span>
                <strong>Pedido 1008</strong>
                <b>preparando</b>
                <small>WhatsApp</small>
              </div>
              <div className="blConsoleMetrics">
                <article>
                  <span>Hoje</span>
                  <strong>R$ 2.300,22</strong>
                </article>
                <article>
                  <span>Pedidos</span>
                  <strong>53</strong>
                </article>
                <article>
                  <span>CMV</span>
                  <strong>31%</strong>
                </article>
              </div>
              <div className="blConsoleOrder">
                <span>Total da venda</span>
                <strong>R$ 96,70</strong>
                <small>Pix aprovado</small>
              </div>
              <div className="blConsoleList">
                <span>Fila de produção</span>
                <p><b>2x</b> X-Burger Prime</p>
                <p><b>1x</b> Coca-Cola lata</p>
                <p><b>1x</b> Batata especial</p>
              </div>
            </div>
          </div>

          <div className="blHeroMessageCard">
            <strong>Atendimento no WhatsApp</strong>
            <span>Pedido confirmado</span>
            <p>#1008 entrou na fila de produção.</p>
          </div>

          <div className="blHeroAgendaCard">
            <strong>Operação conectada</strong>
            <span>Cardápio online, mesas e estoque no mesmo fluxo.</span>
          </div>

          <div className="blDeviceFrame">
            <div className="blDeviceBar">
              <span className="blDeviceDots" aria-hidden="true"><i /><i /><i /></span>
              <strong>Balcão Livre PDV Online</strong>
              <small>Caixa, mesas, delivery e cardápio</small>
            </div>
            <img src="/brand/pdv-real-screen.png" alt="Print real do Balcão Livre PDV Online em uso" />
          </div>

          <div className="blHeroSalesCard">
            <span>Hoje</span>
            <strong>R$ 2.300,22</strong>
            <small>vendas acompanhadas em tempo real</small>
          </div>

          <div className="blHeroDrawer" aria-hidden="true">
            <span />
          </div>

          <div className="blHeroQrCard" aria-hidden="true">
            <img src="/brand/bl-modern-icon.png" alt="" />
            <div className="blHeroQrIcon">QR</div>
            <strong>Cardápio</strong>
          </div>
        </div>
      </section>

      <section className="blSocialProof" aria-label="Provas rápidas">
        <p>Mais de 10.000 restaurantes já usam ou recomendam o Balcão Livre PDV</p>
        <div>
          {["Buteco do Zé", "Pizzaria do João", "Tá na Mesa", "Chef's Burger", "Sabor & Cia", "Bistrô Gourmet"].map((name) => (
            <span key={name}>{name}</span>
          ))}
        </div>
      </section>

      <section className="blDarkStrip" aria-label="Diferenciais do PDV">
        {[
          ["100% online seguro", "Seus dados protegidos com sincronização e backup diário."],
          ["Suporte humano", "Atendimento rápido para implantação e dúvidas reais."],
          ["Atualizações constantes", "Novas funções e melhorias sem custo adicional."],
          ["Funciona online e offline", "Continue vendendo mesmo quando a internet oscilar."]
        ].map(([title, text]) => (
          <article key={title}>
            <span>✓</span>
            <strong>{title}</strong>
            <p>{text}</p>
          </article>
        ))}
      </section>

      <section className="blSection blCenter blFeatureSection" id="recursos">
        <p className="blEyebrow">Recursos</p>
        <h2>Tudo que você precisa para gerenciar seu restaurante</h2>
        <p>Ferramentas completas para facilitar sua operação e aumentar seus resultados.</p>
        <div className="blFeatureGrid">
          {featureCards.map(([title, text], index) => (
            <article key={title}>
              <b>{String(index + 1).padStart(2, "0")}</b>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="blSection blCenter" id="segmentos">
        <p className="blEyebrow">Segmentos</p>
        <h2>Perfeito para todos os tipos de estabelecimentos</h2>
        <p>O Balcão Livre PDV se adapta ao seu negócio.</p>
        <div className="blSegmentGrid">
          {segments.map((segment) => (
            <article key={segment.title}>
              <img src={segment.image} alt="" />
              <div>
                <h3>{segment.title}</h3>
                <p>{segment.text}</p>
              </div>
            </article>
          ))}
        </div>
        <div className="blStatsStrip">
          <span>+10.000 restaurantes</span>
          <span>96% de satisfação</span>
          <span>Suporte pelo WhatsApp</span>
          <span>Atualizações sem custo adicional</span>
        </div>
      </section>

      <section className="blSection" id="planos">
        <div className="blSectionSplit">
          <div>
            <p className="blEyebrow">Planos</p>
            <h2>Escolha o plano ideal para o seu restaurante</h2>
          </div>
          <div className="blBillingToggle" aria-label="Opções de cobrança">
            <span>Mensal</span>
            <span>Anual</span>
            <strong>Economize no anual</strong>
          </div>
        </div>
        <div className="blPlansGrid">
          {plans.map((plan) => (
            <article key={plan.name} className={plan.featured ? "blPlanCard blPlanFeatured" : "blPlanCard"}>
              <span className="blPlanBadge">{plan.badge}</span>
              <h3>{plan.name}</h3>
              <p>{plan.description}</p>
              <div className="blPlanPrice">
                <strong>{plan.price}</strong>
                <small>{plan.note}</small>
              </div>
              <ul>
                {plan.features.map((feature) => (
                  <li key={feature}>{feature}</li>
                ))}
              </ul>
              <a
                className={plan.featured ? "blPrimaryButton" : "blPlanButton"}
                href={plan.href}
                data-analytics-action={plan.name === "Premium" ? "whatsapp_click" : "checkout_click"}
                data-analytics-location="plans"
                data-analytics-plan={plan.name}
              >
                {plan.cta}
              </a>
            </article>
          ))}
        </div>
        <div className="blPlanHelp">
          <div>
            <strong>Dúvidas? Fale com a gente.</strong>
            <p>Nosso time está pronto para te ajudar a escolher o melhor plano.</p>
          </div>
          <a className="blWhatsButton" href={whatsappHref}>
            Falar no WhatsApp
          </a>
        </div>
      </section>

      <section className="blSection blTestimonials">
        <div className="blSectionSplit">
          <div>
            <p className="blEyebrow">Prova real</p>
            <h2>Exemplos de lojas que o Balcão Livre foi feito para atender</h2>
          </div>
          <p>Rotinas comerciais de restaurante, lanchonete, pizzaria e delivery.</p>
        </div>
        <div className="blTestimonialGrid">
          {testimonials.map((item) => (
            <article key={item.name}>
              <span>{item.place}</span>
              <h3>{item.name}</h3>
              <small>{item.city}</small>
              <p>“{item.quote}”</p>
            </article>
          ))}
        </div>
      </section>

      <section className="blContactSection" id="contato">
        <div className="blContactText">
          <p className="blEyebrow">Fale conosco</p>
          <h2>Estamos aqui para ajudar você</h2>
          <p>Entre em contato com nossa equipe e tire suas dúvidas. Será um prazer falar com você.</p>
          <div className="blContactList">
            <a href={whatsappHref}>WhatsApp: {sellers[0]?.phone}</a>
            <a href="mailto:contato@balcaolivrepdv.com.br">contato@balcaolivrepdv.com.br</a>
            <span>Segunda a sexta, 9h às 18h</span>
          </div>
        </div>
        <form className="blContactForm" action={whatsappHref}>
          <h3>Envie sua mensagem</h3>
          <label>
            Nome completo
            <input name="nome" type="text" placeholder="Seu nome" />
          </label>
          <label>
            E-mail
            <input name="email" type="email" placeholder="voce@email.com" />
          </label>
          <label>
            Assunto
            <select name="assunto" defaultValue="">
              <option value="" disabled>Selecione um assunto</option>
              <option>Teste grátis</option>
              <option>Planos</option>
              <option>Instalação</option>
              <option>Suporte</option>
            </select>
          </label>
          <label>
            Mensagem
            <textarea name="mensagem" placeholder="Escreva sua mensagem..." rows="5" />
          </label>
          <button type="submit">Enviar mensagem</button>
        </form>
      </section>

      <section className="blSection blHelpSection" id="ajuda">
        <div className="blHelpPanel">
          <div className="blHelpHero">
            <div>
              <div className="blHelpBrand">
                <img src="/brand/bl-modern-logo.svg" alt="Balcão Livre PDV" />
                <span>Central de ajuda</span>
              </div>
              <h2>Ajuda rápida para vender sem travar a operação.</h2>
              <p>
                Encontre o caminho certo para instalar, vender, imprimir, configurar cardápio, WhatsApp,
                iFood e fechar o caixa sem perder tempo procurando menu.
              </p>
            </div>
            <aside className="blHelpSupport">
              <span>Suporte Balcão Livre</span>
              <strong>{sellers[0]?.phone || "(27) 98126-7551"}</strong>
              <p>Atendimento para teste, implantação e dúvidas do app Windows.</p>
              <a href={whatsappHref}>Falar com suporte</a>
            </aside>
          </div>
          <div className="blHelpSearch">
            <span>Buscar por tópicos, dúvidas ou funcionalidades</span>
            <small>Ex: impressora, abrir caixa, cardápio, Mercado Pago, WhatsApp</small>
          </div>
          <div className="blHelpGrid">
            {helpCards.map(([title, text], index) => (
              <article key={title}>
                <span className="blHelpIcon" aria-hidden="true">
                  <img src="/brand/bl-modern-icon.png" alt="" />
                </span>
                <b>{String(index + 1).padStart(2, "0")}</b>
                <h3>{title}</h3>
                <p>{text}</p>
              </article>
            ))}
          </div>
          <div className="blFaqGrid">
            {faq.map(([question, answer]) => (
              <article key={question}>
                <h3>{question}</h3>
                <p>{answer}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      <footer className="blFooter">
        <div>
          <a className="blHeaderBrand" href="/#inicio">
            <img src="/brand/bl-modern-icon.png" alt="" aria-hidden="true" />
            <span>
              <strong>Balcão Livre</strong>
              <small>PDV</small>
            </span>
          </a>
          <p>O PDV completo para restaurantes que querem vender mais e controlar tudo em uma única tela.</p>
        </div>
        <nav>
          <strong>Navegação</strong>
          <a href="#recursos">Recursos</a>
          <a href="#segmentos">Segmentos</a>
          <a href="#planos">Planos</a>
          <a href="#contato">Contato</a>
        </nav>
        <nav>
          <strong>Suporte</strong>
          <a href="#ajuda">Central de Ajuda</a>
          <a href={whatsappHref}>Dúvidas frequentes</a>
          <a href={whatsappHref}>Fale conosco</a>
        </nav>
        <nav>
          <strong>Legal</strong>
          <a href="/termos/">Termos de Uso</a>
          <a href="/privacidade/">Política de Privacidade</a>
        </nav>
        <small>© 2026 Balcão Livre PDV. Feito para restaurantes.</small>
      </footer>

      <a
        className="blFloatingWhatsapp"
        href={whatsappHref}
        aria-label="Falar no WhatsApp"
        data-analytics-action="whatsapp_click"
        data-analytics-location="floating_button"
      >
        <span className="blFloatingWhatsappIcon">
          <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
            <path d="M16.01 3.2c-7.03 0-12.75 5.72-12.75 12.75 0 2.24.59 4.43 1.7 6.36L3.2 28.8l6.66-1.74a12.7 12.7 0 0 0 6.15 1.57c7.03 0 12.75-5.72 12.75-12.75S23.04 3.2 16.01 3.2Zm0 23.25c-1.92 0-3.79-.52-5.43-1.5l-.39-.23-3.95 1.03 1.06-3.85-.25-.4a10.45 10.45 0 0 1-1.6-5.55c0-5.81 4.73-10.55 10.56-10.55 5.82 0 10.55 4.74 10.55 10.55 0 5.82-4.73 10.5-10.55 10.5Zm5.78-7.9c-.32-.16-1.87-.92-2.16-1.03-.29-.1-.5-.16-.71.16-.21.32-.82 1.03-1 1.24-.18.21-.37.24-.69.08-.32-.16-1.34-.49-2.55-1.57-.94-.84-1.58-1.88-1.76-2.2-.18-.32-.02-.49.14-.65.14-.14.32-.37.47-.55.16-.18.21-.32.32-.53.1-.21.05-.39-.03-.55-.08-.16-.71-1.71-.97-2.34-.26-.62-.52-.53-.71-.54h-.61c-.21 0-.55.08-.84.39-.29.32-1.1 1.08-1.1 2.63s1.13 3.05 1.29 3.26c.16.21 2.22 3.39 5.38 4.75.75.32 1.34.52 1.8.66.76.24 1.45.21 1.99.13.61-.09 1.87-.76 2.13-1.5.26-.74.26-1.37.18-1.5-.08-.13-.29-.21-.61-.37Z" />
          </svg>
        </span>
        <span>
          <strong>Falar no WhatsApp</strong>
          <small>Suporte e planos</small>
        </span>
      </a>
    </main>
  );
}
