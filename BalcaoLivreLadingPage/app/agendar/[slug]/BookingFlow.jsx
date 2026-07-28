"use client";

import Image from "next/image";
import {
  ArrowLeft,
  ArrowRight,
  BadgeCheck,
  CalendarDays,
  Check,
  CheckCircle2,
  CircleHelp,
  Clock3,
  Images,
  AtSign,
  LoaderCircle,
  MapPin,
  MessageCircle,
  Phone,
  ShoppingCart,
  Quote,
  RefreshCw,
  Scissors,
  ShieldCheck,
  Sparkles,
  Tag,
  UserRound,
  UsersRound,
  WifiOff
} from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import styles from "./booking.module.css";

const money = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL"
});

function hasPromotionalPrice(service) {
  const price = Number(service?.price || 0);
  const originalPrice = Number(service?.originalPrice || price);
  return originalPrice > price && price >= 0;
}

function ServicePrice({ service }) {
  const price = Number(service?.price || 0);
  const originalPrice = Number(service?.originalPrice || price);
  if (!hasPromotionalPrice(service)) {
    return <span className={styles.catalogServicePrice}>{money.format(price)}</span>;
  }
  return (
    <span className={`${styles.catalogServicePrice} ${styles.catalogServicePromoPrice}`}>
      <span className={styles.catalogServiceOriginalPrice}>{money.format(originalPrice)}</span>
      <strong>{money.format(price)}</strong>
      <small>{Number(service?.discountPercent || 0) > 0 ? `-${service.discountPercent}%` : "OFERTA"}</small>
    </span>
  );
}

function isCatalogPromotionActive(promotion) {
  if (!promotion || promotion.isPublished !== true) return false;
  const now = new Date();
  const start = new Date(promotion.startDate);
  const end = new Date(promotion.endDate);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return false;
  end.setHours(23, 59, 59, 999);
  return now >= start && now <= end;
}

function safeHex(value, fallback) {
  return /^#[0-9a-f]{6}$/i.test(String(value || "").trim())
    ? String(value).trim()
    : fallback;
}

function formatPhone(value) {
  const digits = String(value || "").replace(/\D/g, "").slice(0, 11);
  if (digits.length <= 2) return digits ? `(${digits}` : "";
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  }
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
}

function shortDate(dateValue) {
  const date = new Date(`${dateValue}T12:00:00`);
  if (Number.isNaN(date.getTime())) return { weekday: "Dia", day: "--", month: "" };
  return {
    weekday: new Intl.DateTimeFormat("pt-BR", { weekday: "short" })
      .format(date)
      .replace(".", ""),
    day: new Intl.DateTimeFormat("pt-BR", { day: "2-digit" }).format(date),
    month: new Intl.DateTimeFormat("pt-BR", { month: "short" })
      .format(date)
      .replace(".", "")
  };
}

function fullDate(dateValue) {
  const date = new Date(`${dateValue}T12:00:00`);
  if (Number.isNaN(date.getTime())) return dateValue;
  const text = new Intl.DateTimeFormat("pt-BR", {
    weekday: "long",
    day: "2-digit",
    month: "long"
  }).format(date);
  return text.charAt(0).toUpperCase() + text.slice(1);
}

function bundleDays(services) {
  if (!services.length) return [];
  const firstService = services[0];

  return (firstService.days || []).map((day) => {
    const availableSlots = (day.availableSlots || []).flatMap((firstSlot) => {
      const firstStart = Date.parse(firstSlot.start);
      if (!Number.isFinite(firstStart)) return [];

      let elapsedMinutes = 0;
      const bundleSlots = [];
      for (const service of services) {
        const serviceDay = (service.days || []).find((candidate) => candidate.date === day.date);
        const expectedStart = firstStart + elapsedMinutes * 60_000;
        const slot = (serviceDay?.availableSlots || []).find((candidate) =>
          candidate.professionalId === firstSlot.professionalId &&
          Date.parse(candidate.start) === expectedStart
        );
        if (!slot) return [];
        bundleSlots.push({ ...slot, serviceId: service.id, serviceName: service.name });
        elapsedMinutes += Number(service.durationMinutes || 0);
      }

      return [{
        ...firstSlot,
        id: bundleSlots.map((slot) => slot.id).join("~"),
        bundleSlots,
        durationMinutes: elapsedMinutes
      }];
    });

    return { ...day, availableSlots };
  }).filter((day) => day.availableSlots.length > 0);
}

function initials(value) {
  const parts = String(value || "Agenda Livre")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  return parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join("") || "AL";
}

function safeStoreLogoUrl(value) {
  const candidate = String(value || "").trim();
  return candidate.length <= 132000 && /^data:image\/png;base64,[A-Za-z0-9+/]+={0,2}$/.test(candidate)
    ? candidate
    : "";
}

function StoreAvatar({ name, logoUrl }) {
  const safeLogoUrl = safeStoreLogoUrl(logoUrl);
  const [failedLogoUrl, setFailedLogoUrl] = useState("");
  const showLogo = safeLogoUrl && failedLogoUrl !== safeLogoUrl;

  return (
    <span className={styles.storeAvatar}>
      {showLogo ? (
        <img
          className={styles.storeAvatarImage}
          src={safeLogoUrl}
          alt={`Logo de ${name}`}
          width="44"
          height="44"
          decoding="async"
          onError={() => setFailedLogoUrl(safeLogoUrl)}
        />
      ) : (
        initials(name)
      )}
    </span>
  );
}

function newIdempotencyKey() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `web-${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}

function normalizeAvailability(payload, fallbackStoreName, slug) {
  const store = payload?.store || payload?.profile || {};
  const theme = store.theme && typeof store.theme === "object" ? store.theme : {};
  const services = Array.isArray(payload?.services)
    ? payload.services
    : Array.isArray(payload?.bookingServices)
      ? payload.bookingServices
      : [];
  return {
    store: {
      slug: store.slug || slug,
      name: store.name || store.storeName || fallbackStoreName || "Agenda online",
      segment: store.segment || "Atendimento com hora marcada",
      publicUrl: store.publicUrl || `https://${slug}.minhaagendalivre.com.br`,
      generatedAt: store.generatedAt || payload?.generatedAt || "",
      theme,
      logoUrl: safeStoreLogoUrl(store.logoUrl || theme.logoUrl),
      catalog: store.catalog && typeof store.catalog === "object" ? store.catalog : {},
      customDomain: store.customDomain && typeof store.customDomain === "object"
        ? store.customDomain
        : null
    },
    services
  };
}

function StepPill({ number, label, active, completed }) {
  return (
    <div
      className={`${styles.stepPill} ${active ? styles.stepPillActive : ""} ${completed ? styles.stepPillDone : ""}`}
      aria-current={active ? "step" : undefined}
    >
      <span>{completed ? <Check size={14} strokeWidth={3} /> : number}</span>
      <strong>{label}</strong>
    </div>
  );
}

function LegacyCatalogHome({ store, services, pageStyle, onSelectService }) {
  const catalog = store.catalog || {};
  const title = String(catalog.title || "Sua beleza, do seu jeito");
  const supportText = String(
    catalog.supportText || "Conheça nossos serviços e escolha o melhor horário para você.",
  );
  const buttonText = String(catalog.buttonText || "Agendar agora");
  const alignment = ["left", "center", "right"].includes(catalog.alignment)
    ? catalog.alignment
    : "left";
  const titleFont = ["Georgia", "Segoe UI", "Playfair Display"].includes(catalog.titleFont)
    ? catalog.titleFont
    : "Georgia";
  const spacingClass = catalog.spacing === "wide"
    ? styles.catalogHeroWide
    : catalog.spacing === "comfortable"
      ? styles.catalogHeroComfortable
      : styles.catalogHeroCompact;
  const imageOpacity = 0.8 + Math.min(100, Math.max(0, Number(catalog.imageContrast) || 64)) / 500;
  const scrollToServices = () => {
    document.getElementById("catalog-services")?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  return (
    <main className={`${styles.page} ${styles.catalogPage}`} style={pageStyle}>
      <header className={styles.catalogHeader}>
        <div className={styles.catalogHeaderInner}>
          <a className={styles.catalogBrand} href="#inicio" aria-label={`Início de ${store.name}`}>
            <StoreAvatar name={store.name} logoUrl={store.logoUrl} />
            <span><strong>{store.name}</strong><small>{store.segment}</small></span>
          </a>
          <nav className={styles.catalogNavigation} aria-label="Navegação do catálogo">
            <a href="#inicio">Início</a>
            <a href="#catalog-services">Serviços</a>
            <a href="#contato">Contato</a>
          </nav>
          {catalog.showButton !== false ? (
            <button className={styles.catalogHeaderCta} type="button" onClick={scrollToServices}>
              {buttonText}
            </button>
          ) : null}
        </div>
      </header>

      <div className={styles.catalogShell}>
        <section id="inicio" className={`${styles.catalogHero} ${spacingClass}`}>
          <div className={styles.catalogHeroCopy} style={{ textAlign: alignment, alignItems: alignment === "center" ? "center" : alignment === "right" ? "flex-end" : "flex-start" }}>
            <p className={styles.catalogEyebrow}><Sparkles size={15} /> Catálogo online</p>
            <h1 style={{ fontFamily: titleFont }}>{title}</h1>
            <span className={styles.catalogTitleLine} />
            <p>{supportText}</p>
            <div className={styles.catalogHeroActions}>
              {catalog.showButton !== false ? (
                <button type="button" onClick={scrollToServices}>{buttonText}</button>
              ) : null}
              <a href="#catalog-services">Conhecer serviços <ArrowRight size={16} /></a>
            </div>
          </div>
          <div className={styles.catalogHeroMedia}>
            {catalog.heroImageUrl ? (
              <img
                src={catalog.heroImageUrl}
                alt={`Ambiente e serviços de ${store.name}`}
                style={{ opacity: imageOpacity }}
                decoding="async"
              />
            ) : (
              <div className={styles.catalogHeroFallback}><Sparkles size={42} /><span>{store.name}</span></div>
            )}
          </div>
        </section>

        <section id="catalog-services" className={styles.catalogServices}>
          <div className={styles.catalogSectionHeading}>
            <div><p>Serviços</p><h2>Escolha seu atendimento</h2></div>
            <span>{services.length} disponíveis</span>
          </div>
          {services.length ? (
            <div className={styles.catalogServiceGrid}>
              {services.map((service, index) => (
                <button type="button" key={service.id} className={styles.catalogServiceCard} onClick={() => onSelectService(service)}>
                  <span className={styles.catalogServiceNumber}>{String(index + 1).padStart(2, "0")}</span>
                  <span className={styles.catalogServiceDetails}>
                    <strong>{service.name}</strong>
                    <small><Clock3 size={14} /> {service.durationMinutes} minutos</small>
                  </span>
                  <ServicePrice service={service} />
                  <ArrowRight size={18} />
                </button>
              ))}
            </div>
          ) : (
            <div className={styles.emptyState}>
              <CalendarDays size={30} />
              <h3>Novos horários em breve</h3>
              <p>Entre em contato com a loja para saber quando a agenda será atualizada.</p>
            </div>
          )}
        </section>

        <footer id="contato" className={styles.catalogFooter}>
          <div><strong>{store.name}</strong><span>{store.segment}</span></div>
          <a href="https://minhaagendalivre.com.br" target="_blank" rel="noreferrer">Criado com Agenda Livre</a>
        </footer>
      </div>
    </main>
  );
}

const catalogSectionLabels = {
  services: "Serviços",
  benefits: "Diferenciais",
  team: "Equipe",
  gallery: "Galeria",
  "before-after": "Resultados",
  process: "Como funciona",
  testimonials: "Depoimentos",
  faq: "Dúvidas",
  brands: "Especialidades",
  location: "Contato",
  callout: "Agendar"
};

function safeCatalogSectionId(value, fallback) {
  const normalized = String(value || "")
    .toLowerCase()
    .replace(/[^a-z0-9-]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);
  return normalized || fallback;
}

function catalogAction(section, scrollToServices, scrollToContact) {
  const target = String(section?.buttonTarget || "booking");
  if (target === "contact") return scrollToContact;
  return scrollToServices;
}

function CatalogSection({
  section,
  sectionIndex,
  services,
  selectedServiceIds,
  onToggleService,
  scrollToServices,
  scrollToContact
}) {
  const type = String(section?.type || "benefits");
  const items = Array.isArray(section?.items) ? section.items : [];
  const sectionId = `catalog-section-${safeCatalogSectionId(section?.id, `${type}-${sectionIndex}`)}`;
  const alignment = ["left", "center", "right"].includes(section?.alignment)
    ? section.alignment
    : "left";
  const backgroundClass = section?.background === "soft"
    ? styles.catalogSectionSoft
    : section?.background === "accent"
      ? styles.catalogSectionAccent
      : section?.background === "dark"
        ? styles.catalogSectionDark
        : styles.catalogSectionLight;
  const layoutClass = section?.layout === "columns"
    ? styles.catalogItemsColumns
    : section?.layout === "split"
      ? styles.catalogItemsSplit
      : section?.layout === "steps"
        ? styles.catalogItemsSteps
        : section?.layout === "gallery" || section?.layout === "comparison"
          ? styles.catalogItemsGallery
          : styles.catalogItemsCards;
  const action = catalogAction(section, scrollToServices, scrollToContact);

  if (type === "services") {
    return (
      <section id={sectionId} className={`${styles.catalogContentSection} ${backgroundClass}`}>
        <div className={styles.catalogSectionHeading} style={{ textAlign: alignment }}>
          <div>
            <p>{section?.subtitle || "Serviços"}</p>
            <h2>{section?.title || "Escolha seu atendimento"}</h2>
            {section?.body ? <span className={styles.catalogSectionLead}>{section.body}</span> : null}
          </div>
          <span>{services.length} disponíveis</span>
        </div>
        {services.length ? (
          <div className={styles.catalogServiceGrid}>
            {services.map((service, index) => (
              <button
                type="button"
                key={service.id}
                className={`${styles.catalogServiceCard} ${selectedServiceIds.includes(service.id) ? styles.catalogServiceCardSelected : ""}`}
                onClick={() => onToggleService(service)}
                aria-pressed={selectedServiceIds.includes(service.id)}
              >
                <span className={styles.catalogServiceNumber}>{String(index + 1).padStart(2, "0")}</span>
                <span className={styles.catalogServiceDetails}>
                  <strong>{service.name}</strong>
                  <small><Clock3 size={14} /> {service.durationMinutes} minutos</small>
                </span>
                <ServicePrice service={service} />
                <span className={styles.catalogServiceAdd}>
                  {selectedServiceIds.includes(service.id) ? <><Check size={15} /> Adicionado</> : <>Adicionar <span>+</span></>}
                </span>
              </button>
            ))}
          </div>
        ) : (
          <div className={styles.emptyState}>
            <CalendarDays size={30} />
            <h3>Novos horários em breve</h3>
            <p>Entre em contato com a loja para saber quando a agenda será atualizada.</p>
          </div>
        )}
      </section>
    );
  }

  if (type === "callout") {
    return (
      <section
        id={sectionId}
        className={`${styles.catalogContentSection} ${styles.catalogCalloutSection} ${backgroundClass}`}
        style={{ textAlign: alignment }}
      >
        <p className={styles.catalogSectionKicker}>{section?.subtitle || "Agendamento online"}</p>
        <h2>{section?.title || "Pronto para reservar seu horário?"}</h2>
        {section?.body ? <span className={styles.catalogSectionLead}>{section.body}</span> : null}
        <button type="button" onClick={action}>
          {section?.buttonText || "Agendar agora"}
          <ArrowRight size={17} />
        </button>
      </section>
    );
  }

  if (type !== "location" && !items.length) return null;

  return (
    <section
      id={sectionId}
      className={`${styles.catalogContentSection} ${backgroundClass}`}
      style={{ textAlign: alignment }}
    >
      <div className={styles.catalogSectionHeading}>
        <div>
          <p>{section?.subtitle || catalogSectionLabels[type] || "Conheça"}</p>
          <h2>{section?.title || catalogSectionLabels[type] || "Mais informações"}</h2>
          {section?.body ? <span className={styles.catalogSectionLead}>{section.body}</span> : null}
        </div>
      </div>

      {type === "faq" ? (
        <div className={styles.catalogFaqList}>
          {items.map((item, index) => (
            <details key={item.id || index}>
              <summary><CircleHelp size={18} /> {item.title || "Pergunta"}</summary>
              <p>{item.text}</p>
            </details>
          ))}
        </div>
      ) : (
        <div className={`${styles.catalogItemsGrid} ${layoutClass}`}>
          {items.map((item, index) => {
            const Icon = type === "team"
              ? UsersRound
              : type === "testimonials"
                ? Quote
                : type === "gallery" || type === "before-after"
                  ? Images
                  : type === "location"
                    ? index === 0 ? MapPin : index === 1 ? Clock3 : Phone
                    : BadgeCheck;
            return (
              <article className={styles.catalogItemCard} key={item.id || index}>
                {item.imageUrl ? (
                  <img src={item.imageUrl} alt={item.title || `Imagem ${index + 1}`} loading="lazy" />
                ) : (
                  <span className={styles.catalogItemIcon}><Icon size={22} /></span>
                )}
                <div>
                  <strong>{item.title || "Item"}</strong>
                  {item.text ? <p>{item.text}</p> : null}
                  {item.detail ? <small>{item.detail}</small> : null}
                </div>
              </article>
            );
          })}
        </div>
      )}

      {section?.buttonText ? (
        <button className={styles.catalogSectionButton} type="button" onClick={action}>
          {section.buttonText}
          <ArrowRight size={16} />
        </button>
      ) : null}
    </section>
  );
}

function CatalogHome({
  store,
  services,
  pageStyle,
  selectedServiceIds,
  onToggleService,
  onContinueBooking
}) {
  const catalog = store.catalog || {};
  const header = catalog.header && typeof catalog.header === "object" ? catalog.header : {};
  const footer = catalog.footer && typeof catalog.footer === "object" ? catalog.footer : {};
  const design = catalog.design && typeof catalog.design === "object" ? catalog.design : {};
  const title = String(catalog.title || "Sua beleza, do seu jeito");
  const supportText = String(
    catalog.supportText || "Conheça nossos serviços e escolha o melhor horário para você.",
  );
  const buttonText = String(catalog.buttonText || "Agendar agora");
  const alignment = ["left", "center", "right"].includes(catalog.alignment)
    ? catalog.alignment
    : "left";
  const titleFont = ["Georgia", "Segoe UI", "Playfair Display"].includes(catalog.titleFont)
    ? catalog.titleFont
    : "Georgia";
  const spacingClass = catalog.spacing === "wide"
    ? styles.catalogHeroWide
    : catalog.spacing === "comfortable"
      ? styles.catalogHeroComfortable
      : styles.catalogHeroCompact;
  const imageOpacity = 0.8 + Math.min(100, Math.max(0, Number(catalog.imageContrast) || 64)) / 500;
  const rawSections = Array.isArray(catalog.sections) && catalog.sections.length
    ? catalog.sections
    : [{
        id: "services",
        type: "services",
        title: "Escolha seu atendimento",
        subtitle: "Serviços",
        enabled: true,
        background: "light",
        layout: "cards",
        items: []
      }];
  const sections = rawSections.filter((section) => section && section.enabled !== false);
  const firstServicesId = sections.find((section) => section.type === "services")?.id;
  const scrollToServices = () => {
    const id = firstServicesId
      ? `catalog-section-${safeCatalogSectionId(firstServicesId, "services")}`
      : "catalog-services";
    document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });
  };
  const scrollToContact = () => {
    document.getElementById("contato")?.scrollIntoView({ behavior: "smooth", block: "start" });
  };
  const designClasses = [
    design.colorScheme === "dark" ? styles.catalogDesignDark : "",
    design.colorScheme === "light" ? styles.catalogDesignLight : "",
    design.buttonStyle === "pill" ? styles.catalogButtonsPill : "",
    design.buttonStyle === "square" ? styles.catalogButtonsSquare : "",
    design.cornerStyle === "soft" ? styles.catalogCornersSoft : "",
    design.cornerStyle === "sharp" ? styles.catalogCornersSharp : "",
    design.contentWidth === "compact" ? styles.catalogWidthCompact : "",
    design.contentWidth === "wide" ? styles.catalogWidthWide : ""
  ].filter(Boolean).join(" ");
  const navigationSections = sections
    .filter((section) => section.type !== "callout")
    .slice(0, 4);
  const phone = String(footer.whatsApp || footer.phone || "").replace(/\D/g, "");
  const whatsAppHref = phone ? `https://wa.me/${phone.startsWith("55") ? phone : `55${phone}`}` : "";
  const instagram = String(footer.instagram || "").replace(/^@+/, "");
  const cartServices = services.filter((service) => selectedServiceIds.includes(service.id));
  const cartDuration = cartServices.reduce((total, service) => total + Number(service.durationMinutes || 0), 0);
  const cartTotal = cartServices.reduce((total, service) => total + Number(service.price || 0), 0);
  const promotion = catalog.promotion && typeof catalog.promotion === "object"
    ? catalog.promotion
    : null;
  const showPromotion = Boolean(
    promotion?.highlightInCatalog !== false &&
    isCatalogPromotionActive(promotion) &&
    services.some(hasPromotionalPrice)
  );
  const promotionDiscount = services.reduce(
    (maximum, service) => Math.max(maximum, Number(service.discountPercent || 0)),
    0
  );

  return (
    <main className={`${styles.page} ${styles.catalogPage} ${designClasses}`} style={pageStyle}>
      <header className={`${styles.catalogHeader} ${header.sticky === false ? styles.catalogHeaderStatic : ""} ${styles[`catalogHeader${String(header.background || "solid").charAt(0).toUpperCase()}${String(header.background || "solid").slice(1)}`] || ""}`}>
        <div className={styles.catalogHeaderInner}>
          <a className={styles.catalogBrand} href="#inicio" aria-label={`Início de ${store.name}`}>
            {header.showLogo === false ? null : <StoreAvatar name={store.name} logoUrl={store.logoUrl} />}
            <span>
              <strong>{header.businessName || store.name}</strong>
              <small>{header.subtitle || store.segment}</small>
            </span>
          </a>
          {header.showNavigation === false ? <span /> : (
            <nav className={styles.catalogNavigation} aria-label="Navegação do catálogo">
              <a href="#inicio">Início</a>
              {navigationSections.map((section, index) => (
                <a
                  href={`#catalog-section-${safeCatalogSectionId(section.id, `${section.type}-${index}`)}`}
                  key={section.id || index}
                >
                  {catalogSectionLabels[section.type] || section.subtitle || "Seção"}
                </a>
              ))}
            </nav>
          )}
          {header.showButton === false || catalog.showButton === false ? null : (
            <button className={styles.catalogHeaderCta} type="button" onClick={scrollToServices}>
              {header.buttonText || buttonText}
            </button>
          )}
        </div>
      </header>

      <div className={styles.catalogShell}>
        <section id="inicio" className={`${styles.catalogHero} ${spacingClass}`}>
          <div
            className={styles.catalogHeroCopy}
            style={{
              textAlign: alignment,
              alignItems: alignment === "center" ? "center" : alignment === "right" ? "flex-end" : "flex-start"
            }}
          >
            <p className={styles.catalogEyebrow}><Sparkles size={15} /> Catálogo online</p>
            <h1 style={{ fontFamily: titleFont }}>{title}</h1>
            <span className={styles.catalogTitleLine} />
            <p>{supportText}</p>
            <div className={styles.catalogHeroActions}>
              {catalog.showButton === false ? null : (
                <button type="button" onClick={scrollToServices}>{buttonText}</button>
              )}
              <a href={firstServicesId ? `#catalog-section-${safeCatalogSectionId(firstServicesId, "services")}` : "#contato"}>
                Conhecer serviços <ArrowRight size={16} />
              </a>
            </div>
          </div>
          <div className={styles.catalogHeroMedia}>
            {catalog.heroImageUrl ? (
              <img
                src={catalog.heroImageUrl}
                alt={`Ambiente e serviços de ${store.name}`}
                style={{ opacity: imageOpacity }}
                decoding="async"
              />
            ) : (
              <div className={styles.catalogHeroFallback}><Sparkles size={42} /><span>{store.name}</span></div>
            )}
          </div>
        </section>

        {showPromotion ? (
          <section className={styles.catalogPromotionBanner} aria-label="Promoção vigente">
            <span className={styles.catalogPromotionIcon}><Tag size={19} /></span>
            <div>
              <small>Oferta por tempo limitado</small>
              <strong>{promotion.name || "Condição especial no site"}</strong>
              <p>Selecione um dos serviços destacados e agende pelo preço promocional.</p>
            </div>
            <span className={styles.catalogPromotionBadge}>
              {promotionDiscount > 0 ? `ATÉ ${promotionDiscount}% OFF` : "OFERTA"}
            </span>
          </section>
        ) : null}

        <div className={styles.catalogSections}>
          {sections.map((section, index) => (
            <CatalogSection
              key={section.id || `${section.type}-${index}`}
              section={section}
              sectionIndex={index}
              services={services}
              selectedServiceIds={selectedServiceIds}
              onToggleService={onToggleService}
              scrollToServices={scrollToServices}
              scrollToContact={scrollToContact}
            />
          ))}
        </div>

        <footer id="contato" className={styles.catalogFooter}>
          <div className={styles.catalogFooterIdentity}>
            <strong>{footer.businessName || store.name}</strong>
            <span>{footer.description || store.segment}</span>
          </div>
          <div className={styles.catalogFooterDetails}>
            {footer.address ? <span><MapPin size={14} /> {footer.address}</span> : null}
            {footer.showHours === false || !footer.hours ? null : <span><Clock3 size={14} /> {footer.hours}</span>}
            {footer.showContact === false || !footer.phone ? null : <a href={`tel:${String(footer.phone).replace(/[^\d+]/g, "")}`}><Phone size={14} /> {footer.phone}</a>}
          </div>
          {footer.showSocial === false ? null : (
            <div className={styles.catalogFooterSocial}>
              {whatsAppHref ? <a href={whatsAppHref} target="_blank" rel="noreferrer"><MessageCircle size={15} /> WhatsApp</a> : null}
              {instagram ? <a href={`https://instagram.com/${instagram}`} target="_blank" rel="noreferrer"><AtSign size={15} /> Instagram</a> : null}
            </div>
          )}
          <a className={styles.catalogAgendaCredit} href="https://minhaagendalivre.com.br" target="_blank" rel="noreferrer">
            Desenvolvido por <strong>Agenda Livre</strong>
          </a>
        </footer>
      </div>
      {cartServices.length ? (
        <aside className={styles.catalogCartDock} aria-live="polite">
          <span className={styles.catalogCartIcon}><ShoppingCart size={20} /></span>
          <div className={styles.catalogCartCopy}>
            <small>{cartServices.length} {cartServices.length === 1 ? "serviço escolhido" : "serviços escolhidos"}</small>
            <strong>{cartServices.map((service) => service.name).join(" + ")}</strong>
            <span>{cartDuration} min • {money.format(cartTotal)}</span>
          </div>
          <button type="button" onClick={onContinueBooking}>
            {buttonText}
            <ArrowRight size={18} />
          </button>
        </aside>
      ) : null}
    </main>
  );
}

export default function BookingFlow({ slug, fallbackStoreName }) {
  const [availability, setAvailability] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [stage, setStage] = useState(0);
  const [serviceId, setServiceId] = useState("");
  const [selectedServiceIds, setSelectedServiceIds] = useState([]);
  const [date, setDate] = useState("");
  const [slotId, setSlotId] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const [booking, setBooking] = useState(null);
  const idempotencyKey = useRef(newIdempotencyKey());

  const loadAvailability = useCallback(async ({ quiet = false } = {}) => {
    quiet ? setRefreshing(true) : setLoading(true);
    setError("");
    try {
      const response = await fetch(`/api/agendar/${encodeURIComponent(slug)}/availability`, {
        cache: "no-store",
        headers: { Accept: "application/json" }
      });
      const payload = await response.json().catch(() => ({}));
      if (!response.ok) {
        throw new Error(
          payload?.message ||
          payload?.error?.message ||
          (typeof payload?.error === "string" ? payload.error : "") ||
          "A agenda não está disponível agora."
        );
      }
      setAvailability(normalizeAvailability(payload, fallbackStoreName, slug));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Não foi possível abrir a agenda.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [fallbackStoreName, slug]);

  useEffect(() => {
    loadAvailability();
  }, [loadAvailability]);

  const selectedService = useMemo(
    () => availability?.services?.find((service) => service.id === serviceId) || null,
    [availability, serviceId]
  );
  const selectedServices = useMemo(
    () => (availability?.services || []).filter((service) => selectedServiceIds.includes(service.id)),
    [availability, selectedServiceIds]
  );
  const selectedBundleDays = useMemo(
    () => bundleDays(selectedServices),
    [selectedServices]
  );
  const selectedServicesDuration = selectedServices.reduce(
    (total, service) => total + Number(service.durationMinutes || 0),
    0
  );
  const selectedServicesTotal = selectedServices.reduce(
    (total, service) => total + Number(service.price || 0),
    0
  );
  const selectedServicesName = selectedServices.map((service) => service.name).join(" + ");
  const selectedServicesSummary = selectedServices.length
    ? {
        ...selectedServices[0],
        name: selectedServicesName,
        durationMinutes: selectedServicesDuration,
        price: selectedServicesTotal
      }
    : null;
  const selectedDay = useMemo(
    () => selectedBundleDays.find((item) => item.date === date) || null,
    [selectedBundleDays, date]
  );
  const selectedSlot = useMemo(
    () => selectedDay?.availableSlots?.find((slot) => slot.id === slotId) || null,
    [selectedDay, slotId]
  );

  useEffect(() => {
    if (!booking?.id || !booking?.statusToken || !["pending", "requested"].includes(booking.status)) {
      return undefined;
    }

    let cancelled = false;
    let attempts = 0;
    const poll = async () => {
      attempts += 1;
      try {
        const response = await fetch(
          `/api/agendar/${encodeURIComponent(slug)}/appointments/${encodeURIComponent(booking.id)}?token=${encodeURIComponent(booking.statusToken)}`,
          { cache: "no-store" }
        );
        const payload = await response.json().catch(() => ({}));
        if (!cancelled && response.ok && payload?.booking) {
          setBooking((current) => ({ ...current, ...payload.booking }));
        }
      } catch {
        // A confirmação segue no servidor; uma falha curta de rede não perde a reserva.
      }
      if (!cancelled && attempts < 40) {
        window.setTimeout(poll, 3000);
      }
    };
    const timer = window.setTimeout(poll, 1800);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [booking?.id, booking?.status, booking?.statusToken, slug]);

  const store = availability?.store || {
    name: fallbackStoreName,
    segment: "Atendimento com hora marcada",
    theme: {}
  };
  const theme = store.theme || {};
  const catalog = store.catalog || {};
  const pageStyle = {
    "--booking-accent": safeHex(catalog.accentColor || theme.accent, "#c96555"),
    "--booking-accent-deep": safeHex(theme.accentDark || theme.dark, "#a94a3d"),
    "--booking-soft": safeHex(theme.accentSoft || theme.soft, "#fce7e2"),
    "--booking-on-accent": safeHex(theme.onAccent || theme.textOnAccent, "#ffffff")
  };

  const toggleService = (service) => {
    setSelectedServiceIds((current) =>
      current.includes(service.id)
        ? current.filter((id) => id !== service.id)
        : [...current, service.id].slice(0, 6)
    );
    setDate("");
    setSlotId("");
    setSubmitError("");
  };

  const continueWithServices = () => {
    if (!selectedServices.length) return;
    setServiceId(selectedServices[0].id);
    setStage(1);
  };

  const selectDate = (nextDate) => {
    setDate(nextDate);
    setSlotId("");
    setSubmitError("");
    setStage(2);
  };

  const selectSlot = (slot) => {
    setSlotId(slot.id);
    setSubmitError("");
    setStage(3);
  };

  const goBack = () => {
    setSubmitError("");
    if (stage === 3) {
      setSlotId("");
      setStage(2);
    } else if (stage === 2) {
      setDate("");
      setStage(1);
    } else if (stage === 1) {
      setStage(0);
    }
  };

  const submitBooking = async (event) => {
    event.preventDefault();
    setSubmitError("");
    const phoneDigits = customerPhone.replace(/\D/g, "");
    if (customerName.trim().length < 2) {
      setSubmitError("Digite seu nome para continuar.");
      return;
    }
    if (phoneDigits.length < 10 || phoneDigits.length > 11) {
      setSubmitError("Digite um WhatsApp válido com DDD.");
      return;
    }
    if (!selectedServices.length || !selectedSlot) {
      setSubmitError("Escolha novamente os serviços e o horário.");
      return;
    }

    setSubmitting(true);
    try {
      const response = await fetch(`/api/agendar/${encodeURIComponent(slug)}/appointments`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({
          serviceId: selectedServices[0].id,
          slotId: selectedSlot.bundleSlots?.[0]?.id || selectedSlot.id,
          items: selectedServices.map((service, index) => ({
            serviceId: service.id,
            slotId: selectedSlot.bundleSlots?.[index]?.id || selectedSlot.id
          })),
          customerName: customerName.trim(),
          customerPhone: phoneDigits,
          notes: notes.trim(),
          idempotencyKey: idempotencyKey.current
        })
      });
      const payload = await response.json().catch(() => ({}));
      if (!response.ok) {
        if (response.status === 409) {
          idempotencyKey.current = newIdempotencyKey();
          await loadAvailability({ quiet: true });
          setSlotId("");
          setStage(2);
        }
        throw new Error(
          payload?.message ||
          payload?.error?.message ||
          (typeof payload?.error === "string" ? payload.error : "") ||
          "Não foi possível reservar este horário."
        );
      }
      setBooking(payload.booking || payload);
      setStage(4);
    } catch (requestError) {
      setSubmitError(requestError instanceof Error ? requestError.message : "Não foi possível concluir o agendamento.");
    } finally {
      setSubmitting(false);
    }
  };

  const restart = () => {
    setBooking(null);
    setServiceId("");
    setDate("");
    setSlotId("");
    setCustomerName("");
    setCustomerPhone("");
    setNotes("");
    setSubmitError("");
    idempotencyKey.current = newIdempotencyKey();
    setStage(0);
    loadAvailability({ quiet: true });
  };

  if (loading) {
    return (
      <main className={styles.page} style={pageStyle} aria-busy="true">
        <div className={styles.ambientTop} />
        <section className={styles.loadingShell}>
          <div className={styles.loadingBrand} />
          <div className={styles.loadingTitle} />
          <div className={styles.loadingLine} />
          <div className={styles.loadingGrid}><div /><div /><div /><div /></div>
        </section>
      </main>
    );
  }

  if (error || !availability) {
    return (
      <main className={styles.page} style={pageStyle}>
        <div className={styles.ambientTop} />
        <section className={styles.errorCard}>
          <span className={styles.errorIcon}><WifiOff size={27} /></span>
          <p className={styles.eyebrow}>Agenda temporariamente indisponível</p>
          <h1>Vamos tentar de novo?</h1>
          <p>{error || "Esta loja ainda não publicou os horários disponíveis."}</p>
          <button type="button" onClick={() => loadAvailability()}>
            <RefreshCw size={17} /> Atualizar agenda
          </button>
        </section>
      </main>
    );
  }

  if (stage === 0) {
    return (
      <CatalogHome
        store={store}
        services={availability.services}
        pageStyle={pageStyle}
        selectedServiceIds={selectedServiceIds}
        onToggleService={toggleService}
        onContinueBooking={continueWithServices}
      />
    );
  }

  return (
    <main className={styles.page} style={pageStyle}>
      <div className={styles.ambientTop} aria-hidden="true" />
      <div className={styles.ambientBottom} aria-hidden="true" />

      <header className={styles.header}>
        <div className={styles.headerInner}>
          <div className={styles.brandBlock}>
            <StoreAvatar name={store.name} logoUrl={store.logoUrl} />
            <span>
              <strong>{store.name}</strong>
              <small>{store.segment}</small>
            </span>
          </div>
          <div className={styles.secureBadge}>
            <ShieldCheck size={16} />
            <span>Agendamento seguro</span>
          </div>
        </div>
      </header>

      <div className={styles.shell}>
        <section className={styles.hero}>
          <div className={styles.heroCopy}>
            <p className={styles.eyebrow}><Sparkles size={15} /> Agende online em poucos passos</p>
            <h1>Qual cuidado você quer reservar?</h1>
            <p>Veja os horários livres em tempo real e escolha o melhor momento para você.</p>
          </div>
          <div className={styles.heroTrust}>
            <span><CalendarDays size={20} /></span>
            <div><strong>Horários atualizados</strong><small>Direto da agenda da loja</small></div>
          </div>
        </section>

        {stage < 4 ? (
          <nav className={styles.steps} aria-label="Etapas do agendamento">
            <StepPill number="1" label="Serviços" active={stage === 0} completed={stage > 0} />
            <span className={styles.stepLine} />
            <StepPill number="2" label="Dia" active={stage === 1} completed={stage > 1} />
            <span className={styles.stepLine} />
            <StepPill number="3" label="Horário" active={stage === 2} completed={stage > 2} />
            <span className={styles.stepLine} />
            <StepPill number="4" label="Seus dados" active={stage === 3} completed={false} />
          </nav>
        ) : null}

        {stage > 0 && stage < 4 && selectedService ? (
          <div className={styles.selectionBar}>
            <div className={styles.selectionIcon}><Scissors size={18} /></div>
            <div>
              <small>Seu agendamento</small>
              <strong>{selectedServicesName}</strong>
              <span>
                {selectedServicesDuration} min
                {selectedSlot ? ` • ${fullDate(date)} às ${selectedSlot.time}` : ""}
              </span>
            </div>
            <strong className={styles.selectionPrice}>{money.format(selectedServicesTotal)}</strong>
          </div>
        ) : null}

        <section className={styles.contentCard}>
          {stage === 0 ? (
            <div className={styles.panel}>
              <div className={styles.panelHeading}>
                <div><p className={styles.panelKicker}>Serviços</p><h2>Escolha uma opção</h2></div>
                <span>{availability.services.length} disponíveis</span>
              </div>
              {availability.services.length ? (
                <div className={styles.serviceGrid}>
                  {availability.services.map((service, index) => (
                    <button
                      className={styles.serviceCard}
                      type="button"
                      key={service.id}
                      onClick={() => toggleService(service)}
                    >
                      <span className={styles.serviceIcon}>{index % 3 === 0 ? <Sparkles size={21} /> : index % 3 === 1 ? <Scissors size={21} /> : <HeartMark />}</span>
                      <span className={styles.serviceInfo}>
                        <strong>{service.name}</strong>
                        <small><Clock3 size={14} /> {service.durationMinutes} minutos</small>
                      </span>
                      <span className={styles.servicePrice}>{money.format(Number(service.price || 0))}</span>
                      <ChevronMark />
                    </button>
                  ))}
                </div>
              ) : (
                <div className={styles.emptyState}>
                  <CalendarDays size={30} />
                  <h3>Nenhum serviço com horário livre</h3>
                  <p>Peça um novo link à loja ou tente novamente mais tarde.</p>
                </div>
              )}
            </div>
          ) : null}

          {stage === 1 && selectedService ? (
            <div className={styles.panel}>
              <button className={styles.backButton} type="button" onClick={goBack}><ArrowLeft size={17} /> Trocar serviços</button>
              <div className={styles.panelHeading}>
                <div><p className={styles.panelKicker}>Datas disponíveis</p><h2>Qual é o melhor dia?</h2></div>
                <button className={styles.refreshButton} type="button" disabled={refreshing} onClick={() => loadAvailability({ quiet: true })}>
                  <RefreshCw size={15} className={refreshing ? styles.spinning : ""} /> Atualizar
                </button>
              </div>
              <div className={styles.dateGrid}>
                {selectedBundleDays.map((item) => {
                  const parts = shortDate(item.date);
                  return (
                    <button key={item.date} className={styles.dateCard} type="button" onClick={() => selectDate(item.date)}>
                      <small>{parts.weekday}</small><strong>{parts.day}</strong><span>{parts.month}</span>
                      <em>{item.availableSlots?.length || 0} horários</em>
                    </button>
                  );
                })}
              </div>
              {!selectedBundleDays.length ? (
                <div className={styles.emptyState}><CalendarDays size={30} /><h3>Sem encaixe para esse conjunto</h3><p>Remova um serviço do carrinho ou tente novamente mais tarde.</p></div>
              ) : null}
            </div>
          ) : null}

          {stage === 2 && selectedDay ? (
            <div className={styles.panel}>
              <button className={styles.backButton} type="button" onClick={goBack}><ArrowLeft size={17} /> Trocar dia</button>
              <div className={styles.panelHeading}>
                <div><p className={styles.panelKicker}>{fullDate(date)}</p><h2>Escolha o horário</h2></div>
                <span>{selectedDay.availableSlots?.length || 0} opções</span>
              </div>
              <div className={styles.slotGrid}>
                {(selectedDay.availableSlots || []).map((slot) => (
                  <button className={styles.slotButton} type="button" key={slot.id} onClick={() => selectSlot(slot)}>
                    <Clock3 size={17} /><strong>{slot.time}</strong>
                    <small>{slot.professionalName || "Profissional disponível"}</small>
                  </button>
                ))}
              </div>
            </div>
          ) : null}

          {stage === 3 && selectedService && selectedSlot ? (
            <div className={styles.panel}>
              <button className={styles.backButton} type="button" onClick={goBack}><ArrowLeft size={17} /> Trocar horário</button>
              <div className={styles.formGrid}>
                <div className={styles.formIntro}>
                  <p className={styles.panelKicker}>Último passo</p>
                  <h2>Para quem é este horário?</h2>
                  <p>Usaremos seu WhatsApp somente para confirmar este agendamento e enviar o lembrete.</p>
                  <div className={styles.summaryCard}>
                    <div><span><Scissors size={17} /></span><p><small>Serviços</small><strong>{selectedServicesName}</strong></p></div>
                    <div><span><CalendarDays size={17} /></span><p><small>Data e horário</small><strong>{fullDate(date)}, {selectedSlot.time}</strong></p></div>
                    <div><span><UserRound size={17} /></span><p><small>Profissional</small><strong>{selectedSlot.professionalName || "Profissional disponível"}</strong></p></div>
                  </div>
                </div>

                <form className={styles.bookingForm} onSubmit={submitBooking} noValidate>
                  <label>
                    <span>Seu nome</span>
                    <input
                      value={customerName}
                      onChange={(event) => setCustomerName(event.target.value.slice(0, 100))}
                      placeholder="Como podemos chamar você?"
                      autoComplete="name"
                      required
                    />
                  </label>
                  <label>
                    <span>WhatsApp com DDD</span>
                    <input
                      value={customerPhone}
                      onChange={(event) => setCustomerPhone(formatPhone(event.target.value))}
                      placeholder="(00) 00000-0000"
                      inputMode="tel"
                      autoComplete="tel"
                      required
                    />
                    <small><MessageCircle size={13} /> A confirmação e o lembrete chegarão neste número.</small>
                  </label>
                  <label>
                    <span>Observação <em>opcional</em></span>
                    <textarea
                      value={notes}
                      onChange={(event) => setNotes(event.target.value.slice(0, 300))}
                      placeholder="Alguma preferência ou informação importante?"
                      rows={3}
                    />
                  </label>
                  {submitError ? <p className={styles.formError} role="alert">{submitError}</p> : null}
                  <button className={styles.submitButton} type="submit" disabled={submitting}>
                    {submitting ? <><LoaderCircle className={styles.spinning} size={19} /> Reservando...</> : <>Confirmar agendamento <ArrowRight size={19} /></>}
                  </button>
                  <p className={styles.privacyNote}><ShieldCheck size={14} /> Seus dados são usados somente para este atendimento.</p>
                </form>
              </div>
            </div>
          ) : null}

          {stage === 4 && booking ? (
            <BookingResult
              booking={booking}
              store={store}
              service={selectedServicesSummary || selectedService}
              date={date}
              slot={selectedSlot}
              customerName={customerName}
              onRestart={restart}
            />
          ) : null}
        </section>

        <footer className={styles.footer}>
          <span>Agendamento protegido por</span>
          <a href="https://minhaagendalivre.com.br" target="_blank" rel="noreferrer">
            <Image src="/agenda-livre/agenda-livre-mark.png" alt="" width={62} height={32} unoptimized />
            <strong>Agenda Livre</strong>
          </a>
        </footer>
      </div>
    </main>
  );
}

function BookingResult({ booking, store, service, date, slot, customerName, onRestart }) {
  const status = String(booking.status || "pending").toLowerCase();
  const confirmed = status === "confirmed";
  const rejected = ["rejected", "slot_conflict", "cancelled"].includes(status);

  return (
    <div className={styles.resultPanel} aria-live="polite">
      <div className={`${styles.resultIcon} ${confirmed ? styles.resultConfirmed : ""} ${rejected ? styles.resultRejected : ""}`}>
        {rejected ? <CalendarDays size={34} /> : confirmed ? <CheckCircle2 size={38} /> : <LoaderCircle className={styles.spinning} size={34} />}
      </div>
      <p className={styles.panelKicker}>{confirmed ? "Tudo certo" : rejected ? "Precisamos escolher de novo" : "Pedido recebido"}</p>
      <h2>{confirmed ? "Agendamento confirmado!" : rejected ? "Este horário não está mais livre" : "Estamos confirmando com a agenda"}</h2>
      <p className={styles.resultLead}>
        {confirmed
          ? `${customerName.split(" ")[0]}, seu horário em ${store.name} está reservado.`
          : rejected
            ? booking.message || "Outro cliente acabou de reservar esse horário. Escolha uma nova opção."
            : "Isso costuma levar apenas alguns segundos. Você receberá a confirmação no WhatsApp informado."}
      </p>
      {!rejected ? (
        <div className={styles.resultSummary}>
          <div><small>Serviço</small><strong>{booking.serviceName || service?.name}</strong></div>
          <div><small>Quando</small><strong>{fullDate(date)}, às {slot?.time}</strong></div>
          <div><small>Local</small><strong>{store.name}</strong></div>
        </div>
      ) : null}
      {confirmed ? (
        <div className={styles.whatsappNotice}>
          <MessageCircle size={22} />
          <p><strong>Fique de olho no WhatsApp</strong><span>Enviaremos também um lembrete cerca de 4 horas antes.</span></p>
        </div>
      ) : null}
      {rejected ? <button className={styles.submitButton} type="button" onClick={onRestart}>Ver outros horários <ArrowRight size={18} /></button> : null}
      {!confirmed && !rejected ? <p className={styles.waitNote}><span /> Não feche esta página enquanto confirmamos.</p> : null}
      {confirmed ? <button className={styles.linkButton} type="button" onClick={onRestart}>Fazer outro agendamento</button> : null}
    </div>
  );
}

function ChevronMark() {
  return <ArrowRight className={styles.serviceArrow} size={18} aria-hidden="true" />;
}

function HeartMark() {
  return (
    <svg viewBox="0 0 24 24" width="21" height="21" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1.1L12 21l7.8-7.5 1.1-1.1a5.5 5.5 0 0 0-.1-7.8Z" />
    </svg>
  );
}
