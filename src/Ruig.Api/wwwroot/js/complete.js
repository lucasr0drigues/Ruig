(function () {
  "use strict";

  const state = {
    slug: null,
    selectedTheme: null,
    selectedAccent: null,
    themes: [],
    accents: []
  };

  document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(window.location.search);
    state.slug = params.get("slug");

    const previewMeta = document.getElementById("preview-meta");
    const badgeImage = document.getElementById("badge-image");
    const markdownEl = document.getElementById("snippet-markdown");
    const htmlEl = document.getElementById("snippet-html");
    const urlEl = document.getElementById("snippet-url");

    if (!state.slug || !/^[a-zA-Z0-9_-]+$/.test(state.slug)) {
      const message = "We couldn't find your badge. Please start over.";
      [markdownEl, htmlEl, urlEl].forEach(el => { if (el) el.textContent = message; });
      if (previewMeta) previewMeta.textContent = "";
      return;
    }

    refreshSnippets();

    badgeImage.addEventListener("load", function () {
      if (previewMeta) {
        const today = new Date().toISOString().slice(0, 10);
        previewMeta.textContent = "Generated " + today + ".";
      }
    });

    bindCopyButtons();
    loadStyles();
  });

  function buildBadgeUrl(absolute, withOverride) {
    let path = "/badges/" + encodeURIComponent(state.slug) + ".svg";
    if (withOverride) {
      const params = new URLSearchParams();
      if (state.selectedTheme) params.set("theme", state.selectedTheme);
      if (state.selectedAccent) params.set("accent", state.selectedAccent);
      const qs = params.toString();
      if (qs) path += "?" + qs;
    }
    return absolute ? window.location.origin + path : path;
  }

  function refreshSnippets() {
    // Snippets always reference the saved (canonical) URL, not the override.
    const canonicalUrl = window.location.origin + "/badges/" + encodeURIComponent(state.slug) + ".svg";
    const markdown = "![Ruig heatmap](" + canonicalUrl + ")";
    const html = '<img src="' + canonicalUrl + '" alt="Ruig heatmap" />';

    document.getElementById("snippet-markdown").textContent = markdown;
    document.getElementById("snippet-html").textContent = html;
    document.getElementById("snippet-url").textContent = canonicalUrl;

    document.getElementById("badge-image").src = buildBadgeUrl(false, true);
  }

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
      const stops = theme.levels.map(c => `<span class="swatch-cell" style="background:${c}"></span>`).join("");
      button.innerHTML = `<span class="swatch-strip">${stops}</span><span class="swatch-name">${theme.label}</span>`;
      button.addEventListener("click", () => {
        state.selectedTheme = theme.key;
        document.querySelectorAll(".swatch-theme").forEach(el => el.classList.toggle("is-selected", el.dataset.themeKey === theme.key));
        refreshSnippets();
      });
      container.appendChild(button);
    });
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
      button.innerHTML = `<span class="swatch-ring" style="border-color:${accent.color}"></span><span class="swatch-name">${accent.label}</span>`;
      button.addEventListener("click", () => {
        state.selectedAccent = accent.key;
        document.querySelectorAll(".swatch-accent").forEach(el => el.classList.toggle("is-selected", el.dataset.accentKey === accent.key));
        refreshSnippets();
      });
      container.appendChild(button);
    });
  }

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
