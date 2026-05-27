create schema if not exists private;

create table if not exists private.balcao_whatsapp_store_connections (
  license_key text primary key,
  machine_hash text,
  store_phone text,
  waba_id text,
  business_id text,
  phone_number_id text not null,
  phone_display_number text,
  access_token text not null,
  token_type text,
  status text not null default 'ATIVO',
  last_error text,
  connected_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  meta_payload jsonb not null default '{}'::jsonb
);

create table if not exists private.balcao_whatsapp_onboarding_states (
  state text primary key,
  license_key text not null,
  machine_hash text,
  store_phone text,
  expires_at timestamptz not null,
  created_at timestamptz not null default now()
);

alter table private.balcao_whatsapp_store_connections enable row level security;
alter table private.balcao_whatsapp_onboarding_states enable row level security;

create index if not exists balcao_whatsapp_store_connections_phone_idx
  on private.balcao_whatsapp_store_connections (phone_number_id);

create index if not exists balcao_whatsapp_store_connections_store_phone_idx
  on private.balcao_whatsapp_store_connections (store_phone);

create index if not exists balcao_whatsapp_onboarding_states_expires_idx
  on private.balcao_whatsapp_onboarding_states (expires_at);

revoke all on table private.balcao_whatsapp_store_connections from public, anon, authenticated;
revoke all on table private.balcao_whatsapp_onboarding_states from public, anon, authenticated;
grant usage on schema private to service_role;
grant select, insert, update, delete on table private.balcao_whatsapp_store_connections to service_role;
grant select, insert, update, delete on table private.balcao_whatsapp_onboarding_states to service_role;
