// 사이트에서 바꿔 쓰는 값은 전부 여기에 모아 둔다. 다른 파일은 건드리지 않아도 된다.
window.SITE_CONFIG = {
  title: "와사비를 먹으면 강해지는 군요",
  tagline: "스시왕국의 용사들과 함께 5분을 버텨라 — 몰려오는 초밥 군단을 쓸어버리는 서바이벌 로그라이크",

  version: "1.0",
  fileSize: "약 477MB",
  releaseDate: "2026-10-10",

  // GitHub Release에 올린 zip의 다운로드 주소. 비워 두면 버튼이 "곧 공개됩니다"로 잠긴다.
  // 예: "https://github.com/<계정>/<저장소>/releases/download/v1.0/WasabiSurvival_v1.0_win64.zip"
  downloadUrl: "https://github.com/luseuss/sushi-survival/releases/download/v1.0/WasabiSurvival_v1.0_win64.zip",
  // 전체 릴리스 목록 페이지(선택). 비워 두면 링크를 숨긴다.
  releasesUrl: "https://github.com/luseuss/sushi-survival/releases",

  // 게임 화면 스크린샷. site/assets/screenshots/ 에 이미지를 넣고 아래처럼 적으면 갤러리가 나타난다.
  // 비워 두면 스크린샷 섹션이 통째로 숨겨진다.
  // 예: { src: "assets/screenshots/01.png", caption: "보스전" }
  screenshots: [],

  // 크레딧. 비워 두면 섹션이 숨겨진다.
  // 예: { role: "기획", name: "이름" }
  credits: [],

  // 문의 링크. label과 url이 모두 있어야 나타난다. 예: { label: "이메일", url: "mailto:..." }
  contact: { label: "", url: "" }
};
