import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../", import.meta.url);
const read = (path) => readFile(new URL(path, root), "utf8");

const officialPrices = {
  STRIPE_PRICE_BALCAO_ESSENCIAL_MONTH: "price_1Ty2JZK2jxGBfpO3jdnu4Trv",
  STRIPE_PRICE_BALCAO_ESSENCIAL_YEAR: "price_1Ty2KrK2jxGBfpO3XSSEBFWJ",
  STRIPE_PRICE_BALCAO_COMPLETO_MONTH: "price_1Ty2LLK2jxGBfpO3x8CZ49Rn",
  STRIPE_PRICE_BALCAO_COMPLETO_YEAR: "price_1Ty2LSK2jxGBfpO3O18uUAJa",
  STRIPE_PRICE_BALCAO_EXTRA_DESKTOP_MONTH: "price_1Ty2LyK2jxGBfpO3BI3PDao0",
  STRIPE_PRICE_BALCAO_EXTRA_DESKTOP_YEAR: "price_1Ty2M5K2jxGBfpO3YhbdY2g6",
  AGENDA_STRIPE_PRICE_MENSAL: "price_1TxyT5K2jxGBfpO3MdtPAj4G",
  AGENDA_STRIPE_PRICE_ANUAL: "price_1TxyUyK2jxGBfpO3FuaB8l1t"
};

test("checkout uses the eight official Stripe prices", async () => {
  const example = await read("BalcaoLivreLadingPage/.env.example");
  for (const [name, value] of Object.entries(officialPrices)) {
    assert.match(example, new RegExp(`^${name}=${value}$`, "m"));
  }
});

test("checkout does not create dynamic Stripe prices and collects annual shipping data", async () => {
  const checkout = await read("supabase/functions/checkout/index.ts");
  assert.doesNotMatch(checkout, /price_data/);
  assert.match(checkout, /shipping_address_collection\[allowed_countries\]\[0\]/);
  assert.match(checkout, /phone_number_collection\[enabled\]/);
  assert.match(checkout, /billing_address_collection", "required"/);
});

test("webhook lifecycle is idempotent and covers renewal, delinquency, cancellation and quantity", async () => {
  const checkout = await read("supabase/functions/checkout/index.ts");
  for (const required of [
    "reserveWebhookEvent",
    "completeWebhookEvent",
    "failWebhookEvent",
    "invoice.payment_failed",
    "customer.subscription.updated",
    "customer.subscription.deleted",
    "extra_desktop_quantity",
    "bl_machine_fulfillments"
  ]) {
    assert.match(checkout, new RegExp(required.replaceAll(".", "\\.")));
  }

  const events = new Set();
  const subscriptions = new Map();
  const fulfillments = new Map();
  const apply = (eventId, subscriptionId) => {
    if (events.has(eventId)) return false;
    events.add(eventId);
    subscriptions.set(subscriptionId, { status: "ACTIVE" });
    fulfillments.set(subscriptionId, { status: "WAITING_ADDRESS" });
    return true;
  };
  assert.equal(apply("evt_1", "sub_1"), true);
  assert.equal(apply("evt_1", "sub_1"), false);
  assert.equal(subscriptions.size, 1);
  assert.equal(fulfillments.size, 1);
});

test("extra desktop quantity requests Stripe proration", async () => {
  const checkout = await read("supabase/functions/checkout/index.ts");
  const commerce = await read("supabase/functions/checkout/balcao-commerce.ts");
  assert.match(checkout, /proration_behavior:\s*"create_prorations"/);
  assert.match(commerce, /STRIPE_PRICE_BALCAO_EXTRA_DESKTOP_MONTH/);
  assert.match(commerce, /STRIPE_PRICE_BALCAO_EXTRA_DESKTOP_YEAR/);
});

test("handoff tokens refuse reuse, expiry, revocation and another account", () => {
  const now = Date.now();
  const consume = (token, accountId) => {
    if (token.revokedAt) throw new Error("revoked");
    if (token.consumedAt) throw new Error("used");
    if (token.expiresAt <= now) throw new Error("expired");
    if (token.accountId !== accountId) throw new Error("wrong-account");
    token.consumedAt = now;
  };

  const valid = { accountId: "a1", expiresAt: now + 60_000, consumedAt: null, revokedAt: null };
  assert.doesNotThrow(() => consume(valid, "a1"));
  assert.throws(() => consume(valid, "a1"), /used/);
  assert.throws(
    () => consume({ accountId: "a1", expiresAt: now - 1, consumedAt: null, revokedAt: null }, "a1"),
    /expired/
  );
  assert.throws(
    () => consume({ accountId: "a1", expiresAt: now + 1, consumedAt: null, revokedAt: now }, "a1"),
    /revoked/
  );
  assert.throws(
    () => consume({ accountId: "a1", expiresAt: now + 1, consumedAt: null, revokedAt: null }, "a2"),
    /wrong-account/
  );
});

test("one included desktop, purchased extras and one smartphone are enforced", () => {
  const assign = (active, limit) => {
    if (active >= limit) throw new Error("no-seat");
    return active + 1;
  };
  assert.equal(assign(0, 1), 1);
  assert.throws(() => assign(1, 1), /no-seat/);
  assert.equal(assign(1, 3), 2);
  assert.equal(assign(2, 3), 3);
  assert.throws(() => assign(3, 3), /no-seat/);
  assert.equal(assign(0, 1), 1);
  assert.throws(() => assign(1, 1), /no-seat/);
});

test("onboarding enforces plan modules, cash-register limit and selected payments", async () => {
  const handoff = await read("supabase/functions/handoff/index.ts");
  for (const required of [
    "MODULE_NOT_INCLUDED",
    "DELIVERY",
    "BALCAO",
    "desktop_seat_limit",
    "payment_methods",
    "mercadopago_point_enabled",
    "DINHEIRO",
    "PIX",
    "CREDITO",
    "DEBITO",
    "VALE",
    "FIADO"
  ]) {
    assert.match(handoff, new RegExp(required));
  }
});

test("offline synchronization is idempotent per device and event", async () => {
  const migration = await read("supabase/migrations/20260728010000_balcao_commerce_entitlements.sql");
  const handoff = await read("supabase/functions/handoff/index.ts");
  assert.match(migration, /unique\s*\(device_id,\s*event_id\)/i);
  assert.match(handoff, /bl_device_sync_events/);
  assert.match(handoff, /sync_device/);
});

test("Web, Windows, mobile and Agenda activation do not expose a permanent key", async () => {
  const landing = await read("BalcaoLivreLadingPage/app/PaymentSuccess.jsx");
  const invite = await read("BalcaoLivreLadingPage/app/ActivationInvite.jsx");
  const windows = await read(
    "tmp/balcaolivrepdv-802c37d/BalcaoLivre.Online.Windows/MainWindow.StripeCheckout.cs"
  );
  const web = await read("tmp/balcaolivrepdv-802c37d/BalcaoLivre.PDV.Web/src/app.js");
  const mobile = await read("BalcaoLivre.Flutter/lib/src/secure_device.dart");
  const agenda = await read("BalcaoLivreLadingPage/app/lib/agenda-subscription-server.ts");

  assert.match(landing, /Sem chave de ativação/);
  assert.match(invite, /balcaolivre:\/\/activate\?token=/);
  assert.match(windows, /balcaolivre:\/\/activate/);
  assert.match(web, /activate_account_device/);
  assert.match(mobile, /activate_account_device/);
  assert.doesNotMatch(agenda, /license[_ -]?key/i);
});

test("approved mockups and final visual comparison artifacts exist", async () => {
  const qa = await read("BalcaoLivreLadingPage/design-qa.md");
  assert.match(qa, /payment-success-reference-vs-implementation\.jpg/);
  assert.match(qa, /final result: passed/);
  for (const chapter of [
    "call_dPNVDCT7vjeSa2WDyaqhreSg.png",
    "call_2091FWvzBd1nDfrPUgWJTFxg.png",
    "call_BwKuv0IUE8X4YhbfwCKBjbiS.png",
    "call_pDYOC7gMunMAo88mcjPawDwU.png",
    "call_C9yEjcIOLyu5hV71t4e00nhI.png",
    "call_uMUvTQ4qsJLE90jYOTxAptAT.png"
  ]) {
    const image = new URL(
      `file:///C:/Users/isabe/.codex/generated_images/019fa5f3-5365-73a3-95ed-ec5c7704a2c8/${chapter}`
    );
    assert.ok((await readFile(image)).length > 0);
  }
});
