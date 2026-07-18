CREATE TABLE `agenda_cloud_accounts` (
	`user_id` text PRIMARY KEY NOT NULL,
	`email` text NOT NULL,
	`payload_json` text,
	`revision` integer DEFAULT 0 NOT NULL,
	`schema_version` integer DEFAULT 1 NOT NULL,
	`trial_started_at` integer NOT NULL,
	`trial_ends_at` integer NOT NULL,
	`last_device_id` text DEFAULT '' NOT NULL,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
