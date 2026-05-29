create table if not exists public.bv_mercadopago_connections (
    license_key text primary key,
    machine_hash text,
    status text not null default 'DISCONNECTED',
    seller_user_id text,
    access_token text,
    refresh_token text,
    public_key text,
    token_type text,
    scope text,
    expires_at timestamptz,
    selected_terminal_id text,
    selected_terminal_label text,
    connected_at timestamptz,
    last_sync_at timestamptz,
    last_error text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.bv_mercadopago_oauth_states (
    state text primary key,
    license_key text not null,
    machine_hash text,
    code_verifier text,
    expires_at timestamptz not null,
    used_at timestamptz,
    created_at timestamptz not null default now()
);

create table if not exists public.bv_mercadopago_payment_attempts (
    id uuid primary key default gen_random_uuid(),
    license_key text not null,
    machine_hash text,
    local_reference text not null,
    method text not null default 'POINT',
    amount numeric(12,2) not null,
    order_id text,
    payment_id text,
    terminal_id text,
    terminal_label text,
    status text not null default 'CREATED',
    status_detail text,
    raw_response jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists idx_bv_mp_oauth_states_license_created
    on public.bv_mercadopago_oauth_states (license_key, created_at desc);

create index if not exists idx_bv_mp_payment_attempts_license_created
    on public.bv_mercadopago_payment_attempts (license_key, created_at desc);

create unique index if not exists idx_bv_mp_payment_attempts_local_reference
    on public.bv_mercadopago_payment_attempts (license_key, local_reference);

alter table public.bv_mercadopago_connections enable row level security;
alter table public.bv_mercadopago_oauth_states enable row level security;
alter table public.bv_mercadopago_payment_attempts enable row level security;

revoke all on public.bv_mercadopago_connections from anon, authenticated;
revoke all on public.bv_mercadopago_oauth_states from anon, authenticated;
revoke all on public.bv_mercadopago_payment_attempts from anon, authenticated;
