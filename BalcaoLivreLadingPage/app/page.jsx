import CashierDemo from "./CashierDemo";
import PaymentSuccess from "./PaymentSuccess";
import SiteHeader from "./SiteHeader";
import { checkoutFunctionUrl, downloadUrl, onlineDownloadUrl, sellers } from "./siteLinks";
import { absoluteUrl, defaultDescription, defaultTitle, siteName, siteUrl } from "./seo";

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
    label: "Hibrido Online",
    title: "Balcao Livre PDV Hibrido Online",
    description: "Para restaurante que quer caixa local, PDV web, cardapio, garcom e integracoes sem WhatsApp.",
    monthly: "R$ 139,00",
    annual: "R$ 1.390,00",
    features: [
      "PDV Windows local com sincronizacao em nuvem",
      "PDV web e acesso por dispositivos moveis",
      "Cardapio digital e pedidos online",
      "Garcom no celular em tempo real",
      "Entrega por zona com taxa configuravel",
      "iFood e Mercado Pago conforme escopo",
      "Sincronizacao entre caixa, web, atendimento e cozinha",
      "Nao inclui atendimento/pedidos por WhatsApp"
    ],
    featured: true
  },
  {
    id: "complete",
    order: "Plano 3",
    label: "Completo",
    title: "Completo com Integracoes",
    description: "Mesmo pacote do Hibrido Online, adicionando atendimento e pedidos pelo WhatsApp.",
    monthly: "R$ 179,00",
    annual: "R$ 1.790,00",
    features: [
      "PDV Windows local com sincronizacao em nuvem",
      "PDV web e acesso por dispositivos moveis",
      "Cardapio digital e pedidos online",
      "Garcom no celular em tempo real",
      "Entrega por zona com taxa configuravel",
      "iFood e Mercado Pago conforme escopo",
      "Sincronizacao entre caixa, web, atendimento e cozinha",
      "Inclui atendimento e pedidos pelo WhatsApp"
    ],
    featured: true
  },
  {
    id: "custom",
    order: "Plano 4",
    label: "Consultar",
    title: "Personalizado",
    description: "Para loja que precisa de fluxo especial, multiplas unidades, migracao, relatorios ou integracoes sob medida.",
    monthly: "Consultar",
    annual: "Consultar",
    custom: {
      label: "Projeto sob medida",
      text: "Avaliamos o escopo no WhatsApp e fechamos o melhor formato para sua operacao."
    },
    features: [
      "Configuracoes especiais para a rotina da loja",
      "Multiloja ou operacao com varias unidades",
      "Migracao maior de dados e cadastros",
      "Relatorios customizados",
      "Integracao fiscal conforme necessidade",
      "Cardapio amplo e regras especificas",
      "Automacoes e fluxos especiais sob escopo",
      "Implantacao combinada com o vendedor"
    ],
    whatsappOnly: true
  }
];

const faqs = [
  ["Qual plano eu escolho?", "Offline e para caixa local no Windows. Hibrido Online inclui web, cardapio, garcom, iFood e Mercado Pago sem WhatsApp. Completo adiciona WhatsApp."],
  ["Funciona sem internet?", "No Offline, sim: o caixa continua vendendo localmente. Recursos online, nuvem e integracoes precisam de internet."],
  ["O sistema emite nota fiscal?", "O comprovante do PDV e operacional e nao substitui documento fiscal. Emissao fiscal ou integracao fiscal deve ser consultada conforme sua cidade e estado."],
  ["Da para personalizar?", "Sim. Fluxo, cardapio, entregas, impressao, usuarios e relatorios podem ser ajustados. Personalizacoes tem valores sob consulta."],
  ["Tem WhatsApp, iFood e garcom?", "Garcom, web, iFood e Mercado Pago entram no Hibrido Online conforme escopo. WhatsApp entra no plano Completo."],
  ["Posso usar em mais de um computador?", "Offline e licenca por computador. Online pode conectar equipe, web e dispositivos conforme plano e configuracao combinada."],
  ["Como funciona instalacao e suporte?", "Orientamos instalacao Windows, ativacao, primeiros cadastros, impressora, pagamentos e uso do caixa. Migracoes e integracoes especiais sao combinadas."],
  ["Posso testar antes de contratar?", "Sim. A pagina tem demo do caixa e o instalador Windows para conhecer o fluxo. Para Online, chame no WhatsApp e alinhamos seu cenario."]
];

const landingJsonLd = [
  {
    "@context": "https://schema.org",
    "@type": "SoftwareApplication",
    name: siteName,
    applicationCategory: "BusinessApplication",
    operatingSystem: "Windows",
    url: siteUrl,
    image: absoluteUrl("/brand/pdv-online-screen.png"),
    description: defaultDescription,
    offers: plans
      .filter((plan) => !plan.whatsappOnly)
      .flatMap((plan) => [
        {
          "@type": "Offer",
          name: `${plan.title} mensal`,
          price: plan.monthly.replace("R$ ", "").replace(".", "").replace(",", "."),
          priceCurrency: "BRL",
          availability: "https://schema.org/InStock",
          url: `${siteUrl}/#planos`
        },
        {
          "@type": "Offer",
          name: `${plan.title} anual`,
          price: plan.annual.replace("R$ ", "").replace(".", "").replace(",", "."),
          priceCurrency: "BRL",
          availability: "https://schema.org/InStock",
          url: `${siteUrl}/#planos`
        }
      ])
  },
  {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: faqs.map(([question, answer]) => ({
      "@type": "Question",
      name: question,
      acceptedAnswer: {
        "@type": "Answer",
        text: answer
      }
    }))
  },
  {
    "@context": "https://schema.org",
    "@type": "WebPage",
    name: defaultTitle,
    url: siteUrl,
    description: defaultDescription,
    inLanguage: "pt-BR"
  }
];

function SellerLinks() {
  return (
    <div className="lpSellerBox">
      <span>Comprar no WhatsApp</span>
      {sellers.map((seller) => (
        <a
          key={seller.name}
          href={seller.href}
          data-analytics-action="whatsapp_click"
          data-analytics-seller={seller.name}
          data-analytics-location="seller_box"
        >
          {seller.name} {seller.phone}
        </a>
      ))}
    </div>
  );
}

export default function Page({ searchParams }) {
  const checkoutSessionId = searchParams?.checkout === "sucesso" ? searchParams?.session_id : "";

  if (checkoutSessionId) {
    return <PaymentSuccess sessionId={checkoutSessionId} />;
  }

  return (
    <main className="lpPage">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(landingJsonLd) }}
      />
      <SiteHeader id="inicio" />

      <section className="lpHero lpHeroProduct lpHeroDark">
        <div className="lpHeroCopy">
          <p className="lpKicker">PDV online e offline para restaurante</p>
          <h1>O PDV que coloca seu restaurante no controle.</h1>
          <p className="lpLead">
            Caixa, mesas, delivery, estoque, garcom web, iFood, WhatsApp e Mercado Pago em uma rotina so.
          </p>
          <div className="lpHeroActions">
            <a
              className="lpSolidButton lpLargeButton"
              href="#planos"
              data-analytics-action="plans_view_click"
              data-analytics-location="hero"
            >
              Ver planos e comprar
            </a>
            <a className="lpGhostButton lpLargeButton" href="#beneficios">Ver beneficios</a>
            <a className="lpGhostButton lpLargeButton" href="#demo-pdv">Testar demo</a>
          </div>
          <dl className="lpHeroStats" aria-label="Resumo do produto">
            <div><dt>Offline e online</dt><dd>caixa local com operacao conectada quando precisar</dd></div>
            <div><dt>Restaurantes</dt><dd>feito para bar, lanchonete, pizzaria, acai e delivery</dd></div>
            <div><dt>Venda clara</dt><dd>pedido, pagamento, estoque e comprovante no mesmo fluxo</dd></div>
          </dl>
        </div>

        <div className="lpHeroVisual" aria-label="Tela real do Balcao Livre PDV">
          <div className="lpVisualGlow" aria-hidden="true"></div>
          <div className="lpLaptopMock">
            <div className="lpLaptopTop">
              <span>Balcao Livre PDV Online</span>
              <b>Caixa aberto</b>
            </div>
            <div
              className="lpLaptopScreen"
              role="img"
              aria-label="Tela atual do modo guia do Balcao Livre PDV"
              style={{ "--screen-image": "url('/guide/windows-pdv/01-comandas-mesas.png')" }}
            />
          </div>
          <div className="lpPhoneMock" aria-label="Resumo no celular">
            <span>Pedidos</span>
            <strong>Mesa 03</strong>
            <p>Pedido enviado para o caixa em tempo real.</p>
            <b>Garcom web</b>
          </div>
          <div className="lpHeroTiles" aria-label="Modulos principais">
            <span><b>iFood</b>pedido no fluxo</span>
            <span><b>WhatsApp</b>atendimento comercial</span>
            <span><b>Mercado Pago</b>Pix e cartao</span>
          </div>
        </div>
      </section>

      <section className="lpStrip" id="beneficios" aria-label="Diferenciais principais">
        <span><b>Venda offline</b>sem travar o caixa quando a internet cai</span>
        <span><b>Fechamento claro</b>dinheiro, Pix, cartao e retiradas</span>
        <span><b>Comanda simples</b>mesa, balcao, delivery e fiado</span>
        <span><b>Estoque controlado</b>baixa automatica e alerta de minimo</span>
      </section>

      <section className="lpSection lpInstallerSection" id="instaladores">
        <div className="lpSectionHead">
          <p className="lpKicker">Instaladores e teste</p>
          <h2>Baixe o instalador e teste o PDV por 7 dias.</h2>
          <p>Teste caixa, mesas, estoque, impressao e Mercado Pago. WhatsApp e iFood entram somente no plano pago.</p>
        </div>
        <div className="lpInstallerGrid">
          <article className="lpInstallerCard">
            <div className="lpInstallerTop"><span>Offline</span><b>Caixa local</b></div>
            <h3>Instalador PDV Offline</h3>
            <p>Para testar o caixa Windows local, vendas, mesas, estoque, pagamentos e impressao sem depender da internet.</p>
            <a
              className="lpSolidButton lpLargeButton"
              href={downloadUrl}
              data-analytics-action="trial_download"
              data-analytics-plan="offline"
            >
              Baixar instalador Offline
            </a>
            <div className="lpTrialFlow">
              <span>Teste completo do caixa</span>
              <p>Venda no Windows, imprima comprovante, controle estoque e teste Mercado Pago no fluxo do PDV.</p>
            </div>
          </article>
          <article className="lpInstallerCard lpInstallerCardOnline">
            <div className="lpInstallerTop"><span>Online</span><b>Conectado</b></div>
            <h3>Instalador PDV Online</h3>
            <p>Para testar PDV conectado, cardapio, web, sincronizacao e equipe. iFood e WhatsApp sao liberados somente em plano pago.</p>
            <a
              className="lpSolidButton lpLargeButton"
              href={onlineDownloadUrl}
              data-analytics-action="trial_download"
              data-analytics-plan="online"
            >
              Baixar instalador Online
            </a>
            <div className="lpTrialFlow">
              <span>Teste conectado</span>
              <p>Use PDV web, cardapio, sincronizacao e Mercado Pago. WhatsApp e iFood ficam para contratacao paga.</p>
            </div>
            <p className="lpPaidOnlyNote">Mercado Pago disponivel para teste no PDV. WhatsApp, iFood, homologacao e automacoes conectadas entram apenas depois da contratacao paga.</p>
          </article>
        </div>
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

        <div className="lpPlanCompare">
          {plans.map((plan) => (
            <article className={`lpPlanColumn ${plan.featured ? "lpPlanColumnFeatured" : ""}`} key={plan.id}>
              <div className="lpPlanColumnHead">
                <span>{plan.order}</span>
                <b>{plan.label}</b>
                <h3>{plan.title}</h3>
                <p>{plan.description}</p>
              </div>
              <div className="lpPlanColumnPrice">
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
                {plan.features.slice(0, 6).map((feature) => <li key={feature}>{feature}</li>)}
              </ul>
              {plan.whatsappOnly ? (
                <a
                  className="lpPlanButton"
                  href={sellers[0].href}
                  data-analytics-action="whatsapp_click"
                  data-analytics-seller={sellers[0].name}
                  data-analytics-location="plan_custom"
                  data-analytics-plan={plan.id}
                >
                  Consultar no WhatsApp
                </a>
              ) : (
                <div className="lpPlanActions">
                  <form
                    action={checkoutFunctionUrl}
                    method="post"
                    data-analytics-action="plan_checkout"
                    data-analytics-plan={plan.id}
                    data-analytics-billing="mensal"
                  >
                    <input type="hidden" name="plan" value={`${plan.id}-mensal`} />
                    <button className="lpPlanButton" type="submit">Comprar mensal</button>
                  </form>
                  <form
                    action={checkoutFunctionUrl}
                    method="post"
                    data-analytics-action="plan_checkout"
                    data-analytics-plan={plan.id}
                    data-analytics-billing="anual"
                  >
                    <input type="hidden" name="plan" value={`${plan.id}-anual`} />
                    <button className="lpPlanButton lpPlanButtonSecondary" type="submit">Comprar anual</button>
                  </form>
                </div>
              )}
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
          <img className="lpBrandIcon" src="/brand/bl-modern-icon.png" alt="" aria-hidden="true" />
          <span className="lpBrandText">
            <strong>Balcao Livre</strong>
            <small>PDV Para Restaurantes</small>
          </span>
        </div>
        <div className="lpFooterPitch">
          <b>PDV Windows para restaurante vender, imprimir e fechar caixa sem depender de gambiarra.</b>
          <span>Offline para caixa local. Online para web, zonas e garcom. iFood e WhatsApp somente no plano pago.</span>
        </div>
        <div className="lpFooterColumn">
          <b>Produto</b>
          <a href="#produto">Recursos</a>
          <a href="#demo-pdv">Demo PDV</a>
          <a href="#impressao">Impressao</a>
          <a href="#operacao">Operacao</a>
          <a
            href="#planos"
            data-analytics-action="plans_view_click"
            data-analytics-location="footer"
          >
            Planos
          </a>
        </div>
        <div className="lpFooterColumn">
          <b>Suporte</b>
          <a href="/como-usar/">Como usar</a>
          <a href="/termos/">Termos e condicoes</a>
          <a href="https://pdv.balcaolivrepdv.com.br">Login do PDV</a>
        </div>
        <div className="lpFooterWhatsapp">
          <b>Compre no WhatsApp</b>
          <a
            href={sellers[0].href}
            data-analytics-action="whatsapp_click"
            data-analytics-seller={sellers[0].name}
            data-analytics-location="footer"
          >
            Vendedor Wender: {sellers[0].phone}
          </a>
          <a
            href={sellers[1].href}
            data-analytics-action="whatsapp_click"
            data-analytics-seller={sellers[1].name}
            data-analytics-location="footer"
          >
            Vendedor Lucas: {sellers[1].phone}
          </a>
        </div>
      </footer>
      <a
        className="lpFloatingWhatsapp"
        href={sellers[1].href}
        aria-label="Falar com Lucas no WhatsApp"
        data-analytics-action="whatsapp_click"
        data-analytics-seller={sellers[1].name}
        data-analytics-location="floating_button"
      >
        <span>WhatsApp</span>
        <strong>Falar com Lucas</strong>
      </a>
    </main>
  );
}
