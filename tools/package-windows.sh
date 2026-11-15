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
# 표준 deflate zip을 만든다(윈도우 탐색기의 기본 압축 풀기로도 열린다). Git Bash의 GNU tar는 zip을 못 만들고
# 확장자만 .zip인 tar를 만들어서(v1.0~v1.6이 그랬다) 쓰지 않는다. 자세한 내용은 tools/make_zip.py 맨 위.
python tools/make_zip.py "$BUILD_DIR" "$ZIP"

