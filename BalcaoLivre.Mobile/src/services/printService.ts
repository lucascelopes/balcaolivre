import { Order, OrderItem, Settings } from "../types";
import { money } from "../utils/format";

export type PrintJobKind = "receipt" | "kitchen" | "delivery" | "cash";

export function buildReceiptText(order: Order, items: OrderItem[], title = "COMPROVANTE NAO FISCAL") {
  const width = 32;
  const line = "-".repeat(width);
  const rows = [
    "BALCAO LIVRE PDV ONLINE",
    title,
    line,
    `${order.kind} ${order.number}`,
    order.customerName ? `CLIENTE: ${order.customerName}` : "",
    line,
    ...items.flatMap((item) => [
      item.name.toUpperCase(),
      `${item.quantity} x ${money(item.unitPrice)} = ${money(item.total)}`
    ]),
    line,
    `TOTAL ${money(order.total)}`,
    line,
    "OBRIGADO PELA PREFERENCIA"
  ];
  return rows.filter(Boolean).join("\n");
}

export async function printViaWindowsBridge(settings: Settings, kind: PrintJobKind, content: string, jobName: string) {
  if (!settings.windowsBridgeUrl.trim()) {
    throw new Error("Informe o IP do Windows bridge em Config.");
  }

  const response = await fetch(`${settings.windowsBridgeUrl.replace(/\/$/, "")}/api/mobile/print`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ kind, content, jobName, compact: true })
  });
  const data = await response.json().catch(() => ({}));
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || "Windows bridge nao confirmou a impressao.");
  }
  return data;
}

export async function printDirectEscPos(settings: Settings, _kind: PrintJobKind, _content: string) {
  if (settings.printMode === "ESC_POS_NETWORK") {
    throw new Error("Impressao TCP/IP direta precisa do modulo nativo react-native-tcp-socket no Dev Build.");
  }
  if (settings.printMode === "ESC_POS_BLUETOOTH") {
    throw new Error("Impressao Bluetooth direta precisa do modulo nativo ESC/POS no Dev Build.");
  }
  throw new Error("Modo de impressao direto nao configurado.");
}

export async function printJob(settings: Settings, kind: PrintJobKind, content: string, jobName: string) {
  if (settings.printMode === "WINDOWS_BRIDGE") {
    return printViaWindowsBridge(settings, kind, content, jobName);
  }
  return printDirectEscPos(settings, kind, content);
}
