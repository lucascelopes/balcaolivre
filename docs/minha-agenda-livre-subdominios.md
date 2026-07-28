# Subdomínios de agendamento — Minha Agenda Livre

Cada estabelecimento recebe um endereço no formato:

```text
nomedaloja.minhaagendalivre.com.br
```

O slug (`nomedaloja`) é escolhido pelo aplicativo Agenda Livre no primeiro sync. Se já existir outra loja com o mesmo nome, a API acrescenta automaticamente um identificador estável e devolve o endereço efetivo ao aplicativo.

## DNS e Sites

No provedor DNS de `minhaagendalivre.com.br`, crie um registro curinga (`*`) apontando para o destino informado pelo Sites. Cadastre também `*.minhaagendalivre.com.br` como domínio personalizado do mesmo projeto para que o certificado TLS cubra todos os estabelecimentos.

Os nomes `www`, `app`, `admin`, `pdv`, `cardapio` e `api` são reservados e não podem virar slug de loja.

## Roteamento

Uma visita a `https://nomedaloja.minhaagendalivre.com.br/` é reescrita internamente para `/agendar/nomedaloja`. Durante o desenvolvimento, a mesma página continua acessível por `http://localhost:3000/agendar/nomedaloja`.

As APIs públicas ficam em:

- `GET /api/agendar/{slug}/availability`
- `POST /api/agendar/{slug}/appointments`
- `GET /api/agendar/{slug}/appointments/{id}?token=...`

O aplicativo Windows sincroniza por rotas internas autenticadas:

- `POST /api/internal/agenda/sync`
- `PATCH /api/internal/agenda/bookings/{id}`

## Variáveis hospedadas

Defina no ambiente do Sites:

- `AGENDA_BOOKING_ROOT_DOMAIN=minhaagendalivre.com.br`
- `NEXT_PUBLIC_BOOKING_ROOT_DOMAIN=minhaagendalivre.com.br`
- `AGENDA_SNAPSHOT_TTL_SECONDS=90`
- `AGENDA_LICENSE_SECRET` com o segredo compatível com a assinatura BLV do aplicativo
- `AGENDA_STATUS_TOKEN_SECRET` com um segredo longo e exclusivo do ambiente

Não exponha os dois segredos como variáveis `NEXT_PUBLIC_*`.
