import Link from "next/link";
import { notFound } from "next/navigation";
import { absoluteUrl, openGraphImage, siteName } from "../seo";
import { sellers } from "../siteLinks";
import {
  checkoutHrefForPlan,
  featuredSeoPages,
  getPlanCopy,
  getSeoPage,
  seoPageJsonLd,
  seoPages
} from "../seoPages";

export function generateStaticParams() {
  return seoPages.map((page) => ({ slug: page.slug }));
}

export async function generateMetadata({ params }) {
  const routeParams = await params;
  const page = getSeoPage(routeParams.slug);
  if (!page) return {};

  const canonical = absoluteUrl(`/${page.slug}/`);

  return {
    title: page.metaTitle || `${page.title} | ${siteName}`,
    description: page.description,
    keywords: page.keywords,
    alternates: {
      canonical
    },
    openGraph: {
      title: page.metaTitle || page.title,
      description: page.description,
      url: canonical,
      siteName,
      locale: "pt_BR",
      type: "website",
      images: [
        {
          url: absoluteUrl(openGraphImage),
          width: 1200,
          height: 630,
          alt: siteName
        }
      ]
    },
    twitter: {
      card: "summary_large_image",
      title: page.metaTitle || page.title,
      description: page.description,
      images: [absoluteUrl(openGraphImage)]
    }
  };
}

function relatedPages(currentPage) {
  const samePlan = seoPages.filter(
    (page) => page.slug !== currentPage.slug && page.plan === currentPage.plan
  );
  const fallback = featuredSeoPages.filter((page) => page.slug !== currentPage.slug);
  return [...samePlan, ...fallback]
    .filter((page, index, list) => list.findIndex((item) => item.slug === page.slug) === index)
    .slice(0, 6);
}

export default async function SeoLandingPage({ params }) {
  const routeParams = await params;
  const page = getSeoPage(routeParams.slug);
  if (!page) notFound();

  const plan = getPlanCopy(page.plan);
  const checkoutHref = checkoutHrefForPlan(page.plan);
  const canonical = absoluteUrl(`/${page.slug}/`);
  const jsonLd = seoPageJsonLd(page, canonical);
  const seller = sellers[1] || sellers[0];

  return (
    <main className="seoPage">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />

      <section className="seoHero">
        <div className="seoHeroCopy">
          <Link className="seoBackLink" href="/">
            Balcão Livre PDV
          </Link>
          <span className="seoEyebrow">{page.eyebrow}</span>
          <h1>{page.h1}</h1>
          <p>{page.lead}</p>
          <div className="seoCtas">
            <a
              className="lpButton primary"
              href={plan.href}
              data-analytics-action="trial_download"
              data-analytics-plan={plan.analyticsPlan}
              data-analytics-location={`seo_${page.slug}`}
            >
              Testar grátis por 7 dias
            </a>
            <a
              className="lpButton secondary"
              href={seller.href}
              data-analytics-action="whatsapp_click"
              data-analytics-seller={seller.name}
              data-analytics-location={`seo_${page.slug}`}
            >
              Falar no WhatsApp
            </a>
          </div>
        </div>

        <aside className="seoHeroCard" aria-label="Plano recomendado">
          <span>{page.segment}</span>
          <strong>{plan.label}</strong>
          <b>{plan.price}</b>
          <a
            href={checkoutHref}
            data-analytics-action="plan_click"
            data-analytics-plan={plan.analyticsPlan}
            data-analytics-location={`seo_${page.slug}`}
          >
            Comprar mensal
          </a>
        </aside>
      </section>

      <section className="seoTrustStrip" aria-label="Pontos principais">
        <span>Windows</span>
        <span>Teste grátis</span>
        <span>Impressora 58/80mm</span>
        <span>Caixa offline</span>
        <span>Suporte na implantação</span>
      </section>

      <section className="seoSection">
        <div className="seoSectionTitle">
          <span>Resultado</span>
          <h2>O que melhora na rotina da loja</h2>
        </div>
        <div className="seoGrid">
          {page.outcomes.map((outcome) => (
            <article className="seoCard" key={outcome}>
              <strong>{outcome}</strong>
              <p>Fluxo mais claro para vender, acompanhar e conferir sem depender de planilha.</p>
            </article>
          ))}
        </div>
      </section>

      <section className="seoSection">
        <div className="seoSectionTitle">
          <span>Funcionalidades</span>
          <h2>Recursos que entram no PDV</h2>
        </div>
        <div className="seoFeatureList">
          {page.features.map((feature) => (
            <span key={feature}>{feature}</span>
          ))}
        </div>
      </section>

      <section className="seoPlanBox">
        <div>
          <span>Plano indicado</span>
          <h2>{plan.label}</h2>
          <p>
            Comece pelo teste de 7 dias. Se a rotina bater com sua loja, assine o plano
            mensal ou fale com o vendedor para configurar tudo.
          </p>
        </div>
        <div className="seoPlanActions">
          <strong>{plan.price}</strong>
          <a className="lpButton primary" href={plan.href}>
            Baixar teste
          </a>
          <a className="lpButton secondary" href={checkoutHref}>
            Comprar mensal
          </a>
        </div>
      </section>

      <section className="seoSection">
        <div className="seoSectionTitle">
          <span>Dúvidas</span>
          <h2>Perguntas rápidas</h2>
        </div>
        <div className="seoFaqGrid">
          {page.faq.map(([question, answer]) => (
            <article className="seoFaq" key={question}>
              <h3>{question}</h3>
              <p>{answer}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="seoSection seoRelated">
        <div className="seoSectionTitle">
          <span>Mais buscas</span>
          <h2>Veja também</h2>
        </div>
        <div className="seoRelatedLinks">
          {relatedPages(page).map((related) => (
            <Link key={related.slug} href={`/${related.slug}/`}>
              {related.title}
            </Link>
          ))}
        </div>
      </section>
    </main>
  );
}
