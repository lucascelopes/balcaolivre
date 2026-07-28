CREATE TABLE `agenda_catalog_assets` (
	`store_id` text PRIMARY KEY NOT NULL,
	`content_type` text NOT NULL,
	`body` blob NOT NULL,
	`updated_at` integer NOT NULL,
	FOREIGN KEY (`store_id`) REFERENCES `agenda_stores`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
DROP INDEX `agenda_stores_instance_unique`;--> statement-breakpoint
ALTER TABLE `agenda_stores` ADD `owner_user_id` text DEFAULT '' NOT NULL;--> statement-breakpoint
ALTER TABLE `agenda_stores` ADD `catalog_version` integer DEFAULT 0 NOT NULL;--> statement-breakpoint
ALTER TABLE `agenda_stores` ADD `catalog_published_at` integer DEFAULT 0 NOT NULL;--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_stores_owner_unique` ON `agenda_stores` (`owner_user_id`) WHERE "agenda_stores"."owner_user_id" <> '';