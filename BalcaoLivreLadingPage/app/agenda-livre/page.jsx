import Image from "next/image";
import {
  ArrowRight,
  BadgeCheck,
  BarChart3,
  BriefcaseBusiness,
  CalendarDays,
  Check,
  CircleDollarSign,
  ExternalLink,
  Globe2,
  HeartHandshake,
  Laptop,
  MessageCircle,
  MonitorSmartphone,
  PawPrint,
  Play,
  Scissors,
  ShieldCheck,
  Smartphone,
  Sparkles,
  Stethoscope,
  Store,
  UsersRound,
  Wrench
} from "lucide-react";
import styles from "./agenda-livre.module.css";

const whatsappHref =
  "https://wa.me/5533999609457?text=Ol%C3%A1%2C%20quero%20conhecer%20o%20Agenda%20Livre.%20Pode%20me%20mostrar%20como%20ele%20funciona%20no%20meu%20neg%C3%B3cio%3F";
const trialWhatsappHref =
  "https://wa.me/5533999609457?text=Ol%C3%A1%2C%20quero%20testar%20o%20Agenda%20Livre%20gr%C3%A1tis%20por%207%20dias.%20Pode%20me%20ajudar%3F";
const webTrialHref = "/agenda-livre/app/index.html";

const benefits = [
  {
    icon: CalendarDays,
    title: "Agenda sem atrito",
    text: "Organize o dia em quadro, lista ou semana e acompanhe cada atendimento pelo status certo."
  },
  {
    icon: UsersRound,
    title: "Clientes e equipe juntos",
    text: "Centralize clientes, profissionais, serviços, horários e recursos em um só lugar."
  },
  {
    icon: MessageCircle,
    title: "Conversas no contexto",
    text: "Prepare confirmações e abra a conversa no WhatsApp sem perder o contexto da agenda."
  },
  {
    icon: CircleDollarSign,
    title: "Financeiro do dia",
    text: "Acompanhe entradas, despesas, valores a receber e a saúde financeira da operação."
  },
  {
    icon: BarChart3,
    title: "Relatórios que ajudam",
    text: "Veja atendimentos, receita, serviços e desempenho da equipe em uma leitura objetiva."
  },
  {
    icon: MonitorSmartphone,
    title: "Feito para sua rotina",
    text: "Uma experiência consistente no Windows e na Web, do computador ao celular."
  }
];

const segments = [
  { icon: Scissors, label: "Salões e barbearias" },
  { icon: Sparkles, label: "Estética, podologia e spas" },
  { icon: Stethoscope, label: "Clínicas e consultórios" },
  { icon: PawPrint, label: "Pet shops" },
  { icon: Wrench, label: "Oficinas" }
];

const faqs = [
  {
    question: "O Agenda Livre funciona no computador e no celular?",
    answer:
      "Sim. Você pode usar o Agenda Livre no Windows ou abrir a versão Web responsiva no computador, tablet e celular."
  },
  {
    question: "Como funciona o teste grátis de 7 dias?",
    answer:
      "Abra a versão Web sem instalar e use o sistema por 7 dias para validar sua rotina. Durante o teste, os dados ficam salvos localmente no navegador usado."
  },
  {
    question: "O WhatsApp envia mensagens automaticamente?",
    answer:
      "Hoje o Agenda Livre ajuda a preparar a mensagem e abre a conversa no WhatsApp. Automações completas dependem de uma integração segura configurada para o negócio."
  },
  {
    question: "Meus dados sincronizam entre todos os aparelhos?",
    answer:
      "Na versão atual, os dados ficam locais em cada instalação ou navegador. A sincronização entre aparelhos exige uma estrutura de nuvem compartilhada."
  },
  {
    question: "Posso levar meus dados do Windows para a Web?",
    answer:
      "Sim. O fluxo de backup em JSON permite importar na versão Web os dados exportados pelo aplicativo Windows."
  }
];

function WhatsAppButton({ children, location, light = false }) {
  return (
    <a
      className={light ? styles.lightButton : styles.primaryButton}
      href={whatsappHref}
      target="_blank"
      rel="noreferrer"
      data-analytics-action="whatsapp_click"
      data-analytics-seller="Lucas"
      data-analytics-location={location}
    >
      <MessageCircle size={19} strokeWidth={2.2} aria-hidden="true" />
      <span>{children}</span>
      <ArrowRight size={18} aria-hidden="true" />
    </a>
  );
}

export default function AgendaLivrePage() {
  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <a className={styles.brand} href="#inicio" aria-label="Agenda Livre — início">
            <Image
              src="/agenda-livre/agenda-livre-mark.png"
              unoptimized
              width={900}
              height={480}
              alt=""
              className={styles.brandMark}
              priority
            />
            <span className={styles.brandText}>
              <strong>Agenda Livre</strong>
              <small>Sistema de agendamentos</small>
            </span>
          </a>

          <nav className={styles.nav} aria-label="Navegação principal">
            <a href="#produto">Produto</a>
            <a href="#recursos">Recursos</a>
            <a href="#segmentos">Para quem</a>
            <a href="#duvidas">Dúvidas</a>
          </nav>

          <a
            className={styles.headerCta}
            href="#teste"
            data-analytics-action="agenda_trial_start"
            data-analytics-location="header"
          >
            Testar grátis 7 dias
            <ArrowRight size={17} aria-hidden="true" />
          </a>
        </div>
      </header>

      <section className={styles.hero} id="inicio">
        <div className={styles.heroGlow} aria-hidden="true" />
        <div className={styles.container}>
          <div className={styles.heroGrid}>
            <div className={styles.heroCopy}>
              <div className={styles.eyebrow}>
                <Sparkles size={16} aria-hidden="true" />
                Gestão leve para negócios com hora marcada
              </div>
              <h1>
                Sua agenda. Seu tempo. <span>Seu negócio.</span>
              </h1>
              <p className={styles.heroLead}>
                Centralize clientes, equipe, serviços e finanças em uma agenda feita para a
                rotina real do seu negócio.
              </p>

              <div className={styles.heroActions}>
                <a
                  className={styles.primaryButton}
                  href="#teste"
                  data-analytics-action="agenda_trial_start"
                  data-analytics-location="hero"
                >
                  <Play size={18} fill="currentColor" aria-hidden="true" />
                  Testar grátis por 7 dias
                  <ArrowRight size={18} aria-hidden="true" />
                </a>
                <a
                  className={styles.secondaryButton}
                  href={webTrialHref}
                  target="_blank"
                  rel="noreferrer"
                  data-analytics-action="agenda_web_trial"
                  data-analytics-location="hero"
                >
                  <Globe2 size={18} aria-hidden="true" />
                  Abrir versão Web
                </a>
              </div>

              <ul className={styles.heroChecks} aria-label="Destaques do produto">
                <li>
                  <Check size={15} aria-hidden="true" />
                  Windows e Web responsiva
                </li>
                <li>
                  <Check size={15} aria-hidden="true" />
                  Computador e celular
                </li>
                <li>
                  <Check size={15} aria-hidden="true" />
                  Teste grátis por 7 dias
                </li>
              </ul>
            </div>

            <div className={styles.heroVisual} aria-label="Capturas reais do Agenda Livre no desktop e no celular">
              <div className={styles.desktopFrame}>
                <div className={styles.windowBar} aria-hidden="true">
                  <span />
                  <span />
                  <span />
                  <small>Agenda Livre para Windows</small>
                </div>
                <Image
                  src="/agenda-livre/windows-dashboard.png"
                  unoptimized
                  width={1373}
                  height={682}
                  alt="Painel real do Agenda Livre para Windows, com agenda, confirmações e caixa do dia"
                  className={styles.desktopImage}
                  priority
                  sizes="(max-width: 900px) 92vw, 58vw"
                />
              </div>

              <div className={styles.phoneFrame}>
                <div className={styles.phoneNotch} aria-hidden="true" />
                <Image
                  src="/agenda-livre/web-mobile-finance.png"
                  unoptimized
                  width={390}
                  height={844}
                  alt="Tela real do financeiro do Agenda Livre em um celular"
                  className={styles.phoneImage}
                  priority
                  sizes="(max-width: 650px) 32vw, 180px"
                />
              </div>

              <div className={styles.visualBadge}>
                <span className={styles.visualBadgeIcon}>
                  <BadgeCheck size={20} aria-hidden="true" />
                </span>
                <span>
                  <strong>Tela real do produto</strong>
                  <small>Conteúdo interno demonstrativo</small>
                </span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className={styles.valueStrip} aria-label="Principais áreas do Agenda Livre">
        <div className={styles.container}>
          <div className={styles.valueGrid}>
            <div>
              <CalendarDays aria-hidden="true" />
              <span>
                <strong>Agenda em tempo real</strong>
                <small>Dia, lista e semana</small>
              </span>
            </div>
            <div>
              <MessageCircle aria-hidden="true" />
              <span>
                <strong>WhatsApp no fluxo</strong>
                <small>Confirmações com contexto</small>
              </span>
            </div>
            <div>
              <CircleDollarSign aria-hidden="true" />
              <span>
                <strong>Visão do caixa</strong>
                <small>Entradas, saídas e cobranças</small>
              </span>
            </div>
            <div>
              <BarChart3 aria-hidden="true" />
              <span>
                <strong>Relatórios objetivos</strong>
                <small>Operação em uma leitura</small>
              </span>
            </div>
          </div>
        </div>
      </section>

      <section className={styles.productSection} id="produto">
        <div className={styles.container}>
          <div className={styles.sectionHeading}>
            <div>
              <span className={styles.kicker}>O produto por dentro</span>
              <h2>Uma visão completa, sem complicar sua rotina.</h2>
            </div>
            <p>
              Estas são telas reais do Agenda Livre no Windows e na Web. A mesma lógica
              acompanha você da recepção ao fechamento do dia.
            </p>
          </div>

          <div className={styles.demoNotice}>
            <BadgeCheck size={17} aria-hidden="true" />
            <p>
              <strong>Telas reais dos aplicativos.</strong> Nomes, valores e atendimentos exibidos
              nas capturas são dados fictícios usados apenas para demonstração.
            </p>
          </div>

          <div className={styles.showcaseGrid}>
            <article className={`${styles.showcaseCard} ${styles.showcaseWide}`}>
              <div className={styles.cardTopline}>
                <span className={styles.platformIcon}>
                  <Laptop size={18} aria-hidden="true" />
                </span>
                <span>
                  <strong>Agenda Livre Windows</strong>
                  <small>Agenda visual com equipe e status</small>
                </span>
                <span className={styles.realTag}>Tela real · dados demo</span>
              </div>
              <div className={styles.screenshotShell}>
                <Image
                  src="/agenda-livre/windows-agenda.png"
                  unoptimized
                  width={1373}
                  height={682}
                  alt="Agenda visual real do aplicativo Agenda Livre para Windows"
                  className={styles.showcaseImage}
                  sizes="(max-width: 800px) 92vw, 72vw"
                />
              </div>
            </article>

            <article className={`${styles.showcaseCard} ${styles.showcaseDesktop}`}>
              <div className={styles.cardTopline}>
                <span className={styles.platformIcon}>
                  <MonitorSmartphone size={18} aria-hidden="true" />
                </span>
                <span>
                  <strong>Agenda Livre Web</strong>
                  <small>Financeiro no navegador do computador</small>
                </span>
                <span className={styles.realTag}>Tela real · dados demo</span>
              </div>
              <div className={styles.screenshotShell}>
                <Image
                  src="/agenda-livre/web-desktop-finance.png"
                  unoptimized
                  width={1366}
                  height={768}
                  alt="Tela real do financeiro do Agenda Livre na Web em um computador"
                  className={styles.showcaseImage}
                  sizes="(max-width: 800px) 92vw, 55vw"
                />
              </div>
            </article>

            <article className={`${styles.showcaseCard} ${styles.mobileShowcase}`}>
              <div className={styles.mobileCopy}>
                <div className={styles.cardTopline}>
                  <span className={styles.platformIcon}>
                    <Smartphone size={18} aria-hidden="true" />
                  </span>
                  <span>
                    <strong>No celular: Web responsiva</strong>
                    <small>A rotina cabe na sua mão</small>
                  </span>
                </div>
                <h3>Consulte, agende e acompanhe de onde estiver.</h3>
                <p>
                  A navegação se adapta ao celular mantendo agenda, painel e financeiro fáceis
                  de encontrar.
                </p>
                <ul>
                  <li>
                    <Check size={15} aria-hidden="true" /> Navegação pensada para toque
                  </li>
                  <li>
                    <Check size={15} aria-hidden="true" /> Informações essenciais primeiro
                  </li>
                  <li>
                    <Check size={15} aria-hidden="true" /> Mesma identidade do desktop
                  </li>
                </ul>
              </div>
              <div className={styles.phonePair} aria-label="Telas reais do painel e da agenda no celular">
                <div className={styles.miniPhone}>
                  <Image
                    src="/agenda-livre/web-mobile-dashboard.png"
                    unoptimized
                    width={390}
                    height={844}
                    alt="Painel real do Agenda Livre no celular"
                    sizes="(max-width: 600px) 38vw, 210px"
                  />
                </div>
                <div className={`${styles.miniPhone} ${styles.miniPhoneOffset}`}>
                  <Image
                    src="/agenda-livre/web-mobile-agenda.png"
                    unoptimized
                    width={390}
                    height={844}
                    alt="Agenda real do Agenda Livre no celular"
                    sizes="(max-width: 600px) 38vw, 210px"
                  />
                </div>
              </div>
            </article>
          </div>
        </div>
      </section>

      <section className={styles.trialSection} id="teste">
        <div className={styles.container}>
          <div className={styles.trialHeading}>
            <div>
              <span className={styles.kicker}>Teste grátis por 7 dias</span>
              <h2>Escolha onde você quer começar.</h2>
            </div>
            <p>
              Abra no navegador sem instalar e use esses 7 dias para colocar sua rotina de verdade
              dentro da agenda, no computador, tablet ou celular.
            </p>
          </div>

          <div className={styles.trialGrid}>
            <article className={`${styles.trialCard} ${styles.trialFeatured}`}>
              <div className={styles.trialCardTop}>
                <span className={styles.trialCardIcon}>
                  <Globe2 size={23} aria-hidden="true" />
                </span>
                <span className={styles.trialBadge}>Sem instalar</span>
              </div>
              <h3>Testar na Web</h3>
              <p>
                Funciona no computador, tablet e celular. Abra, configure seu negócio e comece
                agora.
              </p>
              <ul>
                <li>
                  <Check size={15} aria-hidden="true" /> Acesso imediato
                </li>
                <li>
                  <Check size={15} aria-hidden="true" /> Responsivo no celular
                </li>
                <li>
                  <Check size={15} aria-hidden="true" /> Dados salvos neste navegador
                </li>
              </ul>
              <a
                className={styles.primaryButton}
                href={webTrialHref}
                target="_blank"
                rel="noreferrer"
                data-analytics-action="agenda_web_trial"
                data-analytics-location="trial"
              >
                <Play size={18} fill="currentColor" aria-hidden="true" />
                Abrir teste na Web
                <ExternalLink size={17} aria-hidden="true" />
              </a>
              <small className={styles.trialMeta}>Abre em uma nova guia.</small>
            </article>

            <article className={`${styles.trialCard} ${styles.trialWindows}`}>
              <div className={styles.trialCardTop}>
                <span className={styles.trialCardIcon}>
                  <Laptop size={23} aria-hidden="true" />
                </span>
                <span className={styles.trialBadge}>Windows</span>
              </div>
              <h3>Quer instalar no Windows?</h3>
              <p>
                Peça a versão para computador e receba ajuda para fazer a primeira configuração
                do seu negócio.
              </p>
              <a
                className={styles.trialSupportLink}
                href={trialWhatsappHref}
                target="_blank"
                rel="noreferrer"
                data-analytics-action="whatsapp_click"
                data-analytics-seller="Lucas"
                data-analytics-location="trial-windows"
              >
                <MessageCircle size={18} aria-hidden="true" />
                Solicitar versão Windows
                <ArrowRight size={17} aria-hidden="true" />
              </a>
            </article>
          </div>

          <p className={styles.trialFootnote}>
            O teste usa dados locais e a versão Web salva neste navegador. Sincronização entre
            dispositivos depende de uma configuração adicional.
          </p>
        </div>
      </section>

      <section className={styles.featuresSection} id="recursos">
        <div className={styles.container}>
          <div className={styles.centerHeading}>
            <span className={styles.kicker}>Tudo conversa entre si</span>
            <h2>Atendimento, agenda e gestão no mesmo ritmo.</h2>
            <p>
              Menos abas, menos anotações soltas e mais clareza para decidir o que vem a seguir.
            </p>
          </div>

          <div className={styles.featuresGrid}>
            {benefits.map(({ icon: Icon, title, text }) => (
              <article className={styles.featureCard} key={title}>
                <span className={styles.featureIcon}>
                  <Icon size={22} strokeWidth={1.9} aria-hidden="true" />
                </span>
                <h3>{title}</h3>
                <p>{text}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className={styles.reportsSection}>
        <div className={styles.container}>
          <div className={styles.reportsGrid}>
            <div className={styles.reportsVisual}>
              <div className={styles.reportFrame}>
                <Image
                  src="/agenda-livre/web-desktop-reports.png"
                  unoptimized
                  width={1366}
                  height={768}
                  alt="Tela real de relatórios do Agenda Livre na Web"
                  className={styles.reportImage}
                  sizes="(max-width: 900px) 92vw, 55vw"
                />
              </div>
              <div className={styles.insightChip}>
                <BarChart3 size={19} aria-hidden="true" />
                <span>
                  <strong>Leitura rápida</strong>
                  <small>Dados essenciais sem planilhas soltas</small>
                </span>
              </div>
            </div>

            <div className={styles.reportsCopy}>
              <span className={styles.kicker}>Gestão com contexto</span>
              <h2>Do primeiro horário ao resultado do período.</h2>
              <p>
                Entenda o movimento do negócio com indicadores de atendimento, receita,
                serviços e profissionais — todos conectados à rotina da agenda.
              </p>
              <ul className={styles.statementList}>
                <li>
                  <span>
                    <BriefcaseBusiness size={19} aria-hidden="true" />
                  </span>
                  <div>
                    <strong>Operação em um só lugar</strong>
                    <p>Clientes, equipe, serviços e agenda compartilham a mesma visão.</p>
                  </div>
                </li>
                <li>
                  <span>
                    <ShieldCheck size={19} aria-hidden="true" />
                  </span>
                  <div>
                    <strong>Backup sob seu controle</strong>
                    <p>Exporte seus dados e leve o backup do Windows para a Web.</p>
                  </div>
                </li>
                <li>
                  <span>
                    <HeartHandshake size={19} aria-hidden="true" />
                  </span>
                  <div>
                    <strong>Implantação mais humana</strong>
                    <p>Fale com uma pessoa para entender a melhor configuração para você.</p>
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section className={styles.segmentsSection} id="segmentos">
        <div className={styles.container}>
          <div className={styles.centerHeading}>
            <span className={styles.kicker}>Um sistema, diferentes rotinas</span>
            <h2>Feito para quem transforma tempo em atendimento.</h2>
          </div>

          <div className={styles.segmentGrid}>
            {segments.map(({ icon: Icon, label }) => (
              <div className={styles.segmentCard} key={label}>
                <Icon size={23} strokeWidth={1.8} aria-hidden="true" />
                <span>{label}</span>
              </div>
            ))}
          </div>

          <div className={styles.onboardingCard}>
            <div className={styles.onboardingCopy}>
              <span className={styles.kicker}>Comece com o seu jeito</span>
              <h2>Configure o negócio sem enfrentar uma tela vazia.</h2>
              <p>
                O onboarding guia os primeiros dados, o segmento, a equipe e os serviços. Depois,
                você escolhe a identidade visual que combina com sua operação.
              </p>
              <div className={styles.onboardingPoints}>
                <span>
                  <Check size={15} aria-hidden="true" /> Passo a passo inicial
                </span>
                <span>
                  <Check size={15} aria-hidden="true" /> Temas por segmento
                </span>
                <span>
                  <Check size={15} aria-hidden="true" /> Dados exportáveis
                </span>
              </div>
              <a
                className={styles.primaryButton}
                href="#teste"
                data-analytics-action="agenda_trial_start"
                data-analytics-location="onboarding"
              >
                <Play size={18} fill="currentColor" aria-hidden="true" />
                Começar meu teste de 7 dias
                <ArrowRight size={18} aria-hidden="true" />
              </a>
            </div>
            <div className={styles.onboardingPhone}>
              <Image
                src="/agenda-livre/web-mobile-onboarding.png"
                unoptimized
                width={390}
                height={844}
                alt="Tela real de configuração inicial do Agenda Livre no celular"
                sizes="(max-width: 700px) 68vw, 280px"
              />
            </div>
          </div>
        </div>
      </section>

      <section className={styles.faqSection} id="duvidas">
        <div className={styles.container}>
          <div className={styles.faqGrid}>
            <div className={styles.faqIntro}>
              <span className={styles.kicker}>Perguntas frequentes</span>
              <h2>Clareza antes de começar.</h2>
              <p>
                Sem promessas escondidas. Se ainda tiver alguma dúvida, converse com a gente pelo
                WhatsApp.
              </p>
              <WhatsAppButton location="faq">Tirar uma dúvida</WhatsAppButton>
            </div>
            <div className={styles.faqList}>
              {faqs.map((faq, index) => (
                <details className={styles.faqItem} key={faq.question} open={index === 0}>
                  <summary>
                    {faq.question}
                    <span aria-hidden="true">+</span>
                  </summary>
                  <p>{faq.answer}</p>
                </details>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className={styles.finalCta}>
        <div className={styles.container}>
          <div className={styles.finalCtaCard}>
            <div className={styles.ctaMark} aria-hidden="true">
              <Store size={26} />
            </div>
            <span className={styles.ctaEyebrow}>Sua próxima agenda começa aqui</span>
            <h2>Mais organização para você. Mais atenção para seus clientes.</h2>
            <p>
              Abra na Web e use 7 dias para ver o Agenda Livre aplicado à sua rotina, no
              computador ou no celular.
            </p>
            <a
              className={styles.lightButton}
              href="#teste"
              data-analytics-action="agenda_trial_start"
              data-analytics-location="final-cta"
            >
              <Play size={18} fill="currentColor" aria-hidden="true" />
              Testar grátis por 7 dias
              <ArrowRight size={18} aria-hidden="true" />
            </a>
            <small>Sem cartão e sem compromisso. Seus dados ficam no dispositivo usado.</small>
          </div>
        </div>
      </section>

      <footer className={styles.footer}>
        <div className={styles.container}>
          <div className={styles.footerInner}>
            <a className={styles.brand} href="#inicio" aria-label="Agenda Livre — voltar ao início">
              <Image
                src="/agenda-livre/agenda-livre-mark.png"
                unoptimized
                width={900}
                height={480}
                alt=""
                className={styles.brandMark}
              />
              <span className={styles.brandText}>
                <strong>Agenda Livre</strong>
                <small>Sistema de agendamentos</small>
              </span>
            </a>
            <p>© 2026 Agenda Livre. Feito para negócios que atendem com hora marcada.</p>
            <a href={whatsappHref} target="_blank" rel="noreferrer">
              Falar pelo WhatsApp
              <ArrowRight size={16} aria-hidden="true" />
            </a>
          </div>
        </div>
      </footer>
    </main>
  );
}
