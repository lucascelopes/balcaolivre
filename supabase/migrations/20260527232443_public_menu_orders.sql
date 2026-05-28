create extension if not exists pgcrypto;

create table if not exists public.bv_public_orders (
    id uuid primary key default gen_random_uuid(),
    menu_id uuid references public.bv_public_menus(id) on delete set null,
    store_id text not null,
    slug text not null,
    source text not null default 'CARDAPIO_ONLINE',
    status text not null default 'NOVO',
    customer_name text,
    customer_phone text,
    customer_document text,
    order_type text not null,
    table_label text,
    address text,
    district text,
    reference text,
    desired_time text,
    notes text,
    subtotal numeric(12, 2) not null default 0,
    delivery_fee numeric(12, 2) not null default 0,
    total numeric(12, 2) not null default 0,
    items jsonb not null default '[]'::jsonb,
    customer jsonb not null default '{}'::jsonb,
    payload jsonb not null default '{}'::jsonb,
    pdv_order_id text,
    imported_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

alter table public.bv_public_orders
    add column if not exists customer_document text,
    add column if not exists district text,
    add column if not exists delivery_fee numeric(12, 2) not null default 0,
    add column if not exists pdv_order_id text,
    add column if not exists imported_at timestamptz;

create index if not exists idx_bv_public_orders_store_status_created
    on public.bv_public_orders (store_id, status, created_at);

create index if not exists idx_bv_public_orders_menu_created
    on public.bv_public_orders (menu_id, created_at desc);

alter table public.bv_public_orders enable row level security;

revoke all on public.bv_public_orders from anon, authenticated;
