import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../", import.meta.url);

test("Web trial repairs a stale entitlement from the account trial window", async () => {
  const source = await readFile(
    new URL("BalcaoLivreLadingPage/app/lib/agenda-android-server.ts", root),
    "utf8",
  );

  assert.match(source, /accountTrialIsActive && !state\.canUse/);
  assert.match(source, /SET status = 'trialing'/);
  assert.match(source, /trial_ends_at = \?2/);
});

test("authenticated Stripe checkout preserves only the remaining trial", async () => {
  const source = await readFile(
    new URL(
      "BalcaoLivreLadingPage/app/lib/agenda-subscription-server.ts",
      root,
    ),
    "utf8",
  );

  assert.match(source, /checkoutTrialContext\(request\)/);
  assert.match(source, /subscription_data\[trial_end\]/);
  assert.match(source, /agenda_trial_days_remaining/);
  assert.match(source, /trialDaysRemaining: trial\.daysRemaining/);
});

test("failed payment keeps access for five days before blocking", async () => {
  const source = await readFile(
    new URL("BalcaoLivreLadingPage/app/lib/agenda-android-server.ts", root),
    "utf8",
  );

  assert.match(source, /PAYMENT_GRACE_DURATION_MS = 5 \* 24 \* 60 \* 60 \* 1000/);
  assert.match(source, /update\.eventAt \+ PAYMENT_GRACE_DURATION_MS/);
  assert.match(source, /rawStatus === "past_due"[\s\S]*graceEndsAt !== null && graceEndsAt > now/);
});
