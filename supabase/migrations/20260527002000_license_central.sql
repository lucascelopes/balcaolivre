create extension if not exists pgcrypto;

create table if not exists public.bv_licenses (
    key text primary key,
    status text not null default 'DISPONIVEL',
    plan text not null default 'Licenca comercial',
    customer_name text,
    email text,
    business_name text,
    owner_name text,
    cnpj text,
    phone text,
    city text,
    state text,
    machine_hash text,
    machine_code text,
    app_version text,
    client_kind text,
    profile jsonb not null default '{}'::jsonb,
    settings jsonb not null default '{}'::jsonb,
    metrics jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    expires_at timestamptz not null,
    activated_at timestamptz,
    last_seen_at timestamptz,
    updated_at timestamptz not null default now()
);

create table if not exists public.bv_license_events (
    id uuid primary key default gen_random_uuid(),
    license_key text,
    machine_code text,
    event_type text not null,
    message text not null default '',
    payload jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create index if not exists idx_bv_licenses_status_expires
    on public.bv_licenses (status, expires_at);

create index if not exists idx_bv_licenses_email
    on public.bv_licenses (lower(email));

create index if not exists idx_bv_license_events_key_created
    on public.bv_license_events (license_key, created_at desc);

alter table public.bv_licenses enable row level security;
alter table public.bv_license_events enable row level security;

revoke all on public.bv_licenses from anon, authenticated;
revoke all on public.bv_license_events from anon, authenticated;
