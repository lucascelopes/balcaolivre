import {
  ChevronDown,
  Headphones,
  Pizza,
  Sandwich,
  Scissors,
  Sparkles,
  Star,
  Stethoscope,
  Truck,
  UserRound,
  Utensils,
  Wine
} from "lucide-react";
import PaymentSuccess from "./PaymentSuccess";
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

const solutionCards = [
  {
    name: "Balcão Livre PDV",
    badge: "PDV",
    text: "O sistema de vendas completo para o seu negócio.",
    image: "/brand/pdv-real-screen.png",
    features: ["Vendas rápidas", "Controle de caixa", "Produtos e estoque", "Clientes e histórico", "Pagamentos e relatórios"],
    accent: "blue"
  },
  {
    name: "Agenda Livre",
    badge: "Agenda",
    text: "Gerencie seus agendamentos com praticidade e reduza faltas.",
    image: "/brand/hero-pdv-restaurante.png",
    features: ["Agenda online", "Agendamento 24h", "Lembretes automáticos", "Confirmação via WhatsApp", "Relatórios de ocupação"],
    accent: "green"
  },
  {
    name: "Gestão Livre",
    badge: "Gestão",
    text: "Tenha o controle financeiro e indicadores na palma da sua mão.",
    image: "/brand/pdv-online-screen.png",
    features: ["Fluxo de caixa", "Contas a pagar/receber", "Indicadores e metas", "Relatórios gerenciais", "Visão 360 do negócio"],
    accent: "purple"
  }
];

const segments = [
  { title: "Restaurantes", text: "Atendimento ágil no salão e delivery.", image: "https://images.unsplash.com/photo-1551218808-94e220e084d2?auto=format&fit=crop&w=700&q=80", Icon: Utensils, tone: "orange" },
  { title: "Lanchonetes", text: "Mais giro e ticket médio maior.", image: "https://images.unsplash.com/photo-1571091718767-18b5b1457add?auto=format&fit=crop&w=700&q=80", Icon: Sandwich, tone: "yellow" },
  { title: "Pizzarias", text: "Pedidos e entregas organizados.", image: "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=700&q=80", Icon: Pizza, tone: "red" },
  { title: "Bares", text: "Comandas, estoque e vendas em tempo real.", image: "https://images.unsplash.com/photo-1572116469696-31de0f17cc34?auto=format&fit=crop&w=700&q=80", Icon: Wine, tone: "purple" },
  { title: "Food Trucks", text: "Mobilidade para vender onde estiver.", image: "https://images.unsplash.com/photo-1565123409695-7b5ef63a2efb?auto=format&fit=crop&w=700&q=80", Icon: Truck, tone: "green" },
  { title: "Barbearias", text: "Agenda, comissões e fidelização.", image: "https://images.unsplash.com/photo-1585747860715-2ba37e788b70?auto=format&fit=crop&w=700&q=80", Icon: Scissors, tone: "blue" },
  { title: "Salões", text: "Agenda cheia e clientes bem atendidos.", image: "https://images.unsplash.com/photo-1560066984-138dadb4c035?auto=format&fit=crop&w=700&q=80", Icon: Headphones, tone: "pink" },
  { title: "Clínicas", text: "Prontuários e atendimento organizado.", image: "https://images.unsplash.com/photo-1666214280557-f1b5022eb634?auto=format&fit=crop&w=700&q=80", Icon: Stethoscope, tone: "teal" },
  { title: "Estéticas", text: "Pacotes, sessões e controle completo.", image: "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?auto=format&fit=crop&w=700&q=80", Icon: Sparkles, tone: "violet" },
  { title: "Consultórios", text: "Consultas e relacionamento com clientes.", image: "https://images.unsplash.com/photo-1629909613654-28e377c37b09?auto=format&fit=crop&w=700&q=80", Icon: UserRound, tone: "blue" }
];

const plans = [
  {
    name: "Balcão Livre PDV",
    badge: "PDV",
    tone: "blue",
    price: "R$ 29,99",
    href: `${checkoutFunctionUrl}?plan=pdv-mensal`,
    features: ["Vendas e PDV", "Cadastro de produtos", "Controle de estoque", "Clientes e histórico", "Relatórios de vendas", "Suporte online"]
  },
  {
    name: "Agenda Livre",
    badge: "Agenda",
    tone: "green",
    price: "R$ 39,99",
    href: `${checkoutFunctionUrl}?plan=agenda-mensal`,
    features: ["Agenda online", "Lembretes automáticos", "Agendamento 24h", "Profissionais e serviços", "Relatórios de ocupação", "Suporte online"]
  },
  {
    name: "Gestão Livre",
    badge: "Gestão",
    tone: "purple",
    price: "R$ 49,99",
    href: `${checkoutFunctionUrl}?plan=gestao-mensal`,
    features: ["Fluxo de caixa", "Contas a pagar/receber", "Indicadores e metas", "Relatórios gerenciais", "Análise de desempenho", "Suporte online"]
  },
  {
    name: "Balcão Livre Completo",
    badge: "Mais completo",
    tone: "blue",
    chips: ["PDV", "Agenda", "Gestão"],
    price: "R$ 99,99",
    href: `${checkoutFunctionUrl}?plan=completo-mensal`,
    featured: true,
    features: ["Tudo do PDV", "Tudo da Agenda", "Tudo da Gestão", "Integração total entre sistemas", "Suporte prioritário", "Visão completa do negócio"]
  }
];

const testimonials = [
  {
    name: "Juliana Costa",
    role: "Restaurante Brasa & Sabor",
    quote: "Antes eu conferia pedido e caixa no caderno. Agora fecho o dia sem ficar caçando anotação.",
    avatar: "/brand/testimonials/juliana-restaurante.png"
  },
  {
    name: "Rafael Nunes",
    role: "Info Tech Informática",
    quote: "A venda ficou mais rápida e o estoque parou de escapar. A gente acha tudo em segundos.",
    avatar: "/brand/testimonials/rafael-infotech.png"
  },
  {
    name: "Carla Mendes",
    role: "Pet Shop Bem-vindos",
    quote: "Uso pelo celular para ver vendas e chamar clientes. Ficou bem mais fácil cuidar da loja.",
    avatar: "/brand/testimonials/carla-petshop.png"
  }
];

const faqs = [
  ["Os sistemas funcionam offline?", "O PDV Windows continua vendendo localmente. Recursos online dependem da conexão."],
  ["Posso testar antes de assinar?", "Sim. Você pode testar grátis por 7 dias e decidir com calma."],
  ["Posso cancelar quando quiser?", "Sim. Não existe fidelidade obrigatória."],
  ["A migração dos meus dados é segura?", "Sim. Nossa equipe ajuda a organizar a implantação e os cadastros."],
  ["Meus dados estão seguros?", "Sim. Os sistemas usam estrutura online com segurança e backups."]
];

const productJsonLd = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: siteName,
  applicationCategory: "BusinessApplication",
  operatingSystem: "Web, Windows",
  url: siteUrl,
  image: absoluteUrl("/brand/pdv-real-screen.png"),
  description: defaultDescription,
  offers: plans.map((plan) => ({
    "@type": "Offer",
    priceCurrency: "BRL",
    price: plan.price.replace("R$ ", "").replace(",", "."),
    name: plan.name
  }))
};

const faqJsonLd = {
  "@context": "https://schema.org",
  "@type": "FAQPage",
  mainEntity: faqs.map(([question, answer]) => ({
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
    <main className="lpReferenceLanding">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify([productJsonLd, faqJsonLd]) }}
      />
      <SiteHeader id="inicio" />

      <section className="lpHero">
        <div className="lpHeroCopy">
          <p className="lpKicker">Tudo que seu negócio precisa</p>
          <h1>
            PDV, Agenda e Gestão para vender, agendar e <span>crescer.</span>
          </h1>
          <p className="lpLead">
            Sistemas simples e poderosos que se conectam para facilitar sua rotina, aumentar suas vendas
            e melhorar a experiência dos seus clientes.
          </p>

          <div className="lpFeatureRow">
            <article>
              <b>Fácil</b>
              <small>de usar desde o primeiro dia</small>
            </article>
            <article>
              <b>Nuvem</b>
              <small>seguro para acessar de onde estiver</small>
            </article>
            <article>
              <b>Suporte</b>
              <small>humano sempre próximo</small>
            </article>
            <article>
              <b>Dados</b>
              <small>relatórios para decidir melhor</small>
            </article>
          </div>

          <div className="lpHeroActions">
            <a className="lpSolidButton lpGreenButton" href={downloadUrl}>
              Testar grátis 7 dias
            </a>
            <a className="lpGhostButton lpWhatsButton" href={whatsappHref}>
              <span><WhatsAppLogo /></span>
              Falar com especialista
            </a>
          </div>

          <div className="lpTrustLine">
            <span>Sistema 100% online</span>
            <span>Seus dados sempre seguros</span>
            <span>Sem fidelidade</span>
          </div>
        </div>

        <div className="lpProductStage" aria-label="Prévia do sistema Balcão Livre em notebook e celular">
          <div className="lpLaptop">
            <div className="lpAppTopbar">
              <img src="/brand/bl-modern-icon.png" alt="" aria-hidden="true" />
              <strong>Balcão Livre</strong>
            </div>
            <div className="lpAppShell">
              <aside>
                {["Início", "PDV", "Agenda", "Clientes", "Produtos", "Financeiro", "Relatórios"].map((item) => (
                  <span key={item}>{item}</span>
                ))}
              </aside>
              <div className="lpDashboard">
                <section className="lpSalesList">
                  <h3>Últimas vendas</h3>
                  {["Mariana Silva", "Ricardo Lima", "Camila Pereira", "João Santos"].map((name, index) => (
                    <p key={name}>
                      <span>{name}</span>
                      <strong>R$ {[124, 89.5, 59, 45][index].toFixed(2).replace(".", ",")}</strong>
                    </p>
                  ))}
                  <a href="#planos">Ver todas</a>
                </section>
                <section className="lpSummaryCard">
                  <span>Resumo do dia</span>
                  <small>Total de vendas</small>
                  <strong>R$ 5.290,00</strong>
                  <p><b>Dinheiro</b><em>R$ 1.650,00</em></p>
                  <p><b>Cartão</b><em>R$ 2.890,00</em></p>
                  <p><b>Pix</b><em>R$ 750,00</em></p>
                  <button type="button">Nova venda</button>
                </section>
                <div className="lpQuickActions">
                  <span>Nova venda</span>
                  <span>Novo agendamento</span>
                  <span>Clientes</span>
                  <span>Relatórios</span>
                </div>
              </div>
            </div>
          </div>

          <div className="lpPhone">
            <div className="lpPhoneTop">Balcão Livre</div>
            <h3>Resumo do dia</h3>
            <small>Total de vendas</small>
            <strong>R$ 5.290,00</strong>
            <p><span>Vendas</span><b>68</b></p>
            <p><span>Agendamentos</span><b>23</b></p>
            <p><span>Clientes</span><b>156</b></p>
            <a href="#relatorios">Ver relatórios</a>
          </div>
        </div>
      </section>

      <section className="lpSolutions" id="solucoes">
        <div className="lpSectionHead lpCenter">
          <h2>Três soluções completas que trabalham juntas</h2>
          <p>Escolha o sistema ideal para sua necessidade. Todos 100% online, integrados e fáceis de usar.</p>
        </div>
        <div className="lpSolutionGrid">
          {solutionCards.map((solution) => (
            <article className={`lpSolutionCard lpSolutionCard-${solution.accent}`} key={solution.name}>
              <div className="lpSolutionTitle">
                <h3>{solution.name}</h3>
                <span>{solution.badge}</span>
              </div>
              <p>{solution.text}</p>
              <img src={solution.image} alt={`Tela do ${solution.name}`} />
              <ul>
                {solution.features.map((feature) => (
                  <li key={feature}>{feature}</li>
                ))}
              </ul>
              <a href="#planos">Saiba mais</a>
            </article>
          ))}
        </div>
      </section>

      <section className="lpSegments" id="segmentos">
        <div className="lpSectionHead lpCenter">
          <h2>Feito para diversos segmentos</h2>
          <p>Soluções flexíveis que se adaptam ao seu negócio.</p>
        </div>
        <div className="lpSegmentGrid">
          {segments.map(({ title, text, image, Icon, tone }) => (
            <article key={title}>
              <img src={image} alt="" />
              <div>
                <span className={`lpSegmentIcon lpSegmentIcon-${tone}`} aria-hidden="true">
                  <Icon size={16} strokeWidth={2.4} />
                </span>
                <h3>{title}</h3>
                <p>{text}</p>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="lpPricing" id="planos">
        <div className="lpSectionHead lpCenter">
          <h2>Planos que cabem no seu bolso</h2>
          <p>Escolha o plano ideal e comece hoje mesmo. Teste grátis por 7 dias.</p>
        </div>
        <div className="lpPricingGrid">
          {plans.map((plan) => (
            <article className={`lpPriceCard lpPriceCard-${plan.tone}${plan.featured ? " lpPriceCardFeatured" : ""}`} key={plan.name}>
              {plan.featured ? <div className="lpBestBadge">Mais completo</div> : null}
              <div className="lpPriceTop">
                <h3>{plan.name}</h3>
                {plan.chips ? (
                  <div className="lpPriceChips">
                    {plan.chips.map((chip) => (
                      <span key={chip}>{chip}</span>
                    ))}
                  </div>
                ) : (
                  <span>{plan.badge}</span>
                )}
              </div>
              <div className="lpPrice">
                <strong>{plan.price}</strong>
                <small>/mês</small>
              </div>
              <ul>
                {plan.features.map((feature) => (
                  <li key={feature}>{feature}</li>
                ))}
              </ul>
              <a className={plan.featured ? "lpSolidButton" : "lpPlanButton"} href={plan.href}>
                Testar grátis 7 dias
              </a>
            </article>
          ))}
        </div>
        <div className="lpPlanBenefits">
          <span>Cancelamento fácil</span>
          <span>Sem fidelidade</span>
          <span>7 dias grátis para testar</span>
          <span>Suporte em português</span>
        </div>
      </section>

      <section className="lpBottomGrid" id="depoimentos">
        <div className="lpTestimonials">
          <h2>O que nossos clientes dizem</h2>
          <div>
            {testimonials.map(({ name, role, quote, avatar }) => (
              <article key={name}>
                <img src={avatar} alt={name} />
                <p>"{quote}"</p>
                <strong>{name}</strong>
                <small>{role}</small>
                <b aria-label="5 de 5 estrelas">
                  {Array.from({ length: 5 }).map((_, index) => (
                    <Star key={index} size={9} fill="currentColor" strokeWidth={2.2} />
                  ))}
                </b>
              </article>
            ))}
          </div>
        </div>

        <div className="lpFaq" id="suporte">
          <h2>Dúvidas frequentes</h2>
          {faqs.map(([question]) => (
            <details key={question}>
              <summary>
                <span>{question}</span>
                <ChevronDown size={12} strokeWidth={2.4} />
              </summary>
              <p>{faqs.find(([item]) => item === question)?.[1]}</p>
            </details>
          ))}
          <a href={whatsappHref}>Ver todas as perguntas</a>
        </div>

        <aside className="lpFinalCta" id="contato">
          <h2>Pronto para transformar seu negócio?</h2>
          <p>Teste grátis por 7 dias e descubra como nossas soluções podem te ajudar a vender mais, agendar melhor e crescer.</p>
          <a className="lpSolidButton lpGreenButton" href={downloadUrl}>Testar grátis 7 dias</a>
          <a className="lpGhostButton lpWhatsButton" href={whatsappHref}>
            <span><WhatsAppLogo /></span>
            Falar via WhatsApp
          </a>
        </aside>
      </section>

      <footer className="lpFooter">
        <div>
          <a className="lpFooterBrand" href="/#inicio">
            <img src="/brand/bl-modern-icon.png" alt="" />
            <span>
              <strong>Balcão Livre</strong>
              <small>Soluções completas para vender, agendar e gerir seu negócio.</small>
            </span>
          </a>
        </div>
        <nav>
          <strong>Navegação</strong>
          <a href="#solucoes">Soluções</a>
          <a href="#segmentos">Segmentos</a>
          <a href="#planos">Planos</a>
          <a href="#depoimentos">Depoimentos</a>
        </nav>
        <nav>
          <strong>Soluções</strong>
          <a href="#solucoes">PDV</a>
          <a href="#solucoes">Agenda</a>
          <a href="#solucoes">Gestão</a>
          <a href="#solucoes">Integrações</a>
        </nav>
        <nav>
          <strong>Recursos</strong>
          <a href="#planos">Funcionalidades</a>
          <a href="#suporte">Segurança</a>
          <a href="#suporte">Blog</a>
          <a href="#suporte">Ajuda</a>
        </nav>
        <nav>
          <strong>Fale conosco</strong>
          <a href={whatsappHref}>{sellers[0]?.phone || "(27) 99999-9999"}</a>
          <a href="mailto:contato@balcaolivrepdv.com.br">contato@balcaolivrepdv.com.br</a>
          <span>Seg a Sex, 8h às 18h</span>
        </nav>
        <small>© 2026 Balcão Livre. Todos os direitos reservados.</small>
      </footer>

      <a className="lpFloatingWhatsapp" href={whatsappHref} aria-label="Falar no WhatsApp">
        <WhatsAppLogo />
      </a>
    </main>
  );
}
