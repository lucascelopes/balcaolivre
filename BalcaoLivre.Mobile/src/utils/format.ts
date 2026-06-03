export function money(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL"
  }).format(Number.isFinite(value) ? value : 0);
}

export function nowIso() {
  return new Date().toISOString();
}

export function normalizeKey(value: string) {
  return value.trim().toUpperCase().replace(/\s+/g, "").replace(/_/g, "-");
}

export function onlyNumbers(value: string) {
  return value.replace(/\D/g, "");
}

export function padOrderNumber(value: string) {
  const clean = onlyNumbers(value);
  return clean ? clean.padStart(6, "0").slice(-6) : "";
}
