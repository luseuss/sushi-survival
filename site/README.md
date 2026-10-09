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

## 3. 공개하기 (GitHub Pages + Release)

이 게임 저장소는 **비공개**라서 거기에서는 Pages도, 누구나 받을 수 있는 Release도 쓸 수 없다. **공개 저장소를 새로 하나** 만든다(예: `wasabi-survival`).

1. GitHub에서 새 저장소를 **Public**으로 만든다.
2. `site/` 폴더의 **내용물**(폴더 자체가 아니라 안의 파일)을 그 저장소 루트에 올린다(`.nojekyll` 포함).
3. 저장소 **Settings → Pages → Source: Deploy from a branch → `main` / `/ (root)`** 로 켠다. 몇 분 뒤 `https://<계정>.github.io/<저장소>/`로 열린다.
4. 같은 저장소의 **Releases → Draft a new release**에서 태그 `v1.0`을 만들고 zip을 끌어다 놓아 올린다(파일당 2GB까지 가능).
5. 올라간 zip의 주소(`.../releases/download/v1.0/WasabiSurvival_v1.0_win64.zip`)를 `config.js`의 `downloadUrl`에 넣고 다시 커밋한다. 버튼이 **"곧 공개됩니다" → "Windows용 다운로드"** 로 바뀐다.

새 버전을 낼 때는 zip을 새 Release로 올리고 `config.js`의 `version`·`fileSize`·`releaseDate`·`downloadUrl`만 고치면 된다.

## 4. 올리기 전에 채울 것

- `config.js`의 **`screenshots`**: 실제 게임 화면을 `assets/screenshots/`에 넣고 적으면 갤러리가 나타난다(비어 있으면 섹션이 숨겨진다).
- `config.js`의 **`credits`**, **`contact`**: 비워 두면 해당 섹션이 숨겨진다. 연락처에 개인 이메일을 넣을지는 직접 정한다.
- 본문 문구는 `index.html`에서 직접 고친다(게임 소개·특징은 기획 내용을 간추린 것이다).
- 캐릭터 일러스트는 게임 안의 그림을 그대로 쓴 것이다. 공개해도 되는 그림인지 확인한다.
