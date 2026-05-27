import CashierDemo from "./CashierDemo";

const downloadUrl =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows/BalcaoLivrePDV-Setup-1.0.2026.exe";

const sellers = [
  {
    name: "Vendedor Wender",
    phone: "+55 27 98126-7551",
    href: "https://wa.me/5527981267551"
  },
  {
    name: "Vendedor Lucas",
    phone: "33 99960-9457",
    href: "https://wa.me/5533999609457"
  }
];

const quickFacts = [
  ["Restaurantes", "bares e eventos"],
  ["Windows", "app nativo"],
  ["Impressoras", "termica ou padrao"],
  ["Nao fiscal", "comprovante simples"]
];

const heroFlow = [
  ["Mesas e comandas", "Abra mesa, ficha de balcao ou delivery sem mudar de sistema."],
  ["Produtos e estoque", "Lance itens, agrupe quantidades e acompanhe baixa de estoque."],
  ["Pagamento e impressao", "Receba em dinheiro, Pix ou cartao e imprima o comprovante."]
];

const features = [
  ["Venda rapida", "Digite o codigo, quantidade e Enter. O produto agrupa automaticamente na comanda."],
  ["Balcao com fichas", "Atendimento rapido para retirada, consumo no balcao e venda comum."],
  ["Mapa de mesas", "A loja escolhe se usa 5, 30, 50 ou mais de 100 mesas."],
  ["Comissao por garcom", "Numero do garcom aparece na comanda, no controle e no comprovante."],
  ["Delivery completo", "Cliente, telefone, endereco, taxa e observacao no comprovante."],
  ["Pix com valor pronto", "QR Code de pagamento ja nasce com o total da venda."],
  ["Estoque e margem", "Preco de compra, preco de venda, lucro, entrada, saida e alerta de minimo."],
  ["Relatorios de caixa", "Resumo do dia, vendas, estoque critico e impressao do fechamento."]
];

const workflow = [
  ["01", "Abre o caixa", "O operador informa o dinheiro vivo inicial e entra com numero/senha."],
  ["02", "Vende sem mouse", "Setas, Enter, F2, F9 e F10 resolvem o fluxo principal do caixa."],
  ["03", "Recebe e imprime", "Dinheiro mostra troco. Pix gera QR. Cartao, debito, vale e fiado ficam registrados."],
  ["04", "Fecha o dia", "O sistema bloqueia fechamento com pendencias e imprime resumo do caixa."]
];

const faqs = [
  ["Funciona sem internet?", "Sim. A operacao do PDV e offline. A internet e usada para ativacao da licenca e atualizacao."],
  ["Serve para loja pequena?", "Sim. A ideia e ser simples para balcao e crescer com mesas, delivery, estoque e relatorios."],
  ["Precisa usar mouse?", "O fluxo principal foi pensado para teclado, inclusive numerico, setas, Enter e teclas F."],
  ["Imprime em qualquer impressora?", "Sim. O sistema usa a impressora padrao do Windows ou a impressora escolhida nas configuracoes."]
];

const plans = [
  {
    id: "offline",
    order: "1°",
    name: "Balcao Livre PDV Offline",
    badge: "Caixa local",
    text: "Para restaurante que precisa vender no caixa todos os dias, mesmo quando a internet cai.",
    monthly: "R$ 17,00",
    annual: "R$ 200,00",
    features: [
      "Venda sem internet no Windows",
      "Mesas, balcao, delivery e comandas",
      "Pix, dinheiro, credito e debito",
      "Impressao local de comprovantes",
      "Estoque, fechamento e relatorios",
      "Licenca por computador"
    ]
  },
  {
    id: "online",
    order: "2°",
    name: "Balcao Livre PDV Online",
    badge: "Operacao conectada",
    text: "Para loja que quer integrar atendimento, delivery, garcom no celular e pedidos em tempo real.",
    monthly: "R$ 34,00",
    annual: "R$ 400,00",
    features: [
      "Pedidos do iFood no sistema",
      "Entrega por zona e taxa configuravel",
      "Garcom no celular em tempo real",
      "Sincronizacao entre caixa e atendimento",
      "Pedidos online, mesas e comandas",
      "Visao gerencial para acompanhar a operacao"
    ],
    featured: true
  }
];

const qrCells = [
  1, 1, 1, 0, 1, 0, 1,
  1, 0, 1, 1, 0, 1, 0,
  1, 1, 1, 0, 1, 1, 1,
  0, 1, 0, 1, 1, 0, 1,
  1, 0, 1, 0, 1, 1, 0,
  0, 1, 1, 1, 0, 1, 1,
  1, 0, 1, 1, 1, 0, 1
];

export default function Page() {
  return (
    <main>
      <header className="topbar">
        <a className="brand" href="#inicio" aria-label="Balcão Livre PDV">
          <img src="/balcao-livre-icon.png" alt="" />
          <span>Balcão Livre PDV</span>
        </a>
        <nav aria-label="Navegacao principal">
          <a href="#produto">Produto</a>
          <a href="#impressao">Impressao</a>
          <a href="#operacao">Operacao</a>
          <a href="#preco">Preco</a>
          <a href="#faq">FAQ</a>
          <a href="/como-usar/">Como usar</a>
          <a href="/admin/">Admin</a>
          <a href="/pdv">Login</a>
        </nav>
        <a className="topbarAction" href={downloadUrl}>
          Baixar instalador
        </a>
      </header>

      <section className="hero" id="inicio">
        <div className="heroCopy">
          <div className="productMark">
            <img src="/balcao-livre-icon.png" alt="" />
            <div>
              <span>Balcão Livre</span>
              <b>PDV para restaurante</b>
            </div>
          </div>
          <p className="eyebrow">Restaurantes, bares, lanchonetes e casas de eventos</p>
          <h1>PDV para restaurante controlar caixa, mesas e delivery.</h1>
          <p className="heroLead">
            Venda no balcao, acompanhe comandas, registre pagamentos e imprima
            comprovantes em uma rotina feita para operacao real.
          </p>
          <div className="heroBenefit">
            <strong>Caixa offline, atendimento agil e fechamento com controle.</strong>
            <span>
              O Balcao Livre PDV organiza pedidos, pagamentos, estoque e
              relatorios em um fluxo direto para equipe e dono.
            </span>
          </div>
          <div className="heroFlow">
            {heroFlow.map(([title, text], index) => (
              <article key={title}>
                <b>{index + 1}</b>
                <div>
                  <strong>{title}</strong>
                  <span>{text}</span>
                </div>
              </article>
            ))}
          </div>
          <div className="heroActions">
            <a className="primaryButton" href={downloadUrl}>
              Baixar Balcão Livre
            </a>
            <a className="secondaryButton" href="#preco">
              Ver planos
            </a>
          </div>
          <div className="quickFacts">
            {quickFacts.map(([title, text]) => (
              <span key={title}>
                <b>{title}</b>
                {text}
              </span>
            ))}
          </div>
        </div>

        <div className="impactPanel" aria-label="Beneficios para restaurantes">
          <article>
            <span>Atendimento</span>
            <strong>Pedido entra mais rapido</strong>
            <p>Mesa, balcao e delivery seguem um fluxo simples para o operador.</p>
          </article>
          <article>
            <span>Caixa</span>
            <strong>Recebimento com troco</strong>
            <p>Dinheiro, Pix, cartao e fiado ficam registrados no fechamento.</p>
          </article>
          <article>
            <span>Controle</span>
            <strong>Estoque e relatorios</strong>
            <p>Produtos vendidos, estoque minimo e resumo do dia em poucos cliques.</p>
          </article>
          <article>
            <span>Fechamento</span>
            <strong>Resumo impresso do dia</strong>
            <p>Entradas, retiradas, recebimentos e pendencias aparecem antes de fechar.</p>
          </article>
          <article className="impactWide controlPreview">
            <div>
              <span>Controle do restaurante</span>
              <strong>O dono enxerga venda, estoque e caixa do dia.</strong>
              <p>O PDV nao fica so registrando pedido. Ele mostra se a loja esta vendendo e onde precisa repor produto.</p>
            </div>
            <div className="controlMetrics" aria-label="Indicadores principais do PDV">
              <b><span>Hoje</span>R$ 1.284,00</b>
              <b><span>Ticket medio</span>R$ 42,80</b>
              <b><span>Estoque baixo</span>3 itens</b>
              <b><span>Margem</span>48%</b>
            </div>
          </article>
        </div>
      </section>

      <section className="shortcutBand" aria-label="Diferenciais principais do sistema">
        <span><b>Offline no caixa</b>vende mesmo se a internet cair</span>
        <span><b>Pix com valor</b>QR sai pronto no comprovante</span>
        <span><b>Impressao automatica</b>recibo sai ao receber</span>
        <span><b>Fechamento seguro</b>bloqueia pendencias abertas</span>
        <span><b>Estoque e lucro</b>baixa venda e mostra margem</span>
      </section>

      <CashierDemo />

      <section className="section" id="produto">
        <div className="sectionIntro">
          <p className="eyebrow">Produto</p>
          <h2>Como o PDV funciona na pratica.</h2>
          <p>
            O cliente precisa bater o olho e entender o uso real: abrir caixa,
            digitar produto, controlar mesa ou ficha, receber pagamento,
            imprimir comprovante e fechar o dia.
          </p>
        </div>
        <div className="featureGrid">
          {features.map(([title, text]) => (
            <article className="featureCard" key={title}>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="splitSection" id="operacao">
        <div className="splitCopy">
          <p className="eyebrow">Operacao</p>
          <h2>Fluxo de caixa que qualquer funcionario entende.</h2>
          <p>
            O operador entra com numero e senha, abre o caixa, vende pelo
            teclado e fecha o dia com resumo impresso.
          </p>
        </div>
        <div className="timeline">
          {workflow.map(([number, title, text]) => (
            <article className="timelineItem" key={title}>
              <b>{number}</b>
              <div>
                <h3>{title}</h3>
                <p>{text}</p>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="printSection" id="impressao">
        <div className="receiptMock">
          <h3>BALCAO LIVRE PDV</h3>
          <p>COMPROVANTE DO PDV</p>
          <div className="receiptMeta">
            <span>COMANDA 000012</span>
            <span>GARCOM: 2</span>
            <span>OPERADOR: CAIXA</span>
          </div>
          <div className="receiptInstruction">
            <strong>Monte a venda no PDV demo acima.</strong>
            <span>Ao finalizar, a pagina desce para o comprovante com os produtos testados.</span>
          </div>
          <div className="receiptRows receiptTotals">
            <span>TOTAL</span><strong>R$ 0,00</strong>
            <span>PAGAMENTO</span><strong>aguardando</strong>
            <span>TROCO</span><strong>R$ 0,00</strong>
          </div>
          <div className="receiptControl">CONTROLE GERADO NA VENDA</div>
          <div className="qrMock" aria-label="QR Code de exemplo">
            {qrCells.map((cell, index) => (
              <i className={cell ? "on" : undefined} key={index} />
            ))}
          </div>
          <small>QR Pix, Instagram, mapa ou link opcional</small>
        </div>
        <div className="printCopy">
          <p className="eyebrow">Impressao</p>
          <h2>Comprovante grande, legivel e pronto para a impressora da loja.</h2>
          <p>
            O recibo usa os dados configurados da empresa, mostra operador ou
            garcom, calcula troco, imprime fechamento de caixa e pode incluir QR
            Code quando a loja quiser.
          </p>
          <div className="pillList">
            <span>Impressora padrao do Windows</span>
            <span>Termica 58/80mm</span>
            <span>USB, rede ou compartilhada</span>
            <span>Resumo do dia</span>
            <span>QR opcional</span>
          </div>
          <div className="printOperations" aria-label="Rotina de impressao no PDV">
            <article>
              <b>01</b>
              <div>
                <strong>Recebeu pagamento</strong>
                <span>O comprovante abre igual na demo e ja pode sair na impressora.</span>
              </div>
            </article>
            <article>
              <b>02</b>
              <div>
                <strong>Pix com valor</strong>
                <span>O QR usa o total da comanda, sem o cliente digitar valor.</span>
              </div>
            </article>
            <article>
              <b>03</b>
              <div>
                <strong>Dados da loja</strong>
                <span>Nome, CNPJ, endereco e telefone vem das configuracoes.</span>
              </div>
            </article>
            <article>
              <b>04</b>
              <div>
                <strong>Fechamento do dia</strong>
                <span>Ao fechar caixa, imprime resumo das vendas e movimentos.</span>
              </div>
            </article>
          </div>
          <div className="printerStatus">
            <span>Demo da impressao</span>
            <strong>Comprovante grande + QR centralizado + troco calculado</strong>
          </div>
        </div>
      </section>

      <section className="pricingSection" id="preco">
        <div className="sectionIntro">
          <p className="eyebrow">Preco</p>
          <h2>Escolha o PDV certo para a sua operacao.</h2>
          <p>
            Duas modalidades objetivas: um caixa offline para estabilidade na
            loja e uma operacao online para delivery, equipe e pedidos em tempo real.
          </p>
        </div>
        <div className="pricingPromise">
          <strong>PDV para rotina real de restaurante, nao tela promocional.</strong>
          <span>Offline para caixa estavel. Online para operacao conectada.</span>
        </div>
        <div className="pricingGrid">
          {plans.map((plan) => (
            <article className={plan.featured ? "priceCard onlinePlan" : "priceCard offlinePlan"} key={plan.id}>
              <div className="priceCardHeader">
                <span>{plan.order}</span>
                <div>
                  <small>{plan.badge}</small>
                  <h3>{plan.name}</h3>
                </div>
              </div>
              <p>{plan.text}</p>
              <div className="priceOptions" aria-label={`Valores do ${plan.name}`}>
                <div>
                  <span>Mensal</span>
                  <strong>{plan.monthly}</strong>
                  <em>por mes</em>
                </div>
                <div>
                  <span>Anual</span>
                  <strong>{plan.annual}</strong>
                  <em>por ano</em>
                </div>
              </div>
              <ul>
                {plan.features.map((feature) => (
                  <li key={feature}>{feature}</li>
                ))}
              </ul>
              <form action="/api/checkout" method="post" className="planActions">
                <button type="submit" name="plan" value={`${plan.id}-mensal`}>
                  Contratar mensal
                </button>
                <button type="submit" name="plan" value={`${plan.id}-anual`} className="secondary">
                  Contratar anual
                </button>
              </form>
              <div className="sellerContacts" aria-label="Comprar pelo WhatsApp">
                <span>Comprar pelo WhatsApp</span>
                {sellers.map((seller) => (
                  <a href={seller.href} key={seller.name} target="_blank" rel="noreferrer">
                    <strong>{seller.name}</strong>
                    <b>{seller.phone}</b>
                  </a>
                ))}
              </div>
            </article>
          ))}
        </div>
        <div className="paymentTrust">
          <span>Mensal ou anual</span>
          <span>Ativacao por licenca</span>
          <span>Atualizacoes inclusas</span>
          <span>Suporte de implantacao</span>
        </div>
      </section>

      <section className="faqSection" id="faq">
        <div className="sectionIntro">
          <p className="eyebrow">Duvidas rapidas</p>
          <h2>Informacao direta para vender melhor.</h2>
        </div>
        <div className="faqGrid">
          {faqs.map(([question, answer]) => (
            <article key={question}>
              <h3>{question}</h3>
              <p>{answer}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="finalCta">
        <img src="/balcao-livre-icon.png" alt="" />
        <div>
          <h2>Instale o Balcão Livre PDV no Windows.</h2>
          <p>PDV para restaurante com mesas, balcao, delivery, impressao, Pix e estoque.</p>
        </div>
        <a className="primaryButton" href={downloadUrl}>
          Baixar instalador
        </a>
      </section>

      <footer className="siteFooter expandedFooter">
        <div className="footerBrand">
          <div>
            <strong>Balcão Livre PDV</strong>
            <span>Nagazaki Software</span>
          </div>
        </div>
        <p>2026 Balcão Livre PDV. Caixa simples, rápido e sem complicação.</p>
        <div className="footerWhatsapp">
          <strong>Compre no WhatsApp</strong>
          {sellers.map((seller) => (
            <a href={seller.href} key={seller.name} target="_blank" rel="noreferrer">
              <span>{seller.name}</span>
              <b>{seller.phone}</b>
            </a>
          ))}
        </div>
        <nav aria-label="Links do rodape">
          <a href="#produto">Produto</a>
          <a href="#impressao">Impressao</a>
          <a href="#preco">Planos</a>
          <a href="/como-usar/">Como usar</a>
          <a href="/termos/">Termos</a>
        </nav>
      </footer>
    </main>
  );
}
