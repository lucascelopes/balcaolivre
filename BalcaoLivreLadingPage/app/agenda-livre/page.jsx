import Image from "next/image";
import {
  ArrowRightIcon,
  AtIcon,
  BrowsersIcon,
  CalendarBlankIcon,
  CalendarCheckIcon,
  ChartLineUpIcon,
  CheckCircleIcon,
  CheckIcon,
  ClockIcon,
  CoinsIcon,
  DesktopIcon,
  DeviceMobileIcon,
  DownloadSimpleIcon,
  FirstAidIcon,
  GiftIcon,
  GlobeIcon,
  HairDryerIcon,
  InstagramLogoIcon,
  LaptopIcon,
  LeafIcon,
  PaletteIcon,
  PawPrintIcon,
  PhoneIcon,
  ScissorsIcon,
  SparkleIcon,
  StorefrontIcon,
  UserCircleIcon,
  UserIcon,
  UsersThreeIcon,
  WhatsappLogoIcon,
} from "@phosphor-icons/react/ssr";
import AgendaLivreMotion from "./AgendaLivreMotion";
import styles from "./agenda-livre.module.css";

const webTrialHref = "https://app.minhaagendalivre.com.br/";
const windowsDownloadHref =
  "https://minhaagendalivre.com.br/agenda-livre/agenda-livre-windows-1.0.0.zip";
const whatsappHref =
  "https://wa.me/5533991314125?text=Ol%C3%A1%2C%20quero%20conhecer%20o%20Agenda%20Livre.%20Pode%20me%20mostrar%20como%20ele%20funciona%20no%20meu%20neg%C3%B3cio%3F";
const monthlySubscriptionFallbackHref =
  "https://wa.me/5533991314125?text=Ol%C3%A1%2C%20quero%20assinar%20o%20Agenda%20Livre%20mensal%20por%20R%24%2049%2C90.";
const annualSubscriptionFallbackHref =
  "https://wa.me/5533991314125?text=Ol%C3%A1%2C%20quero%20assinar%20o%20Agenda%20Livre%20anual%20por%20R%24%20598%2C80%20e%20receber%20a%20Point%20Pro%203.";
const stripeSubscriptionsEnabled =
  process.env.NEXT_PUBLIC_AGENDA_STRIPE_READY === "true" ||
  process.env.NODE_ENV !== "production";
const monthlySubscriptionHref = stripeSubscriptionsEnabled
  ? "/api/agenda/subscriptions/checkout?plan=mensal"
  : monthlySubscriptionFallbackHref;
const annualSubscriptionHref = stripeSubscriptionsEnabled
  ? "/api/agenda/subscriptions/checkout?plan=anual"
  : annualSubscriptionFallbackHref;
const instagramHref = "https://www.instagram.com/minhaagendalivre/";

const journeySteps = [
  { icon: CalendarBlankIcon, title: "Escolhe o serviço", text: "Vê detalhes e valor" },
  { icon: ClockIcon, title: "Seleciona o horário", text: "Só aparecem vagas livres" },
  { icon: UserCircleIcon, title: "Informa os dados", text: "Sem cadastro e sem app" },
  { icon: CheckCircleIcon, title: "Pronto!", text: "O pedido entra na agenda" },
];

const featureItems = [
  { icon: CalendarCheckIcon, title: "Agenda atualizada", text: "Horários e confirmações em um lugar." },
  { icon: UsersThreeIcon, title: "Clientes e equipe", text: "Histórico, profissionais e serviços." },
  { icon: WhatsappLogoIcon, title: "WhatsApp no fluxo", text: "Confirmações e lembretes conectados." },
  { icon: CoinsIcon, title: "Financeiro organizado", text: "Entradas, caixa e visão do período." },
  { icon: ChartLineUpIcon, title: "Relatórios objetivos", text: "Resultados claros para decidir melhor." },
];

const segments = [
  { icon: HairDryerIcon, accent: SparkleIcon, label: "Salões e beleza" },
  { icon: ScissorsIcon, accent: SparkleIcon, label: "Barbearias" },
  { icon: FirstAidIcon, accent: LeafIcon, label: "Clínicas e consultórios" },
  { icon: UserCircleIcon, accent: SparkleIcon, label: "Estética e bem-estar" },
  { icon: PawPrintIcon, accent: SparkleIcon, label: "Pet shops" },
  { icon: UserIcon, accent: StorefrontIcon, label: "Autônomos" },
];

const faqs = [
  {
    question: "Preciso de cartão para testar?",
    answer:
      "Sim. O cartão é salvo com segurança pela Stripe e a cobrança só começa depois dos 7 dias grátis.",
  },
  {
    question: "O cliente precisa instalar aplicativo?",
    answer:
      "Não. Ele agenda no navegador pelo seu endereço minhaloja.minhaagendalivre.com.br.",
  },
  {
    question: "A mesma conta funciona em todas as telas?",
    answer: "Sim. Seus dados acompanham você na Web, no Windows e no Android.",
  },
];

function ChapterMarker({ number, label, dark = false }) {
  return (
    <span className={`${styles.chapterMarker} ${dark ? styles.chapterMarkerDark : ""}`} aria-hidden="true">
      <strong>{number}</strong>
      <small>{label}</small>
      <i />
    </span>
  );
}

function Brand({ inverse = false }) {
  return (
    <span className={`${styles.brandLockup} ${inverse ? styles.brandInverse : ""}`}>
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
    </span>
  );
}

function PrimaryLink({ children, href = webTrialHref, location, external = true, light = false }) {
  return (
    <a
      className={`${styles.primaryButton} ${light ? styles.primaryButtonLight : ""}`}
      href={href}
      target={external ? "_blank" : undefined}
      rel={external ? "noreferrer" : undefined}
      data-analytics-action="agenda_trial_start"
      data-analytics-location={location}
    >
      <span>{children}</span>
      <ArrowRightIcon size={18} weight="bold" aria-hidden="true" />
    </a>
  );
}

function SecondaryLink({ children, href = webTrialHref, dark = false, download = false }) {
  return (
    <a
      className={`${styles.secondaryButton} ${dark ? styles.secondaryButtonDark : ""}`}
      href={href}
      target={download ? undefined : "_blank"}
      rel={download ? undefined : "noreferrer"}
      download={download || undefined}
    >
      {children}
    </a>
  );
}

export default function AgendaLivrePage() {
  return (
    <main className={styles.page} data-agenda-landing>
      <AgendaLivreMotion />

      <header className={styles.header}>
        <div className={styles.headerInner}>
          <a href="#inicio" aria-label="Agenda Livre — início">
            <Brand />
          </a>

          <nav className={styles.nav} aria-label="Navegação principal">
            <a href="#produto">Produto</a>
            <a href="#recursos">Recursos</a>
            <a href="#planos">Preços</a>
            <a href="#segmentos">Para quem</a>
            <a href="#duvidas">Dúvidas</a>
          </nav>

          <div className={styles.headerActions}>
            <a className={styles.headerPrimary} href={webTrialHref} target="_blank" rel="noreferrer">
              Testar grátis por 7 dias
              <ArrowRightIcon size={15} weight="bold" aria-hidden="true" />
            </a>
            <a className={styles.headerSecondary} href={webTrialHref} target="_blank" rel="noreferrer">
              <GlobeIcon size={17} weight="duotone" aria-hidden="true" />
              Abrir versão Web
            </a>
          </div>
        </div>
      </header>

      <section className={`${styles.section} ${styles.hero}`} id="inicio">
        <div className={styles.sectionShell}>
          <ChapterMarker number="01" label="Início" />
          <div className={styles.heroGrid}>
            <div className={styles.heroCopy} data-reveal>
              <span className={styles.eyebrow}>
                <SparkleIcon size={16} weight="fill" aria-hidden="true" /> Web, Windows e Android
              </span>
              <h1>
                <span>Sua agenda.</span>
                <span>Seu tempo.</span>
                <em><span>Seu negócio.</span></em>
              </h1>
              <p>
                Centralize agendamentos, clientes, equipe, serviços e finanças em um só lugar —
                no computador ou no celular.
              </p>
              <div className={styles.buttonRow}>
                <PrimaryLink location="hero">Testar grátis por 7 dias</PrimaryLink>
                <SecondaryLink>
                  <GlobeIcon size={18} weight="duotone" aria-hidden="true" /> Abrir versão Web
                </SecondaryLink>
              </div>
            </div>

            <div className={styles.heroVisual} data-reveal>
              <figure className={styles.windowFrame}>
                <figcaption>
                  <span><LaptopIcon size={16} weight="duotone" /> Agenda Livre para Windows</span>
                  <small>Tela real do produto</small>
                </figcaption>
                <Image
                  src="/agenda-livre/windows-home-client-studio-fluxo.png"
                  unoptimized
                  width={1200}
                  height={608}
                  alt="Painel real do Agenda Livre para Windows da empresa Studio Fluxo"
                  className={styles.windowScreenshot}
                  priority
                  sizes="(max-width: 800px) 92vw, 800px"
                />
              </figure>
              <figure className={styles.heroPhone}>
                <Image
                  src="/agenda-livre/mobile-home-studio-fluxo.png"
                  unoptimized
                  width={390}
                  height={844}
                  alt="Painel real do Agenda Livre Web no celular da empresa Studio Fluxo"
                  className={styles.phoneScreenshot}
                  priority
                  sizes="(max-width: 600px) 34vw, 190px"
                />
              </figure>
            </div>
          </div>

          <div className={styles.trustRow} data-reveal>
            <span><CheckIcon size={15} weight="bold" /> Sem cartão de crédito</span>
            <span><CheckIcon size={15} weight="bold" /> 7 dias para testar</span>
            <span><CheckIcon size={15} weight="bold" /> A mesma conta em todas as telas</span>
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.bookingSection}`} id="produto">
        <span id="agendamento-online" className={styles.anchorTarget} />
        <div className={styles.sectionShell}>
          <ChapterMarker number="02" label="Site" />
          <div className={styles.bookingGrid}>
            <figure className={styles.bookingVisual} data-reveal>
              <Image
                src="/agenda-livre/site-editor-current.png"
                unoptimized
                width={1366}
                height={720}
                alt="Aba Editar site do Agenda Livre com prévia do catálogo, seções, domínio e publicação"
                className={styles.bookingScreenshot}
                sizes="(max-width: 800px) 92vw, 720px"
              />
            </figure>

            <div className={styles.bookingCopy} data-reveal>
              <span className={styles.eyebrow}>Aba Editar site</span>
              <h2>Você monta o site.<br />O cliente agenda.</h2>
              <p>
                Na aba Editar site você escolhe capa, textos, seções, serviços e equipe.
                Depois publica um catálogo com a identidade do seu negócio e agendamento integrado.
              </p>
              <div className={styles.storeAddress}>
                <GlobeIcon size={20} weight="duotone" aria-hidden="true" />
                <span><small>Seu catálogo, seu endereço</small><strong>sualoja.minhaagendalivre.com.br</strong></span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.journeySection}`} id="recursos">
        <div className={styles.sectionShell}>
          <ChapterMarker number="03" label="Jornada" />
          <div className={styles.journeyIntro} data-reveal>
            <span className={styles.eyebrow}>Do começo ao fim</span>
            <h2>Do primeiro clique<br />ao atendimento.</h2>
            <p>A jornada do seu cliente é simples como deve ser.</p>
          </div>
          <div className={styles.journeySteps} data-reveal>
            {journeySteps.map(({ icon: Icon, title, text }, index) => (
              <article key={title}>
                <Icon size={46} weight="duotone" aria-hidden="true" />
                <strong>{title}</strong>
                <small>{text}</small>
                {index < journeySteps.length - 1 ? (
                  <ArrowRightIcon className={styles.journeyArrow} size={22} weight="bold" aria-hidden="true" />
                ) : null}
              </article>
            ))}
          </div>

          <div className={styles.featureRibbon} data-reveal>
            {featureItems.map(({ icon: Icon, title, text }) => (
              <article key={title}>
                <span><Icon size={27} weight="duotone" aria-hidden="true" /></span>
                <div><strong>{title}</strong><small>{text}</small></div>
              </article>
            ))}
          </div>

          <div className={styles.segmentRibbon} id="segmentos" data-reveal>
            <span className={styles.segmentTitle}>Feito para quem trabalha com hora marcada</span>
            <div>
              {segments.map(({ icon: Icon, accent: Accent, label }) => (
                <span className={styles.segmentItem} key={label}>
                  <i aria-hidden="true"><Icon size={27} weight="duotone" /><Accent size={12} weight="fill" /></i>
                  <strong>{label}</strong>
                </span>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.designSection}`} id="design">
        <div className={styles.sectionShell}>
          <ChapterMarker number="04" label="Design" />
          <div className={styles.designGrid}>
            <div className={styles.designCopy} data-reveal>
              <span className={styles.eyebrow}><PaletteIcon size={16} weight="duotone" /> Sua identidade</span>
              <h2>Escolha<br />seu design.</h2>
              <p>
                Escolha o tema que combina com seu negócio. O visual acompanha a mesma conta no
                Windows, na Web e no celular.
              </p>
            </div>

            <figure className={styles.designVisual} data-reveal>
              <figcaption><LaptopIcon size={17} weight="duotone" /><strong>A mesma escolha em todas as telas</strong></figcaption>
              <div className={styles.designStage}>
                <Image
                  src="/agenda-livre/design/theme-windows-current.png"
                  unoptimized
                  width={1200}
                  height={640}
                  alt="Tela real de escolha de tema no Agenda Livre para Windows"
                  className={styles.designScreenshot}
                  sizes="(max-width: 900px) 92vw, 800px"
                />
                <div className={`${styles.designPlatformPreview} ${styles.designWebPreview}`}>
                  <span><BrowsersIcon size={14} weight="duotone" /> Web</span>
                  <Image
                    src="/agenda-livre/design/theme-web-current.png"
                    unoptimized
                    width={1200}
                    height={640}
                    alt="Tela real de escolha de tema no Agenda Livre Web"
                  />
                </div>
                <div className={`${styles.designPlatformPreview} ${styles.designMobilePreview}`}>
                  <span><DeviceMobileIcon size={14} weight="duotone" /> Celular</span>
                  <Image
                    src="/agenda-livre/design/theme-mobile-current.png"
                    unoptimized
                    width={390}
                    height={844}
                    alt="Tela real de escolha de tema no Agenda Livre Web no celular"
                  />
                </div>
              </div>
            </figure>

            <div className={styles.designNotes} data-reveal>
              <span><i /><strong>Temas para<br />todos os estilos</strong></span>
              <span><i /><strong>Cores, botões<br />e destaques personalizáveis</strong></span>
            </div>
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.platformSection}`} id="plataformas">
        <div className={styles.sectionShell}>
          <ChapterMarker number="05" label="Plataformas" />
          <div className={styles.platformIntro} data-reveal>
            <span className={styles.eyebrow}>Uma conta</span>
            <h2>Windows, Web<br />e celular.</h2>
            <p>A mesma empresa e a mesma agenda, adaptadas para cada tela.</p>
            <div className={styles.platformLinks}>
              <a href={webTrialHref} target="_blank" rel="noreferrer"><GlobeIcon size={16} /> Abrir na Web</a>
              <a href={windowsDownloadHref} download><DownloadSimpleIcon size={16} /> Baixar Windows</a>
            </div>
          </div>

          <div className={styles.platformGallery} data-reveal>
            <figure>
              <figcaption><DesktopIcon size={19} weight="duotone" /><strong>Windows</strong><small>Aplicativo real para o computador.</small></figcaption>
              <div className={styles.platformStage}>
                <Image
                  src="/agenda-livre/windows-home-studio-fluxo.png"
                  unoptimized
                  priority
                  width={1200}
                  height={640}
                  alt="Painel real do Agenda Livre para Windows da empresa Studio Fluxo"
                  className={styles.platformWideImage}
                  sizes="(max-width: 700px) 82vw, 330px"
                />
              </div>
            </figure>
            <figure>
              <figcaption><BrowsersIcon size={19} weight="duotone" /><strong>Web</strong><small>O mesmo painel no navegador.</small></figcaption>
              <div className={styles.platformStage}>
                <Image
                  src="/agenda-livre/web-home-studio-fluxo.png"
                  unoptimized
                  priority
                  width={1200}
                  height={640}
                  alt="Painel real do Agenda Livre Web da empresa Studio Fluxo"
                  className={styles.platformWideImage}
                  sizes="(max-width: 700px) 82vw, 330px"
                />
              </div>
            </figure>
            <figure>
              <figcaption><CalendarCheckIcon size={19} weight="duotone" /><strong>Agenda Web</strong><small>Quadro diário com o mesmo visual.</small></figcaption>
              <div className={styles.platformStage}>
                <Image
                  src="/agenda-livre/web-agenda-studio-fluxo.png"
                  unoptimized
                  width={1200}
                  height={640}
                  alt="Agenda real do Agenda Livre Web da empresa Studio Fluxo"
                  className={styles.platformWideImage}
                  sizes="(max-width: 700px) 82vw, 430px"
                />
              </div>
            </figure>
            <figure>
              <figcaption><CoinsIcon size={19} weight="duotone" /><strong>Financeiro Web</strong><small>Entradas e saídas na mesma conta.</small></figcaption>
              <div className={styles.platformStage}>
                <Image
                  src="/agenda-livre/web-finance-studio-fluxo.png"
                  unoptimized
                  width={1200}
                  height={640}
                  alt="Financeiro real do Agenda Livre Web da empresa Studio Fluxo"
                  className={styles.platformWideImage}
                  sizes="(max-width: 700px) 82vw, 430px"
                />
              </div>
            </figure>
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.androidSection}`} id="android">
        <div className={styles.sectionShell}>
          <ChapterMarker number="06" label="Celular" dark />
          <div className={styles.androidIntro} data-reveal>
            <span className={styles.eyebrow}>Web no celular</span>
            <h2>Leve sua<br />agenda no<br />bolso.</h2>
            <p>Abra no navegador e gerencie a mesma conta de onde estiver. Estes são prints reais da versão Web responsiva.</p>
            <PrimaryLink href={webTrialHref} location="mobile-web" light>
              Abrir no celular
            </PrimaryLink>
          </div>

          <div className={styles.phoneGallery} data-reveal>
            {[
              ["/agenda-livre/mobile-home-studio-fluxo.png", "Painel real do Agenda Livre Web no celular da empresa Studio Fluxo", "Painel"],
              ["/agenda-livre/mobile-agenda-studio-fluxo.png", "Agenda real do Agenda Livre Web no celular da empresa Studio Fluxo", "Agenda"],
              ["/agenda-livre/mobile-finance-studio-fluxo.png", "Financeiro real do Agenda Livre Web no celular da empresa Studio Fluxo", "Financeiro"],
            ].map(([src, alt, label]) => (
              <figure key={src}>
                <span>{label}</span>
                <Image
                  src={src}
                  unoptimized
                  width={390}
                  height={844}
                  alt={alt}
                  className={styles.phoneScreenshot}
                  sizes="(max-width: 600px) 39vw, 205px"
                />
              </figure>
            ))}
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.pricingSection}`} id="planos">
        <div className={styles.sectionShell}>
          <ChapterMarker number="07" label="Assinatura" dark />
          <div className={styles.pricingIntro} data-reveal>
            <span className={styles.eyebrow}>Assinatura</span>
            <h2>Escolha como<br />quer continuar.</h2>
            <p>Use por mês ou garanta o ano inteiro com um benefício a mais para o seu negócio.</p>
          </div>

          <div className={styles.pricingOptions}>
            <article data-reveal>
              <span className={styles.planName}>Mensal</span>
              <div className={styles.planPrice}><strong>R$ 49,90</strong><small>/ mês</small></div>
              <p><CheckIcon size={15} weight="bold" /> Teste grátis por 7 dias</p>
              <a
                href={monthlySubscriptionHref}
                aria-label={
                  stripeSubscriptionsEnabled
                    ? "Assinar plano mensal com Stripe"
                    : "Solicitar assinatura mensal"
                }
              >
                {stripeSubscriptionsEnabled ? "Assinar com Stripe" : "Assinar mensal"}{" "}
                <ArrowRightIcon size={17} weight="bold" />
              </a>
            </article>
            <article className={styles.annualPlan} data-reveal>
              <span className={styles.planBadge}>Mais vantajoso</span>
              <span className={styles.planName}>Anual</span>
              <div className={styles.planPrice}><strong>R$ 598,80</strong><small>/ ano</small></div>
              <p><CheckIcon size={15} weight="bold" /> Equivale a R$ 49,90 por mês</p>
              <a
                href={annualSubscriptionHref}
                aria-label={
                  stripeSubscriptionsEnabled
                    ? "Assinar plano anual com Stripe"
                    : "Solicitar assinatura anual"
                }
              >
                {stripeSubscriptionsEnabled ? "Assinar com Stripe" : "Assinar anual"}{" "}
                <ArrowRightIcon size={17} weight="bold" />
              </a>
            </article>
          </div>

          <div className={styles.machineBonus} data-reveal>
            <div className={styles.machineCircle}>
              <Image
                src="/agenda-livre/point-pro-3-clean-v2.png"
                unoptimized
                width={1200}
                height={800}
                alt="Point Pro 3 amarela com tela touch e teclado numérico físico"
                className={styles.machineImage}
                sizes="(max-width: 700px) 76vw, 300px"
              />
            </div>
            <div><GiftIcon size={24} weight="duotone" /><small>Bônus do anual</small><strong>Ganhe uma<br />Point Pro 3</strong></div>
            <p>Enquanto houver estoque. Uso e taxas seguem as regras do Mercado Pago.</p>
          </div>
        </div>
      </section>

      <section className={`${styles.section} ${styles.closingSection}`} id="duvidas">
        <div className={styles.sectionShell}>
          <ChapterMarker number="08" label="Comece" dark />
          <div className={styles.closingHeadline} data-reveal>
            <h2>Organize <em>hoje.</em><br />Respire <em>amanhã.</em></h2>
          </div>
          <div className={styles.closingAction} data-reveal>
            <p>É simples, rápido e sem complicação.<br />Comece agora e transforme a rotina do seu negócio.</p>
            <div className={styles.buttonRow}>
              <PrimaryLink location="final">Testar grátis por 7 dias</PrimaryLink>
              <SecondaryLink dark>
                <GlobeIcon size={18} weight="duotone" /> Abrir versão Web
              </SecondaryLink>
            </div>
          </div>
          <div className={styles.faqGrid} data-reveal>
            {faqs.map((faq) => (
              <details key={faq.question}>
                <summary>{faq.question}<span>+</span></summary>
                <p>{faq.answer}</p>
              </details>
            ))}
          </div>
        </div>
      </section>

      <footer className={styles.footer}>
        <div className={styles.footerInner} data-reveal>
          <a className={styles.footerBrand} href="#inicio" aria-label="Agenda Livre — voltar ao início">
            <Brand inverse />
          </a>

          <div className={styles.footerColumn}>
            <strong>Produto</strong>
            <a href="#produto">Agendamento online</a>
            <a href="#recursos">Funcionalidades</a>
            <a href="#design">Design</a>
            <a href="#plataformas">Plataformas</a>
          </div>
          <div className={styles.footerColumn}>
            <strong>Para quem</strong>
            <a href="#segmentos">Salão e beleza</a>
            <a href="#segmentos">Barbearia</a>
            <a href="#segmentos">Clínicas e estética</a>
            <a href="#segmentos">Pet e autônomos</a>
          </div>
          <div className={styles.footerColumn}>
            <strong>Comece</strong>
            <a href={webTrialHref} target="_blank" rel="noreferrer">Teste grátis</a>
            <a href={windowsDownloadHref} download>Baixar Windows</a>
            <a href="/agenda-livre/android">Aplicativo Android</a>
            <a href="/agenda-livre/privacidade">Política de privacidade</a>
          </div>
          <div className={`${styles.footerColumn} ${styles.footerContact}`}>
            <strong>Fale com a gente</strong>
            <a href="tel:+5533991314125"><PhoneIcon size={19} weight="duotone" />(33) 99131-4125</a>
            <a href={instagramHref} target="_blank" rel="noreferrer"><InstagramLogoIcon size={19} weight="duotone" />@minhaagendalivre</a>
            <a href={whatsappHref} target="_blank" rel="noreferrer"><WhatsappLogoIcon size={19} weight="duotone" />WhatsApp</a>
            <a href="mailto:contato@minhaagendalivre.com.br"><AtIcon size={19} weight="duotone" />E-mail</a>
          </div>
        </div>
        <div className={styles.footerBottom} data-reveal>
          <span>© 2026 Agenda Livre. Todos os direitos reservados.</span>
          <span>Web · Windows · Android</span>
        </div>
      </footer>
    </main>
  );
}
