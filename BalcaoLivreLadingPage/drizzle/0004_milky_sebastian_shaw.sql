CREATE TABLE `agenda_store_domains` (
	`hostname` text PRIMARY KEY NOT NULL,
	`store_id` text NOT NULL,
	`status` text DEFAULT 'pending' NOT NULL,
	`provider_status` text DEFAULT '' NOT NULL,
	`ssl_status` text DEFAULT '' NOT NULL,
	`cname_target` text DEFAULT '' NOT NULL,
	`validation_records_json` text DEFAULT '[]' NOT NULL,
	`last_error` text DEFAULT '' NOT NULL,
	`verified_at` integer,
	`created_at` integer NOT NULL,
	`updated_at` integer NOT NULL,
	FOREIGN KEY (`store_id`) REFERENCES `agenda_stores`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_store_domains_store_unique` ON `agenda_store_domains` (`store_id`);--> statement-breakpoint
CREATE INDEX `agenda_store_domains_status_idx` ON `agenda_store_domains` (`status`,`updated_at`);--> statement-breakpoint
ALTER TABLE `agenda_stores` ADD `catalog_json` text DEFAULT '{}' NOT NULL;