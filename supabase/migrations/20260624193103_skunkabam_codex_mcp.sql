create extension if not exists pgcrypto;

create table if not exists public.skunkabam_codex_threads (
    id uuid primary key default gen_random_uuid(),
    license_key text not null,
    machine_hash text not null,
    external_thread_id text not null,
    source text not null default 'codex',
    title text not null default '',
    status text not null default 'active',
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    last_message_at timestamptz,
    constraint skunkabam_codex_threads_status_check
        check (status in ('active', 'paused', 'done', 'blocked', 'archived')),
    constraint skunkabam_codex_threads_license_machine_external_key
        unique (license_key, machine_hash, external_thread_id)
);

create table if not exists public.skunkabam_codex_cards (
    id uuid primary key default gen_random_uuid(),
    license_key text not null,
    machine_hash text not null,
    thread_id uuid references public.skunkabam_codex_threads(id) on delete set null,
    external_card_id text,
    title text not null,
    description text not null default '',
    status text not null default 'backlog',
    priority text not null default 'normal',
    labels text[] not null default '{}'::text[],
    assignee text,
    due_at timestamptz,
    completed_at timestamptz,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint skunkabam_codex_cards_status_check
        check (status in ('backlog', 'todo', 'doing', 'review', 'done', 'blocked', 'archived')),
    constraint skunkabam_codex_cards_priority_check
        check (priority in ('low', 'normal', 'high', 'urgent')),
    constraint skunkabam_codex_cards_license_machine_external_key
        unique (license_key, machine_hash, external_card_id)
);

create table if not exists public.skunkabam_codex_messages (
    id uuid primary key default gen_random_uuid(),
    thread_id uuid not null references public.skunkabam_codex_threads(id) on delete cascade,
    license_key text not null,
    machine_hash text not null,
    local_message_id text,
    role text not null,
    content text not null,
    content_redacted boolean not null default false,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    constraint skunkabam_codex_messages_role_check
        check (role in ('user', 'assistant', 'system', 'developer', 'tool')),
    constraint skunkabam_codex_messages_thread_local_key
        unique (thread_id, local_message_id)
);

create table if not exists public.skunkabam_codex_actions (
    id uuid primary key default gen_random_uuid(),
    thread_id uuid references public.skunkabam_codex_threads(id) on delete cascade,
    card_id uuid references public.skunkabam_codex_cards(id) on delete set null,
    license_key text not null,
    machine_hash text not null,
    action_type text not null,
    title text not null default '',
    summary text not null default '',
    outcome text not null default 'logged',
    payload jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now(),
    constraint skunkabam_codex_actions_outcome_check
        check (outcome in ('logged', 'success', 'failed', 'blocked', 'skipped'))
);

create table if not exists public.skunkabam_codex_links (
    id uuid primary key default gen_random_uuid(),
    thread_id uuid references public.skunkabam_codex_threads(id) on delete cascade,
    card_id uuid references public.skunkabam_codex_cards(id) on delete set null,
    license_key text not null,
    machine_hash text not null,
    link_type text not null default 'url',
    title text not null default '',
    url text,
    path text,
    metadata jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
);

create index if not exists idx_skunkabam_codex_threads_license_updated
    on public.skunkabam_codex_threads (license_key, machine_hash, updated_at desc);

create index if not exists idx_skunkabam_codex_cards_license_status_updated
    on public.skunkabam_codex_cards (license_key, machine_hash, status, updated_at desc);

create index if not exists idx_skunkabam_codex_cards_thread
    on public.skunkabam_codex_cards (thread_id);

create index if not exists idx_skunkabam_codex_messages_thread_created
    on public.skunkabam_codex_messages (thread_id, created_at);

create index if not exists idx_skunkabam_codex_actions_thread_created
    on public.skunkabam_codex_actions (thread_id, created_at desc);

create index if not exists idx_skunkabam_codex_links_thread_created
    on public.skunkabam_codex_links (thread_id, created_at desc);

alter table public.skunkabam_codex_threads enable row level security;
alter table public.skunkabam_codex_cards enable row level security;
alter table public.skunkabam_codex_messages enable row level security;
alter table public.skunkabam_codex_actions enable row level security;
alter table public.skunkabam_codex_links enable row level security;

revoke all on public.skunkabam_codex_threads from anon, authenticated;
revoke all on public.skunkabam_codex_cards from anon, authenticated;
revoke all on public.skunkabam_codex_messages from anon, authenticated;
revoke all on public.skunkabam_codex_actions from anon, authenticated;
revoke all on public.skunkabam_codex_links from anon, authenticated;

grant select, insert, update, delete on public.skunkabam_codex_threads to service_role;
grant select, insert, update, delete on public.skunkabam_codex_cards to service_role;
grant select, insert, update, delete on public.skunkabam_codex_messages to service_role;
grant select, insert, update, delete on public.skunkabam_codex_actions to service_role;
grant select, insert, update, delete on public.skunkabam_codex_links to service_role;

comment on table public.skunkabam_codex_threads is 'Codex conversations captured for SkunKabam Kanban, scoped by linked license and machine.';
comment on table public.skunkabam_codex_cards is 'Kanban cards created or updated by the SkunKabam Codex MCP.';
comment on table public.skunkabam_codex_messages is 'Chat messages captured by the SkunKabam Codex MCP.';
comment on table public.skunkabam_codex_actions is 'Actions, commands, builds, tests, and work logs captured by the SkunKabam Codex MCP.';
comment on table public.skunkabam_codex_links is 'URLs and local paths linked to Codex work in SkunKabam.';
