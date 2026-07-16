import {
  createEvolutionInstance,
  createOnboardingStateKey,
  decodeEvolutionQrImage,
  disconnectEvolutionInstance,
  evolutionConnectionState,
  evolutionHealth,
  evolutionInstanceName,
  extractEvolutionMessages,
  extractEvolutionQr,
  findEvolutionMessages,
  onboardingProviderFromState,
  resolveWhatsAppProvider,
  sendEvolutionText,
} from "./evolution.ts";

const CONFIG = {
  baseUrl: "https://evolution.example",
  apiKey: "server-only-key",
};

Deno.test("selects Meta only when both Evolution secrets are absent", () => {
  assertEquals(resolveWhatsAppProvider(() => undefined), { kind: "meta" });
  const incomplete = resolveWhatsAppProvider((name) =>
    name === "WHATSAPP_EVOLUTION_BASE_URL"
      ? "https://evolution.example"
      : undefined
  );
  assertEquals(incomplete.kind, "invalid");
});

Deno.test("keeps Evolution and Meta onboarding states separated", () => {
  const evolution = createOnboardingStateKey("evolution");
  const meta = createOnboardingStateKey("meta");
  assert(evolution.startsWith("evo_"));
  assert(meta.startsWith("meta_"));
  assertEquals(onboardingProviderFromState(evolution), "evolution");
  assertEquals(onboardingProviderFromState(meta), "meta");
  assertEquals(onboardingProviderFromState("legacy-state"), "meta");
});

Deno.test("health uses an authenticated Evolution API route", async () => {
  let url = "";
  let key = "";
  const healthy = await evolutionHealth(CONFIG, (input, init) => {
    url = String(input);
    key = new Headers(init?.headers).get("apikey") ?? "";
    return new Response("[]");
  });
  assert(healthy.ok);
  assertEquals(url, "https://evolution.example/instance/fetchInstances");
  assertEquals(key, "server-only-key");

  const rejected = await evolutionHealth(
    CONFIG,
    () =>
      new Response(JSON.stringify({ message: "Unauthorized" }), {
        status: 401,
      }),
  );
  assert(!rejected.ok);
});

Deno.test("builds a stable non-reversible instance name from the license", async () => {
  const first = await evolutionInstanceName(
    "BLV-203512312359-AGENDALIVRE-45F756B662",
  );
  const second = await evolutionInstanceName(
    "blv-203512312359-agendalivre-45f756b662",
  );
  assertEquals(first, second);
  assert(first.startsWith("bl-"));
  assertEquals(first.length, 35);
  assert(!first.includes("AGENDALIVRE"));
});

Deno.test("reads the Evolution connection state and preserves a 404", async () => {
  const open = await evolutionConnectionState(
    CONFIG,
    "bl-example",
    () =>
      new Response(
        JSON.stringify({
          instance: { instanceName: "bl-example", state: "open" },
        }),
      ),
  );
  assert(open.ok);
  if (open.ok) assertEquals(open.data.state, "open");

  const missing = await evolutionConnectionState(
    CONFIG,
    "bl-missing",
    () =>
      new Response(
        JSON.stringify({ response: { message: ["Instance not found"] } }),
        { status: 404 },
      ),
  );
  assert(!missing.ok);
  if (!missing.ok) assert(missing.notFound);
});

Deno.test("creates the exact Baileys instance without exposing the API key in the body", async () => {
  let body = "";
  let headers = new Headers();
  const result = await createEvolutionInstance(
    CONFIG,
    "bl-example",
    (_input, init) => {
      body = String(init?.body ?? "");
      headers = new Headers(init?.headers);
      return new Response(
        JSON.stringify({ instance: { instanceName: "bl-example" } }),
        { status: 201 },
      );
    },
    "http://bot:8090/webhook/evolution",
  );

  assert(result.ok);
  assertEquals(headers.get("apikey"), "server-only-key");
  assertEquals(JSON.parse(body), {
    instanceName: "bl-example",
    integration: "WHATSAPP-BAILEYS",
    qrcode: false,
    rejectCall: true,
    webhook: {
      enabled: true,
      url: "http://bot:8090/webhook/evolution",
      byEvents: false,
      base64: false,
      events: [
        "QRCODE_UPDATED",
        "MESSAGES_UPSERT",
        "MESSAGES_UPDATE",
        "CONNECTION_UPDATE",
        "SEND_MESSAGE",
      ],
    },
  });
  assert(!body.includes("server-only-key"));
});

Deno.test("sends text once using the tenant instance", async () => {
  let calls = 0;
  let url = "";
  let body = "";
  const result = await sendEvolutionText(
    CONFIG,
    "bl-example",
    "5527999999999",
    "Pedido pronto",
    (input, init) => {
      calls += 1;
      url = String(input);
      body = String(init?.body ?? "");
      return new Response(JSON.stringify({ key: { id: "message-id" } }), {
        status: 201,
      });
    },
  );

  assert(result.ok);
  assertEquals(calls, 1);
  assertEquals(url, "https://evolution.example/message/sendText/bl-example");
  assertEquals(JSON.parse(body), {
    number: "5527999999999",
    text: "Pedido pronto",
    delay: 800,
    linkPreview: false,
  });
});

Deno.test("queries only the supplied tenant instance and caps message reads at 100", async () => {
  let url = "";
  let body = "";
  const result = await findEvolutionMessages(
    CONFIG,
    "bl-tenant-only",
    5_000,
    (input, init) => {
      url = String(input);
      body = String(init?.body ?? "");
      return new Response(JSON.stringify({ messages: { records: [] } }));
    },
  );

  assert(result.ok);
  assertEquals(
    url,
    "https://evolution.example/chat/findMessages/bl-tenant-only",
  );
  assertEquals(JSON.parse(body), { where: {}, page: 1, limit: 100 });

  const records = Array.from({ length: 130 }, (_, id) => ({ id }));
  const messages = extractEvolutionMessages({
    messages: { records },
  }, 1_000);
  assertEquals(messages.length, 100);
  assertEquals(messages[99], { id: 99 });
});

Deno.test("logs out and deletes only the supplied tenant instance", async () => {
  const requests: Array<{ url: string; method: string; key: string }> = [];
  const result = await disconnectEvolutionInstance(
    CONFIG,
    "bl-tenant-only",
    (input, init) => {
      requests.push({
        url: String(input),
        method: String(init?.method ?? "GET"),
        key: new Headers(init?.headers).get("apikey") ?? "",
      });
      return new Response(JSON.stringify({ status: "SUCCESS" }));
    },
  );

  assert(result.ok);
  assertEquals(requests, [
    {
      url: "https://evolution.example/instance/logout/bl-tenant-only",
      method: "DELETE",
      key: "server-only-key",
    },
    {
      url: "https://evolution.example/instance/delete/bl-tenant-only",
      method: "DELETE",
      key: "server-only-key",
    },
  ]);
});

Deno.test("disconnect is idempotent when the tenant instance is already absent", async () => {
  const result = await disconnectEvolutionInstance(
    CONFIG,
    "bl-missing",
    () =>
      new Response(JSON.stringify({ message: "Instance not found" }), {
        status: 404,
      }),
  );
  assert(result.ok);
});

Deno.test("accepts only safe QR image data", () => {
  const rawImage = new Uint8Array(128);
  rawImage.set([137, 80, 78, 71]);
  const imageBytes = btoa(String.fromCharCode(...rawImage));
  const valid = extractEvolutionQr({
    qrcode: {
      base64: `data:image/png;base64,${imageBytes}`,
      pairingCode: "1234-5678",
    },
  });
  assert(valid.image.startsWith("data:image/png;base64,"));
  assertEquals(valid.pairingCode, "1234-5678");
  const decoded = decodeEvolutionQrImage(valid.image);
  assert(decoded);
  assertEquals(decoded.contentType, "image/png");
  assertEquals(Array.from(decoded.bytes.slice(0, 4)), [137, 80, 78, 71]);

  const invalid = extractEvolutionQr({ base64: "javascript:alert(1)" });
  assertEquals(invalid.image, "");
  assertEquals(decodeEvolutionQrImage("javascript:alert(1)"), null);
});

function assert(
  condition: unknown,
  message = "assertion failed",
): asserts condition {
  if (!condition) throw new Error(message);
}

function assertEquals(actual: unknown, expected: unknown) {
  const left = JSON.stringify(actual);
  const right = JSON.stringify(expected);
  if (left !== right) throw new Error(`expected ${right}, received ${left}`);
}
