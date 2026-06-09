import { checkoutFunctionUrl, downloadUrl, onlineDownloadUrl, sellers } from "./siteLinks";
import seoPageInsights from "./seoPageInsights.json";

const baseFeatures = [
  "Caixa Windows para vender no balcão",
  "Mesas, comandas, delivery e retirada",
  "Estoque, margem e produtos mais vendidos",
  "Comprovante em impressora 58/80mm",
  "Teste grátis por 7 dias"
];

const connectedFeatures = [
  "Cardápio online com QR Code",
  "Garçom no celular",
  "WhatsApp para atendimento e pedidos",
  "Equipe, entregadores e permissões",
  "Mercado Pago, iFood e NFC-e quando configurados"
];

const planCopy = {
  offline: {
    label: "PDV Caixa Local",
    price: "R$17/mês",
    cta: "Testar PDV local",
    href: downloadUrl,
    analyticsPlan: "offline"
  },
  online: {
    label: "Restaurante Profissional",
    price: "R$139/mês",
    cta: "Testar PDV online",
    href: onlineDownloadUrl,
    analyticsPlan: "online"
  },
  whatsapp: {
    label: "Falar com vendedor",
    price: "Plano certo para sua loja",
    cta: "Falar no WhatsApp",
    href: sellers[1].href,
    analyticsPlan: "whatsapp"
  }
};

function mergeUnique(base = [], extra = []) {
  return [...new Set([...base, ...extra].filter(Boolean))];
}

function applySeoInsight(page) {
  const insight = seoPageInsights?.pages?.[page.slug];
  if (!insight) return page;

  return {
    ...page,
    ...insight,
    keywords: mergeUnique(insight.keywords, page.keywords),
    outcomes: insight.outcomes?.length ? insight.outcomes : page.outcomes,
    features: insight.features?.length ? mergeUnique(page.features, insight.features) : page.features,
    faq: insight.faq?.length ? insight.faq : page.faq,
    autoImprovedAt: insight.updatedAt || seoPageInsights.updatedAt || null
  };
}

const rawSeoPages = [
  {
    slug: "pdv-delivery-gratuito",
    eyebrow: "Teste grátis para delivery",
    title: "PDV Delivery Grátis por 7 dias",
    metaTitle: "PDV Delivery Grátis por 7 dias | Balcão Livre PDV",
    description: "Teste um PDV delivery grátis por 7 dias com caixa Windows, pedidos, entregas, cardápio online, WhatsApp, estoque e comprovante.",
    h1: "PDV delivery grátis para testar no Windows",
    lead: "Ideal para quem quer sair do papel e testar um fluxo de delivery antes de contratar. O Balcão Livre organiza pedido, cliente, telefone, endereço, entregador, taxa, pagamento e impressão.",
    segment: "Delivery",
    plan: "online",
    keywords: ["pdv delivery gratuito", "pdv delivery grátis", "sistema delivery grátis", "pdv para delivery"],
    outcomes: ["Receber pedidos sem perder informação", "Organizar entrega, retirada e balcão", "Testar por 7 dias antes de pagar"],
    features: [...baseFeatures, ...connectedFeatures],
    faq: [
      ["O PDV delivery é gratuito?", "O teste é grátis por 7 dias. Depois, o caixa local começa em R$17/mês e o plano restaurante conectado fica em R$139/mês."],
      ["Serve para delivery pequeno?", "Sim. Dá para começar com balcão e entrega simples e depois ativar cardápio online, WhatsApp, equipe e integrações."],
      ["O iFood entra no teste?", "O iFood depende de credenciais e homologação. Ele entra no plano profissional quando a loja já tem as liberações necessárias."]
    ]
  },
  {
    slug: "pdv-para-restaurante",
    eyebrow: "Sistema para restaurante",
    title: "PDV para Restaurante",
    metaTitle: "PDV para Restaurante com Caixa, Mesas e Delivery",
    description: "PDV para restaurante com caixa Windows, mesas, comandas, delivery, estoque, cardápio online, garçom no celular, WhatsApp e NFC-e configurável.",
    h1: "PDV para restaurante que controla caixa, mesa e delivery",
    lead: "Uma rotina única para vender no caixa, lançar pedidos na mesa, acompanhar delivery, controlar estoque e fechar o dia com menos erro.",
    segment: "Restaurante",
    plan: "online",
    keywords: ["pdv para restaurante", "sistema para restaurante", "software restaurante", "caixa restaurante"],
    outcomes: ["Caixa mais rápido", "Pedido na mesa sem retrabalho", "Fechamento do dia com resumo claro"],
    features: [...baseFeatures, ...connectedFeatures],
    faq: [
      ["Funciona em restaurante com mesas?", "Sim. O sistema trabalha com mesas, comandas, balcão, retirada e delivery."],
      ["Funciona sem internet?", "O caixa local continua operando. Recursos de nuvem, cardápio, garçom e WhatsApp precisam de internet."],
      ["Tem NFC-e?", "O plano profissional tem NFC-e configurável conforme certificado, dados fiscais e regra da UF."]
    ]
  },
  {
    slug: "pdv-para-lanchonete",
    eyebrow: "Sistema para lanchonete",
    title: "PDV para Lanchonete",
    metaTitle: "PDV para Lanchonete com Delivery e Comandas",
    description: "PDV para lanchonete com venda rápida, combos, adicionais, mesas, comandas, delivery, WhatsApp, cardápio online e controle de estoque.",
    h1: "PDV para lanchonete vender rápido no balcão e no delivery",
    lead: "Para lanchonete que precisa lançar produtos, adicionais, observações e pagamentos sem travar a fila.",
    segment: "Lanchonete",
    plan: "online",
    keywords: ["pdv para lanchonete", "sistema para lanchonete", "sistema de caixa lanchonete"],
    outcomes: ["Reduzir erro em adicional e observação", "Controlar combos e bebidas", "Receber pedido online sem bagunça"],
    features: [...baseFeatures, "Adicionais e observações no pedido", "Cardápio online para combos e bebidas", "WhatsApp para atendimento"],
    faq: [
      ["Dá para lançar adicional?", "Sim. O pedido aceita adicionais, observações e complementos conforme o cadastro dos produtos."],
      ["Serve para balcão movimentado?", "Sim. O foco do PDV é venda rápida no Windows com busca, código e atalhos."],
      ["Consigo controlar estoque de bebida?", "Sim. O sistema mostra estoque, margem e produtos mais vendidos."]
    ]
  },
  {
    slug: "pdv-para-pizzaria",
    eyebrow: "Sistema para pizzaria",
    title: "PDV para Pizzaria",
    metaTitle: "PDV para Pizzaria com Delivery, Sabores e Entregas",
    description: "PDV para pizzaria com delivery, mesas, comandas, sabores, adicionais, endereço, entregador, taxa, WhatsApp e cardápio online.",
    h1: "PDV para pizzaria controlar pedidos, sabores e entregas",
    lead: "Organize pizza, bebida, adicional, endereço, taxa de entrega e status do pedido em um fluxo direto para o caixa.",
    segment: "Pizzaria",
    plan: "online",
    keywords: ["pdv para pizzaria", "sistema para pizzaria", "sistema delivery pizzaria"],
    outcomes: ["Menos erro em sabores e adicionais", "Entrega com cliente e endereço salvos", "Pedidos por WhatsApp e cardápio online"],
    features: [...baseFeatures, "Delivery com taxa e entregador", "Observações e adicionais", "WhatsApp e cardápio online"],
    faq: [
      ["Dá para usar em pizzaria delivery?", "Sim. O plano profissional organiza delivery, retirada, balcão e mesas."],
      ["Tem controle de entregador?", "Sim. É possível cadastrar entregadores e acompanhar pedidos por status."],
      ["Dá para vender pelo WhatsApp?", "Sim, quando o WhatsApp está ativado no plano profissional."]
    ]
  },
  {
    slug: "pdv-para-hamburgueria",
    eyebrow: "Sistema para hamburgueria",
    title: "PDV para Hamburgueria",
    metaTitle: "PDV para Hamburgueria com Combos e WhatsApp",
    description: "PDV para hamburgueria com combos, adicionais, bebidas, delivery, mesas, comandas, cardápio online, WhatsApp e estoque.",
    h1: "PDV para hamburgueria vender combo sem perder detalhe",
    lead: "Monte pedidos com hambúrguer, adicional, bebida, ponto, observação, entrega e pagamento sem depender de anotação solta.",
    segment: "Hamburgueria",
    plan: "online",
    keywords: ["pdv para hamburgueria", "sistema para hamburgueria", "sistema delivery hamburgueria"],
    outcomes: ["Pedido completo com adicionais", "Cardápio online para combos", "WhatsApp respondendo clientes"],
    features: [...baseFeatures, "Combos e adicionais", "Cardápio online com QR Code", "Delivery e WhatsApp"],
    faq: [
      ["Consigo vender combo?", "Sim. Você cadastra produtos e adicionais para montar o pedido com mais controle."],
      ["O cliente pode pedir pelo cardápio online?", "Sim. O plano profissional publica produtos no cardápio digital."],
      ["O WhatsApp pode atender cliente?", "Sim. O WhatsApp fica conectado ao fluxo do PDV quando configurado."]
    ]
  },
  {
    slug: "pdv-para-acaiteria",
    eyebrow: "Sistema para açaíteria",
    title: "PDV para Açaíteria",
    metaTitle: "PDV para Açaíteria com Complementos e Delivery",
    description: "PDV para açaíteria com tamanhos, complementos, adicionais, delivery, cardápio online, WhatsApp, estoque e comprovante.",
    h1: "PDV para açaíteria controlar tamanhos, complementos e entrega",
    lead: "Venda açaí no balcão ou delivery com tamanho, complemento, observação, pagamento e estoque no mesmo sistema.",
    segment: "Açaíteria",
    plan: "online",
    keywords: ["pdv para açaiteria", "sistema para açaiteria", "pdv açai delivery"],
    outcomes: ["Complementos sem erro", "Controle de estoque por produto", "Pedido online mais organizado"],
    features: [...baseFeatures, "Adicionais e complementos", "Cardápio online", "WhatsApp para pedidos"],
    faq: [
      ["Serve para produtos com complemento?", "Sim. Dá para cadastrar adicionais e observações no pedido."],
      ["Tem estoque?", "Sim. O PDV mostra estoque, margem e produtos vendidos."],
      ["Dá para testar antes?", "Sim. O teste grátis dura 7 dias."]
    ]
  },
  {
    slug: "pdv-para-bar",
    eyebrow: "Sistema para bar",
    title: "PDV para Bar",
    metaTitle: "PDV para Bar com Mesas, Comandas e Estoque",
    description: "PDV para bar com mesas, comandas, balcão, consumo aberto, fechamento de conta, estoque de bebidas e impressão de comprovante.",
    h1: "PDV para bar controlar mesas, comandas e consumo aberto",
    lead: "Abra mesa, lance consumo, acompanhe comandas e feche a conta com menos erro no caixa.",
    segment: "Bar",
    plan: "offline",
    keywords: ["pdv para bar", "sistema para bar", "sistema de comandas bar"],
    outcomes: ["Comandas organizadas", "Conta certa no fechamento", "Estoque de bebidas acompanhado"],
    features: [...baseFeatures, "Mesas e comandas", "Fechamento de conta", "Impressão de comprovante"],
    faq: [
      ["Funciona para bar pequeno?", "Sim. O plano local já atende caixa, mesas, comandas, estoque e impressão."],
      ["Precisa de internet?", "Para o caixa local, não. Recursos online usam internet."],
      ["Tem relatório de fechamento?", "Sim. O sistema mostra vendas, pagamentos e movimento do caixa."]
    ]
  },
  {
    slug: "pdv-para-espetinho",
    eyebrow: "Sistema para espetinho",
    title: "PDV para Espetinho",
    metaTitle: "PDV para Espetinho com Mesas, Balcão e Delivery",
    description: "PDV para espetinho com mesas, comandas, balcão, bebidas, delivery, estoque, pagamentos e comprovante no Windows.",
    h1: "PDV para espetinho vender no balcão, mesa e entrega",
    lead: "Controle espetinhos, bebidas, adicionais, mesa, comanda e pagamento sem depender de caderno.",
    segment: "Espetinho",
    plan: "offline",
    keywords: ["pdv para espetinho", "sistema para espetinho", "sistema de caixa espetinho"],
    outcomes: ["Mais controle no balcão", "Comandas por mesa", "Fechamento rápido"],
    features: [...baseFeatures, "Mesas e comandas", "Controle de bebidas", "Comprovante no Windows"],
    faq: [
      ["Serve para espetinho com mesas?", "Sim. O PDV trabalha com mesa, comanda e balcão."],
      ["Dá para controlar bebidas?", "Sim. Você cadastra produtos e acompanha estoque."],
      ["Qual plano começar?", "Para caixa local, o plano de R$17/mês costuma resolver. Para online e WhatsApp, use o profissional."]
    ]
  },
  {
    slug: "sistema-de-comandas-restaurante",
    eyebrow: "Comandas para restaurante",
    title: "Sistema de Comandas para Restaurante",
    metaTitle: "Sistema de Comandas para Restaurante e Bar",
    description: "Sistema de comandas para restaurante, bar e lanchonete com mesas, consumo aberto, garçom no celular, impressão e fechamento de conta.",
    h1: "Sistema de comandas para restaurante fechar conta sem erro",
    lead: "Abra mesa, lance produtos, acompanhe consumo e feche a conta com histórico claro para operador, garçom e gerente.",
    segment: "Comandas",
    plan: "online",
    keywords: ["sistema de comandas", "comanda restaurante", "sistema de mesa restaurante"],
    outcomes: ["Consumo aberto por mesa", "Garçom lançando no celular", "Fechamento com comprovante"],
    features: [...baseFeatures, "Mesa e comanda", "Garçom no celular", "Fechamento e impressão"],
    faq: [
      ["Tem comanda por mesa?", "Sim. O sistema controla mesas e comandas abertas."],
      ["O garçom lança pelo celular?", "Sim, no plano profissional."],
      ["Dá para imprimir a conta?", "Sim. O PDV imprime comprovante e fechamento."]
    ]
  },
  {
    slug: "sistema-para-mesas-e-comandas",
    eyebrow: "Mesas e comandas",
    title: "Sistema para Mesas e Comandas",
    metaTitle: "Sistema para Mesas e Comandas no Windows",
    description: "Sistema para mesas e comandas com caixa Windows, consumo aberto, garçom no celular, pagamento, impressão e relatório de fechamento.",
    h1: "Sistema para mesas e comandas com caixa Windows",
    lead: "Controle mesas ocupadas, consumo por comanda, pagamentos parciais e fechamento em uma rotina mais simples.",
    segment: "Mesas e comandas",
    plan: "online",
    keywords: ["sistema para mesas e comandas", "pdv mesas e comandas", "controle de mesas restaurante"],
    outcomes: ["Mesa organizada", "Pedido sem retrabalho", "Conta fechada com comprovante"],
    features: [...baseFeatures, "Garçom no celular", "Conta por mesa", "Histórico do atendimento"],
    faq: [
      ["Dá para controlar mesa e comanda?", "Sim. A loja pode trabalhar por mesa, comanda, balcão ou delivery."],
      ["Tem pagamento separado?", "O fluxo do caixa registra recebimentos e fechamento conforme a operação."],
      ["Serve para bar e restaurante?", "Sim. É indicado para restaurante, bar, lanchonete e similares."]
    ]
  },
  {
    slug: "sistema-para-garcom-no-celular",
    eyebrow: "Garçom no celular",
    title: "Sistema para Garçom no Celular",
    metaTitle: "Sistema para Garçom no Celular Integrado ao PDV",
    description: "Sistema para garçom no celular lançar pedidos direto na mesa ou comanda, com caixa Windows, cozinha, estoque e fechamento integrados.",
    h1: "Garçom no celular lançando pedido direto no PDV",
    lead: "O pedido sai do celular da equipe e chega na conta certa, sem voltar ao caixa para redigitar.",
    segment: "Garçom",
    plan: "online",
    keywords: ["garçom no celular", "sistema para garçom", "pedido pelo celular restaurante"],
    outcomes: ["Menos fila no caixa", "Pedido chega mais rápido", "Equipe acompanha a operação"],
    features: [...connectedFeatures, "Mesa e comanda integradas", "Caixa Windows como base"],
    faq: [
      ["O garçom precisa instalar app?", "O acesso é pensado para uso simples no celular conforme a configuração do plano."],
      ["O pedido entra no caixa?", "Sim. O pedido entra no fluxo do PDV."],
      ["Funciona para mesa e comanda?", "Sim. O garçom pode lançar no contexto da mesa ou comanda."]
    ]
  },
  {
    slug: "pdv-com-cardapio-digital",
    eyebrow: "Cardápio digital",
    title: "PDV com Cardápio Digital",
    metaTitle: "PDV com Cardápio Digital, QR Code e WhatsApp",
    description: "PDV com cardápio digital, QR Code, produtos online, pedidos, WhatsApp, estoque e caixa Windows integrados para restaurante.",
    h1: "PDV com cardápio digital ligado ao caixa da loja",
    lead: "Publique produtos no cardápio online, use QR Code e mantenha estoque, preços e pedidos conectados ao PDV.",
    segment: "Cardápio digital",
    plan: "online",
    keywords: ["pdv com cardápio digital", "cardápio digital restaurante", "cardápio com qr code"],
    outcomes: ["Produto publicado com preço certo", "QR Code para o cliente acessar", "Pedidos conectados ao caixa"],
    features: [...connectedFeatures, "QR Code do cardápio", "Produtos e estoque sincronizados"],
    faq: [
      ["Tem QR Code?", "Sim. O cardápio digital gera link e QR Code para a loja divulgar."],
      ["Atualiza estoque?", "A proposta é manter cardápio e estoque conectados ao PDV."],
      ["O cliente pede online?", "Sim, quando o cardápio online está ativado."]
    ]
  },
  {
    slug: "cardapio-digital-com-whatsapp",
    eyebrow: "Cardápio + WhatsApp",
    title: "Cardápio Digital com WhatsApp",
    metaTitle: "Cardápio Digital com WhatsApp para Restaurante",
    description: "Cardápio digital com WhatsApp para restaurante receber pedidos, responder clientes e levar a venda para o fluxo do PDV.",
    h1: "Cardápio digital com WhatsApp para vender mais sem bagunçar o caixa",
    lead: "O cliente vê produtos no cardápio e chama a loja pelo WhatsApp. O atendimento fica conectado ao fluxo do PDV.",
    segment: "WhatsApp",
    plan: "online",
    keywords: ["cardápio digital com whatsapp", "cardápio whatsapp restaurante", "pedido whatsapp restaurante"],
    outcomes: ["Cliente encontra o produto", "WhatsApp vira canal de pedido", "Atendimento com histórico"],
    features: [...connectedFeatures, "Link do cardápio", "Atendimento pelo WhatsApp"],
    faq: [
      ["O cliente pode chamar no WhatsApp?", "Sim. O cardápio pode direcionar para atendimento e pedido pelo WhatsApp."],
      ["Precisa de aprovação da Meta?", "Depende do modelo de WhatsApp configurado para a loja."],
      ["O pedido aparece no PDV?", "A integração foi feita para levar o atendimento ao fluxo do PDV quando configurado."]
    ]
  },
  {
    slug: "pdv-com-whatsapp-para-restaurante",
    eyebrow: "WhatsApp para restaurante",
    title: "PDV com WhatsApp para Restaurante",
    metaTitle: "PDV com WhatsApp para Restaurante e Delivery",
    description: "PDV com WhatsApp para restaurante atender clientes, enviar cardápio, receber pedidos, registrar histórico e organizar delivery.",
    h1: "PDV com WhatsApp para atender e receber pedido",
    lead: "Transforme o WhatsApp em um canal de atendimento ligado à operação da loja, sem deixar o caixa fora da conversa.",
    segment: "WhatsApp",
    plan: "online",
    keywords: ["pdv com whatsapp", "whatsapp para restaurante", "pedido pelo whatsapp restaurante"],
    outcomes: ["Responder cliente mais rápido", "Receber pedido com histórico", "Pausar automático quando precisar de atendente"],
    features: [...connectedFeatures, "Conversas dentro do PDV", "Atendimento automático configurável"],
    faq: [
      ["O WhatsApp responde sozinho?", "Quando configurado, o atendimento pode responder cardápio, horários, status e pedidos."],
      ["A loja pode conversar manualmente?", "Sim. O atendimento manual pausa o automático para aquele cliente enquanto a conversa estiver aberta."],
      ["Funciona com PDV fechado?", "O atendimento online foi pensado para continuar respondendo pelo serviço conectado."]
    ]
  },
  {
    slug: "pdv-com-ifood",
    eyebrow: "iFood no PDV",
    title: "PDV com iFood",
    metaTitle: "PDV com iFood para Restaurante e Delivery",
    description: "PDV com iFood para restaurante receber pedidos, acompanhar status, organizar produção, entrega e caixa conforme credenciais e homologação.",
    h1: "PDV com iFood para centralizar delivery e caixa",
    lead: "Quando a loja tem as credenciais necessárias, o pedido do iFood entra no fluxo operacional junto com balcão, delivery próprio e fechamento.",
    segment: "iFood",
    plan: "online",
    keywords: ["pdv com ifood", "sistema com ifood", "integrar ifood ao pdv"],
    outcomes: ["Menos tela aberta", "Pedido externo no fluxo do caixa", "Produção e status organizados"],
    features: [...connectedFeatures, "Entrada de pedidos iFood conforme credenciais", "Status e despacho"],
    faq: [
      ["O iFood já vem liberado?", "Ele depende de credenciais, homologação e regras do iFood."],
      ["O teste inclui iFood?", "No teste, o iFood fica desabilitado. Ele entra na implantação do plano profissional."],
      ["O pedido aparece no PDV?", "Sim, quando a integração está configurada corretamente."]
    ]
  },
  {
    slug: "pdv-com-mercado-pago",
    eyebrow: "Mercado Pago no caixa",
    title: "PDV com Mercado Pago",
    metaTitle: "PDV com Mercado Pago, Pix e Point",
    description: "PDV com Mercado Pago para restaurante receber Pix, cartão, Point quando configurado, comprovante, caixa e fechamento organizados.",
    h1: "PDV com Mercado Pago para receber sem perder o controle do caixa",
    lead: "Registre pagamentos, organize formas de recebimento e mantenha o fechamento do dia mais claro para a loja.",
    segment: "Pagamentos",
    plan: "online",
    keywords: ["pdv com mercado pago", "pdv com point mercado pago", "sistema mercado pago restaurante"],
    outcomes: ["Pagamento registrado no caixa", "Pix e cartão organizados", "Fechamento com menos conferência manual"],
    features: [...baseFeatures, "Mercado Pago conforme credenciais", "Pix, cartão, dinheiro e troco", "Fechamento de caixa"],
    faq: [
      ["Funciona com Point?", "Funciona quando a conta e a maquininha estão configuradas corretamente."],
      ["Dá para usar Pix?", "Sim. O caixa registra Pix e outras formas de pagamento."],
      ["Mostra no fechamento?", "Sim. O fechamento separa formas de pagamento para conferência."]
    ]
  },
  {
    slug: "pdv-com-nfce-restaurante",
    eyebrow: "Fiscal para restaurante",
    title: "PDV com NFC-e para Restaurante",
    metaTitle: "PDV com NFC-e Configurável para Restaurante",
    description: "PDV com NFC-e configurável para restaurante, caixa Windows, certificado, dados fiscais, produtos, pagamentos e comprovante.",
    h1: "PDV com NFC-e configurável para restaurante",
    lead: "O plano profissional tem área fiscal configurável para empresas que precisam preparar emissão conforme certificado, dados fiscais e regra da UF.",
    segment: "NFC-e",
    plan: "online",
    keywords: ["pdv com nfce", "pdv restaurante nfce", "sistema nfce restaurante"],
    outcomes: ["Dados fiscais organizados", "Produtos preparados para emissão", "Caixa e fiscal no mesmo sistema"],
    features: [...connectedFeatures, "NFC-e configurável", "Certificado e dados fiscais", "Regras conforme UF"],
    faq: [
      ["A NFC-e é automática?", "Ela precisa ser configurada com certificado, dados fiscais e regras do estado."],
      ["Serve para restaurante?", "Sim. NFC-e é o documento mais comum para venda ao consumidor no varejo/restaurante, conforme UF."],
      ["O comprovante substitui nota?", "Não. Comprovante do PDV não substitui documento fiscal."]
    ]
  },
  {
    slug: "sistema-de-caixa-para-restaurante",
    eyebrow: "Caixa para restaurante",
    title: "Sistema de Caixa para Restaurante",
    metaTitle: "Sistema de Caixa para Restaurante no Windows",
    description: "Sistema de caixa para restaurante com venda rápida, dinheiro, Pix, cartão, troco, comprovante, mesas, comandas e fechamento do dia.",
    h1: "Sistema de caixa para restaurante vender e fechar o dia",
    lead: "Para loja que precisa de um caixa Windows direto, com produtos, pagamento, troco, comprovante e resumo do movimento.",
    segment: "Caixa",
    plan: "offline",
    keywords: ["sistema de caixa para restaurante", "caixa restaurante", "pdv caixa restaurante"],
    outcomes: ["Venda rápida no Windows", "Troco e pagamento registrados", "Fechamento do caixa mais claro"],
    features: [...baseFeatures, "Dinheiro, Pix manual e cartão", "Resumo do caixa", "Impressão"],
    faq: [
      ["Qual plano serve para caixa?", "O plano PDV Caixa Local de R$17/mês já atende o caixa Windows."],
      ["Tem comprovante?", "Sim. Imprime comprovante em impressora configurada no Windows."],
      ["Tem mesas?", "Sim. O fluxo inclui mesas e comandas."]
    ]
  },
  {
    slug: "controle-de-estoque-restaurante",
    eyebrow: "Estoque e margem",
    title: "Controle de Estoque para Restaurante",
    metaTitle: "Controle de Estoque para Restaurante no PDV",
    description: "Controle de estoque para restaurante com produtos, preço de compra, preço de venda, margem, estoque baixo, vendas e relatórios.",
    h1: "Controle de estoque para restaurante junto com o caixa",
    lead: "Acompanhe produtos, estoque, margem e itens vendidos sem separar o controle da rotina do caixa.",
    segment: "Estoque",
    plan: "offline",
    keywords: ["controle de estoque restaurante", "estoque restaurante", "pdv com estoque"],
    outcomes: ["Ver estoque baixo", "Acompanhar margem", "Identificar produtos mais vendidos"],
    features: [...baseFeatures, "Preço de compra e venda", "Margem", "Relatórios"],
    faq: [
      ["O estoque baixa na venda?", "O PDV controla produtos e movimentações conforme o cadastro e uso da loja."],
      ["Mostra margem?", "Sim. O cadastro permite trabalhar preço de compra, venda e margem."],
      ["Serve para bebida e insumo?", "Serve para produtos cadastrados no fluxo do PDV."]
    ]
  },
  {
    slug: "pdv-para-delivery",
    eyebrow: "PDV para delivery",
    title: "PDV para Delivery",
    metaTitle: "PDV para Delivery com WhatsApp e Cardápio Online",
    description: "PDV para delivery com cliente, endereço, telefone, taxa, entregador, status, WhatsApp, cardápio online, pagamento e impressão.",
    h1: "PDV para delivery controlar pedido do atendimento à entrega",
    lead: "Receba o pedido, registre cliente, endereço, telefone, taxa e entregador, acompanhe status e feche pagamento no caixa.",
    segment: "Delivery",
    plan: "online",
    keywords: ["pdv para delivery", "sistema para delivery", "sistema delivery restaurante"],
    outcomes: ["Endereço e telefone no pedido", "Entregador e taxa organizados", "Status claro para equipe"],
    features: [...baseFeatures, ...connectedFeatures, "Delivery por zona"],
    faq: [
      ["Serve para delivery próprio?", "Sim. O sistema controla pedido, cliente, endereço, taxa e entregador."],
      ["Tem WhatsApp?", "Sim, no plano profissional configurado."],
      ["Tem cardápio online?", "Sim. O cliente pode acessar o cardápio digital quando ativado."]
    ]
  },
  {
    slug: "pdv-restaurante-windows",
    eyebrow: "PDV Windows",
    title: "PDV Restaurante Windows",
    metaTitle: "PDV Restaurante Windows com Caixa Offline",
    description: "PDV restaurante Windows com caixa offline, mesas, comandas, estoque, pagamentos, impressão e opção online com cardápio, WhatsApp e garçom.",
    h1: "PDV restaurante Windows para vender mesmo sem internet",
    lead: "O caixa local roda no Windows e mantém a venda funcionando. Quando a loja precisa crescer, o plano profissional conecta cardápio, equipe, garçom, WhatsApp e nuvem.",
    segment: "Windows",
    plan: "offline",
    keywords: ["pdv restaurante windows", "pdv windows restaurante", "sistema windows restaurante"],
    outcomes: ["Caixa local mais confiável", "Impressão pelo Windows", "Plano online quando precisar conectar a loja"],
    features: [...baseFeatures, "Aplicativo Windows", "Modo local offline", "Plano online opcional"],
    faq: [
      ["É Windows mesmo?", "Sim. O PDV principal é instalado no Windows."],
      ["Funciona offline?", "O caixa local funciona sem depender da internet."],
      ["Posso mudar para online depois?", "Sim. O plano profissional adiciona recursos conectados."]
    ]
  },
  {
    slug: "software-para-restaurante-pequeno",
    eyebrow: "Restaurante pequeno",
    title: "Software para Restaurante Pequeno",
    metaTitle: "Software para Restaurante Pequeno com PDV Windows",
    description: "Software para restaurante pequeno com caixa Windows, mesas, comandas, estoque, impressão, delivery e opção de cardápio online e WhatsApp.",
    h1: "Software para restaurante pequeno começar simples e crescer",
    lead: "Comece pelo caixa local e evolua para cardápio online, WhatsApp, garçom no celular e integrações quando fizer sentido para a operação.",
    segment: "Restaurante pequeno",
    plan: "offline",
    keywords: ["software para restaurante pequeno", "sistema para restaurante pequeno", "pdv restaurante pequeno"],
    outcomes: ["Baixo custo para começar", "Fluxo simples no caixa", "Crescimento para online sem trocar de sistema"],
    features: [...baseFeatures, "Plano local acessível", "Plano profissional opcional", "Suporte na implantação"],
    faq: [
      ["Qual o menor plano?", "O caixa local começa em R$17/mês."],
      ["Dá para testar?", "Sim. O teste é grátis por 7 dias."],
      ["Preciso contratar tudo de uma vez?", "Não. Você pode começar local e depois ir para o profissional."]
    ]
  },
  {
    slug: "sistema-para-delivery-com-whatsapp",
    eyebrow: "Delivery pelo WhatsApp",
    title: "Sistema para Delivery com WhatsApp",
    metaTitle: "Sistema para Delivery com WhatsApp e PDV",
    description: "Sistema para delivery com WhatsApp, cardápio, pedidos, cliente, endereço, taxa, entregador, status, pagamento e caixa Windows.",
    h1: "Sistema para delivery com WhatsApp integrado ao PDV",
    lead: "Atenda pelo WhatsApp, envie cardápio, registre pedido e leve a venda para o fluxo do caixa e da entrega.",
    segment: "Delivery WhatsApp",
    plan: "online",
    keywords: ["sistema delivery whatsapp", "delivery com whatsapp", "pedido whatsapp delivery"],
    outcomes: ["WhatsApp como canal de venda", "Pedido com cliente e endereço", "Equipe acompanha status"],
    features: [...connectedFeatures, "Delivery com taxa", "Conversas com histórico", "Pedido no PDV"],
    faq: [
      ["O cliente pode pedir pelo WhatsApp?", "Sim, quando a opção de pedidos por WhatsApp está ativada na loja."],
      ["Também tem cardápio online?", "Sim. O plano profissional trabalha com cardápio digital e WhatsApp."],
      ["O PDV precisa ficar aberto?", "O atendimento online foi criado para continuar respondendo pelo serviço conectado."]
    ]
  },
  {
    slug: "pdv-com-pix-restaurante",
    eyebrow: "Pix no restaurante",
    title: "PDV com Pix para Restaurante",
    metaTitle: "PDV com Pix para Restaurante, Caixa e Delivery",
    description: "PDV com Pix para restaurante registrar pagamentos, caixa, delivery, mesas, comandas, troco, comprovante e fechamento do dia.",
    h1: "PDV com Pix para restaurante fechar o caixa sem confusão",
    lead: "Registre Pix, dinheiro, cartão, troco e comprovante no mesmo fechamento para saber quanto entrou em cada forma de pagamento.",
    segment: "Pix",
    plan: "offline",
    keywords: ["pdv com pix", "sistema restaurante pix", "caixa com pix restaurante"],
    outcomes: ["Pix registrado no caixa", "Formas de pagamento separadas", "Fechamento mais fácil de conferir"],
    features: [...baseFeatures, "Pix manual", "Dinheiro e cartão", "Resumo por forma de pagamento"],
    faq: [
      ["O Pix entra no fechamento?", "Sim. O pagamento por Pix fica registrado como forma de recebimento."],
      ["Tem QR Pix?", "O fluxo pode trabalhar com Pix conforme configuração da loja."],
      ["Também aceita dinheiro e cartão?", "Sim. O PDV registra dinheiro, Pix, cartão e troco."]
    ]
  }
];

const commercialSeoPages = [
  {
    slug: "pdv-gratis-para-restaurante",
    eyebrow: "Teste gratis para restaurante",
    title: "PDV Gratis para Restaurante",
    metaTitle: "PDV Gratis para Restaurante por 7 dias | Balcao Livre PDV",
    description: "Teste um PDV gratis para restaurante por 7 dias com caixa Windows, mesas, comandas, estoque, comprovante, delivery e WhatsApp.",
    h1: "PDV gratis para restaurante testar antes de pagar",
    lead: "Para restaurante que quer testar o caixa na pratica antes de contratar. Comece vendendo no Windows, lance produtos, imprima comprovante e veja se o fluxo encaixa na loja.",
    segment: "Teste gratis",
    plan: "offline",
    keywords: ["pdv gratis para restaurante", "sistema gratis para restaurante", "teste gratis pdv restaurante"],
    outcomes: ["Testar antes de assinar", "Comecar com baixo custo", "Validar caixa e impressao no Windows"],
    features: [...baseFeatures, "Teste gratis por 7 dias", "Plano local a partir de R$17", "Upgrade para restaurante conectado"],
    faq: [
      ["O PDV e gratis?", "O teste e gratis por 7 dias. Depois, a loja escolhe entre caixa local ou plano restaurante conectado."],
      ["Precisa de cartao para testar?", "O teste foi pensado para a loja conhecer o fluxo antes de contratar."],
      ["O que consigo testar?", "Caixa Windows, produtos, mesas, estoque, pagamentos, comprovante e recursos conectados conforme o instalador escolhido."]
    ]
  },
  {
    slug: "sistema-para-lanchonete-pequena",
    eyebrow: "Lanchonete pequena",
    title: "Sistema para Lanchonete Pequena",
    metaTitle: "Sistema para Lanchonete Pequena com Caixa e Delivery",
    description: "Sistema para lanchonete pequena com caixa Windows, produtos, combos, estoque, comprovante, mesas, delivery, cardapio online e WhatsApp.",
    h1: "Sistema para lanchonete pequena vender sem complicar",
    lead: "Comece simples no caixa, controle produtos e estoque, e ative cardapio online, WhatsApp e delivery quando a loja precisar crescer.",
    segment: "Lanchonete pequena",
    plan: "offline",
    keywords: ["sistema para lanchonete pequena", "pdv lanchonete pequena", "caixa para lanchonete pequena"],
    outcomes: ["Comecar com custo menor", "Organizar combos e bebidas", "Crescer para delivery online"],
    features: [...baseFeatures, "Combos e adicionais", "Estoque de bebidas", "Plano profissional opcional"],
    faq: [
      ["Serve para loja pequena?", "Sim. O caixa local atende a rotina inicial e o plano profissional entra quando precisar de recursos online."],
      ["Dá para controlar estoque?", "Sim. O sistema mostra estoque, margem e produtos vendidos."],
      ["Posso vender no delivery depois?", "Sim. O plano profissional adiciona cardapio online, WhatsApp e recursos de entrega."]
    ]
  },
  {
    slug: "pdv-para-pizzaria-delivery",
    eyebrow: "Pizzaria delivery",
    title: "PDV para Pizzaria Delivery",
    metaTitle: "PDV para Pizzaria Delivery com WhatsApp e Entregas",
    description: "PDV para pizzaria delivery com pedido, sabores, adicionais, cliente, endereco, taxa, entregador, WhatsApp, cardapio online e caixa Windows.",
    h1: "PDV para pizzaria delivery controlar pedido ate a entrega",
    lead: "Organize pedido por cliente, endereco, taxa, entregador, status e pagamento sem depender de anotacao solta no papel.",
    segment: "Pizzaria delivery",
    plan: "online",
    keywords: ["pdv para pizzaria delivery", "sistema pizzaria delivery", "delivery pizzaria pdv"],
    outcomes: ["Menos erro no endereco", "Pedido com status e entregador", "WhatsApp e cardapio no mesmo fluxo"],
    features: [...baseFeatures, ...connectedFeatures, "Delivery com taxa por zona", "Status de producao e entrega"],
    faq: [
      ["Serve para pizzaria so delivery?", "Sim. O fluxo aceita retirada, entrega e balcao."],
      ["Tem controle de entregador?", "Sim. A loja pode cadastrar entregadores e acompanhar status."],
      ["Pode receber pedido pelo WhatsApp?", "Sim, quando a opcao de pedidos por WhatsApp estiver ativada."]
    ]
  },
  {
    slug: "pdv-com-whatsapp",
    eyebrow: "WhatsApp no PDV",
    title: "PDV com WhatsApp",
    metaTitle: "PDV com WhatsApp para Restaurante e Delivery",
    description: "PDV com WhatsApp para restaurante receber mensagens, enviar cardapio, registrar atendimento, abrir conversa e transformar pedido em venda.",
    h1: "PDV com WhatsApp para atender cliente sem abrir outro sistema",
    lead: "O WhatsApp vira canal de atendimento dentro da rotina do PDV. A loja acompanha conversa, pedido, cliente e historico sem depender de celular solto no balcao.",
    segment: "WhatsApp",
    plan: "online",
    keywords: ["pdv com whatsapp", "sistema com whatsapp", "whatsapp para restaurante", "pedido por whatsapp"],
    outcomes: ["Responder cliente dentro do PDV", "Registrar historico de atendimento", "Receber pedido por mensagem quando ativado"],
    features: [...connectedFeatures, "Conversas no PDV", "Atendimento automatico", "Pedido por codigo quando a loja liberar"],
    faq: [
      ["O cliente pode pedir pelo WhatsApp?", "Sim, quando a loja ativa pedidos digitados no WhatsApp."],
      ["Tambem tem cardapio digital?", "Sim. Se a loja ativar cardapio digital e pedidos por mensagem, o atendimento pode oferecer as duas opcoes."],
      ["Funciona com PDV fechado?", "O atendimento online foi feito para responder pelo servico conectado, mesmo sem o app aberto na loja."]
    ]
  },
  {
    slug: "pdv-com-nfce",
    eyebrow: "Fiscal configuravel",
    title: "PDV com NFC-e",
    metaTitle: "PDV com NFC-e Configuravel para Restaurante",
    description: "PDV com NFC-e configuravel para restaurante, caixa Windows, certificado, dados fiscais, produtos, estoque, pagamentos e fechamento.",
    h1: "PDV com NFC-e configuravel para restaurante",
    lead: "Para restaurante que quer vender no caixa e preparar a emissao fiscal conforme certificado, credenciais e regras da UF.",
    segment: "NFC-e",
    plan: "online",
    keywords: ["pdv com nfce", "sistema com nfce restaurante", "pdv restaurante nfce"],
    outcomes: ["Fiscal no fluxo do caixa", "Produtos e dados organizados", "Configuracao por UF e certificado"],
    features: [...baseFeatures, "NFC-e configuravel", "Certificado e dados fiscais", "Produtos com informacoes fiscais"],
    faq: [
      ["A NFC-e ja sai pronta?", "Depende de certificado, dados fiscais, ambiente, credenciais e regras da UF do cliente."],
      ["Serve para restaurante?", "Sim. NFC-e e o modelo comum para venda ao consumidor no restaurante."],
      ["Tem suporte para configurar?", "Sim. A implantacao orienta dados, certificado e parametros necessarios."]
    ]
  },
  {
    slug: "alternativa-anota-ai",
    eyebrow: "Comparativo comercial",
    title: "Alternativa ao Anota AI",
    metaTitle: "Alternativa ao Anota AI com PDV Windows e Caixa Offline",
    description: "Alternativa ao Anota AI para restaurante que quer PDV Windows, caixa offline, mesas, estoque, cardapio, WhatsApp e plano conectado.",
    h1: "Alternativa ao Anota AI para quem precisa de PDV Windows",
    lead: "Se a loja quer atendimento online, mas tambem precisa de caixa Windows, impressao, estoque, mesas e fechamento local, o Balcao Livre entra como operacao de PDV completa.",
    segment: "Alternativa",
    plan: "online",
    keywords: ["alternativa anota ai", "sistema parecido com anota ai", "pdv com whatsapp restaurante"],
    outcomes: ["Caixa Windows junto com atendimento online", "Operacao local e conectada", "Mais controle de estoque e fechamento"],
    features: [...baseFeatures, ...connectedFeatures, "Caixa local offline", "Fechamento do dia", "Impressao no Windows"],
    faq: [
      ["E igual ao Anota AI?", "Nao. A proposta e operar o PDV Windows com recursos online, cardapio e WhatsApp no mesmo fluxo."],
      ["Tem WhatsApp?", "Sim. O plano profissional inclui atendimento por WhatsApp."],
      ["Tem caixa offline?", "Sim. O caixa local e uma diferenca importante para operacao no Windows."]
    ]
  },
  {
    slug: "alternativa-consumer",
    eyebrow: "Comparativo comercial",
    title: "Alternativa ao Consumer",
    metaTitle: "Alternativa ao Consumer para Restaurante com PDV Windows",
    description: "Alternativa ao Consumer para restaurante com caixa Windows, mesas, comandas, estoque, delivery, cardapio online, WhatsApp e NFC-e configuravel.",
    h1: "Alternativa ao Consumer para restaurante que quer caixa simples e conectado",
    lead: "Para loja que procura um PDV direto, com caixa Windows, plano local barato e caminho para cardapio online, WhatsApp, equipe, entregadores e NFC-e.",
    segment: "Alternativa",
    plan: "online",
    keywords: ["alternativa consumer", "sistema parecido com consumer", "pdv restaurante windows"],
    outcomes: ["Plano local de entrada", "Upgrade para online", "Fluxo de restaurante sem trocar de sistema"],
    features: [...baseFeatures, ...connectedFeatures, "Plano de R$17 para comecar", "Plano profissional para conectar a loja"],
    faq: [
      ["E uma copia do Consumer?", "Nao. E uma alternativa para quem quer fluxo de PDV Windows com opcao online no Balcao Livre."],
      ["Da para testar?", "Sim. O teste dura 7 dias."],
      ["Qual plano comeca?", "Caixa local para entrada; Restaurante Profissional para loja conectada."]
    ]
  },
  {
    slug: "programa-para-controlar-comandas",
    eyebrow: "Dor de operacao",
    title: "Programa para Controlar Comandas",
    metaTitle: "Programa para Controlar Comandas de Bar e Restaurante",
    description: "Programa para controlar comandas de bar, restaurante e lanchonete com mesas, consumo aberto, caixa Windows, pagamento e impressao.",
    h1: "Programa para controlar comandas sem perder consumo",
    lead: "Abra comanda, lance produto, acompanhe mesa e feche a conta no caixa com historico mais claro para operador e gerente.",
    segment: "Comandas",
    plan: "offline",
    keywords: ["programa para controlar comandas", "controle de comandas", "sistema de comandas bar"],
    outcomes: ["Consumo aberto organizado", "Menos erro no fechamento", "Impressao da conta no Windows"],
    features: [...baseFeatures, "Mesa e comanda", "Conta por cliente ou mesa", "Fechamento de consumo"],
    faq: [
      ["Serve para bar?", "Sim. Bar, lanchonete e restaurante podem trabalhar com mesa e comanda."],
      ["Precisa de internet?", "Para caixa local, nao. Recursos online usam internet."],
      ["Imprime conta?", "Sim. O PDV imprime comprovante e fechamento."]
    ]
  },
  {
    slug: "como-controlar-estoque-de-lanchonete",
    eyebrow: "Dor de estoque",
    title: "Como Controlar Estoque de Lanchonete",
    metaTitle: "Como Controlar Estoque de Lanchonete com PDV",
    description: "Veja como controlar estoque de lanchonete com PDV, cadastro de produtos, preco de compra, preco de venda, margem, baixo estoque e vendas.",
    h1: "Como controlar estoque de lanchonete pelo PDV",
    lead: "O estoque precisa aparecer junto com a venda. Cadastre produto, preco de compra, preco de venda, margem e acompanhe o que mais sai no caixa.",
    segment: "Estoque",
    plan: "offline",
    keywords: ["como controlar estoque de lanchonete", "estoque lanchonete", "pdv estoque lanchonete"],
    outcomes: ["Comprar melhor", "Ver produto parado", "Evitar vender sem estoque"],
    features: [...baseFeatures, "Preco de compra e venda", "Margem por produto", "Produtos mais vendidos"],
    faq: [
      ["O PDV baixa estoque?", "O sistema organiza produtos e estoque conforme a configuracao da loja."],
      ["Mostra margem?", "Sim. Voce pode comparar preco de compra, venda e margem."],
      ["Ajuda em delivery?", "Sim. O estoque fica ligado ao cadastro usado no caixa e no cardapio."]
    ]
  }
];

export const seoPages = [...rawSeoPages, ...commercialSeoPages].map(applySeoInsight);

const featuredSeoSlugs = [
  "pdv-gratis-para-restaurante",
  "pdv-delivery-gratuito",
  "pdv-para-restaurante",
  "pdv-para-pizzaria-delivery",
  "sistema-para-lanchonete-pequena",
  "pdv-com-whatsapp",
  "pdv-com-cardapio-digital",
  "pdv-com-nfce",
  "alternativa-anota-ai",
  "alternativa-consumer",
  "programa-para-controlar-comandas",
  "como-controlar-estoque-de-lanchonete"
];

export const featuredSeoPages = featuredSeoSlugs
  .map((slug) => seoPages.find((page) => page.slug === slug))
  .filter(Boolean);

export function getSeoPage(slug) {
  return seoPages.find((page) => page.slug === slug);
}

export function getPlanCopy(planId) {
  return planCopy[planId] || planCopy.online;
}

export function seoPageJsonLd(page, canonicalUrl) {
  return [
    {
      "@context": "https://schema.org",
      "@type": "SoftwareApplication",
      name: page.title,
      applicationCategory: "BusinessApplication",
      operatingSystem: "Windows",
      url: canonicalUrl,
      description: page.description,
      offers: {
        "@type": "Offer",
        priceCurrency: "BRL",
        price: page.plan === "offline" ? "17.00" : "139.00",
        availability: "https://schema.org/InStock"
      }
    },
    {
      "@context": "https://schema.org",
      "@type": "FAQPage",
      mainEntity: page.faq.map(([question, answer]) => ({
        "@type": "Question",
        name: question,
        acceptedAnswer: {
          "@type": "Answer",
          text: answer
        }
      }))
    }
  ];
}

export function checkoutHrefForPlan(planId) {
  if (planId === "offline") return `${checkoutFunctionUrl}?plan=offline-mensal`;
  if (planId === "online") return `${checkoutFunctionUrl}?plan=online-mensal`;
  return sellers[1].href;
}
