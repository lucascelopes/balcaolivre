import { cp, mkdir, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const appRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const repoRoot = path.resolve(appRoot, "..");
const sourceDir = path.join(repoRoot, "BalcaoLivre.PDV.Web");
const targetDir = path.join(appRoot, "public", "pdv");

await rm(targetDir, { recursive: true, force: true });
await mkdir(targetDir, { recursive: true });
await cp(sourceDir, targetDir, {
  recursive: true,
  force: true,
  filter(source) {
    const relative = path.relative(sourceDir, source);
    return !relative.split(path.sep).includes("node_modules");
  }
});

console.log(`PDV web synced to ${path.relative(appRoot, targetDir)}`);
