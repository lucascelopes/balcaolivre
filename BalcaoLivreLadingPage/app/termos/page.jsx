export const metadata = {
  title: "Termos e condicoes | Balcao Livre PDV"
};

export default function TermsPage() {
  return (
    <main>
      <header className="topbar">
        <a className="brand" href="/" aria-label="Balcao Livre PDV">
          <img src="/balcao-livre-icon.png" alt="" />
          <span>Balcao Livre PDV</span>
        </a>
        <nav aria-label="Navegacao principal">
          <a href="/">Inicio</a>
          <a href="/#preco">Planos</a>
          <a href="/como-usar/">Como usar</a>
          <a href="/termos/">Termos</a>
          <a href="/pdv">Login</a>
        </nav>
        <a className="topbarAction" href="/#preco">Contratar</a>
      </header>

      <section className="infoPage">
        <div className="infoHero">
          <p className="eyebrow">Termos e condicoes</p>
          <h1>Regras de uso do Balcao Livre PDV.</h1>
          <p>Estes termos explicam a contratacao, licenca, suporte, uso do sistema e responsabilidades basicas do cliente.</p>
        </div>
        <div className="infoLayout">
          <aside className="infoAside" aria-label="Indice dos termos">
            <a href="#licenca">Licenca</a>
            <a href="#pagamento">Pagamento</a>
            <a href="#uso">Uso do sistema</a>
            <a href="#suporte">Suporte</a>
            <a href="#dados">Dados</a>
            <a href="#cancelamento">Cancelamento</a>
          </aside>
          <div className="infoContent">
            <section className="infoBlock" id="licenca"><h2>1. Licenca de uso</h2><p>O Balcao Livre PDV e licenciado para uso em restaurante, bar, lanchonete, evento ou operacao semelhante. A licenca nao transfere propriedade do software ao cliente.</p></section>
            <section className="infoBlock" id="pagamento"><h2>2. Pagamento e renovacao</h2><p>Os planos podem ser mensais ou anuais. A liberacao, renovacao e continuidade de uso dependem da confirmacao do pagamento do plano contratado.</p></section>
            <section className="infoBlock" id="uso"><h2>3. Uso correto do sistema</h2><ul><li>O cliente deve manter dados de produtos, operadores, impressoras e configuracoes corretos.</li><li>O comprovante gerado pelo PDV nao substitui documento fiscal quando a lei exigir emissao fiscal.</li><li>O cliente e responsavel por conferir valores, formas de pagamento e fechamento de caixa.</li></ul></section>
            <section className="infoBlock" id="suporte"><h2>4. Suporte e implantacao</h2><p>O suporte orienta instalacao, ativacao, uso das principais telas e ajustes operacionais. Demandas fora do funcionamento padrao podem exigir avaliacao separada.</p></section>
            <section className="infoBlock" id="dados"><h2>5. Dados e backups</h2><p>O cliente deve cuidar do equipamento, acesso ao Windows e copias de seguranca quando aplicavel. Em planos online, recursos conectados podem depender de internet e servicos de terceiros.</p></section>
            <section className="infoBlock" id="cancelamento"><h2>6. Cancelamento</h2><p>O cancelamento encerra renovacoes futuras. Valores ja pagos podem seguir as regras do meio de pagamento, promocao contratada ou acordo comercial vigente.</p></section>
          </div>
        </div>
      </section>
    </main>
  );
}
