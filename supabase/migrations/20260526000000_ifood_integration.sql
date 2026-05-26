create extension if not exists pgcrypto;

create table if not exists public.bv_ifood_connections (
    id uuid primary key default gen_random_uuid(),
    license_key text not null,
    machine_hash text not null,
    machine_code text,
    business_name text,
    legal_name text,
    cnpj text,
    phone text,
    address text,
    city text,
    state text,
    app_version text,
    status text not null default 'pending',
    user_code text,
    authorization_code_verifier text,
    verification_url text,
    verification_url_complete text,
    merchant_id text,
    merchant_name text,
    access_token text,
    refresh_token text,
    token_expires_at timestamptz,
    webhook_url text,
    last_sync_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (license_key, machine_hash)
);

create table if not exists public.bv_ifood_webhook_events (
    id uuid primary key default gen_random_uuid(),
    event_id text,
    connection_id uuid references public.bv_ifood_connections(id) on delete set null,
    merchant_id text,
    order_id text,
    payload jsonb not null,
    received_at timestamptz not null default now()
);

create table if not exists public.bv_ifood_orders (
    id uuid primary key default gen_random_uuid(),
    order_id text not null unique,
    connection_id uuid references public.bv_ifood_connections(id) on delete set null,
    merchant_id text,
    payload jsonb not null,
    imported_at timestamptz not null default now()
);

alter table public.bv_ifood_connections enable row level security;
alter table public.bv_ifood_webhook_events enable row level security;
alter table public.bv_ifood_orders enable row level security;
