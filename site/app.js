// config.js의 값으로 페이지의 가변 부분을 채운다. 프레임워크 없이 한 파일.
(function () {
  "use strict";

  var cfg = window.SITE_CONFIG || {};

  function byId(id) { return document.getElementById(id); }
  function show(el, visible) { if (el) el.hidden = !visible; }

  // 제목·소개
  if (cfg.title) {
    document.title = cfg.title;
    byId("title").textContent = cfg.title;
    byId("footer-title").textContent = cfg.title;
  }
  if (cfg.tagline) byId("tagline").textContent = cfg.tagline;
  if (cfg.version) byId("footer-version").textContent = cfg.version;
  if (cfg.fileSize) byId("spec-size").textContent = cfg.fileSize;

  // 다운로드 버튼
  var button = byId("download-button");
  var meta = byId("download-meta");
  if (cfg.downloadUrl) {
    button.href = cfg.downloadUrl;
    button.setAttribute("aria-disabled", "false");
    button.removeAttribute("aria-describedby");
    button.textContent = "Windows용 다운로드";
    var parts = [];
    if (cfg.version) parts.push("v" + cfg.version);
    if (cfg.fileSize) parts.push(cfg.fileSize);
    if (cfg.releaseDate) parts.push(cfg.releaseDate);
    meta.textContent = parts.join(" · ");
  } else {
    button.removeAttribute("href");
    button.setAttribute("aria-disabled", "true");
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
})();
