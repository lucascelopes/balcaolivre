import { sellers } from "./siteLinks";

const navLinks = [
  ["Produto", "/#produto"],
  ["Instaladores", "/#instaladores"],
  ["Demo PDV", "/#demo-pdv"],
  ["Impressao", "/#impressao"],
  ["Operacao", "/#operacao"],
  ["Planos", "/#planos"],
  ["Login", "https://pdv.balcaolivrepdv.com.br"]
];

export default function SiteHeader({ id }) {
  return (
    <header className="lpHeader" id={id}>
      <a className="lpBrand" href="/#inicio" aria-label="Balcao Livre PDV">
        <img className="lpBrandIcon" src="/brand/bl-modern-icon.png" alt="" aria-hidden="true" />
        <span className="lpBrandText">
          <strong>Balcao Livre</strong>
          <small>PDV Para Restaurantes</small>
        </span>
      </a>
      <nav className="lpNav" aria-label="Navegacao principal">
        {navLinks.map(([label, href]) => (
          <a
            key={label}
            href={href}
            data-analytics-action={href.includes("#planos") ? "plans_view_click" : undefined}
            data-analytics-location={href.includes("#planos") ? "header_nav" : undefined}
          >
            {label}
          </a>
        ))}
      </nav>
      <div className="lpHeaderActions">
        <a
          className="lpGhostButton"
          href={sellers[0].href}
          data-analytics-action="whatsapp_click"
          data-analytics-seller={sellers[0].name}
          data-analytics-location="header"
        >
          WhatsApp
        </a>
        <a
          className="lpSolidButton"
          href="/#planos"
          data-analytics-action="plans_view_click"
          data-analytics-location="header"
        >
          Planos
        </a>
      </div>
    </header>
  );
}
