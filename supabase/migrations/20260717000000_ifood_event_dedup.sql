-- Deduplica eventos iFood para que cada evento recebido seja persistido uma unica vez por conexao.
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
