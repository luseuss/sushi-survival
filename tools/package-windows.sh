#!/usr/bin/env bash
# unity/Build/Windows/ 를 배포용 zip으로 묶는다. 먼저 Unity 배치 빌드(BuildTools.BuildWindowsCli)를 돌려 둘 것.
#   사용법: tools/package-windows.sh [버전]      (버전을 안 주면 ProjectSettings의 bundleVersion을 쓴다)
#   결과:   unity/Build/WasabiSurvival_v<버전>_win64.zip
set -euo pipefail

cd "$(dirname "$0")/.."

BUILD_DIR="unity/Build/Windows"
if [ ! -f "$BUILD_DIR/WasabiSurvival.exe" ]; then
  echo "빌드 결과가 없습니다: $BUILD_DIR/WasabiSurvival.exe — 먼저 BuildTools.BuildWindowsCli로 빌드하세요." >&2
  exit 1
fi

VERSION="${1:-$(grep -m1 '^  bundleVersion:' unity/ProjectSettings/ProjectSettings.asset | sed 's/.*: *//' | tr -d '\r')}"
ZIP="unity/Build/WasabiSurvival_v${VERSION}_win64.zip"

rm -f "$ZIP"
# Windows 10 이상에 들어 있는 bsdtar가 표준 zip을 만든다(경로 구분자가 /라서 어디서 풀어도 안전).
tar -a -c -f "$ZIP" -C "$BUILD_DIR" .

SIZE_MB=$(du -m "$ZIP" | cut -f1)
echo "만들었습니다: $ZIP (${SIZE_MB}MB)"
