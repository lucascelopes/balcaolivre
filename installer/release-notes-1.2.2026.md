# Balcao Livre PDV 1.2.2026

- PDV Windows agora aponta por padrao para o admin publico em `https://balcaolivrepdv.onrender.com`.
- Configuracoes antigas com `http://localhost:5188` sao migradas automaticamente para a URL publica.
- Ao abrir o PDV, se existir uma versao maior no `version.json`, o app baixa o instalador, executa em modo silencioso e reabre atualizado.
- Admin atualizado para `1.2.2026` e armazenamento central via Supabase Storage.
- Ajuste na tela de clientes do admin para exibir somente dados cadastrais e ultima sincronizacao.
