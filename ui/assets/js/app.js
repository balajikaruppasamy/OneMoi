/* =====================================================================
   OneMoi UI kit  ·  app.js
   Builds the shared shell (sidebar / topbar / bottom nav) from
   <body data-shell="vendor|individual|operator|admin" data-page="...">,
   plus icons, logo, theme toggle, OTP inputs, toasts and modals.
   Plain JS — no build step, works from file:// and inside WebViews.
   ===================================================================== */
(function () {
  "use strict";

  /* ---------- Icons (24px, stroke) ---------- */
  const P = {
    home: '<path d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z"/>',
    calendar: '<rect x="3" y="4" width="18" height="18" rx="2"/><path d="M16 2v4M8 2v4M3 10h18"/>',
    users: '<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8"/>',
    user: '<circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/>',
    chart: '<path d="M3 3v18h18"/><path d="m7 15 4-4 3 3 5-6"/>',
    settings: '<path d="M4 6h9M17 6h3M4 12h3M11 12h9M4 18h11M19 18h1"/><circle cx="15" cy="6" r="2"/><circle cx="9" cy="12" r="2"/><circle cx="17" cy="18" r="2"/>',
    qr: '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><path d="M14 14h3v3h-3zM20 14v.01M14 20h.01M17 20h4M20 17v4"/>',
    scan: '<path d="M3 7V5a2 2 0 0 1 2-2h2M17 3h2a2 2 0 0 1 2 2v2M21 17v2a2 2 0 0 1-2 2h-2M7 21H5a2 2 0 0 1-2-2v-2M7 12h10"/>',
    plus: '<path d="M12 5v14M5 12h14"/>',
    search: '<circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/>',
    phone: '<rect x="6" y="2" width="12" height="20" rx="3"/><path d="M11 18h2"/>',
    lock: '<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>',
    logout: '<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9"/>',
    bell: '<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10.3 21a1.9 1.9 0 0 0 3.4 0"/>',
    wallet: '<path d="M20 7H5a2 2 0 0 1 0-4h13v4"/><path d="M3 5v14a2 2 0 0 0 2 2h15V7"/><path d="M16 14h.01"/>',
    rupee: '<path d="M6 3h12M6 8h12M6 13l8.5 8M6 13h3a5 5 0 0 0 0-10"/>',
    printer: '<path d="M6 9V2h12v7M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/>',
    download: '<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3"/>',
    filter: '<path d="M22 3H2l8 9.5V19l4 2v-8.5z"/>',
    check: '<path d="M20 6 9 17l-5-5"/>',
    x: '<path d="M18 6 6 18M6 6l12 12"/>',
    chevron: '<path d="m9 18 6-6-6-6"/>',
    back: '<path d="M19 12H5M12 19l-7-7 7-7"/>',
    wifiOff: '<path d="m2 2 20 20M8.5 16.5a5 5 0 0 1 7 0M2 8.8a15 15 0 0 1 4.2-2.7M10.7 5.1A15 15 0 0 1 22 8.8M5 12.9a10 10 0 0 1 5.2-2.8M16.8 11.7a10 10 0 0 1 2.2 1.2M12 20h.01"/>',
    wifi: '<path d="M5 12.6a10 10 0 0 1 14 0M8.5 16.1a5 5 0 0 1 7 0M2 8.8a15 15 0 0 1 20 0M12 20h.01"/>',
    building: '<rect x="4" y="2" width="16" height="20" rx="2"/><path d="M9 22v-4h6v4M8 6h.01M16 6h.01M12 6h.01M12 10h.01M12 14h.01M16 10h.01M16 14h.01M8 10h.01M8 14h.01"/>',
    sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M6.3 17.7l-1.4 1.4M19.1 4.9l-1.4 1.4"/>',
    moon: '<path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9z"/>',
    menu: '<path d="M4 6h16M4 12h16M4 18h16"/>',
    receipt: '<path d="M4 2v20l2-1 2 1 2-1 2 1 2-1 2 1 2-1 2 1V2l-2 1-2-1-2 1-2-1-2 1-2-1-2 1z"/><path d="M8 7h8M8 11h8M8 15h5"/>',
    shield: '<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="m9 12 2 2 4-4"/>',
    store: '<path d="m3 9 1.5-5h15L21 9M3 9v11h18V9M3 9a3 3 0 0 0 6 0 3 3 0 0 0 6 0 3 3 0 0 0 6 0M9 20v-6h6v6"/>',
    clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    edit: '<path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"/>',
    refresh: '<path d="M21 12a9 9 0 1 1-3-6.7L21 8M21 3v5h-5"/>',
    pin: '<path d="M12 22s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12z"/><circle cx="12" cy="10" r="2.5"/>',
    gift: '<rect x="3" y="8" width="18" height="4" rx="1"/><path d="M12 8v13M19 12v9H5v-9M7.5 8a2.5 2.5 0 0 1 0-5C11 3 12 8 12 8s1-5 4.5-5a2.5 2.5 0 0 1 0 5"/>',
    info: '<circle cx="12" cy="12" r="9"/><path d="M12 16v-4M12 8h.01"/>',
    alert: '<path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0zM12 9v4M12 17h.01"/>',
    cash: '<rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="2.5"/><path d="M6 12h.01M18 12h.01"/>',
    upi: '<path d="m7 4 5 8-5 8M13 4l5 8-5 8"/>',
    grid: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
    globe: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18"/>',
    eye: '<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>',
    share: '<circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><path d="m8.6 13.5 6.8 4M15.4 6.5l-6.8 4"/>',
    trash: '<path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6"/>',
    dots: '<circle cx="12" cy="5" r="1.2"/><circle cx="12" cy="12" r="1.2"/><circle cx="12" cy="19" r="1.2"/>',
    file: '<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6M8 13h8M8 17h5"/>',
    help: '<circle cx="12" cy="12" r="9"/><path d="M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3M12 17h.01"/>',
    layers: '<path d="m12 2 10 5-10 5L2 7zM2 17l10 5 10-5M2 12l10 5 10-5"/>',
    sparkle: '<path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/>'
  };
  function icon(name, cls) {
    return '<svg class="' + (cls || "") + '" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + (P[name] || P.info) + "</svg>";
  }

  /* ---------- Logo ---------- */
  let gid = 0;
  function logoMark(size, animated) {
    const id = "OM" + (++gid);
    return '<svg class="logo-mark ' + (animated ? "OM-anim" : "") + '" style="width:' + (size || 40) + "px;height:" + (size || 40) + 'px" viewBox="0 0 120 120" role="img" aria-label="OneMoi">' +
      '<defs><linearGradient id="' + id + 'b" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#A8264F"/><stop offset="1" stop-color="#6A0F28"/></linearGradient>' +
      '<linearGradient id="' + id + 'c" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFC94A"/><stop offset="1" stop-color="#E08E00"/></linearGradient></defs>' +
      '<rect width="120" height="120" rx="28" fill="url(#' + id + 'b)"/>' +
      '<g fill="#F2A516" opacity=".55"><circle cx="20" cy="20" r="2.4"/><circle cx="100" cy="20" r="2.4"/><circle cx="20" cy="100" r="2.4"/><circle cx="100" cy="100" r="2.4"/></g>' +
      '<rect class="OM-env" x="22" y="34" width="76" height="56" rx="10" fill="#FFF9F0"/>' +
      '<path class="OM-m" d="M34 80V48l26 20 26-20v32" fill="none" stroke="#8E1B3A" stroke-width="7.5" stroke-linecap="round" stroke-linejoin="round"/>' +
      '<g class="OM-coin"><circle cx="60" cy="68" r="13" fill="url(#' + id + 'c)" stroke="#FFF9F0" stroke-width="3.5"/>' +
      '<path d="M55 62.5h10M55 66h10M55 62.5h3.2a3.6 3.6 0 0 1 0 7.2H55l7 6" fill="none" stroke="#6A0F28" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round"/></g></svg>';
  }
  function logo(opts) {
    opts = opts || {};
    return '<a class="logo" href="' + (opts.href || "index.html") + '">' + logoMark(opts.size || 40) +
      '<span><div class="logo-word">One<b>Moi</b></div>' + (opts.sub === false ? "" : '<div class="logo-sub">ஒரே மொய் அடையாளம்</div>') + "</span></a>";
  }

  /* ---------- Theme ---------- */
  const THEME_KEY = "OM-theme";
  function getTheme() { try { return localStorage.getItem(THEME_KEY); } catch (e) { return null; } }
  function applyTheme(t) { if (t) document.documentElement.setAttribute("data-theme", t); else document.documentElement.removeAttribute("data-theme"); }
  function isDark() {
    const t = document.documentElement.getAttribute("data-theme");
    return t ? t === "dark" : window.matchMedia("(prefers-color-scheme: dark)").matches;
  }
  function toggleTheme() {
    const next = isDark() ? "light" : "dark";
    applyTheme(next);
    try { localStorage.setItem(THEME_KEY, next); } catch (e) {}
    document.querySelectorAll("[data-theme-toggle]").forEach(b => (b.innerHTML = icon(isDark() ? "sun" : "moon")));
  }
  applyTheme(getTheme());

  /* ---------- Shell ---------- */
  const NAV = {
    vendor: {
      tenant: { name: "JD Moi Tech", sub: "Pro plan · Coimbatore", initials: "JD" },
      user: { name: "Jayadev K", role: "Tenant Owner", initials: "JK" },
      items: [
        { id: "dashboard", label: "Dashboard", icon: "grid", href: "vendor-dashboard.html" },
        { id: "functions", label: "Functions", icon: "calendar", href: "functions.html", badge: "3 live" },
        { id: "operators", label: "Operators", icon: "users", href: "operators.html" },
        { id: "counter", label: "Counter", icon: "receipt", href: "counter.html", fab: true },
        { id: "reports", label: "Reports", icon: "chart", href: "reports.html" },
        { section: "Account" },
        { id: "settings", label: "Settings & Billing", icon: "settings", href: "#", desktopOnly: true },
        { id: "help", label: "Help & Support", icon: "help", href: "#", desktopOnly: true }
      ]
    },
    operator: {
      tenant: { name: "JD Moi Tech", sub: "Surya ♥ Priya · Counter 2", initials: "JD" },
      user: { name: "Bala Murugan", role: "Moi Operator", initials: "BM" },
      items: [
        { id: "counter", label: "Counter", icon: "receipt", href: "counter.html" },
        { id: "entries", label: "My entries", icon: "file", href: "#" },
        { id: "function", label: "Function", icon: "calendar", href: "#" },
        { id: "sync", label: "Sync", icon: "refresh", href: "#" }
      ]
    },
    individual: {
      tenant: null,
      user: { name: "Ram Kumar", role: "+91 98•••• ••210", initials: "RK" },
      items: [
        { id: "my-moi", label: "My Moi", icon: "wallet", href: "my-moi.html" },
        { id: "functions", label: "Functions", icon: "calendar", href: "#" },
        { id: "pay", label: "Scan & Pay", icon: "scan", href: "pay.html", fab: true },
        { id: "qr", label: "My QR", icon: "qr", href: "#" },
        { id: "profile", label: "Profile", icon: "user", href: "#" }
      ]
    },
    admin: {
      tenant: { name: "Platform Console", sub: "Super Admin", initials: "OM" },
      user: { name: "Admin", role: "Super Admin", initials: "SA" },
      items: [
        { id: "admin", label: "Tenants", icon: "store", href: "admin.html", badge: "4" },
        { id: "plans", label: "Plans & Billing", icon: "layers", href: "#" },
        { id: "people", label: "Moi identities", icon: "users", href: "#" },
        { id: "audit", label: "Audit log", icon: "shield", href: "#" },
        { id: "sys", label: "System", icon: "settings", href: "#" }
      ]
    }
  };

  function buildShell() {
    const body = document.body;
    const role = body.dataset.shell;
    if (!role || !NAV[role]) return;
    const cfg = NAV[role];
    const page = body.dataset.page;
    const main = document.querySelector("main.content");
    const title = body.dataset.title || document.title.split("·")[0].trim();
    const crumb = body.dataset.crumb || "";

    const links = cfg.items.map(it => it.section
      ? '<div class="nav-section">' + it.section + "</div>"
      : '<a class="nav-link ' + (it.id === page ? "active" : "") + '" href="' + it.href + '">' + icon(it.icon) + "<span>" + it.label + "</span>" + (it.badge ? '<span class="badge brand">' + it.badge + "</span>" : "") + "</a>"
    ).join("");

    const tenant = cfg.tenant ? '<div class="tenant-card"><div class="avatar brand">' + cfg.tenant.initials + '</div><div class="grow"><div style="font-weight:800;font-size:.9rem">' + cfg.tenant.name + '</div><div class="xs muted">' + cfg.tenant.sub + "</div></div></div>" : "";

    const sidebar = '<aside class="sidebar"><div class="brand">' + logo() + "</div>" + tenant + links +
      '<div class="spacer"></div><div class="tenant-card"><div class="avatar">' + cfg.user.initials + '</div><div class="grow"><div style="font-weight:700;font-size:.88rem">' + cfg.user.name + '</div><div class="xs muted">' + cfg.user.role + '</div></div><a class="btn btn-ghost btn-icon btn-sm" href="login.html" title="Log out">' + icon("logout") + "</a></div></aside>";

    const topbar = '<header class="topbar">' +
      '<span class="hide-desktop">' + logoMark(34) + "</span>" +
      '<div class="grow"><div class="crumb">' + crumb + '</div><div class="page-title">' + title + "</div></div>" +
      (body.dataset.topAction || "") +
      '<button class="btn btn-ghost btn-icon" data-theme-toggle title="Toggle theme">' + icon(isDark() ? "sun" : "moon") + "</button>" +
      '<button class="btn btn-ghost btn-icon hide-mobile" title="Notifications">' + icon("bell") + "</button>" +
      '<div class="avatar sm hide-desktop">' + cfg.user.initials + "</div></header>";

    const bottom = '<nav class="bottom-nav">' + cfg.items.filter(i => !i.section && !i.desktopOnly).slice(0, 5).map(it =>
      it.fab
        ? '<a class="fab ' + (it.id === page ? "active" : "") + '" href="' + it.href + '"><span class="fab-inner">' + icon(it.icon) + "</span>" + it.label + "</a>"
        : '<a class="' + (it.id === page ? "active" : "") + '" href="' + it.href + '">' + icon(it.icon) + it.label + "</a>"
    ).join("") + "</nav>";

    const app = document.createElement("div");
    app.className = "app";
    app.innerHTML = sidebar + '<div class="main-col">' + topbar + "</div>";
    body.insertBefore(app, body.firstChild);
    app.querySelector(".main-col").appendChild(main);
    body.insertAdjacentHTML("beforeend", bottom);
    body.classList.add("has-bottom-nav");
  }

  /* ---------- Toasts ---------- */
  function toast(msg, type) {
    let host = document.querySelector(".toast-host");
    if (!host) { host = document.createElement("div"); host.className = "toast-host"; host.setAttribute("aria-live", "polite"); document.body.appendChild(host); }
    const t = document.createElement("div");
    t.className = "toast " + (type || "");
    t.innerHTML = icon(type === "ok" ? "check" : type === "err" ? "alert" : "info") + "<span>" + msg + "</span>";
    host.appendChild(t);
    setTimeout(() => { t.style.transition = "opacity .3s"; t.style.opacity = "0"; setTimeout(() => t.remove(), 300); }, 2800);
  }

  /* ---------- Modals ---------- */
  function openModal(id) { const m = document.getElementById(id); if (m) { m.classList.add("open"); const f = m.querySelector("input,select,textarea"); if (f && window.innerWidth > 760) setTimeout(() => f.focus(), 50); } }
  function closeModal(el) { const m = el.closest ? el.closest(".modal-backdrop") : el; if (m) m.classList.remove("open"); }

  /* ---------- OTP / PIN inputs ---------- */
  function initOtp(root, onComplete) {
    const inputs = Array.from(root.querySelectorAll("input"));
    const value = () => inputs.map(i => i.value).join("");
    inputs.forEach((inp, idx) => {
      inp.setAttribute("inputmode", "numeric");
      inp.setAttribute("maxlength", "1");
      if (idx === 0) inp.setAttribute("autocomplete", "one-time-code");
      inp.addEventListener("input", () => {
        const v = inp.value.replace(/\D/g, "");
        if (v.length > 1) { // paste / SMS autofill into first box
          v.split("").slice(0, inputs.length - idx).forEach((d, k) => { inputs[idx + k].value = d; inputs[idx + k].classList.add("filled"); });
          inputs[Math.min(idx + v.length, inputs.length - 1)].focus();
        } else {
          inp.value = v; inp.classList.toggle("filled", !!v);
          if (v && inputs[idx + 1]) inputs[idx + 1].focus();
        }
        root.classList.remove("is-error");
        if (value().length === inputs.length && onComplete) onComplete(value());
      });
      inp.addEventListener("keydown", e => {
        if (e.key === "Backspace" && !inp.value && inputs[idx - 1]) { inputs[idx - 1].focus(); inputs[idx - 1].value = ""; inputs[idx - 1].classList.remove("filled"); }
        if (e.key === "ArrowLeft" && inputs[idx - 1]) inputs[idx - 1].focus();
        if (e.key === "ArrowRight" && inputs[idx + 1]) inputs[idx + 1].focus();
      });
      inp.addEventListener("focus", () => inp.select());
    });
    return { value, clear() { inputs.forEach(i => { i.value = ""; i.classList.remove("filled"); }); inputs[0].focus(); } };
  }

  /* ---------- Generic helpers ---------- */
  function inr(n) { return "₹" + Number(n).toLocaleString("en-IN"); }
  function bindGroup(container, cb) { // segmented / chips / tabs single-select
    container.addEventListener("click", e => {
      const b = e.target.closest("button"); if (!b || !container.contains(b)) return;
      container.querySelectorAll("button").forEach(x => x.classList.toggle("active", x === b));
      if (cb) cb(b.dataset.value, b);
    });
  }
  function hydrateIcons(root) { (root || document).querySelectorAll("[data-icon]").forEach(el => { if (!el.dataset.iconDone) { el.insertAdjacentHTML("afterbegin", icon(el.dataset.icon)); el.dataset.iconDone = 1; } }); }
  function hydrateLogos(root) {
    (root || document).querySelectorAll("[data-logo]").forEach(el => {
      const o = el.dataset;
      el.innerHTML = o.logo === "mark" ? logoMark(+o.size || 40, o.animated !== undefined) : logo({ size: +o.size || 40, sub: o.sub !== "false" });
    });
  }

  /* ---------- Boot ---------- */
  document.addEventListener("DOMContentLoaded", () => {
    buildShell();
    hydrateLogos();
    hydrateIcons();
    document.addEventListener("click", e => {
      if (e.target.closest("[data-theme-toggle]")) toggleTheme();
      const open = e.target.closest("[data-open]"); if (open) { e.preventDefault(); openModal(open.dataset.open); }
      if (e.target.closest("[data-close]")) closeModal(e.target);
      if (e.target.classList && e.target.classList.contains("modal-backdrop")) closeModal(e.target);
      const dead = e.target.closest('a[href="#"]'); if (dead) { e.preventDefault(); toast("Template screen — coming in the next set"); }
    });
    document.addEventListener("keydown", e => { if (e.key === "Escape") document.querySelectorAll(".modal-backdrop.open").forEach(m => m.classList.remove("open")); });
    document.querySelectorAll("[data-theme-toggle]").forEach(b => (b.innerHTML = icon(isDark() ? "sun" : "moon")));
    const pl = document.querySelector(".page-loader"); if (pl) setTimeout(() => pl.classList.add("done"), 650);
  });

  window.OM = { icon, logo, logoMark, toast, openModal, closeModal, initOtp, inr, bindGroup, hydrateIcons, toggleTheme, isDark };
})();
