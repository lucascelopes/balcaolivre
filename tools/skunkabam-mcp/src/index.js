#!/usr/bin/env node

import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import process from "node:process";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import { randomUUID } from "node:crypto";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const packageDir = path.dirname(__dirname);
const defaultThreadId = firstNonEmpty(
  process.env.SKUN_KABAM_THREAD_ID,
  process.env.CODEX_THREAD_ID,
  process.env.CODEX_SESSION_ID,
  `codex-${os.hostname()}-${new Date().toISOString().slice(0, 10)}-${randomUUID().slice(0, 8)}`,
);

const jsonObject = z.record(z.any());

const server = new McpServer({
  name: "skunkabam-codex-mcp",
  version: "0.1.0",
});

server.registerTool(
  "skunkabam_status",
  {
    title: "Status SkunKabam MCP",
    description: "Verifica configuracao local e saude da Edge Function skunkabam-codex.",
    inputSchema: {},
  },
  async () => {
    const config = loadConfig();
    const summary = configSummary(config);
    try {
      const health = await callSkunKabam("/health", {}, { method: "GET", config, skipAuth: true });
      return toolJson({ ok: true, config: summary, health });
    } catch (error) {
      return toolJson({ ok: false, config: summary, message: errorMessage(error) }, true);
    }
  },
);

server.registerTool(
  "skunkabam_registrar_chat",
  {
    title: "Registrar Chat",
    description: "Registra uma mensagem de chat do Codex em uma thread do SkunKabam.",
    inputSchema: {
      role: z.enum(["user", "assistant", "system", "developer", "tool"]).default("user"),
      content: z.string().min(1),
      threadId: z.string().optional(),
      threadTitle: z.string().optional(),
      localMessageId: z.string().optional(),
      metadata: jsonObject.optional(),
    },
  },
  async (args) => {
    const result = await callSkunKabam("/sync", {
      thread: threadPayload(args),
      message: {
        role: args.role,
        content: args.content,
        localMessageId: args.localMessageId,
        metadata: args.metadata ?? {},
      },
    });
    return toolJson(result, isFailure(result));
  },
);

server.registerTool(
  "skunkabam_atualizar_card",
  {
    title: "Atualizar Card",
    description: "Cria ou atualiza um card do Kanban SkunKabam ligado a uma thread Codex.",
    inputSchema: {
      title: z.string().min(1),
      description: z.string().optional(),
      status: z.enum(["backlog", "todo", "doing", "review", "done", "blocked", "archived"]).default("backlog"),
      priority: z.enum(["low", "normal", "high", "urgent"]).default("normal"),
      labels: z.array(z.string()).optional(),
      externalCardId: z.string().optional(),
      threadId: z.string().optional(),
      threadTitle: z.string().optional(),
      assignee: z.string().optional(),
      dueAt: z.string().optional(),
      metadata: jsonObject.optional(),
    },
  },
  async (args) => {
    const result = await callSkunKabam("/sync", {
      thread: threadPayload(args),
      card: {
        externalCardId: args.externalCardId,
        title: args.title,
        description: args.description ?? "",
        status: args.status,
        priority: args.priority,
        labels: args.labels ?? [],
        assignee: args.assignee,
        dueAt: args.dueAt,
        metadata: args.metadata ?? {},
      },
    });
    return toolJson(result, isFailure(result));
  },
);

server.registerTool(
  "skunkabam_registrar_acao",
  {
    title: "Registrar Acao",
    description: "Registra comando, teste, build, commit, deploy ou outra acao feita pelo Codex.",
    inputSchema: {
      actionType: z.string().min(1),
      title: z.string().optional(),
      summary: z.string().optional(),
      outcome: z.enum(["logged", "success", "failed", "blocked", "skipped"]).default("logged"),
      payload: jsonObject.optional(),
      externalCardId: z.string().optional(),
      threadId: z.string().optional(),
      threadTitle: z.string().optional(),
    },
  },
  async (args) => {
    const result = await callSkunKabam("/sync", {
      thread: threadPayload(args),
      externalCardId: args.externalCardId,
      action: {
        actionType: args.actionType,
        title: args.title,
        summary: args.summary ?? "",
        outcome: args.outcome,
        payload: args.payload ?? {},
      },
    });
    return toolJson(result, isFailure(result));
  },
);

server.registerTool(
  "skunkabam_registrar_link",
  {
    title: "Registrar Link",
    description: "Anexa URL ou caminho local a uma thread/card do SkunKabam.",
    inputSchema: {
      linkType: z.string().default("url"),
      title: z.string().optional(),
      url: z.string().optional(),
      filePath: z.string().optional(),
      metadata: jsonObject.optional(),
      externalCardId: z.string().optional(),
      threadId: z.string().optional(),
      threadTitle: z.string().optional(),
    },
  },
  async (args) => {
    const result = await callSkunKabam("/sync", {
      thread: threadPayload(args),
      externalCardId: args.externalCardId,
      link: {
        linkType: args.linkType,
        title: args.title,
        url: args.url,
        filePath: args.filePath,
        metadata: args.metadata ?? {},
      },
    });
    return toolJson(result, isFailure(result));
  },
);

server.registerTool(
  "skunkabam_listar_cards",
  {
    title: "Listar Cards",
    description: "Lista cards do Kanban SkunKabam para o PC local vinculado.",
    inputSchema: {
      status: z.enum(["backlog", "todo", "doing", "review", "done", "blocked", "archived"]).optional(),
      limit: z.number().int().min(1).max(100).default(50),
    },
  },
  async (args) => {
    const result = await callSkunKabam("/cards/list", {
      status: args.status,
      limit: args.limit,
    });
    return toolJson(result, isFailure(result));
  },
);

server.registerTool(
  "skunkabam_obter_thread",
  {
    title: "Obter Thread",
    description: "Busca uma thread do Codex com mensagens, cards, acoes e links.",
    inputSchema: {
      threadId: z.string().optional(),
      limit: z.number().int().min(1).max(200).default(80),
    },
  },
  async (args) => {
    const result = await callSkunKabam("/thread/get", {
      threadId: args.threadId || defaultThreadId,
      limit: args.limit,
    });
    return toolJson(result, isFailure(result));
  },
);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

function threadPayload(args = {}) {
  return {
    externalThreadId: args.threadId || defaultThreadId,
    title: args.threadTitle || "Atendimento Codex",
    source: "codex",
    metadata: {
      mcp: "skunkabam-codex-mcp",
      hostname: os.hostname(),
      defaultThreadId,
    },
  };
}

async function callSkunKabam(endpoint, payload, options = {}) {
  const config = options.config ?? loadConfig();
  const functionUrl = resolveFunctionUrl(config);
  const url = endpoint.startsWith("http") ? endpoint : `${trimSlash(functionUrl)}${endpoint}`;
  const headers = {
    "content-type": "application/json",
  };

  if (!options.skipAuth) {
    headers["x-skun-device-id"] = required(config.deviceId, "SKUN_KABAM_DEVICE_ID ou codex-link.json/deviceId");
    headers["x-skun-device-secret"] = required(config.deviceSecret, "SKUN_KABAM_DEVICE_SECRET ou codex-link.json/deviceSecret");
    if (config.licenseKey) headers["x-skun-license"] = config.licenseKey;
    if (config.machineHash) headers["x-skun-machine"] = config.machineHash;
    if (config.machineCode) headers["x-skun-machine-code"] = config.machineCode;
    if (config.storeName) headers["x-skun-store-name"] = config.storeName;
  }
  if (config.supabaseAnonKey) {
    headers.apikey = config.supabaseAnonKey;
    headers.authorization = `Bearer ${config.supabaseAnonKey}`;
  }

  const method = options.method ?? "POST";
  const body = method === "GET" ? undefined : JSON.stringify(payload ?? {});
  const response = await requestJson(url, { method, headers, body });
  const text = response.text;
  const data = parseJson(text);

  if (response.status < 200 || response.status >= 300) {
    return {
      ok: false,
      status: response.status,
      message: data?.message || text || `HTTP ${response.status}`,
      data,
    };
  }

  return data ?? { ok: true, raw: text };
}

async function requestJson(url, options) {
  try {
    const response = await fetch(url, options);
    return {
      status: response.status,
      text: await response.text(),
    };
  } catch (error) {
    if (!shouldUseCurlFallback(error)) {
      throw error;
    }
    return requestWithCurl(url, options);
  }
}

function shouldUseCurlFallback(error) {
  if (process.platform !== "win32") return false;
  if (process.env.SKUN_KABAM_DISABLE_CURL_FALLBACK === "1") return false;
  const code = error?.cause?.code || error?.code || "";
  return [
    "UNABLE_TO_VERIFY_LEAF_SIGNATURE",
    "SELF_SIGNED_CERT_IN_CHAIN",
    "DEPTH_ZERO_SELF_SIGNED_CERT",
    "CERT_HAS_EXPIRED",
    "UNABLE_TO_GET_ISSUER_CERT_LOCALLY",
  ].includes(code);
}

function requestWithCurl(url, options) {
  return new Promise((resolve, reject) => {
    const args = ["--ssl-no-revoke", "-sS", "-w", "\n__SKUN_STATUS__:%{http_code}", "-X", options.method || "POST", url];
    for (const [key, value] of Object.entries(options.headers ?? {})) {
      args.push("-H", `${key}: ${value}`);
    }
    if (options.body) {
      args.push("--data-binary", "@-");
    }

    const child = spawn("curl.exe", args, {
      stdio: ["pipe", "pipe", "pipe"],
      windowsHide: true,
    });
    let stdout = "";
    let stderr = "";
    child.stdout.setEncoding("utf8");
    child.stderr.setEncoding("utf8");
    child.stdout.on("data", (chunk) => {
      stdout += chunk;
    });
    child.stderr.on("data", (chunk) => {
      stderr += chunk;
    });
    child.on("error", reject);
    child.on("close", (code) => {
      if (code !== 0) {
        reject(new Error(stderr || `curl.exe saiu com codigo ${code}`));
        return;
      }

      const marker = "\n__SKUN_STATUS__:";
      const index = stdout.lastIndexOf(marker);
      if (index < 0) {
        resolve({ status: 200, text: stdout });
        return;
      }

      const text = stdout.slice(0, index);
      const status = Number(stdout.slice(index + marker.length).trim());
      resolve({ status: Number.isFinite(status) ? status : 0, text });
    });

    if (options.body) {
      child.stdin.end(options.body);
    } else {
      child.stdin.end();
    }
  });
}

function loadConfig() {
  const files = configFiles();
  const fileConfig = files.map(readJsonFile).find(Boolean) ?? {};
  return {
    supabaseUrl: firstNonEmpty(
      process.env.SKUN_KABAM_SUPABASE_URL,
      process.env.SUPABASE_URL,
      fileConfig.supabaseUrl,
      fileConfig.supabase_url,
    ),
    supabaseAnonKey: firstNonEmpty(
      process.env.SKUN_KABAM_SUPABASE_ANON_KEY,
      process.env.SUPABASE_ANON_KEY,
      process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY,
      fileConfig.supabaseAnonKey,
      fileConfig.supabase_anon_key,
      fileConfig.anonKey,
    ),
    functionUrl: firstNonEmpty(
      process.env.SKUN_KABAM_CODEX_FUNCTION_URL,
      fileConfig.functionUrl,
      fileConfig.function_url,
    ),
    deviceId: normalizeDeviceId(firstNonEmpty(
      process.env.SKUN_KABAM_DEVICE_ID,
      fileConfig.deviceId,
      fileConfig.device_id,
      process.env.SKUN_KABAM_MACHINE_HASH,
      fileConfig.machineHash,
      fileConfig.machine_hash,
      `${os.userInfo().username}-${os.hostname()}`,
    )),
    deviceSecret: firstNonEmpty(
      process.env.SKUN_KABAM_DEVICE_SECRET,
      fileConfig.deviceSecret,
      fileConfig.device_secret,
    ),
    licenseKey: normalizeLicense(firstNonEmpty(
      process.env.SKUN_KABAM_LICENSE_KEY,
      process.env.BALCAO_LIVRE_LICENSE_KEY,
      fileConfig.licenseKey,
      fileConfig.license_key,
    )),
    machineHash: firstNonEmpty(
      process.env.SKUN_KABAM_MACHINE_HASH,
      process.env.BALCAO_LIVRE_MACHINE_HASH,
      fileConfig.machineHash,
      fileConfig.machine_hash,
    ),
    machineCode: firstNonEmpty(
      process.env.SKUN_KABAM_MACHINE_CODE,
      process.env.BALCAO_LIVRE_MACHINE_CODE,
      fileConfig.machineCode,
      fileConfig.machine_code,
    ),
    storeName: firstNonEmpty(
      process.env.SKUN_KABAM_STORE_NAME,
      fileConfig.storeName,
      fileConfig.store_name,
    ),
    configFile: files.find((file) => fs.existsSync(file)) ?? "",
  };
}

function configFiles() {
  const explicit = process.env.SKUN_KABAM_MCP_CONFIG ? [process.env.SKUN_KABAM_MCP_CONFIG] : [];
  const appData = process.env.APPDATA ? [path.join(process.env.APPDATA, "SkunKabam", "codex-link.json")] : [];
  const localAppData = process.env.LOCALAPPDATA ? [path.join(process.env.LOCALAPPDATA, "SkunKabam", "codex-link.json")] : [];
  return [
    ...explicit,
    ...appData,
    ...localAppData,
    path.join(packageDir, "codex-link.json"),
    path.join(process.cwd(), "skunkabam.codex-link.json"),
  ];
}

function readJsonFile(file) {
  if (!file || !fs.existsSync(file)) return null;
  try {
    return JSON.parse(fs.readFileSync(file, "utf8").replace(/^\uFEFF/, ""));
  } catch (error) {
    console.error(`SkunKabam MCP ignorou config invalida em ${file}: ${errorMessage(error)}`);
    return null;
  }
}

function resolveFunctionUrl(config) {
  if (config.functionUrl) return config.functionUrl;
  const supabaseUrl = required(config.supabaseUrl, "SKUN_KABAM_SUPABASE_URL ou codex-link.json/supabaseUrl");
  return `${trimSlash(supabaseUrl)}/functions/v1/skunkabam-codex`;
}

function configSummary(config) {
  return {
    supabaseUrl: config.supabaseUrl || "",
    functionUrl: config.functionUrl || (config.supabaseUrl ? `${trimSlash(config.supabaseUrl)}/functions/v1/skunkabam-codex` : ""),
    hasAnonKey: Boolean(config.supabaseAnonKey),
    deviceId: config.deviceId || "",
    hasDeviceSecret: Boolean(config.deviceSecret),
    licenseKey: mask(config.licenseKey),
    machineHash: mask(config.machineHash),
    machineCode: config.machineCode || "",
    storeName: config.storeName || "",
    configFile: config.configFile || "",
    defaultThreadId,
  };
}

function toolJson(value, isError = false) {
  return {
    isError,
    content: [
      {
        type: "text",
        text: JSON.stringify(value, null, 2),
      },
    ],
  };
}

function isFailure(value) {
  return Boolean(value && typeof value === "object" && value.ok === false);
}

function parseJson(text) {
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function firstNonEmpty(...values) {
  for (const value of values) {
    const text = String(value ?? "").trim();
    if (text) return text;
  }
  return "";
}

function normalizeLicense(value) {
  return firstNonEmpty(value).toUpperCase().replaceAll(" ", "").replaceAll("_", "-");
}

function normalizeDeviceId(value) {
  return firstNonEmpty(value)
    .toLowerCase()
    .replace(/[^a-z0-9._-]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120);
}

function trimSlash(value) {
  return String(value || "").replace(/\/+$/g, "");
}

function required(value, label) {
  const text = firstNonEmpty(value);
  if (!text) {
    throw new Error(`Config obrigatoria ausente: ${label}.`);
  }
  return text;
}

function mask(value) {
  const text = firstNonEmpty(value);
  if (!text) return "";
  if (text.length <= 8) return "****";
  return `${text.slice(0, 4)}...${text.slice(-4)}`;
}

function errorMessage(error) {
  return error instanceof Error ? error.message : String(error);
}

main().catch((error) => {
  console.error(errorMessage(error));
  process.exit(1);
});
