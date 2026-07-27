do $verify$
declare
  expected_index text;
  unique_index_definition text;
begin
  select pg_get_indexdef(index_class.oid)
    into unique_index_definition
  from pg_class index_class
  join pg_namespace index_namespace
    on index_namespace.oid = index_class.relnamespace
  join pg_index index_state
    on index_state.indexrelid = index_class.oid
  where index_namespace.nspname = 'public'
    and index_class.relname = 'bv_ifood_webhook_events_connection_event_uidx'
    and index_state.indisunique
    and index_state.indisvalid
    and index_state.indisready;

  if unique_index_definition is null
     or unique_index_definition not like '%(connection_id, event_id)%' then
    raise exception 'Indice unico de deduplicacao iFood ausente ou invalido.';
  end if;

  if exists (
    select 1
    from public.bv_ifood_webhook_events
    where connection_id is not null
      and event_id is not null
    group by connection_id, event_id
    having count(*) > 1
  ) then
    raise exception 'Ainda existem eventos iFood duplicados por conexao.';
  end if;

  foreach expected_index in array array[
    'bv_ifood_webhook_events_connection_received_idx',
    'bv_ifood_webhook_events_latest_merchant_idx',
    'bv_ifood_connections_merchant_idx',
    'bv_ifood_orders_connection_imported_idx',
    'bv_ifood_orders_merchant_imported_idx'
  ]
  loop
    if not exists (
      select 1
      from pg_class index_class
      join pg_namespace index_namespace
        on index_namespace.oid = index_class.relnamespace
      join pg_index index_state
        on index_state.indexrelid = index_class.oid
      where index_namespace.nspname = 'public'
        and index_class.relname = expected_index
        and index_state.indisvalid
        and index_state.indisready
    ) then
      raise exception 'Indice iFood ausente ou invalido: %', expected_index;
    end if;
  end loop;

  if exists (
    select 1
    from pg_class table_class
    join pg_namespace table_namespace
      on table_namespace.oid = table_class.relnamespace
    where table_namespace.nspname = 'public'
      and table_class.relname in (
        'bv_ifood_connections',
        'bv_ifood_webhook_events',
        'bv_ifood_orders'
      )
      and not table_class.relrowsecurity
  ) then
    raise exception 'RLS nao esta ativo em todas as tabelas iFood expostas.';
  end if;
end
$verify$;
