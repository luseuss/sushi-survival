#!/usr/bin/env bash
# site/ 를 GitHub Pages(gh-pages 브랜치)로 배포한다. 공개되는 작업이므로 직접 실행할 때만 돌아간다.
#   사용법: tools/deploy-site.sh
#   결과:   https://<계정>.github.io/<저장소>/ 가 몇 분 안에 갱신된다.
#
# gh-pages는 배포 전용 브랜치다(항상 site/ 내용 한 커밋으로 덮어쓴다). main과 PR 흐름과는 별개이고,
# 작업 중인 저장소는 건드리지 않도록 임시 폴더에서 새 git 저장소를 만들어 푸시한다.
set -euo pipefail

cd "$(dirname "$0")/.."

REMOTE_URL="$(git remote get-url origin)"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

# README.md는 개발용 안내라 공개 파일에서 뺀다.
mkdir -p "$TMP/out"
cp -r site/. "$TMP/out/"
rm -f "$TMP/out/README.md"

cd "$TMP/out"
git init -q -b gh-pages
git add -A
git commit -q -m "deploy: 소개 사이트 $(date +%Y-%m-%d)"
git remote add origin "$REMOTE_URL"
git push -q --force origin gh-pages

echo "배포했습니다. 몇 분 뒤 사이트가 갱신됩니다."
