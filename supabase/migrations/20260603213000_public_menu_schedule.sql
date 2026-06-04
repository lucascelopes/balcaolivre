alter table public.bv_public_menus
    add column if not exists schedule_enabled boolean not null default true,
    add column if not exists open_time text not null default '00:00',
    add column if not exists close_time text not null default '00:00';

update public.bv_public_menus
set
    schedule_enabled = coalesce(schedule_enabled, true),
    open_time = case
        when open_time ~ '^\d{2}:\d{2}$' then open_time
        else '00:00'
    end,
    close_time = case
        when close_time ~ '^\d{2}:\d{2}$' then close_time
        else '00:00'
    end;
