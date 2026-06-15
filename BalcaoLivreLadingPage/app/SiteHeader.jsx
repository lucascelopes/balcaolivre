import { downloadUrl, sellers } from "./siteLinks";

const navLinks = [
  ["Produto", "/#inicio"],
  ["Recursos", "/#recursos"],
  ["Segmentos", "/#segmentos"],
  ["Planos", "/#planos"],
  ["Contato", "/#contato"]
];

const whatsappHref = sellers[0]?.href || "https://wa.me/5527981267551";

export default function SiteHeader({ id }) {
  return (
    <header className="blSiteHeader" id={id}>
      <a className="blHeaderBrand" href="/#inicio" aria-label="Balcão Livre PDV">
        <img src="/brand/bl-modern-icon.png" alt="" aria-hidden="true" />
        <span>
          <strong>Balcão Livre</strong>
          <small>PDV</small>
        </span>
      </a>

      <nav className="blHeaderNav" aria-label="Navegação principal">
        {navLinks.map(([label, href]) => (
          <a key={label} href={href}>
            {label}
          </a>
        ))}
      </nav>

      <div className="blHeaderActions">
        <a className="blHeaderLogin" href="https://pdv.balcaolivrepdv.com.br">
          Entrar
        </a>
        <a
          className="blHeaderTrial"
          href={downloadUrl}
          data-analytics-action="trial_download"
          data-analytics-location="header"
          data-analytics-plan="offline"
        >
          Testar grátis
        </a>
        <a
          className="blHeaderWhatsapp"
          href={whatsappHref}
          data-analytics-action="whatsapp_click"
          data-analytics-location="header"
        >
          WhatsApp
        </a>
      </div>
    </header>
  );
}
