alter table public.bv_public_menus
    add column if not exists cover_image_url text,
    add column if not exists store_open boolean not null default true,
    add column if not exists wait_min_minutes integer not null default 30,
    add column if not exists wait_max_minutes integer not null default 60;

update public.bv_public_menus
set
    store_open = true,
    wait_min_minutes = case when wait_min_minutes <= 0 then 30 else wait_min_minutes end,
    wait_max_minutes = case
        when wait_max_minutes < wait_min_minutes then greatest(wait_min_minutes, 60)
        else wait_max_minutes
    end;
