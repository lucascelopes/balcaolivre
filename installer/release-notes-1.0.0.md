# Balcao Livre PDV 1.0.0

Release inicial para cliente.

## Principais entregas

- PDV Windows nativo em WPF, offline-first, sem dependencia operacional do Supabase.
- Instalador Windows independente, icone/logo do Balcao Livre PDV e inicializacao com Windows.
- Ativacao por chave com expiracao, bloqueio offline respeitando validade local e alertas de vencimento.
- Criacao inicial de administrador e login obrigatorio por operador, garcom ou gerente.
- Permissoes por perfil: operador/garcom sem acesso a configuracao, estoque e relatorios.
- Mesas criadas pelo usuario, fichas de balcao e pedidos de delivery/retirada.
- Fluxo por teclado: setas, Enter, Tab, F2, F3, F5, F8, F9, F10 e teclado numerico/NumLock.
- Comandas com agrupamento de produtos iguais, exclusao por linha, reabertura permitida somente quando nao paga e transferencia de comanda completa.
- Caixa com abertura por operador, senha, dinheiro vivo inicial, entradas, retiradas e fechamento.
- Bloqueio de fechamento do caixa quando ha mesas/fichas pendentes.
- Impressao automatica de comprovante ao receber pagamento e resumo do dia ao fechar caixa.
- Impressao em impressora padrao/preferida, suporte a POS-58 e modelo de comprovante grande.
- Recebimento em dinheiro, Pix, credito, debito, vale e fiado, com calculo de troco.
- Pix no QR do comprovante com valor da venda e CRC recalculado para Pix copia e cola.
- QR opcional no comprovante para Pix, Instagram, Google Maps, chave/link ou outro destino.
- Cadastro de clientes com uso no delivery e endereco impresso no comprovante.
- Cadastro de produtos com preco de compra, preco de venda, margem de lucro, categoria, setor e estoque.
- Controle de estoque com entrada, saida/perda, estoque minimo e alerta de itens criticos.
- Relatorios com caixa, vendas do dia, vendas totais, estoque critico, produtos vendidos, lucro bruto e margem.
- Configuracoes de empresa/logo, comprovante, impressoras, QR, notificacoes, som/vibracao e atualizacao.
