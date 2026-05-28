import SiteHeader from "../SiteHeader";

export const metadata = {
  title: "Como usar o app Windows | Balcao Livre PDV"
};

const screens = [
  {
    id: "comandas",
    title: "1. Comandas e mesas",
    image: "/guide/windows-pdv/01-comandas-mesas.png",
    caption: "Tela principal do atendimento em mesa, com caixa aberto, comandas, venda rapida e estoque do cardapio.",
    points: [
      "Use a coluna Comanda para ativar mesa, informar operador/garcom e incluir produtos por codigo.",
      "O painel Comandas / Mesas mostra mesa livre, ocupada ou em conta. O operador clica na mesa e continua o pedido.",
      "A Venda rapida lista os produtos ativos com estoque, preco, grupo e quantidade disponivel.",
      "O total da comanda fica sempre no rodape esquerdo antes de fechar ou receber pagamento."
    ]
  },
  {
    id: "balcao",
    title: "2. Balcao e fichas rapidas",
    image: "/guide/windows-pdv/02-balcao-fichas.png",
    caption: "Exemplo de venda de ficha no balcao com produtos ja lancados e total calculado.",
    points: [
      "Use Balcao para venda rapida sem mesa: ficha F00001, F00002 e assim por diante.",
      "Produtos entram na ficha pelo codigo ou pela lista da Venda rapida.",
      "O caixa consegue excluir uma linha antes de receber, sem mexer nas outras fichas.",
      "F5 fecha a conta, F8 registra pagamento antecipado e F9 recebe pagamento."
    ]
  },
  {
    id: "delivery",
    title: "3. Delivery, WhatsApp e iFood",
    image: "/guide/windows-pdv/03-delivery-pedidos.png",
    caption: "Fila de delivery com pedidos WhatsApp, iFood, retirada e rota, todos usando o mesmo estoque.",
    points: [
      "Pedidos aparecem em cards com status: novo, confirmado, preparo, rota ou aguardando confirmacao.",
      "O pedido selecionado mostra cliente, itens, total e acoes de atendimento.",
      "WhatsApp e cardapio digital usam os produtos ativos do estoque; produto sem estoque nao entra no cardapio.",
      "Quando iFood estiver ligado em producao, os pedidos integrados entram nessa mesma fila de Delivery."
    ]
  }
];

const modules = [
  ["CP", "Cadastro Produtos", "Cria categorias, precos, estoque minimo, codigo WhatsApp, adicionais e ficha tecnica."],
  ["CX", "Caixa Movimentos", "Mostra abertura, suprimento, sangria, pagamentos e saldo do caixa."],
  ["F10", "Abrir/Fechar Caixa", "Abre o dia com troco inicial e fecha com conferencia profissional."],
  ["DL", "Novo Delivery", "Cria pedido manual com cliente, telefone, endereco, bairro, taxa e observacao."],
  ["IF", "iFood Pedidos", "Liga a integracao e acompanha pedidos importados quando a loja estiver homologada."],
  ["WA", "Ativar WhatsApp", "Atendimento automatico por cardapio: cliente chama, recebe cardapio e confirma por mensagem."],
  ["GW", "Garcom Web", "Abre o atendimento do garcom no celular para lancar pedidos direto no caixa."],
  ["TZ", "Taxas Delivery", "Configura taxa por bairro, zona ou regra de entrega."],
  ["ES", "Estoque Receitas", "Controla estoque, entradas, saidas, alerta minimo e ficha tecnica."],
  ["QR", "Cardapio Digital", "Publica um cardapio online com os mesmos produtos ativos do PDV."],
  ["BI", "Relatorios", "Acompanha vendas, produtos mais vendidos, margem e movimento por periodo."],
  ["BK", "Backup Dados", "Gera copia local/nuvem para reduzir risco de perda de informacao."]
];

export default function HowToUsePage() {
  return (
    <main className="lpPage">
      <SiteHeader />

      <section className="infoPage guidePage">
        <div className="infoHero">
          <p className="eyebrow">Como usar</p>
          <h1>Guia visual do Balcao Livre PDV no Windows.</h1>
          <p>Fluxo completo com exemplos reais de cardapio, estoque, mesas, balcao, delivery, pagamento e fechamento de caixa.</p>
        </div>

        <div className="guideNotice">
          Este guia usa uma loja exemplo com hamburguers, pizzas, bebidas, fichas de balcao e pedidos delivery para mostrar como o PDV fica em uso real.
        </div>

        <div className="guideSnapshotGrid" aria-label="Resumo rapido">
          <div><strong>24</strong><span>produtos ativos com estoque</span></div>
          <div><strong>12</strong><span>mesas de atendimento</span></div>
          <div><strong>5</strong><span>pedidos delivery de exemplo</span></div>
          <div><strong>R$ 250,00</strong><span>caixa aberto com troco inicial</span></div>
        </div>

        <div className="infoLayout">
          <aside className="infoAside" aria-label="Indice do guia">
            <a href="#comandas">Comandas</a>
            <a href="#balcao">Balcao</a>
            <a href="#delivery">Delivery</a>
            <a href="#modulos">Modulos</a>
            <a href="#rotina">Rotina</a>
          </aside>

          <div className="infoContent">
            {screens.map((screen) => (
              <section className="guideStep guideStepWithImage" id={screen.id} key={screen.id}>
                <div>
                  <h2>{screen.title}</h2>
                  <p>{screen.caption}</p>
                  <ul>
                    {screen.points.map((point) => <li key={point}>{point}</li>)}
                  </ul>
                </div>
                <figure className="guideScreenshot">
                  <img src={screen.image} alt={screen.caption} loading="lazy" />
                </figure>
              </section>
            ))}

            <section className="guideStep" id="modulos">
              <h2>4. O que cada modulo do topo faz</h2>
              <div className="moduleGrid">
                {modules.map(([key, title, text]) => (
                  <div className="moduleCard" key={key}>
                    <span>{key}</span>
                    <strong>{title}</strong>
                    <p>{text}</p>
                  </div>
                ))}
              </div>
            </section>

            <section className="guideStep" id="rotina">
              <h2>5. Rotina recomendada no dia a dia</h2>
              <div className="guideTimeline">
                <div><strong>1</strong><span>Abrir caixa no F10 e conferir troco inicial.</span></div>
                <div><strong>2</strong><span>Atender em Comandas, Balcao ou Delivery conforme o canal do pedido.</span></div>
                <div><strong>3</strong><span>Lancar produtos pelo codigo, pesquisa ou lista de venda rapida.</span></div>
                <div><strong>4</strong><span>Receber em dinheiro, Pix, credito, debito ou pagamento antecipado.</span></div>
                <div><strong>5</strong><span>Resolver pendencias abertas e fechar o caixa com conferencia.</span></div>
              </div>
            </section>
          </div>
        </div>
      </section>
    </main>
  );
}
