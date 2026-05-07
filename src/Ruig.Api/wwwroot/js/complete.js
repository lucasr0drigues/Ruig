(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(window.location.search);
    const slug = params.get("slug");

    const previewMeta = document.getElementById("preview-meta");
    const badgeImage = document.getElementById("badge-image");
    const markdownEl = document.getElementById("snippet-markdown");
    const htmlEl = document.getElementById("snippet-html");
    const urlEl = document.getElementById("snippet-url");

    if (!slug || !/^[a-zA-Z0-9_-]+$/.test(slug)) {
      const message = "We couldn't find your badge. Please start over.";
      [markdownEl, htmlEl, urlEl].forEach(function (el) {
        if (el) el.textContent = message;
      });
      if (previewMeta) previewMeta.textContent = "";
      return;
    }

    const badgePath = "/badges/" + encodeURIComponent(slug) + ".svg";
    const absoluteUrl = window.location.origin + badgePath;
    const markdown = "![Ruig heatmap](" + absoluteUrl + ")";
    const html = '<img src="' + absoluteUrl + '" alt="Ruig heatmap" />';

    badgeImage.src = badgePath;
    badgeImage.addEventListener("load", function () {
      if (previewMeta) {
        const today = new Date().toISOString().slice(0, 10);
        previewMeta.textContent = "Generated " + today + ".";
      }
    });

    markdownEl.textContent = markdown;
    htmlEl.textContent = html;
    urlEl.textContent = absoluteUrl;

    bindCopyButtons();
  });

  function bindCopyButtons() {
    document.querySelectorAll(".copy").forEach(function (button) {
      button.addEventListener("click", async function () {
        const targetId = button.dataset.target;
        const target = document.getElementById(targetId);
        if (!target) return;

        const text = target.textContent;
        const labelEl = button.querySelector("span");
        const originalLabel = labelEl ? labelEl.textContent : button.textContent;

        try {
          await copyText(text);
          if (labelEl) labelEl.textContent = "Copied";
          else button.textContent = "Copied";
          button.classList.add("btn-success");

          setTimeout(function () {
            if (labelEl) labelEl.textContent = originalLabel;
            else button.textContent = originalLabel;
            button.classList.remove("btn-success");
          }, 1400);
        } catch (_) {
          if (labelEl) labelEl.textContent = "Copy failed";
          else button.textContent = "Copy failed";
          setTimeout(function () {
            if (labelEl) labelEl.textContent = originalLabel;
            else button.textContent = originalLabel;
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

    // Fallback for non-secure contexts.
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
