create table if not exists public.bv_ifood_stock_sync (
  id uuid primary key default gen_random_uuid(),
  connection_id uuid null,
  merchant_id text null,
  product_id text null,
  external_code text null,
  product_code text null,
  product_name text null,
  amount integer not null default 0,
  reason text null,
  mode text not null default 'sync',
  payload jsonb not null default '{}'::jsonb,
  synced_at timestamptz not null default now()
);

create index if not exists bv_ifood_stock_sync_synced_at_idx
  on public.bv_ifood_stock_sync (synced_at desc);

create index if not exists bv_ifood_stock_sync_connection_idx
  on public.bv_ifood_stock_sync (connection_id);

create index if not exists bv_ifood_stock_sync_product_code_idx
  on public.bv_ifood_stock_sync (product_code);

alter table public.bv_ifood_stock_sync enable row level security;

do $$
begin
  if not exists (
    select 1
    from pg_policies
    where schemaname = 'public'
      and tablename = 'bv_ifood_stock_sync'
      and policyname = 'service_role_can_manage_ifood_stock_sync'
  ) then
    create policy service_role_can_manage_ifood_stock_sync
      on public.bv_ifood_stock_sync
      for all
      to service_role
      using (true)
      with check (true);
  end if;
end $$;
