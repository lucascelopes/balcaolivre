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
          <a key={label} href={href}>{label}</a>
        ))}
      </nav>
      <div className="lpHeaderActions">
        <a className="lpGhostButton" href={sellers[0].href}>WhatsApp</a>
        <a className="lpSolidButton" href="/#planos">Planos</a>
      </div>
    </header>
  );
}
