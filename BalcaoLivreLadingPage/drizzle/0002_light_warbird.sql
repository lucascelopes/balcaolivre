CREATE TABLE `agenda_android_billing_events` (
	`event_id` text PRIMARY KEY NOT NULL,
	`event_type` text NOT NULL,
	`user_id` text,
	`payload_sha256` text NOT NULL,
	`outcome` text NOT NULL,
	`created_at` integer NOT NULL,
	`processed_at` integer NOT NULL
);
--> statement-breakpoint
CREATE INDEX `agenda_android_billing_events_user_idx` ON `agenda_android_billing_events` (`user_id`,`processed_at`);--> statement-breakpoint
CREATE TABLE `agenda_android_branding` (
	`user_id` text PRIMARY KEY NOT NULL,
	`registration_id` text NOT NULL,
	`business_name` text NOT NULL,
	`icon_object_key` text NOT NULL,
	`icon_content_type` text NOT NULL,
	`icon_sha256` text NOT NULL,
	`cover_object_key` text,
	`cover_content_type` text,
	`cover_sha256` text,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE INDEX `agenda_android_branding_registration_idx` ON `agenda_android_branding` (`registration_id`);--> statement-breakpoint
CREATE TABLE `agenda_android_builds` (
	`id` text PRIMARY KEY NOT NULL,
	`user_id` text NOT NULL,
	`registration_id` text NOT NULL,
	`status` text DEFAULT 'queued' NOT NULL,
	`application_id` text DEFAULT 'br.com.balcaolivre.agenda_livre' NOT NULL,
	`app_name` text NOT NULL,
	`version_code` integer NOT NULL,
	`version_name` text NOT NULL,
	`icon_object_key` text NOT NULL,
	`icon_content_type` text NOT NULL,
	`icon_sha256` text NOT NULL,
	`cover_object_key` text,
	`cover_content_type` text,
	`cover_sha256` text,
	`artifact_object_key` text,
	`artifact_file_name` text,
	`artifact_content_type` text,
	`artifact_size` integer,
	`artifact_sha256` text,
	`download_token_hash` text,
	`download_token_expires_at` integer,
	`worker_id` text,
	`attempt_count` integer DEFAULT 0 NOT NULL,
	`error_code` text,
	`error_message` text,
	`created_at` integer NOT NULL,
	`started_at` integer,
	`completed_at` integer,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE INDEX `agenda_android_builds_user_idx` ON `agenda_android_builds` (`user_id`,`created_at`);--> statement-breakpoint
CREATE INDEX `agenda_android_builds_queue_idx` ON `agenda_android_builds` (`status`,`created_at`);--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_android_builds_artifact_unique` ON `agenda_android_builds` (`artifact_object_key`) WHERE "agenda_android_builds"."artifact_object_key" is not null;--> statement-breakpoint
CREATE TABLE `agenda_android_devices` (
	`id` text PRIMARY KEY NOT NULL,
	`user_id` text NOT NULL,
	`build_id` text NOT NULL,
	`device_public_id` text NOT NULL,
	`platform` text DEFAULT 'android' NOT NULL,
	`app_version` text DEFAULT '' NOT NULL,
	`revoked_at` integer,
	`last_seen_at` integer NOT NULL,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_android_devices_user_public_unique` ON `agenda_android_devices` (`user_id`,`device_public_id`);--> statement-breakpoint
CREATE INDEX `agenda_android_devices_user_idx` ON `agenda_android_devices` (`user_id`,`revoked_at`);--> statement-breakpoint
CREATE TABLE `agenda_android_entitlements` (
	`user_id` text PRIMARY KEY NOT NULL,
	`status` text DEFAULT 'pending_activation' NOT NULL,
	`trial_started_at` integer,
	`trial_ends_at` integer,
	`current_period_ends_at` integer,
	`grace_ends_at` integer,
	`payment_url` text,
	`support_url` text,
	`provider` text,
	`provider_customer_id` text,
	`provider_subscription_id` text,
	`provider_event_id` text,
	`provider_event_at` integer,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE INDEX `agenda_android_entitlements_status_idx` ON `agenda_android_entitlements` (`status`,`trial_ends_at`,`current_period_ends_at`);--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_android_entitlements_subscription_unique` ON `agenda_android_entitlements` (`provider`,`provider_subscription_id`) WHERE "agenda_android_entitlements"."provider_subscription_id" is not null;--> statement-breakpoint
CREATE TABLE `agenda_android_provisioning_tokens` (
	`id` text PRIMARY KEY NOT NULL,
	`build_id` text NOT NULL,
	`user_id` text NOT NULL,
	`token_hash` text NOT NULL,
	`expires_at` integer NOT NULL,
	`used_at` integer,
	`used_device_id` text,
	`created_at` integer NOT NULL
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_android_provisioning_token_hash_unique` ON `agenda_android_provisioning_tokens` (`token_hash`);--> statement-breakpoint
CREATE INDEX `agenda_android_provisioning_build_idx` ON `agenda_android_provisioning_tokens` (`build_id`,`created_at`);--> statement-breakpoint
CREATE TABLE `agenda_android_registrations` (
	`id` text PRIMARY KEY NOT NULL,
	`user_id` text NOT NULL,
	`email` text NOT NULL,
	`business_name` text NOT NULL,
	`status` text DEFAULT 'active' NOT NULL,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE INDEX `agenda_android_registrations_user_idx` ON `agenda_android_registrations` (`user_id`,`created_at`);--> statement-breakpoint
CREATE TABLE `agenda_android_sessions` (
	`id` text PRIMARY KEY NOT NULL,
	`device_id` text NOT NULL,
	`user_id` text NOT NULL,
	`token_hash` text NOT NULL,
	`expires_at` integer NOT NULL,
	`revoked_at` integer,
	`last_seen_at` integer NOT NULL,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_android_sessions_token_hash_unique` ON `agenda_android_sessions` (`token_hash`);--> statement-breakpoint
CREATE INDEX `agenda_android_sessions_device_idx` ON `agenda_android_sessions` (`device_id`,`revoked_at`);--> statement-breakpoint
CREATE INDEX `agenda_android_sessions_expiry_idx` ON `agenda_android_sessions` (`expires_at`,`revoked_at`);