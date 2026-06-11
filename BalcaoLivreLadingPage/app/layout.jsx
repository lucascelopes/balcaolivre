import "./globals.css";
import Script from "next/script";
import {
  absoluteUrl,
  defaultDescription,
  defaultTitle,
  openGraphImage,
  seoKeywords,
  siteName,
  siteUrl
} from "./seo";

const metaPixelId =
  process.env.NEXT_PUBLIC_META_PIXEL_ID ||
  process.env.META_PIXEL_ID ||
  "1609814976758625";
const gaMeasurementId = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID || "G-CPJ89TNX9Q";
const clarityProjectId = process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID || "";
const adminAnalyticsUrl =
  process.env.NEXT_PUBLIC_BALCAO_ADMIN_ANALYTICS_URL ||
  "https://balcaolivrepdv.onrender.com/api/public/analytics";
const googleSiteVerification =
  process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION || "";
const bingSiteVerification =
  process.env.NEXT_PUBLIC_BING_SITE_VERIFICATION || "";

const verification = {};
if (googleSiteVerification) {
  verification.google = googleSiteVerification;
}
if (bingSiteVerification) {
  verification.other = { "msvalidate.01": bingSiteVerification };
}

const siteJsonLd = [
  {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: siteName,
    url: siteUrl,
    logo: absoluteUrl("/brand/bl-modern-icon.png"),
    sameAs: ["https://pdv.balcaolivrepdv.com.br"],
    contactPoint: [
      {
        "@type": "ContactPoint",
        telephone: "+55-27-98126-7551",
        contactType: "sales",
        areaServed: "BR",
        availableLanguage: "Portuguese"
      },
      {
        "@type": "ContactPoint",
        telephone: "+55-33-99960-9457",
        contactType: "sales",
        areaServed: "BR",
        availableLanguage: "Portuguese"
      }
    ]
  },
  {
    "@context": "https://schema.org",
    "@type": "WebSite",
    name: siteName,
    url: siteUrl,
    inLanguage: "pt-BR"
  }
];

export const metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: defaultTitle,
    template: `%s | ${siteName}`
  },
  description: defaultDescription,
  applicationName: siteName,
  keywords: seoKeywords,
  alternates: {
    canonical: "/"
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-image-preview": "large",
      "max-snippet": -1,
      "max-video-preview": -1
    }
  },
  openGraph: {
    type: "website",
    locale: "pt_BR",
    url: siteUrl,
    siteName,
    title: defaultTitle,
    description: defaultDescription,
    images: [
      {
        url: openGraphImage,
        width: 1200,
        height: 630,
        alt: "Tela do Balcão Livre PDV para restaurantes"
      }
    ]
  },
  twitter: {
    card: "summary_large_image",
    title: defaultTitle,
    description: defaultDescription,
    images: [openGraphImage]
  },
  verification,
  icons: {
    icon: "/brand/bl-modern-icon.png",
    shortcut: "/brand/bl-modern-icon.png",
    apple: "/brand/bl-modern-icon.png"
  }
};

export default function RootLayout({ children }) {
  return (
    <html lang="pt-BR">
      <body>
        <Script
          id="site-jsonld"
          type="application/ld+json"
          strategy="beforeInteractive"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(siteJsonLd) }}
        />
        {gaMeasurementId ? (
          <>
            <Script
              src={`https://www.googletagmanager.com/gtag/js?id=${gaMeasurementId}`}
              strategy="beforeInteractive"
            />
            <Script id="google-analytics" strategy="beforeInteractive">
              {`
                window.dataLayer = window.dataLayer || [];
                function gtag(){dataLayer.push(arguments);}
                gtag('js', new Date());
                gtag('consent', 'default', {
                  analytics_storage: 'granted',
                  ad_storage: 'granted',
                  ad_user_data: 'granted',
                  ad_personalization: 'granted'
                });
                gtag('config', '${gaMeasurementId}');
              `}
            </Script>
          </>
        ) : null}
        {clarityProjectId ? (
          <Script id="microsoft-clarity" strategy="afterInteractive">
            {`
              (function(c,l,a,r,i,t,y){
                c[a]=c[a]||function(){(c[a].q=c[a].q||[]).push(arguments)};
                t=l.createElement(r);t.async=1;t.src="https://www.clarity.ms/tag/"+i;
                y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y);
              })(window, document, "clarity", "script", "${clarityProjectId}");
            `}
          </Script>
        ) : null}
        {metaPixelId ? (
          <Script id="meta-pixel" strategy="afterInteractive">
            {`
              !function(f,b,e,v,n,t,s)
              {if(f.fbq)return;n=f.fbq=function(){n.callMethod?
              n.callMethod.apply(n,arguments):n.queue.push(arguments)};
              if(!f._fbq)f._fbq=n;n.push=n;n.loaded=!0;n.version='2.0';
              n.queue=[];t=b.createElement(e);t.async=!0;
              t.src=v;s=b.getElementsByTagName(e)[0];
              s.parentNode.insertBefore(t,s)}(window, document,'script',
              'https://connect.facebook.net/en_US/fbevents.js');
              fbq('init', '${metaPixelId}');
              fbq('track', 'PageView');
            `}
          </Script>
        ) : null}
        <Script id="landing-conversion-analytics" strategy="afterInteractive">
          {`
            (function() {
              var planPrices = {
                "offline-mensal": 29.9,
                "offline-anual": 229.9,
                "online-mensal": 149,
                "online-anual": 1399
              };
              var adminAnalyticsUrl = ${JSON.stringify(adminAnalyticsUrl)};
              var urlParams = new URLSearchParams(window.location.search);

              function storedId(storage, key, prefix) {
                try {
                  var existing = storage.getItem(key);
                  if (existing) return existing;
                  var next = prefix + "-" + Date.now().toString(36) + "-" + Math.random().toString(36).slice(2, 12);
                  storage.setItem(key, next);
                  return next;
                } catch {
                  return prefix + "-volatile-" + Math.random().toString(36).slice(2, 12);
                }
              }

              var visitorId = storedId(window.localStorage, "bl_site_visitor_id", "visitor");
              var sessionId = storedId(window.sessionStorage, "bl_site_session_id", "session");

              function safeUrl(href) {
                try {
                  return new URL(href, window.location.href);
                } catch {
                  return null;
                }
              }

              function planFromForm(form) {
                var input = form && form.querySelector ? form.querySelector('input[name="plan"]') : null;
                return input ? String(input.value || "").trim() : "";
              }

              function splitPlan(plan) {
                var parts = String(plan || "").split("-");
                return {
                  plan: parts[0] || "unknown",
                  billing: parts[1] || "unknown"
                  };
              }

              function adminEventType(eventName) {
                if (eventName === "plan_checkout_click") return "checkout.started";
                if (eventName === "trial_download_click") return "trial.download";
                if (eventName === "whatsapp_click") return "whatsapp.click";
                if (eventName === "plans_view_click") return "plan.view";
                return "";
              }

              function sendAdminAnalytics(type, params) {
                if (!adminAnalyticsUrl || !type) return;
                var payload = Object.assign({
                  type: type,
                  visitorId: visitorId,
                  sessionId: sessionId,
                  path: window.location.pathname + window.location.search,
                  url: window.location.href,
                  referrer: document.referrer || "",
                  source: urlParams.get("utm_source") || "site",
                  campaign: urlParams.get("utm_campaign") || ""
                }, params || {});
                var contentId = Array.isArray(payload.content_ids) ? payload.content_ids[0] : "";
                if (!payload.plan && contentId) payload.plan = contentId;
                payload.amountCents = Math.round(Number(payload.value || 0) * 100);

                try {
                  var body = JSON.stringify(payload);
                  if (navigator.sendBeacon) {
                    navigator.sendBeacon(adminAnalyticsUrl, new Blob([body], { type: "application/json" }));
                    return;
                  }
                  fetch(adminAnalyticsUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: body,
                    keepalive: true
                  }).catch(function() {});
                } catch {}
              }

              function publish(eventName, params, metaStandardEvent) {
                var payload = Object.assign({
                  event_category: "landing",
                  page_location: window.location.href
                }, params || {});

                window.dataLayer = window.dataLayer || [];
                window.dataLayer.push(Object.assign({ event: eventName }, payload));

                if (window.gtag) {
                  window.gtag("event", eventName, payload);
                }

                if (window.fbq) {
                  if (metaStandardEvent) {
                    window.fbq("track", metaStandardEvent, payload);
                  }
                  window.fbq("trackCustom", eventName, payload);
                }

                sendAdminAnalytics(adminEventType(eventName), payload);
              }

              sendAdminAnalytics("site.visit", {});

              document.addEventListener("submit", function(event) {
                var form = event.target;
                if (!form || !form.action || form.action.indexOf("/checkout") === -1) return;
                var checkoutPlan = planFromForm(form);
                var split = splitPlan(checkoutPlan);
                publish("plan_checkout_click", {
                  content_name: "Balcão Livre PDV " + split.plan,
                  content_category: "planos",
                  content_ids: [checkoutPlan],
                  plan: split.plan,
                  billing: split.billing,
                  value: planPrices[checkoutPlan] || 0,
                  currency: "BRL"
                }, "InitiateCheckout");
              }, true);

              document.addEventListener("click", function(event) {
                var target = event.target && event.target.closest
                  ? event.target.closest("a,button,[data-analytics-action]")
                  : null;
                if (!target) return;

                var href = target.getAttribute && target.getAttribute("href") || "";
                var action = target.dataset ? target.dataset.analyticsAction || "" : "";

                if (action === "trial_download" || href.indexOf("/trial-download") !== -1) {
                  var trialUrl = safeUrl(href);
                  var trialPlan = (target.dataset && target.dataset.analyticsPlan)
                    || (trialUrl && trialUrl.searchParams.get("plan"))
                    || "offline";
                  publish("trial_download_click", {
                    content_name: "Teste " + trialPlan + " 7 dias",
                    content_category: "teste_7_dias",
                    content_ids: [trialPlan],
                    plan: trialPlan,
                    trial_days: 7,
                    currency: "BRL",
                    value: 0
                  }, "Lead");
                  return;
                }

                if (action === "whatsapp_click" || href.indexOf("wa.me/") !== -1) {
                  publish("whatsapp_click", {
                    content_name: "WhatsApp comercial",
                    content_category: "contato",
                    seller: target.dataset ? target.dataset.analyticsSeller || "" : "",
                    location: target.dataset ? target.dataset.analyticsLocation || "" : ""
                  }, "Contact");
                  return;
                }

                if (action === "plans_view_click" || href.indexOf("#planos") !== -1) {
                  publish("plans_view_click", {
                    content_name: "Planos Balcão Livre PDV",
                    content_category: "planos",
                    location: target.dataset ? target.dataset.analyticsLocation || "" : ""
                  }, "ViewContent");
                }
              }, true);
            })();
          `}
        </Script>
        {metaPixelId ? (
          <noscript>
            <img
              height="1"
              width="1"
              style={{ display: "none" }}
              src={`https://www.facebook.com/tr?id=${metaPixelId}&ev=PageView&noscript=1`}
              alt=""
            />
          </noscript>
        ) : null}
        {children}
      </body>
    </html>
  );
}
