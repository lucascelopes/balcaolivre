# Balcao Livre WhatsApp Connector

Extensao local para prototipo do conector WhatsApp Web sem API oficial.

Para o cliente final, o PDV abre o navegador ja com esta extensao carregada pelo botao `Instalar conector e abrir WhatsApp`.

Passos manuais abaixo sao somente para desenvolvimento/teste:

1. Abra `chrome://extensions`.
2. Ative `Modo do desenvolvedor`.
3. Clique em `Carregar sem compactacao`.
4. Selecione esta pasta.
5. Abra `https://web.whatsapp.com` e mantenha o PDV Windows aberto.

O PDV escuta `http://127.0.0.1:8787/whatsapp/message`.
A extensao le mensagens recebidas, envia para o PDV local e, quando o PDV retornar uma resposta, tenta preencher e clicar em enviar no WhatsApp Web.

Uso recomendado: apenas mensagens transacionais de pedido, confirmacao e status. Nao usar para disparo em massa.
