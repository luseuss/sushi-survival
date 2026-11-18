// config.js의 값으로 페이지의 가변 부분을 채운다. 프레임워크 없이 한 파일.
(function () {
  "use strict";

  var cfg = window.SITE_CONFIG || {};

  function byId(id) { return document.getElementById(id); }
  function show(el, visible) { if (el) el.hidden = !visible; }

  // 제목·소개
  if (cfg.title) {
    document.title = cfg.title;
    byId("title-logo").alt = cfg.title; // 제목은 로고 그림이라 글자 대신 대체 글로 넣는다
    byId("footer-title").textContent = cfg.title;
  }
  if (cfg.tagline) byId("tagline").textContent = cfg.tagline;
  if (cfg.version) byId("footer-version").textContent = cfg.version;
  if (cfg.fileSize) byId("spec-size").textContent = cfg.fileSize;

  // 다운로드 버튼(첫 화면 버튼과 상단 바 버튼)
  var button = byId("download-button");
  var navButton = byId("nav-download");
  var meta = byId("download-meta");
  if (cfg.downloadUrl) {
    [button, navButton].forEach(function (b) {
      b.href = cfg.downloadUrl;
      b.setAttribute("aria-disabled", "false");
    });
    button.removeAttribute("aria-describedby");
    button.textContent = "Windows용 다운로드";
    var parts = [];
    if (cfg.version) parts.push("v" + cfg.version);
    if (cfg.fileSize) parts.push(cfg.fileSize);
    if (cfg.releaseDate) parts.push(cfg.releaseDate);
    meta.textContent = parts.join(" · ");
  } else {
    [button, navButton].forEach(function (b) {
      b.removeAttribute("href");
      b.setAttribute("aria-disabled", "true");
    });
    button.textContent = "곧 공개됩니다";
    meta.textContent = "";
  }

  var releases = byId("releases-link");
  if (cfg.releasesUrl) {
    releases.href = cfg.releasesUrl;
    show(releases, true);
  }

  // 스크린샷
  var shots = Array.isArray(cfg.screenshots) ? cfg.screenshots.filter(function (s) { return s && s.src; }) : [];
  if (shots.length > 0) {
    var gallery = byId("gallery");
    shots.forEach(function (shot) {
      var figure = document.createElement("figure");
      var img = document.createElement("img");
      img.src = shot.src;
      img.alt = shot.caption || "게임 화면";
      img.loading = "lazy";
      figure.appendChild(img);
      if (shot.caption) {
        var caption = document.createElement("figcaption");
        caption.textContent = shot.caption;
        figure.appendChild(caption);
      }
      gallery.appendChild(figure);
    });
    show(byId("screenshots"), true);
  }

  // 크레딧
  var credits = Array.isArray(cfg.credits) ? cfg.credits.filter(function (c) { return c && c.name; }) : [];
  if (credits.length > 0) {
    var list = byId("credit-list");
    credits.forEach(function (credit) {
      var dt = document.createElement("dt");
      dt.textContent = credit.role || "";
      var dd = document.createElement("dd");
      dd.textContent = credit.name;
      list.appendChild(dt);
      list.appendChild(dd);
    });
    show(byId("credits"), true);
  }

  // 문의
  var contact = cfg.contact || {};
  if (contact.label && contact.url) {
    var link = byId("contact-link");
    link.href = contact.url;
    link.textContent = "문의: " + contact.label;
    show(byId("contact"), true);
  }

  // ---------- 움직임 ----------
  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // 상단 바: 위에서는 투명, 내려가면 어두운 바. 첫 화면 배경은 천천히 따라 올라가는 시차 효과.
  var topbar = byId("topbar");
  var heroBg = byId("hero-bg");
  var ticking = false;
  function onScroll() {
    var y = window.pageYOffset || 0;
    topbar.classList.toggle("solid", y > 24);
    if (!reduceMotion && heroBg && y < window.innerHeight * 1.2) heroBg.style.setProperty("--py", y);
    ticking = false;
  }
  window.addEventListener("scroll", function () {
    if (!ticking) { ticking = true; window.requestAnimationFrame(onScroll); }
  }, { passive: true });
  onScroll();

  // 스크롤하면 구역이 옆에서 미끄러져 들어온다. 관찰을 못 쓰는 환경에서는 그냥 모두 보여 준다.
  var reveals = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window && !reduceMotion) {
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("in");
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.18 });
    reveals.forEach(function (el) { observer.observe(el); });
  } else {
    reveals.forEach(function (el) { el.classList.add("in"); });
  }

  // 캐릭터에 마우스를 올리거나 누르면 표정이 바뀐다(기쁜 표정 그림으로).
  document.querySelectorAll(".cast figure[data-happy]").forEach(function (figure) {
    var img = figure.querySelector("img");
    var base = img.getAttribute("src");
    var happy = figure.getAttribute("data-happy");
    new Image().src = happy; // 미리 불러 두어 바뀔 때 깜빡이지 않게
    function toHappy() { img.src = happy; }
    function toBase() { img.src = base; }
    figure.addEventListener("mouseenter", toHappy);
    figure.addEventListener("mouseleave", toBase);
    figure.addEventListener("touchstart", toHappy, { passive: true });
    figure.addEventListener("touchend", function () { setTimeout(toBase, 900); });
  });
})();
