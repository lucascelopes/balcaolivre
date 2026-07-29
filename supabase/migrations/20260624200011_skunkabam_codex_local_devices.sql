create table if not exists public.skunkabam_codex_devices (
    device_id text primary key,
    secret_hash text not null,
    store_name text not null default 'SkunKabam',
    machine_code text not null default '',
    enabled boolean not null default true,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    last_seen_at timestamptz,
    constraint skunkabam_codex_devices_secret_hash_check
        check (secret_hash ~ '^[a-f0-9]{64}$')
);

create index if not exists idx_skunkabam_codex_devices_enabled
    on public.skunkabam_codex_devices (enabled, updated_at desc);

alter table public.skunkabam_codex_devices enable row level security;
revoke all on public.skunkabam_codex_devices from anon, authenticated;
grant select, insert, update, delete on public.skunkabam_codex_devices to service_role;

comment on table public.skunkabam_codex_devices is 'Local PC devices allowed to write Codex activity into SkunKabam without license validation.';
