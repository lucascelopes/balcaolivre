CREATE TABLE `agenda_catalog_media` (
	`store_id` text NOT NULL,
	`media_id` text NOT NULL,
	`content_type` text NOT NULL,
	`body` blob NOT NULL,
	`updated_at` integer NOT NULL,
	PRIMARY KEY(`store_id`, `media_id`),
	FOREIGN KEY (`store_id`) REFERENCES `agenda_stores`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE INDEX `agenda_catalog_media_store_idx`
	ON `agenda_catalog_media` (`store_id`, `updated_at`);
