ALTER TABLE `agenda_android_registrations` ADD `sideload_consent_at` integer NOT NULL DEFAULT 0;--> statement-breakpoint
ALTER TABLE `agenda_android_registrations` ADD `sideload_consent_version` text NOT NULL DEFAULT 'direct-apk-v1';
