import { sql } from "drizzle-orm";
import {
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

export const agendaStores = sqliteTable(
  "agenda_stores",
  {
    id: text("id").primaryKey(),
    instance: text("instance").notNull(),
    licenseHash: text("license_hash").notNull(),
    machineHash: text("machine_hash").notNull(),
    machineCode: text("machine_code").notNull(),
    desiredSlug: text("desired_slug").notNull(),
    slug: text("slug").notNull(),
    name: text("name").notNull(),
    segment: text("segment").notNull(),
    themeJson: text("theme_json").notNull().default("{}"),
    generatedAt: text("generated_at").notNull(),
    lastSyncedAt: integer("last_synced_at").notNull(),
    createdAt: integer("created_at").notNull(),
    updatedAt: integer("updated_at").notNull(),
  },
  (table) => [
    uniqueIndex("agenda_stores_instance_unique").on(table.instance),
    uniqueIndex("agenda_stores_slug_unique").on(table.slug),
    index("agenda_stores_last_synced_idx").on(table.lastSyncedAt),
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
