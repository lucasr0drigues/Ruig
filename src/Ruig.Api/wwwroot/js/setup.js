(function () {
  "use strict";

  // ---------- State -------------------------------------------------------

  const state = {
    themes: [],
    accents: [],
    selectedTheme: "purple",
    selectedAccent: "strava"
  };

  // ---------- Sample heatmap preview --------------------------------------

  const PREVIEW_WEEKS = 26;
  const CELL = 11;
  const GAP = 3;
  const STRIDE = CELL + GAP;
  const PADDING = 12;

  function mulberry32(seed) {
    return function () {
      seed |= 0;
      seed = (seed + 0x6D2B79F5) | 0;
      let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  function currentPalette() {
    const found = state.themes.find(t => t.key === state.selectedTheme);
    return found ? found.levels : ["#26262e", "#4c1d95", "#7c3aed", "#a78bfa", "#c4b5fd"];
  }

  function currentAccentColor() {
    const found = state.accents.find(a => a.key === state.selectedAccent);
    return found ? found.color : "#fc4c02";
  }

  function buildPreviewSvg() {
    const random = mulberry32(20260507);
    const palette = currentPalette();
    const stravaColor = currentAccentColor();

    const width = PADDING * 2 + PREVIEW_WEEKS * STRIDE - GAP;
    const height = PADDING * 2 + 7 * STRIDE - GAP;

    let svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" width="${width}" height="${height}" role="img" aria-label="Sample heatmap preview">`;

    for (let w = 0; w < PREVIEW_WEEKS; w++) {
      const trend = w / PREVIEW_WEEKS;
      for (let d = 0; d < 7; d++) {
        const x = PADDING + w * STRIDE;
        const y = PADDING + d * STRIDE;

        const weekday = d > 0 && d < 6 ? 1 : 0.55;
        const r = random();
        const score = r * weekday + trend * 0.4;

        let level = 0;
        if (score > 0.45) level = 1;
        if (score > 0.65) level = 2;
        if (score > 0.80) level = 3;
        if (score > 0.92) level = 4;

        const hasStrava = random() < (d === 6 ? 0.55 : d === 0 ? 0.35 : 0.16);

        svg += `<rect x="${x}" y="${y}" width="${CELL}" height="${CELL}" rx="2" ry="2" fill="${palette[level]}"`;
        if (hasStrava) {
          svg += ` stroke="${stravaColor}" stroke-width="1.5"`;
        }
        svg += "/>";
      }
    }

    svg += "</svg>";
    return svg;
  }

  function mountPreview() {
    const mount = document.getElementById("preview-mount");
    if (mount) mount.innerHTML = buildPreviewSvg();
  }

  function syncLegendDots() {
    const palette = currentPalette();
    document.querySelectorAll(".legend .dot-l0").forEach(el => el.style.background = palette[0]);
    document.querySelectorAll(".legend .dot-l1").forEach(el => el.style.background = palette[1]);
    document.querySelectorAll(".legend .dot-l2").forEach(el => el.style.background = palette[2]);
    document.querySelectorAll(".legend .dot-l3").forEach(el => el.style.background = palette[3]);
    document.querySelectorAll(".legend .dot-l4").forEach(el => el.style.background = palette[4]);
    document.querySelectorAll(".legend .dot-strava").forEach(el => el.style.borderColor = currentAccentColor());
  }

  // ---------- Style catalog -----------------------------------------------

  async function loadStyles() {
    try {
      const response = await fetch("/badges/styles", { headers: { Accept: "application/json" } });
      if (!response.ok) throw new Error("style fetch failed");
      const data = await response.json();
      state.themes = data.themes || [];
      state.accents = data.accents || [];
    } catch (_) {
      // Fallback to defaults if the endpoint is unreachable.
      state.themes = [{ key: "purple", label: "Obsidian violet", levels: currentPalette() }];
      state.accents = [{ key: "strava", label: "Strava orange", color: currentAccentColor() }];
    }

    renderThemeSwatches();
    renderAccentSwatches();
    selectTheme(state.selectedTheme);
    selectAccent(state.selectedAccent);
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
      button.setAttribute("role", "radio");
      button.setAttribute("aria-checked", String(theme.key === state.selectedTheme));
      button.title = theme.label;

      const stops = theme.levels.map((color, i) =>
        `<span class="swatch-cell" style="background:${color}"></span>`
      ).join("");
      button.innerHTML = `<span class="swatch-strip">${stops}</span><span class="swatch-name">${theme.label}</span>`;

      button.addEventListener("click", () => selectTheme(theme.key));
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
      button.setAttribute("role", "radio");
      button.setAttribute("aria-checked", String(accent.key === state.selectedAccent));
      button.title = accent.label;
      button.innerHTML = `<span class="swatch-ring" style="border-color:${accent.color}"></span><span class="swatch-name">${accent.label}</span>`;
      button.addEventListener("click", () => selectAccent(accent.key));
      container.appendChild(button);
    });
  }

  function selectTheme(key) {
    state.selectedTheme = key;
    document.getElementById("selected-theme").value = key;
    document.querySelectorAll(".swatch-theme").forEach(el => {
      const active = el.dataset.themeKey === key;
      el.classList.toggle("is-selected", active);
      el.setAttribute("aria-checked", String(active));
    });
    mountPreview();
    syncLegendDots();
  }

  function selectAccent(key) {
    state.selectedAccent = key;
    document.getElementById("selected-accent").value = key;
    document.querySelectorAll(".swatch-accent").forEach(el => {
      const active = el.dataset.accentKey === key;
      el.classList.toggle("is-selected", active);
      el.setAttribute("aria-checked", String(active));
    });
    mountPreview();
    syncLegendDots();
  }

  // ---------- Error banner from ?error= query -----------------------------

  const errorMessages = {
    "invalid-state": "Your authorisation link expired. Please try again.",
    "connection-failed": "We could not finish connecting to Strava. Please try again."
  };

  function showInlineErrorFromQuery() {
    const params = new URLSearchParams(window.location.search);
    const code = params.get("error");
    if (!code) return;

    const banner = document.getElementById("form-error");
    if (!banner) return;

    banner.textContent = errorMessages[code] || "Something went wrong. Please try again.";
    banner.hidden = false;

    history.replaceState({}, "", window.location.pathname);
  }

  // ---------- Form submission --------------------------------------------

  function bindForm() {
    const form = document.getElementById("setup-form");
    if (!form) return;

    const input = document.getElementById("githubUsername");
    const errorEl = document.getElementById("form-error");
    const submitBtn = document.getElementById("submit-btn");

    const usernamePattern = /^[a-zA-Z0-9](?:[a-zA-Z0-9]|-(?=[a-zA-Z0-9])){0,38}$/;

    function setError(message) {
      errorEl.textContent = message;
      errorEl.hidden = false;
    }

    function clearError() {
      errorEl.hidden = true;
      errorEl.textContent = "";
    }

    input.addEventListener("input", clearError);

    form.addEventListener("submit", async function (event) {
      event.preventDefault();
      clearError();

      const username = input.value.trim();

      if (!usernamePattern.test(username)) {
        setError("That doesn't look like a valid GitHub username.");
        input.focus();
        return;
      }

      const originalContent = submitBtn.innerHTML;
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner" aria-hidden="true"></span><span>Connecting…</span>';

      try {
        const params = new URLSearchParams({
          githubUsername: username,
          theme: state.selectedTheme,
          accent: state.selectedAccent
        });
        const response = await fetch("/auth/strava/start?" + params.toString(), {
          headers: { "Accept": "application/json" }
        });

        if (!response.ok) {
          let message = "Could not start the Strava connection.";
          try {
            const body = await response.json();
            if (body && body.message) message = body.message;
          } catch (_) { /* ignore */ }
          throw new Error(message);
        }

        const data = await response.json();
        if (!data || !data.authorizationUrl) {
          throw new Error("Strava authorisation URL was missing from the response.");
        }

        window.location.assign(data.authorizationUrl);
      } catch (e) {
        setError(e.message || "Something went wrong. Please try again.");
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalContent;
      }
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    mountPreview();
    syncLegendDots();
    showInlineErrorFromQuery();
    bindForm();
    loadStyles();
  });
})();
