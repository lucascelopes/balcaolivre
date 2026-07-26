import { List } from "@phosphor-icons/react/ssr";
import { downloadUrl } from "./siteLinks";

const links = [
  ["Recursos", "/#recursos"],
  ["Planos", "/#planos"],
  ["Clientes", "/#clientes"],
  ["Suporte", "/#suporte"],
  ["Contato", "/#contato"]
];

function Navigation() {
  return links.map(([label, href]) => <a key={label} href={href}>{label}</a>);
}

export default function SiteHeader({ id }) {
  return (
    <header className="bl2Header" id={id}>
      <div className="bl2HeaderInner">
        <a className="bl2Brand" href="/#inicio" aria-label="Balcão Livre PDV">
          <img src="/brand/bl-orange-icon.png" alt="" aria-hidden="true" />
          <span>Balcão Livre PDV</span>
        </a>

        <nav className="bl2Nav" aria-label="Navegação principal"><Navigation /></nav>

        <div className="bl2HeaderActions">
          <a className="bl2Login" href="https://pdv.balcaolivrepdv.com.br">Entrar</a>
          <a className="bl2Button bl2ButtonPrimary" href={downloadUrl} data-analytics-action="trial_download" data-analytics-location="header">Testar grátis</a>
        </div>

        <details className="bl2MobileMenu">
          <summary aria-label="Abrir menu"><List size={24} weight="bold" /></summary>
          <nav><Navigation /><a href="https://pdv.balcaolivrepdv.com.br">Entrar</a><a className="bl2Button bl2ButtonPrimary" href={downloadUrl}>Testar grátis</a></nav>
        </details>
      </div>
    </header>
  );
}
