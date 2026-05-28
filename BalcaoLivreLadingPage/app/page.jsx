import CashierDemo from "./CashierDemo";
import SiteHeader from "./SiteHeader";
import { downloadUrl, sellers } from "./siteLinks";

const modules = [
  ["01", "Caixa e pagamentos", "Venda por codigo, quantidade, desconto, Pix, dinheiro, credito, debito e troco calculado."],
  ["02", "Mesas e comandas", "Controle de mesa, garcom, ficha de balcao, delivery, observacoes e impressao do pedido."],
  ["03", "Delivery e retirada", "Cliente, telefone, endereco, taxa por zona, status do pedido e comprovante organizado."],
  ["04", "Estoque e margem", "Preco de compra, preco de venda, lucro previsto, entrada, saida e alerta de produto baixo."],
  ["05", "Relatorios", "Resumo do dia, vendas por forma de pagamento, produtos vendidos, caixa e estoque critico."],
  ["06", "Impressao", "Comprovante simples, pedido para cozinha, fechamento e impressora padrao do Windows."]
];

const flow = [
  ["1", "Abre o caixa", "Operador informa valor inicial e comeca a vender com atalhos de teclado."],
  ["2", "Lanca produtos", "Codigo, busca, quantidade e agrupamento rapido para reduzir erro no atendimento."],
  ["3", "Recebe e imprime", "Pix, cartao e dinheiro ficam registrados com comprovante para o cliente."],
  ["4", "Fecha o dia", "Resumo do caixa mostra entradas, retiradas, vendas e pendencias antes de fechar."]
];

const plans = [
  {
    id: "offline",
    order: "Plano 1",
    label: "Offline",
    title: "Balcao Livre PDV Offline",
    description: "Para loja que quer o caixa Windows vendendo sem depender de internet.",
    monthly: "R$ 17,00",
    annual: "R$ 200,00",
    features: [
      "Venda local no Windows sem depender da internet",
      "Mesas, balcao, delivery e comandas",
      "Pix, dinheiro, credito, debito e comprovante",
      "Estoque, margem, fechamento e relatorios",
      "Licenca por computador com instalador Windows"
    ]
  },
  {
    id: "online",
    order: "Plano 2",
    label: "Online",
    title: "Balcao Livre PDV Online",
    description: "Para restaurante que quer PDV, web, WhatsApp, delivery e equipe conectados, com ajustes para a rotina da loja.",
    monthly: "Consultar",
    annual: "Menos de R$ 999,00",
    custom: {
      label: "Configuravel e personalizavel",
      text: "Se sua operacao precisa de algo diferente, montamos o fluxo com voce. Valores sob consulta."
    },
    features: [
      "Pedidos do iFood entrando no sistema",
      "Atendimento e pedidos pelo WhatsApp",
      "Pode usar tambem pela web",
      "Entrega por zona com taxa configuravel",
      "Cardapio, atendimento, entrega e impressao ajustados ao seu jeito",
      "Garcom no celular em tempo real",
      "Sincronizacao entre caixa, web, atendimento e cozinha",
      "Pedidos online, mesas, comandas e visao gerencial"
    ],
    featured: true,
    whatsappOnly: true
  }
];

const faqs = [
  ["Qual plano eu escolho?", "Offline e para caixa local no Windows. Online e para quem quer web, WhatsApp, iFood, garcom no celular e sincronizacao."],
  ["Funciona sem internet?", "No Offline, sim: o caixa continua vendendo localmente. Recursos online, nuvem e integracoes precisam de internet."],
  ["O sistema emite nota fiscal?", "O comprovante do PDV e operacional e nao substitui documento fiscal. Emissao fiscal ou integracao fiscal deve ser consultada conforme sua cidade e estado."],
  ["Da para personalizar?", "Sim. Fluxo, cardapio, entregas, impressao, usuarios e relatorios podem ser ajustados. Personalizacoes tem valores sob consulta."],
  ["Tem WhatsApp, iFood e garcom?", "No Online, sim, conforme contratacao e configuracao das contas. Algumas integracoes dependem de aprovacao e credenciais do terceiro."],
  ["Posso usar em mais de um computador?", "Offline e licenca por computador. Online pode conectar equipe, web e dispositivos conforme plano e configuracao combinada."],
  ["Como funciona instalacao e suporte?", "Orientamos instalacao Windows, ativacao, primeiros cadastros, impressora, pagamentos e uso do caixa. Migracoes e integracoes especiais sao combinadas."],
  ["Posso testar antes de contratar?", "Sim. A pagina tem demo do caixa e o instalador Windows para conhecer o fluxo. Para Online, chame no WhatsApp e alinhamos seu cenario."]
];

function SellerLinks() {
  return (
    <div className="lpSellerBox">
      <span>Comprar no WhatsApp</span>
      {sellers.map((seller) => (
        <a key={seller.name} href={seller.href}>
          {seller.name} {seller.phone}
        </a>
      ))}
    </div>
  );
}

export default function Page() {
  return (
    <main className="lpPage">
      <SiteHeader id="inicio" />

      <section className="lpHero lpHeroProduct">
        <div className="lpHeroCopy">
          <p className="lpKicker">PDV Windows e online para restaurante, bar e delivery</p>
          <h1>Balcao Livre PDV para caixa, comandas e delivery.</h1>
          <p className="lpLead">
            Venda no Windows, controle mesas e estoque, receba no Pix ou cartao e acompanhe a operacao sem depender de planilha.
          </p>
          <div className="lpHeroActions">
            <a className="lpSolidButton lpLargeButton" href={downloadUrl}>Baixar Windows</a>
            <a className="lpGhostButton lpLargeButton" href="#demo-pdv">Testar demo</a>
          </div>
          <dl className="lpHeroStats" aria-label="Resumo do produto">
            <div><dt>Offline</dt><dd>caixa vendendo mesmo sem internet</dd></div>
            <div><dt>Online</dt><dd>iFood, WhatsApp, web e garcom</dd></div>
            <div><dt>Instalador</dt><dd>pronto para computador Windows</dd></div>
          </dl>
        </div>

        <div className="lpSystemPanel lpHeroConsole" aria-label="Previa visual do sistema">
          <div className="lpConsoleBar">
            <span>Caixa aberto</span>
            <strong>R$ 184,00</strong>
          </div>
          <div className="lpConsoleBody">
            <section className="lpOrderPane">
              <div className="lpOrderHeader">
                <span>Comanda 000012</span>
                <b>Mesa 12 | Garcom 02</b>
              </div>
              <div className="lpOrderRows">
                <p><span>000003</span><strong>X-Burger</strong><b>R$ 18,00</b></p>
                <p><span>000005</span><strong>Batata frita</strong><b>R$ 14,00</b></p>
                <p><span>000004</span><strong>Suco natural</strong><b>R$ 9,00</b></p>
              </div>
              <div className="lpOrderTotal">
                <span>Total</span>
                <strong>R$ 41,00</strong>
              </div>
            </section>
            <section className="lpCheckoutPane">
              <div>
                <span>Pagamento</span>
                <strong>Pix</strong>
              </div>
              <div>
                <span>Recebido</span>
                <strong>R$ 41,00</strong>
              </div>
              <div>
                <span>Troco</span>
                <strong>R$ 0,00</strong>
              </div>
              <button type="button">Finalizar venda</button>
            </section>
          </div>
          <div className="lpConsoleFooter">
            <span>Estoque baixado automaticamente</span>
            <b>Comprovante pronto para imprimir</b>
          </div>
        </div>
      </section>

      <section className="lpStrip" aria-label="Diferenciais principais">
        <span><b>Venda offline</b>sem travar o caixa quando a internet cai</span>
        <span><b>Fechamento claro</b>dinheiro, Pix, cartao e retiradas</span>
        <span><b>Comanda simples</b>mesa, balcao, delivery e fiado</span>
        <span><b>Estoque controlado</b>baixa automatica e alerta de minimo</span>
      </section>

      <div id="demo-pdv" className="lpDemoReturn">
        <CashierDemo />
      </div>

      <section className="lpSection" id="produto">
        <div className="lpSectionHead">
          <p className="lpKicker">Produto</p>
          <h2>Um PDV para a rotina inteira da loja.</h2>
          <p>Interface direta para operador de caixa, gerente, garcom e dono acompanharem o que importa no dia.</p>
        </div>
        <div className="lpModuleGrid">
          {modules.map(([number, title, text]) => (
            <article className="lpModule" key={number}>
              <span>{number}</span>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="lpSection lpOperation" id="operacao">
        <div className="lpSectionHead">
          <p className="lpKicker">Operacao</p>
          <h2>Do pedido ao fechamento, sem tela enfeitada demais.</h2>
        </div>
        <div className="lpFlowGrid">
          {flow.map(([number, title, text]) => (
            <article key={number}>
              <b>{number}</b>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="printSection lpPrintReturn" id="impressao">
        <div className="receiptMock" data-print-receipt>
          <h3>BALCAO LIVRE PDV</h3>
          <p>COMPROVANTE DO PDV</p>
          <div className="receiptMeta">
            <span>COMANDA 000012</span>
            <span>GARCOM: 2</span>
            <span>OPERADOR: CAIXA</span>
          </div>
          <div className="receiptInstruction">
            <strong data-print-title>Monte a venda no PDV demo acima.</strong>
            <span data-print-subtitle>Ao finalizar, a pagina desce para o comprovante com os produtos testados.</span>
          </div>
          <div className="receiptProducts" data-print-products />
          <div className="receiptRows receiptTotals">
            <span>TOTAL</span><strong data-print-total>R$ 0,00</strong>
            <span>PAGAMENTO</span><strong data-print-payment>aguardando</strong>
            <span>TROCO</span><strong data-print-change>R$ 0,00</strong>
          </div>
          <div className="receiptControl" data-print-control>CONTROLE GERADO NA VENDA</div>
          <div className="qrMock" aria-label="QR Code de exemplo">
            {[
              1,1,1,0,1,0,1,
              1,0,1,1,0,1,0,
              1,1,1,0,1,1,1,
              0,1,0,1,1,0,1,
              1,0,1,0,1,1,0,
              0,1,1,1,0,1,1,
              1,0,1,1,1,0,1
            ].map((cell, index) => <i className={cell ? "on" : ""} key={index} />)}
          </div>
          <small data-print-qr-label>QR Pix, Instagram, mapa ou link opcional</small>
        </div>
        <div className="printCopy">
          <p className="eyebrow">Impressao</p>
          <h2>Comprovante grande, legivel e pronto para a impressora da loja.</h2>
          <p>O recibo usa os dados configurados da empresa, mostra operador ou garcom, calcula troco, imprime fechamento de caixa e pode incluir QR Code quando a loja quiser.</p>
          <div className="pillList">
            <span>Impressora padrao do Windows</span>
            <span>Termica 58/80mm</span>
            <span>USB, rede ou compartilhada</span>
            <span>Resumo do dia</span>
            <span>QR opcional</span>
          </div>
          <div className="printOperations" aria-label="Rotina de impressao no PDV">
            <article><b>01</b><div><strong>Recebeu pagamento</strong><span>O comprovante abre igual na demo e ja pode sair na impressora.</span></div></article>
            <article><b>02</b><div><strong>Pix com valor</strong><span>O QR usa o total da comanda, sem o cliente digitar valor.</span></div></article>
            <article><b>03</b><div><strong>Dados da loja</strong><span>Nome, CNPJ, endereco e telefone vem das configuracoes.</span></div></article>
            <article><b>04</b><div><strong>Fechamento do dia</strong><span>Ao fechar caixa, imprime resumo das vendas e movimentos.</span></div></article>
          </div>
          <div className="printerStatus">
            <span>Demo da impressao</span>
            <strong>Comprovante grande + QR centralizado + troco calculado</strong>
          </div>
        </div>
      </section>

      <section className="lpSection lpPlansSection" id="planos">
        <div className="lpSectionHead">
          <p className="lpKicker">Planos</p>
          <h2>Escolha offline para caixa local ou online para operacao conectada.</h2>
          <p>Os dois planos mantem o foco no PDV. A diferenca esta no nivel de integracao e acompanhamento em tempo real.</p>
        </div>

        <div className="lpPlans">
          {plans.map((plan) => (
            <article className={`lpPlan ${plan.featured ? "lpPlanFeatured" : ""}`} key={plan.id}>
              <div className="lpPlanTop">
                <span>{plan.order}</span>
                <b>{plan.label}</b>
              </div>
              <h3>{plan.title}</h3>
              <p>{plan.description}</p>
            <div className="lpPriceRows">
              <div><span>Mensal</span><strong>{plan.monthly}</strong></div>
              <div><span>Anual</span><strong>{plan.annual}</strong></div>
            </div>
            {plan.custom ? (
              <div className="lpPlanCustom">
                <span>{plan.custom.label}</span>
                <strong>{plan.custom.text}</strong>
              </div>
            ) : null}
            <ul>
              {plan.features.map((feature) => <li key={feature}>{feature}</li>)}
            </ul>
              {plan.whatsappOnly ? (
                <>
                  <div className="lpPlanActions">
                    <a className="lpPlanButton" href={sellers[0].href}>Consultar no WhatsApp</a>
                  </div>
                  <div className="lpPlanActions">
                    <a className="lpPlanButton lpPlanButtonMuted" href={sellers[1].href}>Falar com Lucas</a>
                  </div>
                </>
              ) : (
                <>
                  <form action="/api/checkout" method="post" className="lpPlanActions">
                    <input type="hidden" name="plan" value={`${plan.id}-mensal`} />
                    <button className="lpPlanButton" type="submit">Pagar mensal na Stripe</button>
                  </form>
                  <form action="/api/checkout" method="post" className="lpPlanActions">
                    <input type="hidden" name="plan" value={`${plan.id}-anual`} />
                    <button className="lpPlanButton lpPlanButtonMuted" type="submit">Pagar anual na Stripe</button>
                  </form>
                </>
              )}
              <SellerLinks />
            </article>
          ))}
        </div>
      </section>

      <section className="lpGuideBanner">
        <div>
          <p className="lpKicker">Implantacao</p>
          <h2>Guia de uso do app Windows</h2>
          <p>Pagina separada com o caminho para operar caixa, produtos, pagamentos, estoque e fechamento.</p>
        </div>
        <a className="lpSolidButton lpLargeButton" href="/como-usar/">Abrir guia</a>
      </section>

      <section className="lpSection lpFaq" id="faq">
        <div className="lpSectionHead">
          <p className="lpKicker">FAQ</p>
          <h2>Perguntas diretas antes de contratar.</h2>
        </div>
        <div className="lpFaqGrid">
          {faqs.map(([title, text]) => (
            <article key={title}>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <footer className="lpFooter">
        <div className="lpFooterBrand">
          <img className="lpBrandLogo lpFooterBrandLogo" src="/balcao-livre-logo-v2.png" alt="Balcao Livre PDV" />
        </div>
        <div className="lpFooterPitch">
          <b>PDV Windows para restaurante vender, imprimir e fechar caixa sem depender de gambiarra.</b>
          <span>Offline para caixa local. Online para iFood, WhatsApp, web, zonas e garcom em tempo real.</span>
        </div>
        <div className="lpFooterColumn">
          <b>Produto</b>
          <a href="#produto">Recursos</a>
          <a href="#demo-pdv">Demo PDV</a>
          <a href="#impressao">Impressao</a>
          <a href="#operacao">Operacao</a>
          <a href="#planos">Planos</a>
        </div>
        <div className="lpFooterColumn">
          <b>Suporte</b>
          <a href="/como-usar/">Como usar</a>
          <a href="/termos/">Termos e condicoes</a>
          <a href="https://pdv.balcaolivrepdv.com.br">Login do PDV</a>
        </div>
        <div className="lpFooterWhatsapp">
          <b>Compre no WhatsApp</b>
          <a href={sellers[0].href}>Vendedor Wender: {sellers[0].phone}</a>
          <a href={sellers[1].href}>Vendedor Lucas: {sellers[1].phone}</a>
        </div>
      </footer>
    </main>
  );
}
