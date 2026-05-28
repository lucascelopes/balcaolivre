alter table public.bv_public_menus
    add column if not exists discount_enabled boolean not null default true,
    add column if not exists discount_code text not null default 'EXCLUSIVO4',
    add column if not exists discount_amount numeric(12, 2) not null default 4,
    add column if not exists discount_description text not null default 'Use no atendimento para ganhar desconto no pedido.',
    add column if not exists loyalty_enabled boolean not null default true,
    add column if not exists loyalty_goal integer not null default 20,
    add column if not exists loyalty_minimum_order numeric(12, 2) not null default 20;
