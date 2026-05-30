import "./globals.css";
import Script from "next/script";

export const metadata = {
  title: "Balcão Livre PDV | Sistema Windows para restaurantes",
  description:
    "Balcão Livre PDV para restaurantes, bares e lanchonetes. Mesas, balcão, delivery, Pix, estoque, relatórios e impressão pela impressora do Windows.",
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
            fbq('init', '1013239371045485');
            fbq('track', 'PageView');

            document.addEventListener('submit', function(event) {
              var form = event.target;
              if (form && form.action && form.action.indexOf('/checkout') !== -1 && window.fbq) {
                fbq('track', 'InitiateCheckout');
              }
            }, true);

            document.addEventListener('click', function(event) {
              var link = event.target && event.target.closest ? event.target.closest('a[href*="/checkout"]') : null;
              if (link && window.fbq) {
                fbq('track', 'InitiateCheckout');
              }
            }, true);
          `}
        </Script>
        <noscript>
          <img
            height="1"
            width="1"
            style={{ display: "none" }}
            src="https://www.facebook.com/tr?id=1013239371045485&ev=PageView&noscript=1"
            alt=""
          />
        </noscript>
        {children}
      </body>
    </html>
  );
}
