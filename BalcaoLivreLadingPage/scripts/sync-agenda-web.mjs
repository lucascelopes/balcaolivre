import { cp, mkdir, readFile, rm, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const projectDirectory = path.resolve(scriptDirectory, "..");
const repositoryDirectory = path.resolve(projectDirectory, "..");
const flutterDirectory = path.join(repositoryDirectory, "AgendaLivre.Flutter");
const flutterBuild = path.join(flutterDirectory, "build", "web");
const targetDirectory = path.join(
  projectDirectory,
  "public",
  "agenda-livre",
  "app",
);
const targetParent = path.join(projectDirectory, "public", "agenda-livre");

if (path.dirname(targetDirectory) !== targetParent) {
  throw new Error(`Destino do Flutter Web invalido: ${targetDirectory}`);
}

const bundlePath = path.join(flutterBuild, "main.dart.js");
const indexPath = path.join(flutterBuild, "index.html");
const serviceWorkerPath = path.join(
  flutterBuild,
  "flutter_service_worker.js",
);

let bundleStat;
let bundle;
let index;
let serviceWorker;

try {
  [bundleStat, bundle, index, serviceWorker] = await Promise.all([
    stat(bundlePath),
    readFile(bundlePath, "utf8"),
    readFile(indexPath, "utf8"),
    readFile(serviceWorkerPath, "utf8"),
  ]);
} catch (error) {
  if (error?.code !== "ENOENT") throw error;

  const vendoredBundlePath = path.join(targetDirectory, "main.dart.js");
  const vendoredIndexPath = path.join(targetDirectory, "index.html");
  const vendoredServiceWorkerPath = path.join(
    targetDirectory,
    "flutter_service_worker.js",
  );

  const [vendoredBundle, vendoredIndex, vendoredServiceWorker] =
    await Promise.all([
      readFile(vendoredBundlePath, "utf8"),
      readFile(vendoredIndexPath, "utf8"),
      readFile(vendoredServiceWorkerPath, "utf8"),
    ]);

  for (const marker of [
    "Lucas Barbearia",
    "Lucas Cesar Lopes",
    "agenda_livre.data.v1",
  ]) {
    if (vendoredBundle.includes(marker)) {
      throw new Error(`Bundle Flutter precompilado rejeitado: marcador legado (${marker}).`);
    }
  }

  for (const marker of [
    "agenda_livre.auth.session.v2",
    "agenda_livre.auth.session.v1",
    "agenda_livre.data.v2.",
    "session_identity_mismatch",
  ]) {
    if (!vendoredBundle.includes(marker)) {
      throw new Error(`Bundle Flutter precompilado rejeitado: marcador ausente (${marker}).`);
    }
  }

  if (!vendoredIndex.includes('<base href="/">')) {
    throw new Error("Base do Flutter Web precompilado invalida.");
  }

  if (!vendoredServiceWorker.includes("FLUTTER_CACHE_NAMES")) {
    throw new Error("Worker precompilado de limpeza do cache Flutter nao encontrado.");
  }

  console.log(
    "Flutter Web precompilado validado; fonte Flutter externa nao disponivel neste ambiente.",
  );
  process.exit(0);
}

const sourcePaths = [
  path.join(flutterDirectory, "lib"),
  path.join(flutterDirectory, "web"),
  path.join(flutterDirectory, "pubspec.yaml"),
  path.join(flutterDirectory, "pubspec.lock"),
];

async function latestModifiedAt(target) {
  const metadata = await stat(target);
  if (!metadata.isDirectory()) return metadata.mtimeMs;
  const { readdir } = await import("node:fs/promises");
  const entries = await readdir(target, { withFileTypes: true });
  const children = await Promise.all(
    entries.map((entry) => latestModifiedAt(path.join(target, entry.name))),
  );
  return Math.max(metadata.mtimeMs, ...children);
}

const latestSourceMtime = Math.max(
  ...(await Promise.all(sourcePaths.map(latestModifiedAt))),
);
if (bundleStat.mtimeMs < latestSourceMtime) {
  throw new Error(
    "O Flutter Web esta desatualizado. Execute AgendaLivre.Flutter/tool/build_sites_dist.ps1 antes do deploy.",
  );
}

for (const marker of [
  "Lucas Barbearia",
  "Lucas Cesar Lopes",
  "agenda_livre.data.v1",
]) {
  if (bundle.includes(marker)) {
    throw new Error(`Bundle Flutter rejeitado: marcador legado (${marker}).`);
  }
}

for (const marker of [
  "agenda_livre.auth.session.v2",
  "agenda_livre.auth.session.v1",
  "agenda_livre.data.v2.",
  "session_identity_mismatch",
]) {
  if (!bundle.includes(marker)) {
    throw new Error(`Bundle Flutter rejeitado: marcador ausente (${marker}).`);
  }
}

if (!serviceWorker.includes("FLUTTER_CACHE_NAMES")) {
  throw new Error("O worker de limpeza do cache Flutter nao foi aplicado.");
}

await rm(targetDirectory, { recursive: true, force: true });
await mkdir(targetDirectory, { recursive: true });
await cp(flutterBuild, targetDirectory, { recursive: true, force: true });

const targetIndexPath = path.join(targetDirectory, "index.html");
await writeFile(
  targetIndexPath,
  index.replace(/<base href="[^"]*">/, '<base href="/">'),
  "utf8",
);

console.log("Flutter Web autenticado sincronizado em public/agenda-livre/app.");
