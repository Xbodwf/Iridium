#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

echo "[build] dotnet build"
dotnet build

VERSION_TAG="$(python3 - <<'PY'
import json
import re
from pathlib import Path

root = Path('.')
info = json.loads((root / 'Info.json').read_text(encoding='utf-8'))
base_version = info.get('Version', '1.0.0')

vm = (root / 'VersionManager.cs').read_text(encoding='utf-8')

type_match = re.search(r'public\s+static\s+VersionType\s+Type\s*=>\s*VersionType\.(\w+)\s*;', vm)
minor_match = re.search(r'public\s+const\s+int\s+MinorVersion\s*=\s*(\d+)\s*;', vm)

vtype = type_match.group(1).lower() if type_match else 'release'
minor = minor_match.group(1) if minor_match else '0'

bi = (root / 'BuildInfo.cs').read_text(encoding='utf-8')
adofai_match = re.search(r'AdofaiVersion\s*=\s*"([^"]+)"', bi)
adofai_ver = adofai_match.group(1) if adofai_match else 'unknown'

if vtype == 'release':
    print(f"{base_version}+adofai{adofai_ver}")
else:
    print(f"{base_version}_{vtype}{minor}+adofai{adofai_ver}")
PY
)"

ZIP_NAME="Iridium_${VERSION_TAG}.zip"

echo "[build] package -> ${ZIP_NAME}"
cd out
rm -f "Iridium_*.zip"
zip -r "$ZIP_NAME" ./
cd ..
echo "[done] ${ZIP_NAME}"
