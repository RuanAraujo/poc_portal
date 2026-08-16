(function (root, factory) {
  const helpers = factory();
  if (typeof module !== "undefined") module.exports = helpers;
  root.PortalHelpers = helpers;
})(typeof globalThis === "undefined" ? this : globalThis, function () {
  function isRetryableStatus(status) {
    return status === "publishFailed" || status === "indexingFailed";
  }

  function formatFromFile(fileName) {
    const name = String(fileName || "").toLowerCase();
    return name.endsWith(".json") ? "json" : name.endsWith(".yaml") || name.endsWith(".yml") ? "yaml" : "";
  }

  function formatFromContent(content) {
    const text = String(content || "").trim();
    return text.startsWith("{") || text.startsWith("[") ? "json" : "yaml";
  }

  function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>'"]/g, (character) => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", "\"": "&quot;"
    })[character]);
  }

  function inlineMarkdown(value) {
    const tokens = [];
    const token = (html) => `\uE000${tokens.push(html) - 1}\uE001`;
    let text = escapeHtml(value);
    text = text.replace(/`([^`\n]+)`/g, (_, code) => token(`<code>${code}</code>`));
    text = text.replace(/\[([^\]]+)]\((https?:\/\/[^\s)]+)\)/gi, (_, label, url) =>
      token(`<a href="${url}" target="_blank" rel="noopener noreferrer">${label}</a>`));
    text = text
      .replace(/\*\*([^*\n]+)\*\*/g, "<strong>$1</strong>")
      .replace(/(^|[^*])\*([^*\n]+)\*/g, "$1<em>$2</em>");
    return text.replace(/\uE000(\d+)\uE001/g, (_, index) => tokens[Number(index)]);
  }

  function renderMarkdown(markdown) {
    const output = [];
    let paragraph = [];
    let list;
    let code;
    let language = "";
    const flushParagraph = () => {
      if (paragraph.length) output.push(`<p>${inlineMarkdown(paragraph.join(" "))}</p>`);
      paragraph = [];
    };
    const flushList = () => {
      if (list) output.push(`<${list.type}>${list.items.map((item) => `<li>${inlineMarkdown(item)}</li>`).join("")}</${list.type}>`);
      list = undefined;
    };
    const flushCode = () => {
      const className = language ? ` class="language-${language}"` : "";
      output.push(`<pre><code${className}>${escapeHtml(code.join("\n"))}</code></pre>`);
      code = undefined;
      language = "";
    };

    for (const line of String(markdown ?? "").replace(/\r\n?/g, "\n").split("\n")) {
      const fence = line.match(/^```([A-Za-z0-9_-]*)\s*$/);
      if (fence) {
        if (code) flushCode();
        else { flushParagraph(); flushList(); code = []; language = fence[1]; }
        continue;
      }
      if (code) { code.push(line); continue; }
      if (!line.trim()) { flushParagraph(); flushList(); continue; }

      const heading = line.match(/^(#{1,6})\s+(.+)$/);
      const unordered = line.match(/^[-+*]\s+(.+)$/);
      const ordered = line.match(/^\d+\.\s+(.+)$/);
      if (heading) {
        flushParagraph(); flushList();
        output.push(`<h${heading[1].length}>${inlineMarkdown(heading[2])}</h${heading[1].length}>`);
      } else if (unordered || ordered) {
        flushParagraph();
        const type = unordered ? "ul" : "ol";
        if (list?.type !== type) { flushList(); list = { type, items: [] }; }
        list.items.push((unordered || ordered)[1]);
      } else if (line.startsWith("> ")) {
        flushParagraph(); flushList();
        output.push(`<blockquote>${inlineMarkdown(line.slice(2))}</blockquote>`);
      } else {
        flushList(); paragraph.push(line.trim());
      }
    }
    if (code) flushCode();
    flushParagraph(); flushList();
    return output.join("");
  }

  function formatDuration(milliseconds) {
    const duration = Math.max(0, Number(milliseconds) || 0);
    return duration < 1000 ? `${Math.round(duration)} ms` : `${(duration / 1000).toFixed(1).replace(".", ",")} s`;
  }

  return { isRetryableStatus, formatFromFile, formatFromContent, renderMarkdown, formatDuration };
});
