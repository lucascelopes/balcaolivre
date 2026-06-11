import CashierDemo from "./CashierDemo";
import PaymentSuccess from "./PaymentSuccess";
import SiteHeader from "./SiteHeader";
import { checkoutFunctionUrl, downloadUrl, onlineDownloadUrl, sellers } from "./siteLinks";
import { absoluteUrl, defaultDescription, defaultTitle, siteName, siteUrl } from "./seo";

const modules = [
  ["01", "Caixa e pagamentos", "Venda no Windows com dinheiro, Pix, cartão, troco, comprovante e Point/Mercado Pago quando configurado."],
  ["02", "Mesas e comandas", "Controle mesa, comanda e balcão com consumo aberto, adicionais, observações e conta certa."],
  ["03", "Garçom no celular", "Equipe lança pedido sem voltar ao caixa, e a cozinha acompanha o movimento em tempo real."],
  ["04", "Delivery e iFood", "Pedidos com cliente, telefone, endereço, entregador, taxa, status e entrada por iFood conforme credenciais."],
  ["05", "Cardápio e WhatsApp", "Produtos publicados no cardápio digital e atendimento por WhatsApp no plano profissional."],
  ["06", "Estoque e NFC-e", "Preço de compra, venda, margem, estoque baixo e NFC-e configurável com os dados fiscais da loja."],
  ["07", "Equipe e relatórios", "Usuários, permissões, entregadores, fechamento, repasses, caixa e indicadores do dia."]
];

const flow = [
  ["1", "Abre o caixa", "Operador informa valor inicial e começa a vender com atalhos de teclado."],
  ["2", "Lança produtos", "Código, busca, quantidade e agrupamento rápido para reduzir erro no atendimento."],
  ["3", "Recebe e imprime", "Pix, cartão e dinheiro ficam registrados com comprovante para o cliente."],
  ["4", "Fecha o dia", "Resumo do caixa mostra entradas, retiradas, vendas e pendências antes de fechar."]
];

const trustItems = [
  "Funciona em Windows",
  "Impressora 58/80mm",
  "Caixa offline",
  "Suporte na implantação",
  "Teste por 7 dias",
  "Comprovante não fiscal"
];

const idealSegments = [
  "Pizzaria",
  "Lanchonete",
  "Açaíteria",
  "Bar",
  "Espetinho",
  "Hamburgueria",
  "Delivery"
];

const proofQuotes = [
  {
    segment: "Hamburgueria",
    quote: "Antes eu anotava pedido no papel. Agora o caixa, mesa e entrega ficam no mesmo sistema."
  },
  {
    segment: "Pizzaria",
    quote: "A equipe acompanha mesa, delivery e pagamento sem ficar perguntando no balcão."
  },
  {
    segment: "Açaíteria",
    quote: "O cardápio online e o WhatsApp ajudam a receber pedido sem perder o controle do estoque."
  }
];

const plans = [
  {
    id: "offline",
    order: "Plano 1",
    label: "Entrada",
    title: "PDV Caixa Local",
    description: "Para loja que quer vender no Windows, imprimir comprovante, controlar estoque e fechar caixa sem depender da internet.",
    monthly: "R$ 29,90",
    annual: "R$ 229,90",
    features: [
      "Caixa local no Windows",
      "Venda rápida, mesas, comandas e balcão",
      "Dinheiro, Pix manual, cartão/Point e troco",
      "Comprovante e fechamento de caixa",
      "Estoque, margem e relatórios básicos",
      "Licença por computador",
      "Para cardápio, garçom, iFood e WhatsApp, escolha o Profissional"
    ]
  },
  {
    id: "online",
    order: "Plano 2",
    label: "Recomendado",
    title: "Restaurante Profissional",
    description: "Para restaurante operar caixa, cardápio online, garçom no celular, delivery, equipe, NFC-e configurável e WhatsApp no mesmo fluxo.",
    monthly: "R$ 149,00",
    annual: "R$ 1.399,00",
    note: "WhatsApp conectado incluso para atendimento e pedidos. NFC-e depende de certificado, credenciais fiscais, UF e configuração do cliente.",
    features: [
      "PDV Windows com sincronização em nuvem",
      "Acesso web para acompanhar a loja",
      "Cardápio digital e pedidos online",
      "Garçom no celular lançando direto na mesa/comanda",
      "Equipe, entregadores e permissões",
      "Delivery por zona com taxa configurável",
      "NFC-e configurável com certificado e dados fiscais",
      "Mercado Pago/Point conforme credenciais",
      "iFood no fluxo do PDV conforme homologação e credenciais",
      "WhatsApp para atendimento automático",
      "Relatórios de caixa, estoque, repasses e margem"
    ],
    featured: true
  },
  {
    id: "custom",
    order: "Plano 3",
    label: "Sob medida",
    title: "Projeto Personalizado",
    description: "Para operação com várias lojas, fiscal, migração maior, regras especiais ou automações fora do padrão.",
    monthly: "Consultar",
    annual: "Consultar",
    custom: {
      label: "Projeto sob medida",
      text: "Avaliamos o escopo no WhatsApp e fechamos o melhor formato para sua operação."
    },
    features: [
      "Configurações especiais para a rotina da loja",
      "Multiloja ou operação com várias unidades",
      "Migração maior de dados e cadastros",
      "Relatórios customizados",
      "Integração fiscal conforme necessidade",
      "Cardápio amplo e regras específicas",
      "Automações e fluxos especiais sob escopo",
      "Implantação combinada com o vendedor"
    ],
    whatsappOnly: true
  }
];

const whatsappAiAddOn = {
  label: "Adicional",
  title: "WhatsApp IA Pro",
  price: "+R$ 49 a +R$ 89/mês",
  description: "Para restaurante que quer volume maior no WhatsApp, campanhas, automações e atendimento mais personalizado sem trocar o plano principal.",
  features: [
    "Maior volume de atendimentos e conversas",
    "Campanhas e automações para clientes",
    "Recuperação de pedido, carrinho ou cliente parado",
    "Respostas mais personalizadas para a loja",
    "Ajustes de fluxo conforme cardápio e rotina",
    "Limites e custos de mensagens seguem política do plano e regras da Meta"
  ]
};

const faqs = [
  ["Qual plano eu escolho?", "Se você quer só caixa local no Windows, use o Offline de R$29,90. Se quer restaurante conectado com cardápio online, garçom no celular, equipe, entregadores, NFC-e configurável, Mercado Pago, iFood e WhatsApp, use o Restaurante Profissional de R$149."],
  ["Funciona sem internet?", "No Offline, sim: o caixa continua vendendo localmente. Recursos online, nuvem e integrações precisam de internet."],
  ["O sistema emite nota fiscal?", "O plano profissional tem NFC-e configurável quando o cliente fornece certificado, credenciais fiscais e dados exigidos pela UF. Validação fiscal, homologação e parametrização dependem da empresa e da regra do estado."],
  ["Dá para personalizar?", "Sim. Fluxo, cardápio, entregas, impressão, usuários, permissões, entregadores, fiscal e relatórios podem ser ajustados. Personalizações têm valores sob consulta."],
  ["Tem WhatsApp, iFood e garçom?", "Sim. O Restaurante Profissional inclui garçom no celular, cardápio online, equipe, entregadores e WhatsApp. iFood e Mercado Pago dependem das credenciais, homologação e regras dos terceiros."],
  ["O WhatsApp é ilimitado?", "O plano inclui atendimento por WhatsApp dentro da política de uso. Para campanhas, alto volume ou automações mais avançadas, use o adicional WhatsApp IA Pro."],
  ["Posso usar em mais de um computador?", "Offline é licença por computador. Online pode conectar equipe, web e dispositivos conforme plano e configuração combinada."],
  ["Como funciona instalação e suporte?", "Orientamos instalação Windows, ativação, primeiros cadastros, impressora, pagamentos e uso do caixa. Migrações e integrações especiais são combinadas."],
  ["Posso testar antes de contratar?", "Sim. O teste de 7 dias libera PDV, cardápio online, garçom no celular, Mercado Pago e WhatsApp. iFood fica desabilitado no teste e entra no plano Restaurante Profissional de R$149, conforme credenciais e homologação."]
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
          <h1>PDV para restaurante que funciona mesmo sem internet</h1>
          <p className="lpLead">
            Venda no caixa Windows, controle mesas, delivery, estoque, cardápio online, garçom no celular e WhatsApp com IA em uma rotina só.
          </p>
          <p className="lpHeroPriceLine">
            Comece com caixa local por R$29,90/mês ou use o plano restaurante conectado por R$149/mês.
          </p>
          <div className="lpHeroActions">
            <a
              className="lpSolidButton lpLargeButton"
              href={downloadUrl}
              data-analytics-action="trial_download"
              data-analytics-location="hero"
              data-analytics-plan="offline"
            >
              Testar grátis por 7 dias
            </a>
            <a
              className="lpGhostButton lpLargeButton"
              href={sellers[1].href}
              data-analytics-action="whatsapp_click"
              data-analytics-seller={sellers[1].name}
              data-analytics-location="hero"
            >
              Falar no WhatsApp
            </a>
          </div>
          <dl className="lpHeroStats" aria-label="Resumo do produto">
            <div><dt>R$29,90 local</dt><dd>caixa Windows para vender, imprimir e fechar o dia</dd></div>
            <div><dt>R$149 profissional</dt><dd>online, NFC-e, equipe, entregadores, iFood, Mercado Pago e WhatsApp IA</dd></div>
            <div><dt>Teste 7 dias</dt><dd>conheça o fluxo antes de contratar a operação conectada</dd></div>
          </dl>
        </div>

        <div className="lpHeroVisual" aria-label="Tela real do Balcão Livre PDV">
          <div className="lpVisualGlow" aria-hidden="true"></div>
          <div className="lpLaptopMock">
            <div className="lpLaptopTop">
              <span>Balcão Livre PDV Online</span>
              <b>Caixa aberto</b>
            </div>
            <div
              className="lpLaptopScreen"
              role="img"
              aria-label="Tela atual do modo guia do Balcão Livre PDV"
              style={{ "--screen-image": "url('/guide/windows-pdv/01-comandas-mesas.png')" }}
            />
          </div>
          <div className="lpPhoneMock" aria-label="Resumo no celular">
            <span>Pedidos</span>
            <strong>Mesa 03</strong>
            <p>Pedido enviado para o caixa em tempo real.</p>
            <b>Garçom web</b>
          </div>
          <div className="lpHeroTiles" aria-label="Modulos principais">
            <span><b>iFood</b>pedido entra no PDV</span>
            <span><b>WhatsApp IA</b>atende e recebe pedido</span>
            <span><b>Mercado Pago</b>Pix, cartão e Point</span>
          </div>
        </div>
      </section>

      <section className="lpStrip" id="beneficios" aria-label="Diferenciais principais">
        <span><b>Caixa não para</b>venda local continua mesmo quando a internet cai</span>
        <span><b>Pedido conectado</b>mesa, cardápio, garçom, delivery e iFood no mesmo fluxo</span>
        <span><b>Pagamento organizado</b>dinheiro, Pix, cartão, Mercado Pago, troco e comprovante</span>
        <span><b>WhatsApp com IA</b>atendimento e pedidos inclusos no plano profissional</span>
      </section>

      <section className="lpSection lpInstallerSection" id="instaladores">
        <div className="lpSectionHead">
          <p className="lpKicker">Instaladores e teste</p>
          <h2>Baixe o instalador e teste o PDV por 7 dias.</h2>
          <p>Teste o caixa Windows, mesas, estoque, impressão, cardápio online, garçom no celular, Mercado Pago e WhatsApp por 7 dias. iFood fica desabilitado no teste e entra no plano profissional pago.</p>
        </div>
        <div className="lpInstallerGrid">
          <article className="lpInstallerCard">
            <div className="lpInstallerTop"><span>Offline</span><b>Caixa local</b></div>
            <h3>Instalador PDV Offline</h3>
            <p>Para testar o caixa Windows local, vendas, mesas, estoque, pagamentos e impressão sem depender da internet.</p>
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
            <p>Para testar PDV conectado, cardápio online, web, sincronização, equipe, entregadores, NFC-e configurável, Mercado Pago e WhatsApp. iFood fica reservado ao plano profissional pago.</p>
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
              <p>Use PDV web, cardápio, sincronização, garçom, Mercado Pago e WhatsApp. iFood entra na implantação do plano profissional.</p>
            </div>
            <p className="lpPaidOnlyNote">O teste online inclui WhatsApp conectado ao PDV. iFood fica bloqueado no teste e é liberado somente em licença com esse recurso.</p>
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
          <p>Caixa, mesa, delivery, estoque, equipe e atendimento aparecem no mesmo fluxo. O vendedor não precisa procurar informação em outro sistema.</p>
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
          <p className="lpKicker">Operação</p>
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

      <section className="lpSection lpProofSection" id="prova-real">
        <div className="lpSectionHead">
          <p className="lpKicker">Prova real</p>
          <h2>Feito para restaurante que precisa vender sem perder pedido.</h2>
          <p>Ideal para pizzaria, lanchonete, açaíteria, bar, espetinho, hamburgueria e delivery.</p>
        </div>
        <div className="lpSegmentPills" aria-label="Tipos de loja atendidos">
          {idealSegments.map((segment) => <span key={segment}>{segment}</span>)}
        </div>
        <div className="lpProofGrid">
          {proofQuotes.map((item) => (
            <article key={item.segment}>
              <span>{item.segment}</span>
              <p>“{item.quote}”</p>
            </article>
          ))}
        </div>
      </section>

      <section className="printSection lpPrintReturn" id="impressao">
        <div className="receiptMock" data-print-receipt>
          <h3>BALCÃO LIVRE PDV</h3>
          <p>COMPROVANTE DO PDV</p>
          <div className="receiptMeta">
            <span>COMANDA 000012</span>
            <span>GARÇOM: 2</span>
            <span>OPERADOR: CAIXA</span>
          </div>
          <div className="receiptInstruction">
            <strong data-print-title>Monte a venda no PDV demo acima.</strong>
            <span data-print-subtitle>Ao finalizar, a página desce para o comprovante com os produtos testados.</span>
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
          <p className="eyebrow">Impressão</p>
          <h2>Comprovante grande, legível e pronto para a impressora da loja.</h2>
          <p>O recibo usa os dados configurados da empresa, mostra operador ou garçom, calcula troco, imprime fechamento de caixa e pode incluir QR Code quando a loja quiser.</p>
          <div className="pillList">
            <span>Impressora padrão do Windows</span>
            <span>Térmica 58/80mm</span>
            <span>USB, rede ou compartilhada</span>
            <span>Resumo do dia</span>
            <span>QR opcional</span>
          </div>
          <div className="printOperations" aria-label="Rotina de impressão no PDV">
            <article><b>01</b><div><strong>Recebeu pagamento</strong><span>O comprovante abre igual na demo e já pode sair na impressora.</span></div></article>
            <article><b>02</b><div><strong>Pix com valor</strong><span>O QR usa o total da comanda, sem o cliente digitar valor.</span></div></article>
            <article><b>03</b><div><strong>Dados da loja</strong><span>Nome, CNPJ, endereço e telefone vêm das configurações.</span></div></article>
            <article><b>04</b><div><strong>Fechamento do dia</strong><span>Ao fechar caixa, imprime resumo das vendas e movimentos.</span></div></article>
          </div>
          <div className="printerStatus">
            <span>Demo da impressão</span>
            <strong>Comprovante grande + QR centralizado + troco calculado</strong>
          </div>
        </div>
      </section>

      <section className="lpTrustStrip" aria-label="Confiança antes dos planos">
        {trustItems.map((item) => <span key={item}>{item}</span>)}
      </section>

      <section className="lpSection lpPlansSection" id="planos">
        <div className="lpSectionHead">
          <p className="lpKicker">Planos</p>
          <h2>R$29,90 para caixa local. R$149 para restaurante conectado.</h2>
          <p>O R$149 é o plano principal para operar com cardápio online, garçom no celular, equipe, entregadores, NFC-e configurável, Mercado Pago, iFood e WhatsApp. Automações de alto volume entram no adicional IA Pro.</p>
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
              <div className="lpPlanColumnPrice" aria-label={`Preços do plano ${plan.title}`}>
                <div><span>Mensal</span><strong>{plan.monthly}</strong></div>
                <div><span>Anual</span><strong>{plan.annual}</strong></div>
              </div>
              {plan.custom ? (
                <div className="lpPlanCustom">
                  <span>{plan.custom.label}</span>
                  <strong>{plan.custom.text}</strong>
                </div>
              ) : null}
              {plan.note ? <p className="lpPlanNote">{plan.note}</p> : null}
              <ul>
                {plan.features.map((feature) => <li key={feature}>{feature}</li>)}
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

        <article className="lpPlanAddOn">
          <div>
            <span>{whatsappAiAddOn.label}</span>
            <h3>{whatsappAiAddOn.title}</h3>
            <p>{whatsappAiAddOn.description}</p>
          </div>
          <strong>{whatsappAiAddOn.price}</strong>
          <ul>
            {whatsappAiAddOn.features.map((feature) => <li key={feature}>{feature}</li>)}
          </ul>
          <a
            className="lpPlanButton"
            href={sellers[1].href}
            data-analytics-action="whatsapp_click"
            data-analytics-seller={sellers[1].name}
            data-analytics-location="addon_whatsapp_ai"
            data-analytics-plan="whatsapp-ai-pro"
          >
            Consultar IA Pro
          </a>
        </article>
      </section>

      <section className="lpGuideBanner">
        <div>
          <p className="lpKicker">Implantação</p>
          <h2>Guia rápido do Windows</h2>
          <p>Passo a passo para instalar, cadastrar produtos, vender, imprimir, controlar estoque e fechar caixa.</p>
        </div>
        <a className="lpSolidButton lpLargeButton" href="/como-usar/">Abrir passo a passo</a>
      </section>

      <section className="lpSection lpFaq" id="faq">
        <div className="lpSectionHead">
          <p className="lpKicker">FAQ</p>
          <h2>Dúvidas antes de contratar</h2>
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
            <strong>Balcão Livre</strong>
            <small>PDV Para Restaurantes</small>
          </span>
        </div>
        <div className="lpFooterPitch">
          <b>PDV Windows para restaurante vender, imprimir e fechar caixa sem depender de gambiarra.</b>
          <span>Offline para caixa local. Restaurante Profissional para web, cardápio, garçom, equipe, entregadores, NFC-e, iFood, Mercado Pago e WhatsApp.</span>
        </div>
        <div className="lpFooterColumn">
          <b>Produto</b>
          <a href="#produto">Recursos</a>
          <a href="#demo-pdv">Demo PDV</a>
          <a href="#impressao">Impressão</a>
          <a href="#operacao">Operação</a>
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
          <a href="/termos/">Termos e condições</a>
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
