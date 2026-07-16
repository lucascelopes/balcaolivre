create extension if not exists pgcrypto;

create table if not exists public.balcao_instagram_connections (
  license_key text primary key,
  machine_hash text not null,
  instagram_user_id text not null unique,
  username text not null default '',
  display_name text not null default '',
  account_type text not null default '',
  access_token text not null,
  token_type text not null default 'bearer',
  token_expires_at timestamptz,
  scopes text[] not null default '{}'::text[],
  status text not null default 'ATIVO'
    check (status in ('ATIVO', 'EXPIRADO', 'REVOGADO', 'ERRO')),
  webhook_subscribed_at timestamptz,
  last_error text not null default '',
  connected_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  meta_payload jsonb not null default '{}'::jsonb
);

create table if not exists public.balcao_instagram_oauth_states (
  state text primary key,
  license_key text not null,
  machine_hash text not null,
  redirect_uri text not null,
  scopes text[] not null default '{}'::text[],
  expires_at timestamptz not null,
  used_at timestamptz,
  created_at timestamptz not null default now()
);

create table if not exists public.balcao_instagram_webhook_events (
  id uuid primary key default gen_random_uuid(),
  event_key text not null unique,
  license_key text,
  instagram_user_id text not null default '',
  event_type text not null default 'unknown',
  signature_valid boolean not null default false,
  payload jsonb not null default '{}'::jsonb,
  received_at timestamptz not null default now(),
  processed_at timestamptz
);

create table if not exists public.balcao_instagram_messages (
  id uuid primary key default gen_random_uuid(),
  license_key text not null,
  instagram_user_id text not null,
  instagram_scoped_user_id text not null,
  meta_message_id text not null unique,
  direction text not null check (direction in ('entrada', 'saida')),
  message_type text not null default 'text',
  message_text text not null default '',
  status text not null default 'recebida',
  payload jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now(),
  sent_at timestamptz
);

create table if not exists public.balcao_instagram_publications (
  id uuid primary key default gen_random_uuid(),
  license_key text not null,
  instagram_user_id text not null,
  container_id text,
  media_id text,
  media_type text not null check (media_type in ('IMAGE', 'REELS', 'STORIES')),
  media_url text not null,
  caption text not null default '',
  status text not null default 'CRIANDO'
    check (status in ('CRIANDO', 'PROCESSANDO', 'PUBLICADO', 'ERRO')),
  error_message text not null default '',
  payload jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  published_at timestamptz
);

create table if not exists public.balcao_instagram_rate_limits (
  id uuid primary key default gen_random_uuid(),
  license_key text not null,
  route text not null,
  created_at timestamptz not null default now()
);

create index if not exists balcao_instagram_connections_machine_idx
  on public.balcao_instagram_connections (machine_hash);

create index if not exists balcao_instagram_oauth_states_expires_idx
  on public.balcao_instagram_oauth_states (expires_at);

create index if not exists balcao_instagram_oauth_states_license_idx
  on public.balcao_instagram_oauth_states (license_key, created_at desc);

create index if not exists balcao_instagram_webhook_events_account_idx
  on public.balcao_instagram_webhook_events (instagram_user_id, received_at desc);

create index if not exists balcao_instagram_messages_license_created_idx
  on public.balcao_instagram_messages (license_key, created_at desc);

create index if not exists balcao_instagram_messages_conversation_idx
  on public.balcao_instagram_messages
    (license_key, instagram_scoped_user_id, created_at desc);

create index if not exists balcao_instagram_publications_license_created_idx
  on public.balcao_instagram_publications (license_key, created_at desc);

create index if not exists balcao_instagram_rate_limits_lookup_idx
  on public.balcao_instagram_rate_limits (license_key, route, created_at desc);

alter table public.balcao_instagram_connections enable row level security;
alter table public.balcao_instagram_oauth_states enable row level security;
alter table public.balcao_instagram_webhook_events enable row level security;
alter table public.balcao_instagram_messages enable row level security;
alter table public.balcao_instagram_publications enable row level security;
alter table public.balcao_instagram_rate_limits enable row level security;

revoke all on table public.balcao_instagram_connections from public, anon, authenticated;
revoke all on table public.balcao_instagram_oauth_states from public, anon, authenticated;
revoke all on table public.balcao_instagram_webhook_events from public, anon, authenticated;
revoke all on table public.balcao_instagram_messages from public, anon, authenticated;
revoke all on table public.balcao_instagram_publications from public, anon, authenticated;
revoke all on table public.balcao_instagram_rate_limits from public, anon, authenticated;

grant usage on schema public to service_role;
grant select, insert, update, delete on table public.balcao_instagram_connections to service_role;
grant select, insert, update, delete on table public.balcao_instagram_oauth_states to service_role;
grant select, insert, update, delete on table public.balcao_instagram_webhook_events to service_role;
grant select, insert, update, delete on table public.balcao_instagram_messages to service_role;
grant select, insert, update, delete on table public.balcao_instagram_publications to service_role;
grant select, insert, update, delete on table public.balcao_instagram_rate_limits to service_role;

comment on table public.balcao_instagram_connections is
  'Conexoes Instagram profissionais por licenca. Tokens sao acessiveis apenas pelo service_role.';
comment on table public.balcao_instagram_oauth_states is
  'Estados OAuth Instagram de uso unico e expiracao curta.';
comment on table public.balcao_instagram_webhook_events is
  'Eventos Instagram autenticados e deduplicados pelo event_key.';
comment on table public.balcao_instagram_messages is
  'Mensagens Instagram recebidas e enviadas por uma licenca.';
comment on table public.balcao_instagram_publications is
  'Publicacoes Instagram e seus estados de processamento.';
comment on table public.balcao_instagram_rate_limits is
  'Contadores simples por licenca e rota para limitar abuso da funcao publica.';
