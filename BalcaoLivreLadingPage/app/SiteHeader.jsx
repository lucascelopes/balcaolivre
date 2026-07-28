import { sellers } from "./siteLinks";

const navLinks = [
  ["Soluções", "/#solucoes"],
  ["Recursos", "/#solucoes"],
  ["Planos", "/#planos"],
  ["Depoimentos", "/#depoimentos"],
  ["Suporte", "/#suporte"],
  ["Contato", "/#contato"]
];

const whatsappHref = sellers[0]?.href || "https://wa.me/5527981267551";

export default function SiteHeader({ id }) {
  return (
    <header className="lpHeader" id={id}>
      <a className="lpBrand" href="/#inicio" aria-label="Balcão Livre">
        <img src="/brand/bl-modern-icon.png" alt="" aria-hidden="true" />
        <span>
          <strong>Balcão Livre</strong>
        </span>
      </a>

      <nav className="lpNav" aria-label="Navegação principal">
        {navLinks.map(([label, href]) => (
          <a key={label} href={href}>
            {label}
          </a>
        ))}
      </nav>

      <div className="lpHeaderActions">
        <a className="lpHeaderLogin" href="https://app.balcaolivrepdv.com.br">
          Entrar
        </a>
        <a
          className="lpHeaderTrial"
          href="/#planos"
          data-analytics-action="plans_click"
          data-analytics-location="header"
          data-analytics-plan="balcao"
        >
          Ver planos
        </a>
        <a
          className="lpHeaderWhatsapp"
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
