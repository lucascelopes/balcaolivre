create extension if not exists pgcrypto;

create table if not exists public.bv_public_menus (
    id uuid primary key default gen_random_uuid(),
    store_id text,
    slug text not null unique,
    name text not null,
    description text,
    phone text,
    address text,
    city text,
    state text,
    logo_url text,
    theme_color text not null default '#0f766e',
    is_published boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.bv_public_menu_items (
    id uuid primary key default gen_random_uuid(),
    menu_id uuid not null references public.bv_public_menus(id) on delete cascade,
    code text,
    name text not null,
    description text,
    category text not null default 'Cardapio',
    price numeric(12, 2) not null default 0,
    stock_quantity numeric(12, 3) not null default 0,
    is_in_stock boolean not null default true,
    image_url text,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

alter table public.bv_public_menus
    add column if not exists logo_url text;

alter table public.bv_public_menu_items
    add column if not exists stock_quantity numeric(12, 3) not null default 0,
    add column if not exists is_in_stock boolean not null default true;

create index if not exists idx_bv_public_menu_items_menu_active
    on public.bv_public_menu_items (menu_id, is_active, category, sort_order, name);

alter table public.bv_public_menus enable row level security;
alter table public.bv_public_menu_items enable row level security;

drop policy if exists "Public menus can be read when published" on public.bv_public_menus;
create policy "Public menus can be read when published"
    on public.bv_public_menus
    for select
    to anon, authenticated
    using (is_published = true);

drop policy if exists "Public menu items can be read when menu is published" on public.bv_public_menu_items;
create policy "Public menu items can be read when menu is published"
    on public.bv_public_menu_items
    for select
    to anon, authenticated
    using (
        is_active = true
        and exists (
            select 1
            from public.bv_public_menus menus
            where menus.id = bv_public_menu_items.menu_id
              and menus.is_published = true
        )
    );

grant usage on schema public to anon, authenticated;
grant select on public.bv_public_menus to anon, authenticated;
grant select on public.bv_public_menu_items to anon, authenticated;

insert into public.bv_public_menus (
    slug,
    name,
    description,
    phone,
    address,
    city,
    state,
    is_published
) values (
    'demo',
    'Balcao Livre Restaurante',
    'Cardapio digital para demonstracao.',
    '(00) 00000-0000',
    'Endereco do restaurante',
    'Cidade',
    'UF',
    true
) on conflict (slug) do nothing;

insert into public.bv_public_menu_items (menu_id, code, name, description, category, price, sort_order)
select
    menu.id,
    item.code,
    item.name,
    item.description,
    item.category,
    item.price,
    item.sort_order
from public.bv_public_menus menu
cross join (
    values
        ('000001', 'Refrigerante lata', 'Bebida gelada 350 ml.', 'BEBIDAS', 6.00, 10),
        ('000002', 'X-burguer da casa', 'Pao, carne, queijo, salada e molho especial.', 'LANCHES', 24.90, 20),
        ('000003', 'Porcao de batata', 'Batata frita crocante para compartilhar.', 'PORCOES', 18.00, 30)
) as item(code, name, description, category, price, sort_order)
where menu.slug = 'demo'
  and not exists (
      select 1
      from public.bv_public_menu_items existing
      where existing.menu_id = menu.id
        and existing.code = item.code
  );
