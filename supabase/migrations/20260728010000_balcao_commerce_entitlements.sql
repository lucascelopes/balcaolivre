create extension if not exists pgcrypto;

create table if not exists public.bl_accounts (
    id uuid primary key default gen_random_uuid(),
    owner_user_id uuid references auth.users(id) on delete set null,
    email text,
    phone text,
    display_name text,
    stripe_customer_id text unique,
    status text not null default 'PENDING'
        check (status in ('PENDING', 'ACTIVE', 'PAST_DUE', 'SUSPENDED', 'CANCELED')),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_stores (
    id uuid primary key default gen_random_uuid(),
    account_id uuid not null references public.bl_accounts(id) on delete cascade,
    name text not null default 'Meu estabelecimento',
    slug text,
    timezone text not null default 'America/Sao_Paulo',
    currency text not null default 'BRL',
    onboarding_status text not null default 'PENDING'
        check (onboarding_status in ('PENDING', 'IN_PROGRESS', 'COMPLETE')),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (account_id, slug)
);

create table if not exists public.bl_store_members (
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    user_id uuid not null references auth.users(id) on delete cascade,
    role text not null default 'OWNER'
        check (role in ('OWNER', 'MANAGER', 'CASHIER', 'WAITER')),
    status text not null default 'ACTIVE'
        check (status in ('INVITED', 'ACTIVE', 'REVOKED')),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (store_id, user_id)
);

create table if not exists public.bl_subscriptions (
    id uuid primary key default gen_random_uuid(),
    account_id uuid not null references public.bl_accounts(id) on delete cascade,
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    provider text not null default 'STRIPE' check (provider = 'STRIPE'),
    provider_customer_id text,
    provider_subscription_id text unique,
    provider_checkout_session_id text unique,
    plan_code text not null
        check (plan_code in ('basico-mensal', 'basico-anual', 'completo-mensal', 'completo-anual')),
    billing_interval text not null check (billing_interval in ('MONTH', 'YEAR')),
    status text not null default 'PENDING'
        check (status in ('PENDING', 'TRIALING', 'ACTIVE', 'PAST_DUE', 'PAUSED', 'CANCELED', 'EXPIRED')),
    base_quantity integer not null default 1 check (base_quantity = 1),
    extra_desktop_quantity integer not null default 0 check (extra_desktop_quantity >= 0),
    current_period_start timestamptz,
    current_period_end timestamptz,
    cancel_at_period_end boolean not null default false,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_entitlements (
    store_id uuid primary key references public.bl_stores(id) on delete cascade,
    subscription_id uuid references public.bl_subscriptions(id) on delete set null,
    plan_code text not null,
    modules text[] not null default array['PDV', 'SALAO', 'MESAS', 'COMANDAS', 'PRODUTOS', 'CAIXA', 'RELATORIOS_BASICOS'],
    desktop_seat_limit integer not null default 1 check (desktop_seat_limit >= 0),
    mobile_seat_limit integer not null default 1 check (mobile_seat_limit >= 0),
    web_uses_desktop_seat boolean not null default true,
    mercadopago_point_enabled boolean not null default false,
    machine_fulfillment_included boolean not null default false,
    reports_level text not null default 'BASIC'
        check (reports_level in ('BASIC', 'ADVANCED')),
    effective_at timestamptz not null default now(),
    expires_at timestamptz,
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_device_seats (
    id uuid primary key default gen_random_uuid(),
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    subscription_id uuid references public.bl_subscriptions(id) on delete set null,
    seat_kind text not null check (seat_kind in ('DESKTOP', 'MOBILE')),
    source text not null default 'PLAN'
        check (source in ('PLAN', 'EXTRA_SUBSCRIPTION', 'MANUAL')),
    ordinal integer not null default 1 check (ordinal > 0),
    provider_subscription_item_id text,
    status text not null default 'AVAILABLE'
        check (status in ('AVAILABLE', 'ASSIGNED', 'REVOKED')),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (store_id, seat_kind, ordinal)
);

create table if not exists public.bl_devices (
    id uuid primary key default gen_random_uuid(),
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    seat_id uuid not null references public.bl_device_seats(id) on delete restrict,
    device_kind text not null check (device_kind in ('WINDOWS', 'WEB', 'MOBILE')),
    installation_id_hash text not null,
    display_name text,
    platform text,
    app_version text,
    public_key text,
    status text not null default 'ACTIVE'
        check (status in ('PENDING', 'ACTIVE', 'REVOKED', 'REPLACED')),
    activated_at timestamptz,
    last_seen_at timestamptz,
    last_seen_ip inet,
    revoked_at timestamptz,
    replaced_by uuid references public.bl_devices(id) on delete set null,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (store_id, installation_id_hash)
);

create unique index if not exists idx_bl_devices_one_active_per_seat
    on public.bl_devices (seat_id)
    where status in ('PENDING', 'ACTIVE');

create table if not exists public.bl_device_leases (
    id uuid primary key default gen_random_uuid(),
    device_id uuid not null references public.bl_devices(id) on delete cascade,
    token_hash text not null unique,
    issued_at timestamptz not null default now(),
    expires_at timestamptz not null,
    revoked_at timestamptz,
    last_seen_at timestamptz,
    last_seen_ip inet
);

create table if not exists public.bl_onboarding_configs (
    store_id uuid primary key references public.bl_stores(id) on delete cascade,
    current_step integer not null default 1 check (current_step between 1 and 6),
    restaurant jsonb not null default '{}'::jsonb,
    service_mode jsonb not null default '{}'::jsonb,
    floor_plan jsonb not null default '{}'::jsonb,
    cash_setup jsonb not null default '{}'::jsonb,
    payment_methods jsonb not null default '{}'::jsonb,
    review jsonb not null default '{}'::jsonb,
    completed_at timestamptz,
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_handoff_tokens (
    id uuid primary key default gen_random_uuid(),
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    account_id uuid not null references public.bl_accounts(id) on delete cascade,
    seat_id uuid references public.bl_device_seats(id) on delete cascade,
    token_hash text not null unique,
    purpose text not null
        check (purpose in ('CHECKOUT_CLAIM', 'WEB_SIGN_IN', 'WINDOWS_ACTIVATION', 'MOBILE_ACTIVATION', 'EXTRA_SEAT_INVITE')),
    target text,
    expires_at timestamptz not null,
    consumed_at timestamptz,
    consumed_by_device_id uuid references public.bl_devices(id) on delete set null,
    created_at timestamptz not null default now()
);

create table if not exists public.bl_checkout_claims (
    id uuid primary key default gen_random_uuid(),
    token_hash text not null unique,
    provider_checkout_session_id text not null unique,
    plan_code text not null,
    extra_desktop_quantity integer not null default 0 check (extra_desktop_quantity >= 0),
    expires_at timestamptz not null,
    consumed_at timestamptz,
    created_at timestamptz not null default now()
);

create table if not exists public.bl_webhook_events (
    id uuid primary key default gen_random_uuid(),
    provider text not null default 'STRIPE' check (provider = 'STRIPE'),
    event_id text not null unique,
    event_type text not null,
    status text not null default 'PROCESSING'
        check (status in ('PROCESSING', 'COMPLETED', 'FAILED')),
    attempts integer not null default 1 check (attempts > 0),
    last_error text,
    received_at timestamptz not null default now(),
    completed_at timestamptz,
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_device_sync_events (
    id uuid primary key default gen_random_uuid(),
    device_id uuid not null references public.bl_devices(id) on delete cascade,
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    event_id text not null,
    event_type text not null,
    payload jsonb not null default '{}'::jsonb,
    client_created_at timestamptz,
    received_at timestamptz not null default now(),
    unique (device_id, event_id)
);

create table if not exists public.bl_device_snapshots (
    device_id uuid primary key references public.bl_devices(id) on delete cascade,
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    snapshot jsonb not null default '{}'::jsonb,
    client_updated_at timestamptz,
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_cash_registers (
    id uuid primary key default gen_random_uuid(),
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    seat_id uuid references public.bl_device_seats(id) on delete set null,
    name text not null,
    status text not null default 'ACTIVE' check (status in ('ACTIVE', 'ARCHIVED')),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.bl_cash_sessions (
    id uuid primary key default gen_random_uuid(),
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    cash_register_id uuid not null references public.bl_cash_registers(id) on delete restrict,
    device_id uuid not null references public.bl_devices(id) on delete restrict,
    operator_user_id uuid references auth.users(id) on delete set null,
    status text not null default 'OPEN' check (status in ('OPEN', 'CLOSED', 'FORCED_CLOSED')),
    opening_amount numeric(12,2) not null default 0,
    expected_amount numeric(12,2),
    closing_amount numeric(12,2),
    variance_amount numeric(12,2),
    opened_at timestamptz not null default now(),
    closed_at timestamptz,
    metadata jsonb not null default '{}'::jsonb
);

create unique index if not exists idx_bl_cash_sessions_one_open_per_register
    on public.bl_cash_sessions (cash_register_id)
    where status = 'OPEN';

create table if not exists public.bl_machine_fulfillments (
    id uuid primary key default gen_random_uuid(),
    subscription_id uuid not null references public.bl_subscriptions(id) on delete cascade,
    store_id uuid not null references public.bl_stores(id) on delete cascade,
    provider text not null default 'MERCADO_PAGO' check (provider = 'MERCADO_PAGO'),
    model text not null default 'POINT_PRO_3',
    status text not null default 'WAITING_ADDRESS'
        check (status in ('WAITING_ADDRESS', 'READY', 'REQUESTED', 'SHIPPED', 'DELIVERED', 'CANCELED')),
    recipient_name text,
    recipient_phone text,
    shipping_address jsonb not null default '{}'::jsonb,
    tracking_code text,
    requested_at timestamptz,
    shipped_at timestamptz,
    delivered_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (subscription_id)
);

create index if not exists idx_bl_stores_account on public.bl_stores (account_id);
create index if not exists idx_bl_store_members_user on public.bl_store_members (user_id, status);
create index if not exists idx_bl_subscriptions_store_status on public.bl_subscriptions (store_id, status);
create index if not exists idx_bl_device_seats_store_status on public.bl_device_seats (store_id, seat_kind, status);
create index if not exists idx_bl_devices_store_status on public.bl_devices (store_id, device_kind, status);
create index if not exists idx_bl_handoff_tokens_expiry on public.bl_handoff_tokens (expires_at) where consumed_at is null;
create index if not exists idx_bl_checkout_claims_expiry on public.bl_checkout_claims (expires_at) where consumed_at is null;
create index if not exists idx_bl_cash_sessions_store_opened on public.bl_cash_sessions (store_id, opened_at desc);

do $$
declare
    relation_name text;
begin
    foreach relation_name in array array[
        'bl_accounts',
        'bl_stores',
        'bl_store_members',
        'bl_subscriptions',
        'bl_entitlements',
        'bl_device_seats',
        'bl_devices',
        'bl_device_leases',
        'bl_onboarding_configs',
        'bl_handoff_tokens',
        'bl_checkout_claims',
        'bl_webhook_events',
        'bl_device_sync_events',
        'bl_device_snapshots',
        'bl_cash_registers',
        'bl_cash_sessions',
        'bl_machine_fulfillments'
    ]
    loop
        execute format('alter table public.%I enable row level security', relation_name);
        execute format('revoke all on public.%I from anon, authenticated', relation_name);
    end loop;
end
$$;
