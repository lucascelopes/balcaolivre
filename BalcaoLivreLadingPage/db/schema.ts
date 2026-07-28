import { sql } from "drizzle-orm";
import {
  blob,
  index,
  integer,
  primaryKey,
  sqliteTable,
  text,
  uniqueIndex,
} from "drizzle-orm/sqlite-core";

export const agendaCloudAccounts = sqliteTable("agenda_cloud_accounts", {
  userId: text("user_id").primaryKey(),
  email: text("email").notNull(),
  payloadJson: text("payload_json"),
  revision: integer("revision").notNull().default(0),
  schemaVersion: integer("schema_version").notNull().default(1),
  trialStartedAt: integer("trial_started_at").notNull(),
  trialEndsAt: integer("trial_ends_at").notNull(),
  lastDeviceId: text("last_device_id").notNull().default(""),
  createdAt: integer("created_at").notNull(),
  updatedAt: integer("updated_at").notNull(),
});

export const agendaAndroidEntitlements = sqliteTable(
  "agenda_android_entitlements",
  {
    userId: text("user_id").primaryKey(),
    status: text("status").notNull().default("pending_activation"),
    trialStartedAt: integer("trial_started_at"),
    trialEndsAt: integer("trial_ends_at"),
    currentPeriodEndsAt: integer("current_period_ends_at"),
    graceEndsAt: integer("grace_ends_at"),
    paymentUrl: text("payment_url"),
    supportUrl: text("support_url"),
    provider: text("provider"),
    providerCustomerId: text("provider_customer_id"),
    providerSubscriptionId: text("provider_subscription_id"),
    providerEventId: text("provider_event_id"),
    providerEventAt: integer("provider_event_at"),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    index("agenda_android_entitlements_status_idx").on(
      table.status,
      table.trialEndsAt,
      table.currentPeriodEndsAt,
    ),
    uniqueIndex("agenda_android_entitlements_subscription_unique")
      .on(table.provider, table.providerSubscriptionId)
      .where(sql`${table.providerSubscriptionId} is not null`),
  ],
);

export const agendaAndroidRegistrations = sqliteTable(
  "agenda_android_registrations",
  {
    id: text("id").primaryKey(),
    userId: text("user_id").notNull(),
    email: text("email").notNull(),
    businessName: text("business_name").notNull(),
    status: text("status").notNull().default("active"),
    sideloadConsentAt: integer("sideload_consent_at").notNull(),
    sideloadConsentVersion: text("sideload_consent_version").notNull(),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    index("agenda_android_registrations_user_idx").on(
      table.userId,
      table.createdAt,
    ),
  ],
);

export const agendaAndroidBranding = sqliteTable(
  "agenda_android_branding",
  {
    userId: text("user_id").primaryKey(),
    registrationId: text("registration_id").notNull(),
    businessName: text("business_name").notNull(),
    iconObjectKey: text("icon_object_key").notNull(),
    iconContentType: text("icon_content_type").notNull(),
    iconSha256: text("icon_sha256").notNull(),
    coverObjectKey: text("cover_object_key"),
    coverContentType: text("cover_content_type"),
    coverSha256: text("cover_sha256"),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    index("agenda_android_branding_registration_idx").on(
      table.registrationId,
    ),
  ],
);

export const agendaAndroidBuilds = sqliteTable(
  "agenda_android_builds",
  {
    id: text("id").primaryKey(),
    userId: text("user_id").notNull(),
    registrationId: text("registration_id").notNull(),
    status: text("status").notNull().default("queued"),
    applicationId: text("application_id")
      .notNull()
      .default("br.com.balcaolivre.agenda_livre"),
    appName: text("app_name").notNull(),
    versionCode: integer("version_code").notNull(),
    versionName: text("version_name").notNull(),
    iconObjectKey: text("icon_object_key").notNull(),
    iconContentType: text("icon_content_type").notNull(),
    iconSha256: text("icon_sha256").notNull(),
    coverObjectKey: text("cover_object_key"),
    coverContentType: text("cover_content_type"),
    coverSha256: text("cover_sha256"),
    artifactObjectKey: text("artifact_object_key"),
    artifactFileName: text("artifact_file_name"),
    artifactContentType: text("artifact_content_type"),
    artifactSize: integer("artifact_size"),
    artifactSha256: text("artifact_sha256"),
    downloadTokenHash: text("download_token_hash"),
    downloadTokenExpiresAt: integer("download_token_expires_at"),
    workerId: text("worker_id"),
    attemptCount: integer("attempt_count").notNull().default(0),
    errorCode: text("error_code"),
    errorMessage: text("error_message"),
    createdAt: integer("created_at").notNull(),
    startedAt: integer("started_at"),
    completedAt: integer("completed_at"),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    index("agenda_android_builds_user_idx").on(table.userId, table.createdAt),
    index("agenda_android_builds_queue_idx").on(
      table.status,
      table.createdAt,
    ),
    uniqueIndex("agenda_android_builds_artifact_unique")
      .on(table.artifactObjectKey)
      .where(sql`${table.artifactObjectKey} is not null`),
  ],
);

export const agendaAndroidProvisioningTokens = sqliteTable(
  "agenda_android_provisioning_tokens",
  {
    id: text("id").primaryKey(),
    buildId: text("build_id").notNull(),
    userId: text("user_id").notNull(),
    tokenHash: text("token_hash").notNull(),
    expiresAt: integer("expires_at").notNull(),
    usedAt: integer("used_at"),
    usedDeviceId: text("used_device_id"),
    createdAt: integer("created_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_android_provisioning_token_hash_unique").on(
      table.tokenHash,
    ),
    index("agenda_android_provisioning_build_idx").on(
      table.buildId,
      table.createdAt,
    ),
  ],
);

export const agendaAndroidDevices = sqliteTable(
  "agenda_android_devices",
  {
    id: text("id").primaryKey(),
    userId: text("user_id").notNull(),
    buildId: text("build_id").notNull(),
    devicePublicId: text("device_public_id").notNull(),
    platform: text("platform").notNull().default("android"),
    appVersion: text("app_version").notNull().default(""),
    revokedAt: integer("revoked_at"),
    lastSeenAt: integer("last_seen_at").notNull(),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_android_devices_user_public_unique").on(
      table.userId,
      table.devicePublicId,
    ),
    index("agenda_android_devices_user_idx").on(table.userId, table.revokedAt),
  ],
);

export const agendaAndroidSessions = sqliteTable(
  "agenda_android_sessions",
  {
    id: text("id").primaryKey(),
    deviceId: text("device_id").notNull(),
    userId: text("user_id").notNull(),
    tokenHash: text("token_hash").notNull(),
    expiresAt: integer("expires_at").notNull(),
    revokedAt: integer("revoked_at"),
    lastSeenAt: integer("last_seen_at").notNull(),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_android_sessions_token_hash_unique").on(
      table.tokenHash,
    ),
    index("agenda_android_sessions_device_idx").on(
      table.deviceId,
      table.revokedAt,
    ),
    index("agenda_android_sessions_expiry_idx").on(
      table.expiresAt,
      table.revokedAt,
    ),
  ],
);

export const agendaAndroidBillingEvents = sqliteTable(
  "agenda_android_billing_events",
  {
    eventId: text("event_id").primaryKey(),
    eventType: text("event_type").notNull(),
    userId: text("user_id"),
    payloadSha256: text("payload_sha256").notNull(),
    outcome: text("outcome").notNull(),
    createdAt: integer("created_at").notNull(),
    processedAt: integer("processed_at").notNull(),
  },
  (table) => [
    index("agenda_android_billing_events_user_idx").on(
      table.userId,
      table.processedAt,
    ),
  ],
);

export const agendaSubscriptionClaims = sqliteTable(
  "agenda_subscription_claims",
  {
    claimId: text("claim_id").primaryKey(),
    checkoutSessionId: text("checkout_session_id").notNull(),
    providerCustomerId: text("provider_customer_id"),
    providerSubscriptionId: text("provider_subscription_id"),
    plan: text("plan").notNull(),
    status: text("status").notNull().default("checkout_open"),
    userId: text("user_id"),
    checkoutEmailMasked: text("checkout_email_masked"),
    currentPeriodEndsAt: integer("current_period_ends_at"),
    providerEventId: text("provider_event_id"),
    providerEventAt: integer("provider_event_at"),
    claimedAt: integer("claimed_at"),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_subscription_claims_session_unique").on(
      table.checkoutSessionId,
    ),
    uniqueIndex("agenda_subscription_claims_subscription_unique")
      .on(table.providerSubscriptionId)
      .where(sql`${table.providerSubscriptionId} is not null`),
    index("agenda_subscription_claims_user_idx").on(
      table.userId,
      table.updatedAt,
    ),
  ],
);

export const agendaStores = sqliteTable(
  "agenda_stores",
  {
    id: text("id").primaryKey(),
    ownerUserId: text("owner_user_id").notNull().default(""),
    instance: text("instance").notNull(),
    licenseHash: text("license_hash").notNull(),
    machineHash: text("machine_hash").notNull(),
    machineCode: text("machine_code").notNull(),
    desiredSlug: text("desired_slug").notNull(),
    slug: text("slug").notNull(),
    name: text("name").notNull(),
    segment: text("segment").notNull(),
    themeJson: text("theme_json").notNull().default("{}"),
    catalogJson: text("catalog_json").notNull().default("{}"),
    catalogVersion: integer("catalog_version").notNull().default(0),
    catalogPublishedAt: integer("catalog_published_at").notNull().default(0),
    generatedAt: text("generated_at").notNull(),
    lastSyncedAt: integer("last_synced_at").notNull(),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_stores_owner_unique")
      .on(table.ownerUserId)
      .where(sql`${table.ownerUserId} <> ''`),
    uniqueIndex("agenda_stores_slug_unique").on(table.slug),
    index("agenda_stores_last_synced_idx").on(table.lastSyncedAt),
  ],
);

export const agendaStoreDomains = sqliteTable(
  "agenda_store_domains",
  {
    hostname: text("hostname").primaryKey(),
    storeId: text("store_id")
      .notNull()
      .references(() => agendaStores.id, { onDelete: "cascade" }),
    providerId: text("provider_id").notNull().default(""),
    status: text("status").notNull().default("pending"),
    providerStatus: text("provider_status").notNull().default(""),
    sslStatus: text("ssl_status").notNull().default(""),
    cnameTarget: text("cname_target").notNull().default(""),
    validationRecordsJson: text("validation_records_json").notNull().default("[]"),
    lastError: text("last_error").notNull().default(""),
    verifiedAt: integer("verified_at"),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_store_domains_store_unique").on(table.storeId),
    index("agenda_store_domains_status_idx").on(table.status, table.updatedAt),
  ],
);

export const agendaCatalogAssets = sqliteTable("agenda_catalog_assets", {
  storeId: text("store_id")
    .primaryKey()
    .references(() => agendaStores.id, { onDelete: "cascade" }),
  contentType: text("content_type").notNull(),
  body: blob("body", { mode: "buffer" }).notNull(),
  updatedAt: integer("updated_at").notNull(),
});

export const agendaCatalogMedia = sqliteTable(
  "agenda_catalog_media",
  {
    storeId: text("store_id")
      .notNull()
      .references(() => agendaStores.id, { onDelete: "cascade" }),
    mediaId: text("media_id").notNull(),
    contentType: text("content_type").notNull(),
    body: blob("body", { mode: "buffer" }).notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    primaryKey({ columns: [table.storeId, table.mediaId] }),
    index("agenda_catalog_media_store_idx").on(table.storeId, table.updatedAt),
  ],
);

export const agendaSnapshots = sqliteTable("agenda_snapshots", {
  storeId: text("store_id")
    .primaryKey()
    .references(() => agendaStores.id, { onDelete: "cascade" }),
  servicesJson: text("services_json").notNull(),
  generatedAt: text("generated_at").notNull(),
  receivedAt: integer("received_at").notNull(),
});

export const agendaBookings = sqliteTable(
  "agenda_bookings",
  {
    id: text("id").primaryKey(),
    storeId: text("store_id")
      .notNull()
      .references(() => agendaStores.id, { onDelete: "cascade" }),
    source: text("source").notNull().default("web"),
    status: text("status").notNull(),
    statusTokenHash: text("status_token_hash").notNull(),
    idempotencyKey: text("idempotency_key").notNull(),
    slotKey: text("slot_key").notNull(),
    serviceId: text("service_id").notNull(),
    serviceName: text("service_name").notNull(),
    slotId: text("slot_id").notNull(),
    startsAt: text("starts_at").notNull(),
    startsAtMs: integer("starts_at_ms").notNull(),
    durationMinutes: integer("duration_minutes").notNull(),
    priceCents: integer("price_cents").notNull(),
    professionalId: text("professional_id").notNull(),
    professionalName: text("professional_name").notNull(),
    resourceName: text("resource_name").notNull().default(""),
    customerName: text("customer_name").notNull(),
    customerPhone: text("customer_phone").notNull(),
    notes: text("notes").notNull().default(""),
    appointmentId: text("appointment_id"),
    message: text("message"),
    confirmationSentAt: integer("confirmation_sent_at"),
    reminderSentAt: integer("reminder_sent_at"),
    confirmedAt: integer("confirmed_at"),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_bookings_status_token_unique").on(table.statusTokenHash),
    uniqueIndex("agenda_bookings_idempotency_unique").on(
      table.storeId,
      table.idempotencyKey,
    ),
    uniqueIndex("agenda_bookings_active_slot_unique")
      .on(table.storeId, table.slotKey)
      .where(
        sql`${table.status} in ('requested', 'pending', 'confirmed')`,
      ),
    index("agenda_bookings_store_status_idx").on(
      table.storeId,
      table.status,
      table.startsAtMs,
    ),
    index("agenda_bookings_reminder_idx").on(
      table.status,
      table.reminderSentAt,
      table.startsAtMs,
    ),
  ],
);

export const agendaBookingLocks = sqliteTable(
  "agenda_booking_locks",
  {
    storeId: text("store_id")
      .notNull()
      .references(() => agendaStores.id, { onDelete: "cascade" }),
    professionalId: text("professional_id").notNull(),
    lockStartMs: integer("lock_start_ms").notNull(),
    bookingId: text("booking_id")
      .notNull()
      .references(() => agendaBookings.id, { onDelete: "cascade" }),
  },
  (table) => [
    primaryKey({
      name: "agenda_booking_locks_pk",
      columns: [table.storeId, table.professionalId, table.lockStartMs],
    }),
    index("agenda_booking_locks_booking_idx").on(table.bookingId),
  ],
);
