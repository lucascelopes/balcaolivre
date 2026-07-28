import Image from "next/image";
import { headers } from "next/headers";
import {
  ArrowRightIcon,
  ArrowUpRightIcon,
  CalendarCheckIcon,
  CheckCircleIcon,
  DesktopTowerIcon,
  DownloadSimpleIcon,
  GiftIcon,
  HandshakeIcon,
  HeadsetIcon,
  InstagramLogoIcon,
  SignInIcon,
  TagIcon,
  TiktokLogoIcon,
  UserIcon,
  WhatsappLogoIcon,
  YoutubeLogoIcon,
} from "@phosphor-icons/react/ssr";
import { onlineDownloadUrl } from "../siteLinks";
import styles from "./links.module.css";

export const dynamic = "force-dynamic";

const agendaWhatsappNumber = "5533991314125";
const agendaAnnualCheckoutUrl =
  "https://minhaagendalivre.com.br/api/agenda/subscriptions/checkout?plan=anual";
const balcaoAnnualCheckoutUrl =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout?plan=completo-anual";

function whatsappHref(number, message) {
  return `https://wa.me/${number}?text=${encodeURIComponent(message)}`;
}

function getBrand(host = "") {
  const agenda = host.toLowerCase().includes("minhaagendalivre.com.br");

  if (agenda) {
    const description =
      "Conheça os planos do Agenda Livre, fale com o suporte, acesse o sistema ou torne-se um parceiro revendedor.";

    return {
      key: "agenda",
      canonical: "https://minhaagendalivre.com.br/links",
      description,
      metadataTitle: "Agenda Livre | Planos, suporte e parcerias",
      metadataSocialTitle: "Agenda Livre — sua agenda, seus links",
      socialImage: "https://minhaagendalivre.com.br/agenda-livre/links-og.png",
      socialImageAlt: "Agenda Livre — planos, suporte e parcerias",
      logo: "/agenda-livre/agenda-livre-mark.png",
      logoAlt: "Agenda Livre",
      logoLabel: "Ir para o site do Agenda Livre",
      kicker: "Agenda inteligente para o seu negócio",
      title: <>Sua agenda organizada.<br />Seu tempo de volta.</>,
      intro:
        "Agendamentos, clientes, equipe e financeiro em um só lugar — no computador e no celular.",
      linksLabel: "Links do Agenda Livre",
      featured: {
        badge: "Melhor escolha",
        eyebrow: "Plano anual + maquininha",
        title: "12 meses de Agenda Livre",
        price: "R$ 598,80",
        suffix: "/ ano",
        equivalent: "Equivale a R$ 49,90 por mês",
        benefits: ["Point Pro 3 inclusa", "Configuração acompanhada"],
        image: "/agenda-livre/point-pro-3-clean-v2.png",
        href: agendaAnnualCheckoutUrl,
        button: "Quero o anual + maquininha",
        label: "Assinar 12 meses de Agenda Livre no checkout seguro",
      },
      primary: {
        href: "https://minhaagendalivre.com.br/#planos",
        title: "Conheça o plano de R$ 49,90",
        subtitle: "Agenda completa, mês a mês",
        label: "Ver os planos do Agenda Livre",
        icon: <CalendarCheckIcon size={25} weight="duotone" />,
        sameTab: true,
      },
      site: {
        title: "Veja tudo que você recebe",
        subtitle: "Conheça o Agenda Livre por completo",
        label: "Conhecer todos os recursos do Agenda Livre",
      },
      smallLinks: [
        {
          href: "https://app.minhaagendalivre.com.br/",
          label: "Acessar o aplicativo Agenda Livre",
          title: "Acessar",
          subtitle: "Minha agenda",
          icon: <SignInIcon size={23} weight="duotone" />,
        },
        {
          href: "https://minhaagendalivre.com.br/#duvidas",
          label: "Ir para a área de suporte do Agenda Livre",
          title: "Suporte",
          subtitle: "Podemos ajudar",
          icon: <HeadsetIcon size={23} weight="duotone" />,
          sameTab: true,
        },
        {
          href: whatsappHref(
            agendaWhatsappNumber,
            "Olá! Quero saber como funciona a parceria para revender o Agenda Livre.",
          ),
          label: "Quero revender o Agenda Livre",
          title: "Revendas",
          subtitle: "Seja parceiro",
          icon: <HandshakeIcon size={23} weight="duotone" />,
          whatsapp: true,
          location: "links_reseller",
        },
        {
          href: "https://www.instagram.com/minhaagendalivre/",
          label: "Seguir o Agenda Livre no Instagram",
          title: "Instagram",
          subtitle: "@minhaagendalivre",
          icon: <InstagramLogoIcon size={23} weight="duotone" />,
        },
      ],
      footer: ["Agenda Livre", "Feito para quem cuida de pessoas"],
    };
  }

  const description =
    "Teste o Balcão Livre PDV, acesse o sistema, baixe o aplicativo para Windows ou fale com o suporte.";

  return {
    key: "balcao",
    canonical: "https://balcaolivrepdv.com.br/links",
    description,
    metadataTitle: "Balcão Livre PDV | Teste, acesso e suporte",
    metadataSocialTitle: "Balcão Livre PDV — menos improviso, mais restaurante rodando",
    socialImage: "https://balcaolivrepdv.com.br/brand/links-og.png",
    socialImageAlt: "Balcão Livre PDV — teste, acesso e suporte",
    logo: "/brand/bl-modern-icon.png",
    logoAlt: "Balcão Livre PDV",
    logoLabel: "Ir para o site do Balcão Livre PDV",
    kicker: "PDV criado para restaurantes",
    title: <>Menos improviso.<br />Mais restaurante rodando.</>,
    intro:
      "Pedidos, comandas, caixa, estoque e delivery em uma operação só — no Windows e na web.",
    linksLabel: "Links do Balcão Livre PDV",
    featured: {
      badge: "Plano anual completo",
      eyebrow: "PDV completo + maquininha",
      title: "Seu restaurante pronto. Point Pro 3 no caixa.",
      price: "Oferta anual",
      suffix: "",
      equivalent: "Condição especial no plano Completo",
      benefits: ["Point Pro 3 inclusa", "Implantação acompanhada"],
      image: "/agenda-livre/point-pro-3-clean-v2.png",
      href: balcaoAnnualCheckoutUrl,
      button: "Quero o anual + Point Pro 3",
      label: "Assinar o plano anual Completo no checkout seguro",
    },
    primary: {
      href: "https://app.balcaolivrepdv.com.br/",
      title: "Fazer um teste grátis de 7 dias",
      subtitle: "Crie sua conta e comece agora",
      label: "Criar uma conta e testar o Balcão Livre PDV por 7 dias",
      icon: <CalendarCheckIcon size={25} weight="duotone" />,
    },
    site: {
      title: "Conheça todos os recursos",
      subtitle: "Caixa, salão, cozinha e delivery",
      label: "Conhecer todos os recursos do Balcão Livre PDV",
    },
    smallLinks: [
      {
        href: "https://app.balcaolivrepdv.com.br/",
        label: "Entrar ou criar uma conta no Balcão Livre PDV Web",
        title: "Entrar",
        subtitle: "Usar na web",
        icon: <SignInIcon size={23} weight="duotone" />,
      },
      {
        href: onlineDownloadUrl,
        label: "Baixar o Balcão Livre PDV para Windows",
        title: "Windows",
        subtitle: "Baixar aplicativo",
        icon: <DownloadSimpleIcon size={23} weight="duotone" />,
      },
      {
        href: "https://balcaolivrepdv.com.br/#suporte",
        label: "Ir para a área de suporte do Balcão Livre PDV",
        title: "Suporte",
        subtitle: "Podemos ajudar",
        icon: <HeadsetIcon size={23} weight="duotone" />,
        sameTab: true,
      },
      {
        href: "https://balcaolivrepdv.com.br/#planos",
        label: "Ver os planos do Balcão Livre PDV",
        title: "Planos",
        subtitle: "Comparar opções",
        icon: <DesktopTowerIcon size={23} weight="duotone" />,
        sameTab: true,
      },
    ],
    footer: ["Balcão Livre PDV", "Feito para restaurante rodar sem improviso"],
  };
}

async function getRequestBrand() {
  const requestHeaders = await headers();
  return getBrand(requestHeaders.get("host") || "");
}

export async function generateMetadata({ searchParams }) {
  const params = await searchParams;
  const brand =
    params?.brand === "agenda"
      ? getBrand("minhaagendalivre.com.br")
      : await getRequestBrand();

  return {
    title: brand.metadataTitle,
    description: brand.description,
    alternates: {
      canonical: brand.canonical,
    },
    openGraph: {
      type: "website",
      locale: "pt_BR",
      url: brand.canonical,
      title: brand.metadataSocialTitle,
      description: brand.description,
      images: [
        {
          url: brand.socialImage,
          width: 1536,
          height: 1024,
          alt: brand.socialImageAlt,
        },
      ],
    },
    twitter: {
      card: "summary_large_image",
      title: brand.metadataSocialTitle,
      description: brand.description,
      images: [brand.socialImage],
    },
  };
}

function WhatsAppLink({ href, className, location, children, label }) {
  return (
    <a
      className={className}
      href={href}
      target="_blank"
      rel="noreferrer"
      aria-label={label}
      data-analytics-action="whatsapp_click"
      data-analytics-location={location}
    >
      {children}
    </a>
  );
}

function ExternalLink({ link, className, children }) {
  if (link.whatsapp) {
    return (
      <WhatsAppLink
        href={link.href}
        className={className}
        location={link.location}
        label={link.label}
      >
        {children}
      </WhatsAppLink>
    );
  }

  return (
    <a
      className={className}
      href={link.href}
      target={link.sameTab ? undefined : "_blank"}
      rel={link.sameTab ? undefined : "noreferrer"}
      aria-label={link.label}
    >
      {children}
    </a>
  );
}

function BalcaoLinksPage({ brand }) {
  const actions = [
    {
      ...brand.smallLinks[0],
      eyebrow: "Acesso web",
      description: "Entre na sua operação ou crie uma conta.",
      icon: <SignInIcon size={34} weight="regular" />,
      dark: true,
    },
    {
      ...brand.smallLinks[1],
      eyebrow: "Aplicativo",
      description: "Baixe o PDV para usar no computador.",
      icon: <DownloadSimpleIcon size={34} weight="regular" />,
      dark: false,
    },
    {
      ...brand.smallLinks[2],
      eyebrow: "Atendimento",
      description: "Tire dúvidas com a nossa equipe.",
      icon: <HeadsetIcon size={34} weight="regular" />,
      dark: false,
    },
    {
      ...brand.smallLinks[3],
      eyebrow: "Para o seu negócio",
      description: "Encontre a operação ideal para o restaurante.",
      icon: <DesktopTowerIcon size={34} weight="regular" />,
      dark: true,
    },
  ];

  const socialLinks = [
    {
      title: "Instagram",
      href: "https://www.instagram.com/balcaolivrepdv/",
      icon: <InstagramLogoIcon size={25} weight="regular" />,
    },
    {
      title: "TikTok",
      href: "https://www.tiktok.com/@balcaolivrepdv",
      icon: <TiktokLogoIcon size={25} weight="regular" />,
    },
    {
      title: "YouTube",
      href: "https://www.youtube.com/@balcaolivrepdv",
      icon: <YoutubeLogoIcon size={25} weight="regular" />,
    },
  ];

  return (
    <main className={styles.balcaoPage}>
      <link rel="preconnect" href="https://fonts.googleapis.com" />
      <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
      <link
        href="https://fonts.googleapis.com/css2?family=Barlow+Condensed:wght@600;700;800;900&family=Inter:wght@400;500;600;700;800&display=swap"
        rel="stylesheet"
      />

      <div className={styles.balcaoShell}>
        <header className={styles.balcaoMasthead}>
          <a className={styles.balcaoBrand} href="/" aria-label={brand.logoLabel}>
            BL<span>.</span>
          </a>
          <span className={styles.balcaoDivider} aria-hidden="true" />
          <p>PDV para restaurantes</p>
        </header>

        <section className={styles.balcaoHero} aria-labelledby="balcao-links-title">
          <div className={styles.balcaoHeroCopy}>
            <p className={styles.balcaoHeroKicker}>Operação sem improviso</p>
            <h1 id="balcao-links-title">
              Tudo do seu restaurante.<br />
              Em uma operação.
            </h1>
            <p>
              Pedidos, comandas, caixa, estoque e delivery em uma operação só —
              no Windows e na web.
            </p>
          </div>
          <div className={styles.balcaoHeroVisual} aria-hidden="true">
            <div className={styles.balcaoScreenFrame}>
              <Image
                src="/brand/pdv-orange-dashboard.png"
                width={1920}
                height={1080}
                sizes="(max-width: 700px) 58vw, 440px"
                priority
                alt=""
              />
            </div>
          </div>
        </section>

        <a
          className={styles.balcaoTrial}
          href={brand.primary.href}
          target="_blank"
          rel="noreferrer"
          aria-label={brand.primary.label}
        >
          <span className={styles.balcaoTrialMark} aria-hidden="true">BL.</span>
          <span>
            <small>Comece agora</small>
            <strong>Testar grátis por 7 dias</strong>
          </span>
          <ArrowRightIcon size={27} weight="bold" aria-hidden="true" />
        </a>

        <section className={styles.balcaoActionGrid} aria-label={brand.linksLabel}>
          {actions.map((action) => (
            <ExternalLink
              key={action.title}
              link={action}
              className={`${styles.balcaoAction} ${
                action.dark ? styles.balcaoActionDark : styles.balcaoActionLight
              }`}
            >
              <span className={styles.balcaoActionTop}>
                {action.icon}
                <ArrowRightIcon size={19} weight="bold" aria-hidden="true" />
              </span>
              <span>
                <small>{action.eyebrow}</small>
                <strong>{action.title}</strong>
                <em>{action.description}</em>
              </span>
            </ExternalLink>
          ))}
        </section>

        <section className={styles.balcaoSocial} aria-labelledby="balcao-social-title">
          <h2 id="balcao-social-title">Acompanhe o Balcão Livre PDV</h2>
          <div>
            {socialLinks.map((social) => (
              <a
                key={social.title}
                href={social.href}
                target="_blank"
                rel="noreferrer"
                aria-label={`Acompanhar o Balcão Livre PDV no ${social.title}`}
              >
                {social.icon}
                <span>
                  <strong>{social.title}</strong>
                  <small>@balcaolivrepdv</small>
                </span>
              </a>
            ))}
          </div>
        </section>

        <section className={styles.balcaoPoint} aria-labelledby="point-title">
          <div className={styles.balcaoPointCopy}>
            <span>PDV completo + maquininha</span>
            <h2 id="point-title">
              Completo no sistema.<br />
              Point Pro 3 no caixa.
            </h2>
            <p><strong>Oferta anual</strong><br />Condição especial no plano Completo</p>
            <ExternalLink
              link={brand.featured}
              className={styles.balcaoPointButton}
            >
              Ir para o pagamento
              <ArrowRightIcon size={18} weight="bold" aria-hidden="true" />
            </ExternalLink>
          </div>
          <div className={styles.balcaoPointMachine} aria-hidden="true">
            <Image
              src="/brand/point-pro-3-cutout-v1.png"
              width={1536}
              height={1024}
              sizes="(max-width: 700px) 66vw, 430px"
              alt=""
            />
          </div>
        </section>
      </div>
    </main>
  );
}

function AgendaLinksPage({ brand }) {
  const actions = [
    {
      ...brand.smallLinks[0],
      eyebrow: "Sua conta",
      title: "Entrar",
      description: "Acesse sua agenda ou crie sua conta.",
      icon: <UserIcon size={30} weight="regular" />,
      dark: false,
    },
    {
      ...brand.smallLinks[1],
      eyebrow: "Atendimento",
      title: "Suporte",
      description: "Tire dúvidas com a nossa equipe.",
      icon: <HeadsetIcon size={30} weight="regular" />,
      dark: true,
    },
    {
      ...brand.primary,
      eyebrow: "Escolha ideal",
      title: "Planos",
      description: "Compare os planos e escolha o seu.",
      icon: <TagIcon size={30} weight="regular" />,
      dark: true,
    },
    {
      ...brand.featured,
      eyebrow: "Plano anual",
      title: "12 meses de Agenda Livre",
      description: "Agenda completa com Point Pro 3 inclusa.",
      icon: <GiftIcon size={30} weight="regular" />,
      dark: false,
    },
  ];

  const socialLinks = [
    {
      title: "Instagram",
      handle: "@minhaagendalivre",
      href: "https://www.instagram.com/minhaagendalivre/",
      icon: <InstagramLogoIcon size={25} weight="regular" />,
    },
    {
      title: "TikTok",
      handle: "@minhaagendalivre",
      href: "https://www.tiktok.com/@minhaagendalivre",
      icon: <TiktokLogoIcon size={25} weight="regular" />,
    },
    {
      title: "YouTube",
      handle: "@minhaagendalivre",
      href: "https://www.youtube.com/@minhaagendalivre",
      icon: <YoutubeLogoIcon size={25} weight="regular" />,
    },
  ];

  return (
    <main className={styles.agendaLinksPage}>
      <link rel="preconnect" href="https://fonts.googleapis.com" />
      <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
      <link
        href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Inter:wght@400;500;600;700;800&display=swap"
        rel="stylesheet"
      />

      <div className={styles.agendaLinksShell}>
        <header className={styles.agendaMasthead}>
          <a className={styles.agendaBrandMark} href="/" aria-label={brand.logoLabel}>
            <Image
              src={brand.logo}
              width={900}
              height={480}
              sizes="74px"
              priority
              alt={brand.logoAlt}
            />
          </a>
          <span className={styles.agendaMastDivider} aria-hidden="true" />
          <p>Agenda inteligente para o seu negócio</p>
        </header>

        <section className={styles.agendaHero} aria-labelledby="agenda-links-title">
          <div className={styles.agendaHeroCopy}>
            <span>Agenda Livre</span>
            <h1 id="agenda-links-title">
              Sua agenda organizada.<br />
              Seu tempo de volta.
            </h1>
          </div>
          <div className={styles.agendaHeroVisual} aria-hidden="true">
            <div className={styles.agendaScreenFrame}>
              <Image
                src="/agenda-livre/web-agenda-studio-fluxo.png"
                width={1200}
                height={640}
                sizes="(max-width: 700px) 70vw, 600px"
                priority
                alt=""
              />
            </div>
          </div>
        </section>

        <a
          className={styles.agendaStart}
          href="https://app.minhaagendalivre.com.br/"
          target="_blank"
          rel="noreferrer"
          aria-label="Começar grátis no Agenda Livre"
        >
          <span>Começar grátis</span>
          <ArrowRightIcon size={24} weight="bold" aria-hidden="true" />
        </a>

        <section className={styles.agendaActionGrid} aria-label={brand.linksLabel}>
          {actions.map((action) => (
            <ExternalLink
              key={action.title}
              link={action}
              className={`${styles.agendaAction} ${
                action.dark ? styles.agendaActionDark : styles.agendaActionLight
              }`}
            >
              <span className={styles.agendaActionIcon}>{action.icon}</span>
              <span className={styles.agendaActionCopy}>
                <small>{action.eyebrow}</small>
                <strong>{action.title}</strong>
                <em>{action.description}</em>
              </span>
              <ArrowUpRightIcon
                className={styles.agendaActionArrow}
                size={17}
                weight="bold"
                aria-hidden="true"
              />
            </ExternalLink>
          ))}
        </section>

        <section className={styles.agendaSocial} aria-labelledby="agenda-social-title">
          <h2 id="agenda-social-title">Acompanhe o Agenda Livre</h2>
          <div>
            {socialLinks.map((social) => (
              <a
                key={social.title}
                href={social.href}
                target="_blank"
                rel="noreferrer"
                aria-label={`Acompanhar o Agenda Livre no ${social.title}`}
              >
                {social.icon}
                <span>
                  <strong>{social.title}</strong>
                  <small>{social.handle}</small>
                </span>
              </a>
            ))}
          </div>
        </section>

        <section className={styles.agendaAnnual} aria-labelledby="agenda-annual-title">
          <div className={styles.agendaAnnualCopy}>
            <span>Plano anual</span>
            <h2 id="agenda-annual-title">12 meses de Agenda Livre</h2>
            <p className={styles.agendaAnnualPrice}>
              <strong>R$ 598,80</strong> / ano
            </p>
            <p className={styles.agendaAnnualBenefit}>
              <CheckCircleIcon size={16} weight="fill" aria-hidden="true" />
              Point Pro 3 inclusa
            </p>
            <ExternalLink
              link={brand.featured}
              className={styles.agendaAnnualButton}
            >
              Ir para o pagamento
              <ArrowRightIcon size={16} weight="bold" aria-hidden="true" />
            </ExternalLink>
          </div>
          <div className={styles.agendaAnnualMachine} aria-hidden="true">
            <Image
              src="/brand/point-pro-3-cutout-v1.png"
              width={1536}
              height={1024}
              sizes="(max-width: 700px) 58vw, 470px"
              alt=""
            />
          </div>
        </section>
      </div>
    </main>
  );
}

export default async function LinksPage({ searchParams }) {
  const params = await searchParams;
  const forceAgenda = params?.brand === "agenda";
  const brand = forceAgenda ? getBrand("minhaagendalivre.com.br") : await getRequestBrand();

  return brand.key === "balcao" ? (
    <BalcaoLinksPage brand={brand} />
  ) : (
    <AgendaLinksPage brand={brand} />
  );
}
