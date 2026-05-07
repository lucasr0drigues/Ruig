(function () {
  "use strict";

  const DEFAULT_THEME = "purple";
  const DEFAULT_ACCENT = "strava";

  const state = {
    slug: null,
    selectedTheme: DEFAULT_THEME,
    selectedAccent: DEFAULT_ACCENT,
    themes: [],
    accents: []
  };

  document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(window.location.search);
    state.slug = params.get("slug");

    const previewMeta = document.getElementById("preview-meta");
    const markdownEl = document.getElementById("snippet-markdown");
    const htmlEl = document.getElementById("snippet-html");
    const urlEl = document.getElementById("snippet-url");

    if (!state.slug || !/^[a-zA-Z0-9_-]+$/.test(state.slug)) {
      const message = "We couldn't find your badge. Please start over.";
      [markdownEl, htmlEl, urlEl].forEach(el => { if (el) el.textContent = message; });
      if (previewMeta) previewMeta.textContent = "";
      return;
    }

    restoreSelectionFromStorage();
    refreshSnippets();

    const badgeImage = document.getElementById("badge-image");
    badgeImage.addEventListener("load", function () {
      if (previewMeta) {
        const today = new Date().toISOString().slice(0, 10);
        previewMeta.textContent = "Generated " + today + ".";
      }
    });

    bindCopyButtons();
    bindResetButton();
    loadStyles();
  });

  // ---------- Persistence ------------------------------------------------

  function storageKey() {
    return "ruig:badge:" + state.slug + ":style";
  }

  function persistSelection() {
    try {
      const isDefault = state.selectedTheme === DEFAULT_THEME && state.selectedAccent === DEFAULT_ACCENT;
      if (isDefault) {
        localStorage.removeItem(storageKey());
      } else {
        localStorage.setItem(storageKey(), JSON.stringify({
          theme: state.selectedTheme,
          accent: state.selectedAccent
        }));
      }
    } catch (_) { /* ignore quota / private mode */ }
  }

  function restoreSelectionFromStorage() {
    try {
      const raw = localStorage.getItem(storageKey());
      if (!raw) return;
      const parsed = JSON.parse(raw);
      if (parsed && typeof parsed.theme === "string") state.selectedTheme = parsed.theme;
      if (parsed && typeof parsed.accent === "string") state.selectedAccent = parsed.accent;
    } catch (_) { /* ignore */ }
  }

  // ---------- URL building ------------------------------------------------

  function buildBadgePath() {
    let path = "/badges/" + encodeURIComponent(state.slug) + ".svg";
    const params = new URLSearchParams();
    if (state.selectedTheme && state.selectedTheme !== DEFAULT_THEME) {
      params.set("theme", state.selectedTheme);
    }
    if (state.selectedAccent && state.selectedAccent !== DEFAULT_ACCENT) {
      params.set("accent", state.selectedAccent);
    }
    const qs = params.toString();
    if (qs) path += "?" + qs;
    return path;
  }

  function refreshSnippets() {
    const path = buildBadgePath();
    const absoluteUrl = window.location.origin + path;
    const markdown = "![Ruig heatmap](" + absoluteUrl + ")";
    const html = '<img src="' + absoluteUrl + '" alt="Ruig heatmap" />';

    document.getElementById("snippet-markdown").textContent = markdown;
    document.getElementById("snippet-html").textContent = html;
    document.getElementById("snippet-url").textContent = absoluteUrl;

    document.getElementById("badge-image").src = path;
  }

  // ---------- Style catalog -----------------------------------------------

  async function loadStyles() {
    try {
      const response = await fetch("/badges/styles", { headers: { Accept: "application/json" } });
      if (!response.ok) return;
      const data = await response.json();
      state.themes = data.themes || [];
      state.accents = data.accents || [];
    } catch (_) { return; }

    renderThemeSwatches();
    renderAccentSwatches();
  }

  function renderThemeSwatches() {
    const container = document.getElementById("theme-swatches");
    if (!container) return;
    container.innerHTML = "";
    state.themes.forEach(theme => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "swatch swatch-theme";
      button.dataset.themeKey = theme.key;
      button.title = theme.label;
      button.setAttribute("role", "radio");
      const stops = theme.levels.map(c => `<span class="swatch-cell" style="background:${c}"></span>`).join("");
      button.innerHTML = `<span class="swatch-strip">${stops}</span><span class="swatch-name">${theme.label}</span>`;
      button.addEventListener("click", () => selectTheme(theme.key));
      container.appendChild(button);
    });
    syncThemeSelection();
  }

  function renderAccentSwatches() {
    const container = document.getElementById("accent-swatches");
    if (!container) return;
    container.innerHTML = "";
    state.accents.forEach(accent => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "swatch swatch-accent";
      button.dataset.accentKey = accent.key;
      button.title = accent.label;
      button.setAttribute("role", "radio");
      button.innerHTML = `<span class="swatch-ring" style="border-color:${accent.color}"></span><span class="swatch-name">${accent.label}</span>`;
      button.addEventListener("click", () => selectAccent(accent.key));
      container.appendChild(button);
    });
    syncAccentSelection();
  }

  function selectTheme(key) {
    state.selectedTheme = key;
    syncThemeSelection();
    persistSelection();
    refreshSnippets();
  }

  function selectAccent(key) {
    state.selectedAccent = key;
    syncAccentSelection();
    persistSelection();
    refreshSnippets();
  }

  function syncThemeSelection() {
    document.querySelectorAll(".swatch-theme").forEach(el => {
      const active = el.dataset.themeKey === state.selectedTheme;
      el.classList.toggle("is-selected", active);
      el.setAttribute("aria-checked", String(active));
    });
  }

  function syncAccentSelection() {
    document.querySelectorAll(".swatch-accent").forEach(el => {
      const active = el.dataset.accentKey === state.selectedAccent;
      el.classList.toggle("is-selected", active);
      el.setAttribute("aria-checked", String(active));
    });
  }

  // ---------- Reset -------------------------------------------------------

  function bindResetButton() {
    const btn = document.getElementById("reset-style");
    if (!btn) return;
    btn.addEventListener("click", function () {
      state.selectedTheme = DEFAULT_THEME;
      state.selectedAccent = DEFAULT_ACCENT;
      syncThemeSelection();
      syncAccentSelection();
      persistSelection();
      refreshSnippets();
    });
  }

  // ---------- Copy -------------------------------------------------------

  function bindCopyButtons() {
    document.querySelectorAll(".copy").forEach(button => {
      button.addEventListener("click", async function () {
        const targetId = button.dataset.target;
        const target = document.getElementById(targetId);
        if (!target) return;

        const text = target.textContent;
        const labelEl = button.querySelector("span");
        const originalLabel = labelEl ? labelEl.textContent : button.textContent;

        try {
          await copyText(text);
          if (labelEl) labelEl.textContent = "Copied"; else button.textContent = "Copied";
          button.classList.add("btn-success");
          setTimeout(() => {
            if (labelEl) labelEl.textContent = originalLabel; else button.textContent = originalLabel;
            button.classList.remove("btn-success");
          }, 1400);
        } catch (_) {
          if (labelEl) labelEl.textContent = "Copy failed"; else button.textContent = "Copy failed";
          setTimeout(() => {
            if (labelEl) labelEl.textContent = originalLabel; else button.textContent = originalLabel;
          }, 1400);
        }
      });
    });
  }

  async function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }

    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "absolute";
    textarea.style.left = "-9999px";
    document.body.appendChild(textarea);
    textarea.select();
    try {
      document.execCommand("copy");
    } finally {
      document.body.removeChild(textarea);
    }
  }
})();
