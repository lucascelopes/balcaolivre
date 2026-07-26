#!/usr/bin/env python3
"""Prepare per-establishment Android resources from a private build manifest.

The manifest can contain a one-time provisioning token. This script deliberately
never prints the manifest or token and writes generated resources only inside the
Flutter project selected by --project.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import unicodedata
from pathlib import Path

try:
    from PIL import Image, ImageOps
except ImportError as exc:  # pragma: no cover - dependency is installed by CI
    raise SystemExit("Pillow is required: python -m pip install Pillow") from exc


STABLE_APPLICATION_ID = "br.com.balcaolivre.agenda_livre"
MIPMAP_SIZES = {
    "mipmap-mdpi": 48,
    "mipmap-hdpi": 72,
    "mipmap-xhdpi": 96,
    "mipmap-xxhdpi": 144,
    "mipmap-xxxhdpi": 192,
}


def require_string(source: dict, *names: str, maximum: int = 4096) -> str:
    for name in names:
        value = source.get(name)
        if isinstance(value, str) and value.strip():
            clean = value.strip()
            if len(clean) > maximum or any(ord(char) < 32 for char in clean):
                raise ValueError(f"Invalid manifest field: {name}")
            return clean
    raise ValueError(f"Missing manifest field: {'/'.join(names)}")


def optional_string(source: dict, *names: str, maximum: int = 4096) -> str:
    for name in names:
        value = source.get(name)
        if isinstance(value, str) and value.strip():
            clean = value.strip()
            if len(clean) > maximum or any(ord(char) < 32 for char in clean):
                raise ValueError(f"Invalid manifest field: {name}")
            return clean
    return ""


def positive_int(source: dict, *names: str) -> int:
    for name in names:
        value = source.get(name)
        if isinstance(value, bool):
            continue
        try:
            parsed = int(value)
        except (TypeError, ValueError):
            continue
        if 1 <= parsed <= 2_100_000_000:
            return parsed
    raise ValueError(f"Missing or invalid manifest field: {'/'.join(names)}")


def slug(value: str) -> str:
    normalized = unicodedata.normalize("NFKD", value).encode("ascii", "ignore").decode()
    clean = re.sub(r"[^A-Za-z0-9]+", "-", normalized).strip("-")[:64]
    return clean or "estabelecimento"


def load_build_manifest(path: Path) -> dict:
    raw = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(raw, dict):
        raise ValueError("Build manifest must be a JSON object")
    source = raw.get("build", raw)
    if not isinstance(source, dict):
        raise ValueError("Build manifest build field must be an object")
    provisioning = source.get("provisioning")
    if isinstance(provisioning, dict):
        source = {
            **source,
            "provisioning_token": provisioning.get("token"),
            "provisioning_expires_at": provisioning.get("expiresAt"),
        }
    return source


def ensure_inside(project: Path, target: Path) -> None:
    project = project.resolve()
    target = target.resolve()
    try:
        target.relative_to(project)
    except ValueError as exc:
        raise ValueError(f"Generated target escapes Flutter project: {target}") from exc


def normalized_image(source_path: Path, output_path: Path, size: tuple[int, int]) -> None:
    with Image.open(source_path) as source:
        source = ImageOps.exif_transpose(source).convert("RGBA")
        fitted = ImageOps.fit(source, size, method=Image.Resampling.LANCZOS, centering=(0.5, 0.5))
        output_path.parent.mkdir(parents=True, exist_ok=True)
        fitted.save(output_path, format="PNG", optimize=True)


def ensure_app_label(project: Path) -> None:
    manifest_path = project / "android" / "app" / "src" / "main" / "AndroidManifest.xml"
    manifest = manifest_path.read_text(encoding="utf-8")
    replaced, count = re.subn(
        r'(<application\b[\s\S]*?\bandroid:label\s*=\s*)"[^"]*"',
        r'\1"@string/agenda_app_name"',
        manifest,
        count=1,
    )
    if count != 1:
        raise ValueError("AndroidManifest.xml application label was not found")
    manifest_path.write_text(replaced, encoding="utf-8")


def verify_application_id(project: Path) -> None:
    gradle_path = project / "android" / "app" / "build.gradle.kts"
    gradle = gradle_path.read_text(encoding="utf-8")
    match = re.search(r'applicationId\s*=\s*"([^"]+)"', gradle)
    if not match or match.group(1) != STABLE_APPLICATION_ID:
        raise ValueError(
            f"Android applicationId must remain {STABLE_APPLICATION_ID} for compatible updates"
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--icon", type=Path, required=True)
    parser.add_argument("--cover", type=Path, required=True)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--api-base-url", required=True)
    parser.add_argument("--metadata-output", type=Path, required=True)
    parser.add_argument("--dart-defines-output", type=Path, required=True)
    args = parser.parse_args()

    project = args.project.resolve()
    for required in (args.manifest, args.icon, args.cover):
        if not required.is_file():
            raise ValueError(f"Required input is missing: {required}")
    if not (project / "pubspec.yaml").is_file():
        raise ValueError("--project is not a Flutter project")

    source = load_build_manifest(args.manifest)
    build_id = require_string(source, "id", "buildId", maximum=128)
    if not re.fullmatch(r"[A-Za-z0-9_-]{8,128}", build_id):
        raise ValueError("Invalid build id")
    business_name = require_string(source, "appName", "businessName", maximum=80)
    provisioning_token = require_string(
        source, "provisioning_token", "provisioningToken", "claimToken", maximum=2048
    )
    api_base_url = str(args.api_base_url).strip().rstrip("/")
    if not api_base_url.startswith("https://"):
        raise ValueError("apiBaseUrl must use HTTPS")
    version_code = positive_int(source, "versionCode")
    version_name = optional_string(source, "versionName", maximum=64) or "1.0.0"
    if not re.fullmatch(r"[0-9]+(?:\.[0-9]+){1,3}(?:[-+][A-Za-z0-9.-]+)?", version_name):
        raise ValueError("Invalid versionName")

    verify_application_id(project)

    branding_dir = project / "assets" / "branding"
    tenant_icon = branding_dir / "android_tenant_icon.png"
    tenant_cover = branding_dir / "android_tenant_cover.png"
    for target in (tenant_icon, tenant_cover):
        ensure_inside(project, target)
    normalized_image(args.icon, tenant_icon, (512, 512))
    normalized_image(args.cover, tenant_cover, (1440, 900))

    res_dir = project / "android" / "app" / "src" / "main" / "res"
    for density, size in MIPMAP_SIZES.items():
        target = res_dir / density / "ic_launcher.png"
        ensure_inside(project, target)
        normalized_image(args.icon, target, (size, size))

    ensure_app_label(project)

    tenant_config = {
        "schemaVersion": 1,
        "buildId": build_id,
        "businessName": business_name,
        "apiBaseUrl": api_base_url,
        "provisioningToken": provisioning_token,
        "branding": {
            "iconAsset": "assets/branding/android_tenant_icon.png",
            "coverAsset": "assets/branding/android_tenant_cover.png",
        },
    }
    config_path = branding_dir / "android_tenant.json"
    ensure_inside(project, config_path)
    config_path.write_text(
        json.dumps(tenant_config, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )

    file_name = f"Agenda-Livre-{slug(business_name)}.apk"
    metadata = {
        "buildId": build_id,
        "businessName": business_name,
        "versionCode": version_code,
        "versionName": version_name,
        "fileName": file_name,
    }
    args.metadata_output.parent.mkdir(parents=True, exist_ok=True)
    args.metadata_output.write_text(
        json.dumps(metadata, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )
    dart_defines = {
        "AGENDA_LIVRE_API_BASE": api_base_url,
        "AGENDA_ANDROID_BUILD_ID": build_id,
        "AGENDA_ANDROID_PROVISIONING_TOKEN": provisioning_token,
        "AGENDA_ANDROID_APP_VERSION": version_name,
        "AGENDA_ANDROID_BUSINESS_NAME": business_name,
        "AGENDA_ANDROID_LOGO_ASSET": "assets/branding/android_tenant_icon.png",
        "AGENDA_ANDROID_COVER_ASSET": "assets/branding/android_tenant_cover.png",
        "AGENDA_ANDROID_PAYMENT_URL": "",
        "AGENDA_ANDROID_SUPPORT_URL": "",
        "AGENDA_ANDROID_DEV_MODE": False,
    }
    args.dart_defines_output.parent.mkdir(parents=True, exist_ok=True)
    args.dart_defines_output.write_text(
        json.dumps(dart_defines, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )
    print("Android tenant resources prepared.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"Preparation failed: {error}", file=sys.stderr)
        raise SystemExit(1)
