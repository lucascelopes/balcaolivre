create table if not exists public.bvpdv_admin_store (
    id text primary key default 'main',
    data jsonb not null default '{"licenses":[],"devices":[],"events":[]}'::jsonb,
    updated_at timestamptz not null default now()
);

alter table public.bvpdv_admin_store enable row level security;

revoke all on table public.bvpdv_admin_store from anon;
revoke all on table public.bvpdv_admin_store from authenticated;
grant all on table public.bvpdv_admin_store to service_role;

insert into public.bvpdv_admin_store (id, data)
values ('main', '{"licenses":[],"devices":[],"events":[]}'::jsonb)
on conflict (id) do nothing;
