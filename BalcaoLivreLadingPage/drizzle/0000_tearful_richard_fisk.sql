CREATE TABLE `agenda_booking_locks` (
	`store_id` text NOT NULL,
	`professional_id` text NOT NULL,
	`lock_start_ms` integer NOT NULL,
	`booking_id` text NOT NULL,
	PRIMARY KEY(`store_id`, `professional_id`, `lock_start_ms`),
	FOREIGN KEY (`store_id`) REFERENCES `agenda_stores`(`id`) ON UPDATE no action ON DELETE cascade,
	FOREIGN KEY (`booking_id`) REFERENCES `agenda_bookings`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE INDEX `agenda_booking_locks_booking_idx` ON `agenda_booking_locks` (`booking_id`);--> statement-breakpoint
CREATE TABLE `agenda_bookings` (
	`id` text PRIMARY KEY NOT NULL,
	`store_id` text NOT NULL,
	`source` text DEFAULT 'web' NOT NULL,
	`status` text NOT NULL,
	`status_token_hash` text NOT NULL,
	`idempotency_key` text NOT NULL,
	`slot_key` text NOT NULL,
	`service_id` text NOT NULL,
	`service_name` text NOT NULL,
	`slot_id` text NOT NULL,
	`starts_at` text NOT NULL,
	`starts_at_ms` integer NOT NULL,
	`duration_minutes` integer NOT NULL,
	`price_cents` integer NOT NULL,
	`professional_id` text NOT NULL,
	`professional_name` text NOT NULL,
	`resource_name` text DEFAULT '' NOT NULL,
	`customer_name` text NOT NULL,
	`customer_phone` text NOT NULL,
	`notes` text DEFAULT '' NOT NULL,
	`appointment_id` text,
	`message` text,
	`confirmation_sent_at` integer,
	`reminder_sent_at` integer,
	`confirmed_at` integer,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL,
	FOREIGN KEY (`store_id`) REFERENCES `agenda_stores`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_bookings_status_token_unique` ON `agenda_bookings` (`status_token_hash`);--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_bookings_idempotency_unique` ON `agenda_bookings` (`store_id`,`idempotency_key`);--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_bookings_active_slot_unique` ON `agenda_bookings` (`store_id`,`slot_key`) WHERE "agenda_bookings"."status" in ('requested', 'pending', 'confirmed');--> statement-breakpoint
CREATE INDEX `agenda_bookings_store_status_idx` ON `agenda_bookings` (`store_id`,`status`,`starts_at_ms`);--> statement-breakpoint
CREATE INDEX `agenda_bookings_reminder_idx` ON `agenda_bookings` (`status`,`reminder_sent_at`,`starts_at_ms`);--> statement-breakpoint
CREATE TABLE `agenda_snapshots` (
	`store_id` text PRIMARY KEY NOT NULL,
	`services_json` text NOT NULL,
	`generated_at` text NOT NULL,
	`received_at` integer NOT NULL,
	FOREIGN KEY (`store_id`) REFERENCES `agenda_stores`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE TABLE `agenda_stores` (
	`id` text PRIMARY KEY NOT NULL,
	`instance` text NOT NULL,
	`license_hash` text NOT NULL,
	`machine_hash` text NOT NULL,
	`machine_code` text NOT NULL,
	`desired_slug` text NOT NULL,
	`slug` text NOT NULL,
	`name` text NOT NULL,
	`segment` text NOT NULL,
	`theme_json` text DEFAULT '{}' NOT NULL,
	`generated_at` text NOT NULL,
	`last_synced_at` integer NOT NULL,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_stores_instance_unique` ON `agenda_stores` (`instance`);--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_stores_slug_unique` ON `agenda_stores` (`slug`);--> statement-breakpoint
CREATE INDEX `agenda_stores_last_synced_idx` ON `agenda_stores` (`last_synced_at`);