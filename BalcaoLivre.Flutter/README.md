# Balcão Livre Flutter

Aplicativo móvel oficial do Balcão Livre PDV. O projeto foi promovido do
artefato de paridade preservado para a árvore ativa do produto.

## Regras de acesso

- A entrada usa a mesma conta criada depois do checkout.
- O smartphone é ativado como dispositivo `MOBILE` por um handoff seguro.
- Não existe chave de licença visível para o cliente.
- Cada assinatura inclui uma vaga móvel; um segundo smartphone é recusado pelo
  backend enquanto não houver outra vaga.
- Plano, módulos, pagamentos e configuração do book vêm do Supabase.
- O caixa móvel é independente do caixa Windows.
- Eventos offline mantêm um identificador estável e são reenviados com
  idempotência quando a conexão volta.

## Executar

```powershell
C:\src\flutter\bin\flutter.bat pub get
C:\src\flutter\bin\flutter.bat run
```

As únicas configurações compiladas no cliente são a URL pública do Supabase e
a chave publicável. Segredos permanecem nas Edge Functions.
