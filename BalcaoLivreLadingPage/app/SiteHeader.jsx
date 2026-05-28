import { downloadUrl, sellers } from "./siteLinks";

const navLinks = [
  ["Produto", "/#produto"],
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
        <img className="lpBrandLogo" src="/balcao-livre-logo-v2.png" alt="Balcao Livre PDV" />
      </a>
      <nav className="lpNav" aria-label="Navegacao principal">
        {navLinks.map(([label, href]) => (
          <a key={label} href={href}>{label}</a>
        ))}
      </nav>
      <div className="lpHeaderActions">
        <a className="lpGhostButton" href={sellers[0].href}>WhatsApp</a>
        <a className="lpSolidButton" href={downloadUrl}>Baixar Windows</a>
      </div>
    </header>
  );
}
