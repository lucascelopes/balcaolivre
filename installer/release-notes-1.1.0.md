# Balcao Livre PDV 1.1.0

## Admin web interno

- Criado projeto `BalcaoLivre.Admin` com painel web para controle de licencas e clientes.
- Login padrao: `balcaoVirtualPDV` / `BVPDV24055`.
- Dashboard com licencas ativas, disponiveis, expiradas, clientes online em 24h e vendas reportadas.
- Lista de licencas com status, cliente, chave, PC vinculado, expiracao e ultimo uso.
- Criacao de chaves por periodo em minutos, dias, meses ou anos.
- Chaves novas no formato `BLV`, unicas por emissao e assinadas para validacao offline no PDV.
- API publica para o app enviar ativacao e check-in sem expor a senha do painel.
- Vinculo da chave ao primeiro PC que ativar quando o admin esta online.
- Registro de empresa/configuracoes/metricas enviadas pelo app Windows.

## App Windows

- PDV agora aceita chaves `BLV` geradas no admin.
- Ao ativar, tenta validar a chave no admin e bloquear uso em outro computador.
- Se o admin estiver offline, mantem a ativacao local para preservar o modo offline.
- Envia check-in periodico com dados da empresa, configuracoes principais e metricas operacionais.
