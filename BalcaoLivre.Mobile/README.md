# Balcao Livre Mobile

App React Native + Expo Dev Build para operar o Balcao Livre PDV no Android com banco local SQLite.

## Entrega desta base

- Login por chave/licenca usando a Edge Function `license`.
- Banco SQLite local com produtos, comandas, itens, caixa, pagamentos, estoque, fila de sync e configuracoes.
- Operacao offline-first: abrir caixa, criar comanda/mesa, adicionar produto, fechar venda, baixar estoque e sincronizar depois.
- Abas operacionais: Caixa, Comandas, Delivery, Produtos, Estoque, Pedidos e Config.
- Servicos para Mercado Pago, iFood, cardapio/sync e impressao.
- Impressao via bridge Windows (`/api/mobile/print`) e estrutura para ESC/POS direto em Dev Build.

## Rodar

```powershell
cd BalcaoLivre.Mobile
npm install
npm run android
```

Use `expo start --dev-client` depois de instalar um Dev Client no Android.

## Observacao de hardware

Bluetooth ESC/POS direto depende de modulo nativo no Dev Build. A primeira rota funcional e a impressao via Windows bridge, que usa o PDV aberto na mesma rede para imprimir na impressora padrao/cozinha/caixa.
