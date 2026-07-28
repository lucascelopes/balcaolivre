CREATE TABLE `agenda_subscription_claims` (
  `claim_id` text PRIMARY KEY NOT NULL,
  `checkout_session_id` text NOT NULL,
  `provider_customer_id` text,
  `provider_subscription_id` text,
  `plan` text NOT NULL,
  `status` text NOT NULL DEFAULT 'checkout_open',
  `user_id` text,
  `checkout_email_masked` text,
  `current_period_ends_at` integer,
  `provider_event_id` text,
  `provider_event_at` integer,
  `claimed_at` integer,
  `created_at` integer NOT NULL,
  `updated_at` integer NOT NULL
);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_subscription_claims_session_unique`
  ON `agenda_subscription_claims` (`checkout_session_id`);
--> statement-breakpoint
CREATE UNIQUE INDEX `agenda_subscription_claims_subscription_unique`
  ON `agenda_subscription_claims` (`provider_subscription_id`)
  WHERE `provider_subscription_id` IS NOT NULL;
--> statement-breakpoint
CREATE INDEX `agenda_subscription_claims_user_idx`
  ON `agenda_subscription_claims` (`user_id`, `updated_at`);
