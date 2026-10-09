# 배포 사이트 (`site/`)

게임을 소개하고 Windows용 zip을 내려받게 하는 한 페이지짜리 정적 사이트다. 프레임워크·빌드 도구 없이 파일만 올리면 된다.

```
site/
├─ index.html     페이지 본문
├─ style.css      스타일
├─ app.js         config.js의 값으로 페이지를 채운다
├─ config.js      ★ 바꿔 쓰는 값은 전부 여기 (다운로드 주소, 버전, 스크린샷, 크레딧, 문의)
├─ .nojekyll      GitHub Pages가 파일을 건드리지 않게 하는 표식
└─ assets/
   ├─ characters/ 캐릭터 일러스트(게임의 캐릭터 선택 화면 그림)
   └─ screenshots/ (직접 만들어서 게임 화면을 넣는다)
```

## 1. 로컬에서 보기

`index.html`을 더블클릭해서 열어도 되지만, 이미지·스크립트 경로를 정확히 보려면 간단한 서버로 연다.

```bash
cd site
npx --yes serve .        # 또는: python -m http.server 8000
```

## 2. zip 만들기

(게임 저장소에서, Unity 에디터를 닫은 상태로)

```bash
cd unity && "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod BuildTools.BuildWindowsCli -logFile build_log.txt
cd .. && bash tools/package-windows.sh
```

결과: `unity/Build/WasabiSurvival_v<버전>_win64.zip` (약 477MB). 이 파일은 저장소에 올라가지 않는다(`.gitignore`).

## 3. 공개 현황과 갱신 (이 저장소는 공개 저장소다)

- **사이트:** https://luseuss.github.io/sushi-survival/ — `gh-pages` 브랜치(루트)에서 GitHub Pages로 서비스한다.
- **다운로드:** 이 저장소의 **Releases** `v1.0`에 zip이 올라가 있다. `config.js`의 `downloadUrl`이 그 주소를 가리킨다.

### 사이트를 고쳤을 때 (문구·스크린샷·크레딧·문의)
1. `site/`를 고치고 PR로 `main`에 머지한다.
2. 아래 명령으로 `gh-pages`에 배포한다(몇 분 뒤 반영, 공개되는 작업이라 직접 실행한다).

```bash
bash tools/deploy-site.sh
```

`gh-pages`는 배포 전용 브랜치다. 항상 `site/` 내용 한 커밋으로 덮어쓰며(강제 푸시), `README.md`는 공개 파일에서 뺀다.

### 새 버전을 낼 때
1. 위 "2. zip 만들기"로 zip을 새로 만든다.
2. 새 Release를 만든다.
   ```bash
   gh release create v1.1 unity/Build/WasabiSurvival_v1.1_win64.zip --title "와사비를 먹으면 강해지는 군요 v1.1 (Windows)" --notes "변경 내용"
   ```
3. `config.js`의 `version`·`fileSize`·`releaseDate`·`downloadUrl`을 고쳐 PR로 머지하고, `bash tools/deploy-site.sh`로 배포한다.

## 4. 올리기 전에 채울 것

- `config.js`의 **`screenshots`**: 실제 게임 화면을 `assets/screenshots/`에 넣고 적으면 갤러리가 나타난다(비어 있으면 섹션이 숨겨진다).
- `config.js`의 **`credits`**, **`contact`**: 비워 두면 해당 섹션이 숨겨진다. 연락처에 개인 이메일을 넣을지는 직접 정한다.
- 본문 문구는 `index.html`에서 직접 고친다(게임 소개·특징은 기획 내용을 간추린 것이다).
- 캐릭터 일러스트는 게임 안의 그림을 그대로 쓴 것이다. 공개해도 되는 그림인지 확인한다.
