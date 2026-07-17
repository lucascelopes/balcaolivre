-- Deduplica eventos iFood e cria os indices usados pelo polling distribuido.
-- O lock curto impede que uma nova duplicata entre entre a limpeza e o indice unico.
begin;

set local lock_timeout = '5s';
set local statement_timeout = '2min';

lock table public.bv_ifood_webhook_events
  in share row exclusive mode;

with ranked as (
  select
    id,
    row_number() over (
      partition by connection_id, event_id
      order by received_at asc, id asc
    ) as occurrence
  from public.bv_ifood_webhook_events
  where connection_id is not null
    and event_id is not null
)
delete from public.bv_ifood_webhook_events target
using ranked
where target.id = ranked.id
  and ranked.occurrence > 1;

create unique index if not exists bv_ifood_webhook_events_connection_event_uidx
  on public.bv_ifood_webhook_events (connection_id, event_id);

create index if not exists bv_ifood_webhook_events_connection_received_idx
  on public.bv_ifood_webhook_events (connection_id, received_at desc)
  where connection_id is not null;

create index if not exists bv_ifood_connections_merchant_idx
  on public.bv_ifood_connections (merchant_id)
  where merchant_id is not null;

create index if not exists bv_ifood_orders_connection_imported_idx
  on public.bv_ifood_orders (connection_id, imported_at desc)
  where connection_id is not null;

create index if not exists bv_ifood_orders_merchant_imported_idx
  on public.bv_ifood_orders (merchant_id, imported_at desc)
  where merchant_id is not null;

commit;
