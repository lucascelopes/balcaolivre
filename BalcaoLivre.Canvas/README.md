# BL. Canvas

Nova interface web independente, em React e Vite, baseada nas quatro referências Canvas aprovadas em 24/10/2026. Não depende de .NET. Não substitui os aplicativos existentes.

## Executar

```sh
npm install
npm run dev -- --port 4188
npm run build
npm test
```

## Estado da implementação

- Salão com ambientes, seleção de mesas, abertura de comandas e inclusão de itens.
- Recebimento por forma de pagamento, liberação de mesa e lançamento no caixa local.
- Pedidos com filtros, criação de retirada e avanço de preparo/entrega.
- Pedido de retirada com vários itens, busca no cardápio, quantidades e observações.
- Cozinha com etapas de preparo para pedidos de retirada e itens enviados das mesas.
- Estoque de ingredientes, reposição e fichas técnicas com custo por porção. Pedidos concluídos podem gerar baixa após conferência e confirmação, com registro de cada movimento.
- Cardápio com busca, categorias, edição e disponibilidade.
- Fotos ilustrativas geradas para os sete produtos de demonstração. O editor permite escolher uma foto própria, reduzida antes de ser salva neste navegador.
- Scanner OCR de imagem via Tesseract no navegador e revisão antes de salvar. O primeiro uso baixa o modelo de português. Alternativa de transcrição manual disponível.
- Caixa com entradas, saídas, fechamento conferido, abertura e histórico.
- Página Início com resumo operacional e Relatórios locais por período, origem do pedido e forma de pagamento.
- Assistente local por comandos e ditado quando suportado pelo navegador. Ainda não é a IA do Agenda Livre.
- Central de automações locais com avisos configuráveis de atraso, mesas ocupadas, reposição e diferença no caixa. O sino e o assistente local mostram esses avisos.
- Compras e reposição com sugestão baseada no mínimo do estoque, rascunho editável, registro local do pedido e entrada conferida que atualiza estoque e movimentações. Não envia o pedido ao fornecedor nem lança despesa automaticamente.
- Estado de demonstração persistido em localStorage. Não usar esta etapa para dados reais de produção.
- Configurações permite baixar e restaurar uma cópia JSON dos dados locais, com conferência antes da substituição.

## Ainda pendente

Validação visual e interativa desktop/mobile no Edge; upload e armazenamento de fotos em produção; autenticação e persistência de produção; conexão segura com assistente/IA; integrações reais iFood, Keeta e 99Food, sujeitas ao acesso às APIs e homologação; refinamentos de acessibilidade e paridade de comportamento.

O navegador local foi bloqueado pelas configurações do Work, inclusive após a usuária solicitar Edge. Não há alegação de aprovação visual, integração ou publicação em produção.

Em 24/10/2026, a usuária escolheu continuar somente com a prévia local. Não conectar o projeto Supabase do Agenda Livre ao BL. O conector atual não oferece acesso ao projeto Supabase legado do Balcão Livre; qualquer futura integração de produção precisa ser tratada separadamente.
