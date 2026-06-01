import * as Crypto from "expo-crypto";

export function newId(prefix: string) {
  return `${prefix}_${Crypto.randomUUID().replace(/-/g, "")}`;
}
