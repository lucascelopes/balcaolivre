import SiteHeader from "../SiteHeader";

export const metadata = {
  title: "Guia completo do app Windows | Balcao Livre PDV",
  description:
    "Manual completo para instalar, configurar e operar o Balcao Livre PDV no Windows: caixa, produtos, mesas, delivery, pagamentos, impressao, estoque e fechamento."
};

const quickStart = [
  ["1", "Instale e ative", "Baixe o instalador, abra o app Windows e libere a licenca com a chave comprada ou de teste."],
  ["2", "Cadastre a loja", "Confirme nome, telefone, endereco, operadores, senha de gerente e dados que aparecem nos comprovantes."],
  ["3", "Monte produtos", "Crie categorias, codigos, precos, estoque, setor de impressao e se o item aparece na venda/cardapio."],
  ["4", "Abra o caixa", "Informe o troco inicial, escolha o operador e comece a vender em mesa, balcao ou delivery."],
  ["5", "Venda e receba", "Lance produtos por codigo, lista rapida ou busca. Receba em dinheiro, Pix, credito, debito ou Mercado Pago quando configurado."],
  ["6", "Feche o dia", "Confira vendas, retiradas, pagamentos, pendencias, estoque e imprima ou salve o resumo do caixa."]
];

const screens = [
  {
    id: "comandas",
    title: "Comandas e mesas",
    image: "/guide/windows-pdv/01-comandas-mesas.png",
    caption: "Tela principal para restaurante com mesa, garcom, produtos, total e status das mesas.",
    points: [
      "Selecione ou digite a mesa/comanda antes de lancar produtos.",
      "Use o operador/garcom correto para separar responsabilidade no atendimento.",
      "Mesa livre fica disponivel; mesa ocupada recebe produtos; mesa em conta indica fechamento em andamento.",
      "Produtos lancados aparecem com codigo, nome, quantidade, total e acao de exclusao quando permitido.",
      "Quando uma comanda estiver fechada, o atendente nao deve alterar itens sem senha de gerente."
    ]
  },
  {
    id: "balcao",
    title: "Balcao e fichas rapidas",
    image: "/guide/windows-pdv/02-balcao-fichas.png",
    caption: "Venda rapida para cliente no caixa, ficha impressa, produto por codigo e pagamento imediato.",
    points: [
      "Use Balcao quando nao precisa abrir mesa: ideal para ficha, retirada, acai, lanche rapido e venda direta.",
      "Cada ficha fica separada para o caixa nao misturar pedidos.",
      "O operador pode lancar produto por codigo, selecionar na lista ou pesquisar pelo nome.",
      "Antes de receber, confira quantidade, desconto, forma de pagamento e troco.",
      "Depois de finalizar, o comprovante fica pronto para imprimir ou apenas consultar na tela."
    ]
  },
  {
    id: "delivery",
    title: "Delivery, retirada e pedidos online",
    image: "/guide/windows-pdv/03-delivery-pedidos.png",
    caption: "Fila operacional para pedidos manuais, retirada, delivery, cardapio digital, iFood e WhatsApp em plano pago.",
    points: [
      "Crie pedidos manuais com cliente, telefone, endereco, bairro, taxa, observacao e itens.",
      "Pedidos online entram na mesma rotina quando o plano e integracao estiverem liberados.",
      "Use status de pedido para separar novo, confirmado, preparo, saiu para entrega e finalizado.",
      "A cozinha pode receber impressao por setor quando os setores e impressoras estiverem cadastrados.",
      "iFood, WhatsApp e automacoes conectadas dependem de plano pago, credenciais e aprovacao dos terceiros."
    ]
  }
];

const chapters = [
  {
    id: "instalacao",
    title: "1. Instalacao, primeira abertura e licenca",
    items: [
      "Baixe o instalador pelo site oficial e execute no computador Windows que sera usado no caixa.",
      "Na primeira abertura, informe a chave de ativacao. Se a chave ja estiver vinculada ao e-mail no Supabase/painel, o login deve liberar o uso sem criar outra compra.",
      "Use um computador estavel, com usuario do Windows conhecido, impressora instalada e internet disponivel na primeira ativacao.",
      "No plano Offline, a venda local deve continuar funcionando mesmo se a internet cair. Recursos online dependem de conexao.",
      "Guarde a chave, e-mail de compra e contato do suporte. Eles ajudam em troca de computador, renovacao e recuperacao."
    ],
    tip: "Antes de colocar em producao, faca uma venda de teste, imprima comprovante e feche um caixa de exemplo."
  },
  {
    id: "configuracao",
    title: "2. Configuracao da loja, operadores e seguranca",
    items: [
      "Revise nome da loja, CNPJ quando usado, telefone, endereco, mensagem do comprovante e dados do responsavel.",
      "Crie operadores separados para caixa, garcom e gerente. Evite todos usando o mesmo operador.",
      "Defina senha de gerente para acoes sensiveis: excluir item de comanda fechada, cancelar venda, alterar pagamento, sangria ou reabrir conta.",
      "Configure permissao por funcao. Atendente vende; gerente corrige; dono consulta relatorios e configuracoes.",
      "Mantenha o computador com senha do Windows e backup. PDV de restaurante fica exposto a uso intenso."
    ],
    tip: "Se algo envolve dinheiro, cancelamento ou reabertura, deixe protegido por gerente."
  },
  {
    id: "produtos",
    title: "3. Cadastro de produtos, categorias e adicionais",
    items: [
      "Crie categorias claras: Lanches, Bebidas, Porcoes, Pizzas, Combos, Sobremesas, Adicionais.",
      "Use codigos curtos para produtos mais vendidos. Codigo bem definido acelera muito o caixa.",
      "Informe preco, unidade, estoque inicial, estoque minimo e se o produto aparece na venda.",
      "Quando o produto tiver sabores ou montagem especial, marque a opcao correta e cadastre adicionais/observacoes.",
      "Produto sem estoque ou marcado para nao mostrar na venda nao deve aparecer para o operador nem no cardapio digital."
    ],
    tip: "Nao deixe setores prontos estranhos. O usuario deve criar setores reais como Balcao, Cozinha, Bar, Pizza ou Entrega."
  },
  {
    id: "setores",
    title: "4. Setores de impressao e impressoras",
    items: [
      "Crie setores de producao conforme a loja realmente trabalha: Cozinha, Chapa, Bar, Pizzaria, Balcao, Sobremesa.",
      "Para cada setor, escolha a impressora correta instalada no Windows ou compartilhada na rede.",
      "Produtos devem apontar para o setor certo. Bebida vai para Bar; lanche para Chapa; pizza para Pizzaria.",
      "Teste uma impressao por setor antes de abrir a loja. Verifique acentuacao, tamanho da bobina, corte e nome do produto.",
      "Se uma impressora cair, o pedido pode ficar sem sair na producao. Tenha rotina para conferir fila e reimprimir."
    ],
    tip: "Setor nao e estoque. Setor e para onde o pedido imprime ou aparece na producao."
  },
  {
    id: "caixa",
    title: "5. Abertura de caixa e movimentos",
    items: [
      "Abra o caixa no inicio do turno com o valor real de troco.",
      "Registre suprimento quando entra dinheiro extra e sangria quando tira dinheiro do caixa.",
      "Separe formas de pagamento: dinheiro, Pix, credito, debito, Mercado Pago, fiado ou antecipado conforme a loja usa.",
      "Nunca misture retirada do dono com venda. Use movimento de caixa para deixar rastreavel.",
      "Se o caixa nao abrir, confira operador, permissao, data do computador e se existe caixa anterior pendente."
    ],
    tip: "Caixa bem aberto e bem fechado evita discussao no fim do expediente."
  },
  {
    id: "atendimento",
    title: "6. Lancamento de pedidos em mesa, balcao e delivery",
    items: [
      "Mesa/comanda: selecione a mesa, informe garcom, lance itens e acompanhe o total.",
      "Balcao: use ficha rapida para pedido que nasce e termina no caixa.",
      "Delivery: cadastre cliente, telefone, endereco, taxa, observacao e forma de entrega.",
      "Garcom web: o garcom no celular deve lancar em tempo real na conta correta, com observacao quando necessario.",
      "Observacoes importantes devem ir no item ou pedido: sem cebola, ponto da carne, trocar refrigerante, retirar talher."
    ],
    tip: "Se a conta ja tem produtos e esta aberta, o fluxo deve levar para a conta. Se esta vazia, leve para produtos."
  },
  {
    id: "pagamentos",
    title: "7. Recebimento, Pix, cartao e Mercado Pago",
    items: [
      "Antes de finalizar, confira total, desconto, taxa, couver, percentual do garcom e itens lancados.",
      "Dinheiro precisa calcular valor recebido e troco.",
      "Pix pode ser apenas registrado ou gerar QR Code quando a integracao estiver configurada.",
      "Credito e debito devem ser separados para relatorio e conferencia da maquininha.",
      "Mercado Pago fica disponivel para teste quando habilitado; em producao, depende de credenciais e plano correto."
    ],
    tip: "Depois que a venda finaliza, a tela deve mostrar venda finalizada e descer para o comprovante sem esconder o fluxo."
  },
  {
    id: "estoque",
    title: "8. Estoque, ficha tecnica e alerta minimo",
    items: [
      "Cadastre estoque apenas de produtos que precisam controle. Nao force estoque em item que a loja nao controla.",
      "Estoque minimo serve para alerta: quando baixar do limite, o sistema avisa para repor.",
      "Entrada aumenta saldo; saida manual corrige perda, quebra, consumo interno ou ajuste.",
      "Ficha tecnica permite baixar ingredientes quando um produto e vendido, mas precisa cadastro correto.",
      "No cardapio digital, produto sem estoque deve sumir ou bloquear venda conforme regra configurada."
    ],
    tip: "Estoque errado piora a operacao. Comece simples e detalhe depois."
  },
  {
    id: "online",
    title: "9. Cardapio digital, garcom web, iFood e WhatsApp",
    items: [
      "Cardapio digital publica produtos ativos com preco, foto, categoria, disponibilidade e dados da loja.",
      "Descontos e fidelidade so devem aparecer quando forem criados no sistema.",
      "Garcom web permite lancar itens pelo celular dentro da rede/local ou no modo online, conforme plano.",
      "iFood e WhatsApp sao recursos conectados de plano pago, com configuracao, homologacao e regras de terceiros.",
      "Pedidos online devem cair no PDV sem o caixa precisar buscar manualmente."
    ],
    tip: "Nao mostre recurso online falso para cliente final. Se a loja nao configurou, a pagina deve ficar limpa."
  },
  {
    id: "fechamento",
    title: "10. Fechamento do dia e relatorios",
    items: [
      "Antes de fechar, resolva mesas abertas, pedidos pendentes, fichas sem pagamento e vendas em conta.",
      "Confira dinheiro fisico com o total em dinheiro do sistema.",
      "Compare Pix/cartao com comprovantes, maquininha ou gateway.",
      "Imprima ou salve resumo do caixa com vendas, retiradas, suprimentos, cancelamentos e formas de pagamento.",
      "Relatorios ajudam a ver produtos mais vendidos, horario de pico, margem e estoque baixo."
    ],
    tip: "Fechamento bom e aquele que outra pessoa consegue conferir no dia seguinte."
  },
  {
    id: "problemas",
    title: "11. Problemas comuns e como resolver",
    items: [
      "Nao imprime: confira se a impressora aparece no Windows, se esta como padrao/setor correto e se tem papel.",
      "Produto nao aparece: veja se esta ativo, com estoque, marcado para mostrar na venda/cardapio e na categoria correta.",
      "Garcom nao conecta: confira rede, IP, licenca online, permissao do operador e se o PDV esta aberto.",
      "Pagamento nao finaliza: confira forma selecionada, valor recebido, internet quando for Pix/online e permissao do operador.",
      "Cardapio online estranho: limpe cache, confirme produtos publicados, descontos reais e dados da loja no PDV."
    ],
    tip: "Quando chamar suporte, envie print da tela, horario, operador, mesa/ficha e o que tentou fazer."
  }
];

const routine = [
  ["Antes de abrir", "Ligar computador, internet, impressoras, conferir papel, abrir caixa e testar uma impressao."],
  ["Durante venda", "Lancar tudo no pedido certo, usar observacao quando necessario, conferir total antes de receber."],
  ["Troca de turno", "Registrar sangria/suprimento, conferir dinheiro e deixar pendencias anotadas."],
  ["Fim do dia", "Fechar mesas e delivery, conferir pagamentos, imprimir resumo e salvar backup quando aplicavel."]
];

const glossary = [
  ["Comanda", "Conta aberta para mesa, cliente ou atendimento.", "Use quando o pedido ainda pode receber novos itens antes do pagamento."],
  ["Mesa", "Numero fisico ou identificacao usada pelo restaurante.", "Mesa livre nao tem consumo; mesa ocupada tem itens; mesa em conta esta no fechamento."],
  ["Ficha", "Venda rapida de balcao sem mesa.", "Boa para retirada, pedido direto no caixa, acai, lanche rapido ou evento."],
  ["Operador", "Pessoa logada no caixa ou responsavel pela acao.", "Ajuda a saber quem vendeu, recebeu, cancelou ou alterou uma conta."],
  ["Garcom", "Atendente vinculado a uma mesa ou pedido.", "Pode aparecer no percentual de servico e no historico da comanda."],
  ["Produto ativo", "Item liberado para venda.", "Se estiver inativo, nao deve aparecer na venda rapida nem no cardapio digital."],
  ["Categoria", "Grupo visual dos produtos.", "Exemplos: Lanches, Bebidas, Porcoes, Pizzas, Combos e Adicionais."],
  ["Adicional", "Complemento escolhido junto com um produto.", "Exemplos: bacon extra, cheddar, borda recheada, ovo, molho ou embalagem."],
  ["Observacao", "Texto livre para orientar cozinha ou entrega.", "Use para sem cebola, ponto da carne, trocar refrigerante, retirar talher."],
  ["Setor", "Destino de producao ou impressao.", "Exemplos: Cozinha, Chapa, Bar, Pizza, Balcao ou Sobremesa."],
  ["Impressora de setor", "Impressora usada por um setor especifico.", "Bebidas podem sair no Bar e lanches na Cozinha, sem misturar tudo no caixa."],
  ["Estoque minimo", "Quantidade que dispara alerta de reposicao.", "Quando o saldo baixa do minimo, o sistema avisa para comprar ou produzir mais."],
  ["Ficha tecnica", "Receita usada para baixar ingredientes.", "Ao vender um x-burger, por exemplo, pode baixar pao, carne, queijo e embalagem."],
  ["Sangria", "Retirada de dinheiro do caixa.", "Use quando o dinheiro sai da gaveta para cofre, dono, fornecedor ou seguranca."],
  ["Suprimento", "Entrada de dinheiro no caixa.", "Use quando adiciona troco, reforco de caixa ou dinheiro que nao veio de venda."],
  ["Pagamento antecipado", "Valor recebido antes de fechar a conta.", "Util para reserva, comanda parcial ou cliente que paga uma parte antes."],
  ["Troco", "Diferenca entre valor recebido e total da venda.", "No dinheiro, confira antes de finalizar para nao registrar errado."],
  ["Pix QR Code", "Codigo gerado para o cliente pagar.", "Quando integrado, usa o valor da venda para evitar digitacao manual."],
  ["Mercado Pago", "Pagamento online/gateway quando configurado.", "Pode ser testado conforme plano e credenciais liberadas."],
  ["Delivery", "Pedido para entrega.", "Inclui cliente, telefone, endereco, taxa, status, observacao e forma de pagamento."],
  ["Retirada", "Pedido que o cliente busca no local.", "Normalmente precisa nome, telefone e horario combinado."],
  ["Cardapio digital", "Pagina online dos produtos da loja.", "Mostra apenas o que estiver publicado e configurado para aparecer."],
  ["Garcom web", "Tela de atendimento no celular.", "Permite lancar item direto na mesa/comanda sem voltar ao caixa."],
  ["Fechamento", "Conferencia final do caixa e das formas de pagamento.", "Deve bater dinheiro, Pix, cartao, retiradas, suprimentos e pendencias."]
];

export default function HowToUsePage() {
  return (
    <main className="lpPage">
      <SiteHeader />

      <section className="infoPage guidePage guideManualPage">
        <div className="infoHero guideManualHero">
          <p className="eyebrow">Manual de operacao</p>
          <h1>Como usar o Balcao Livre PDV no Windows, do primeiro cadastro ao fechamento do caixa.</h1>
          <p>
            Um guia completo para dono, gerente, caixa e atendente entenderem a rotina do sistema sem depender de tentativa e erro.
          </p>
          <div className="guideHeroActions">
            <a className="lpSolidButton" href="#inicio-rapido">Comecar pelo resumo</a>
            <a className="lpGhostButton" href="#problemas">Resolver problema comum</a>
          </div>
        </div>

        <div className="guideNotice guideNoticeStrong">
          Use este manual como roteiro de implantacao: primeiro configure a loja, depois produtos/setores, depois caixa, depois canais online. Assim o restaurante evita cadastro baguncado e venda errada.
        </div>

        <section className="guideStep guideBookSection" id="inicio-rapido">
          <div className="guideSectionHead">
            <p className="eyebrow">Inicio rapido</p>
            <h2>O caminho certo para colocar a loja para vender</h2>
            <p>Se voce esta abrindo o sistema pela primeira vez, siga esta sequencia antes de atender cliente real.</p>
          </div>
          <div className="guideQuickGrid">
            {quickStart.map(([number, title, text]) => (
              <article key={number}>
                <span>{number}</span>
                <strong>{title}</strong>
                <p>{text}</p>
              </article>
            ))}
          </div>
        </section>

        <div className="guideSnapshotGrid" aria-label="Resumo rapido do exemplo">
          <div><strong>24</strong><span>produtos ativos com estoque</span></div>
          <div><strong>12</strong><span>mesas de atendimento</span></div>
          <div><strong>3</strong><span>canais: mesa, balcao e delivery</span></div>
          <div><strong>1</strong><span>caixa por turno para conferir</span></div>
        </div>

        <div className="infoLayout guideManualLayout">
          <aside className="infoAside guideAside" aria-label="Indice do guia">
            <a href="#inicio-rapido">Inicio rapido</a>
            <a href="#instalacao">Instalacao</a>
            <a href="#configuracao">Loja e operadores</a>
            <a href="#produtos">Produtos</a>
            <a href="#setores">Setores e impressoras</a>
            <a href="#caixa">Caixa</a>
            <a href="#atendimento">Pedidos</a>
            <a href="#pagamentos">Pagamentos</a>
            <a href="#estoque">Estoque</a>
            <a href="#online">Online</a>
            <a href="#fechamento">Fechamento</a>
            <a href="#problemas">Problemas</a>
          </aside>

          <div className="infoContent guideManualContent">
            <section className="guideStep guideBookSection" id="telas">
              <div className="guideSectionHead">
                <p className="eyebrow">Telas principais</p>
                <h2>Entenda onde cada tipo de venda acontece</h2>
              </div>
              <div className="guideScreenStack">
                {screens.map((screen) => (
                  <article className="guideStepWithImage guideScreenChapter" id={screen.id} key={screen.id}>
                    <div>
                      <h3>{screen.title}</h3>
                      <p>{screen.caption}</p>
                      <ul>
                        {screen.points.map((point) => <li key={point}>{point}</li>)}
                      </ul>
                    </div>
                    <figure className="guideScreenshot">
                      <img src={screen.image} alt={screen.caption} loading="lazy" />
                    </figure>
                  </article>
                ))}
              </div>
            </section>

            {chapters.map((chapter) => (
              <section className="guideStep guideChapter" id={chapter.id} key={chapter.id}>
                <h2>{chapter.title}</h2>
                <ol className="guideInstructionList">
                  {chapter.items.map((item) => <li key={item}>{item}</li>)}
                </ol>
                <div className="guideTip"><strong>Na pratica:</strong> {chapter.tip}</div>
              </section>
            ))}

            <section className="guideStep guideBookSection" id="rotina">
              <div className="guideSectionHead">
                <p className="eyebrow">Rotina operacional</p>
                <h2>Checklist simples para usar todos os dias</h2>
              </div>
              <div className="guideChecklistGrid">
                {routine.map(([title, text]) => (
                  <article key={title}>
                    <strong>{title}</strong>
                    <p>{text}</p>
                  </article>
                ))}
              </div>
            </section>

            <section className="guideStep guideBookSection" id="glossario">
              <div className="guideSectionHead">
                <p className="eyebrow">Glossario</p>
                <h2>Palavras que aparecem no PDV</h2>
              </div>
              <div className="guideGlossary">
                {glossary.map(([title, text, context]) => (
                  <div key={title}>
                    <strong>{title}</strong>
                    <span>{text}</span>
                    <em>{context}</em>
                  </div>
                ))}
              </div>
            </section>
          </div>
        </div>
      </section>
    </main>
  );
}
