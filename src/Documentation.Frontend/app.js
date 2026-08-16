(() => {
  const config = window.PORTAL_CONFIG || {};
  const apiUrl = String(config.apiUrl || "http://localhost:8080").replace(/\/$/, "");
  const agentUrl = String(config.agentUrl || "http://localhost:8090").replace(/\/$/, "");
  const $ = (selector) => document.querySelector(selector);
  const notice = $("#notice");
  const showNotice = (message, success = false) => { notice.textContent = message; notice.className = `notice${success ? " success" : ""}`; };
  const request = async (url, options = {}, raw = false) => {
    const response = await fetch(url, { headers: { "Content-Type": "application/json", ...options.headers }, ...options });
    if (!response.ok) throw new Error((await response.text()) || `Erro HTTP ${response.status}`);
    if (raw) return response;
    return response.status === 204 ? null : response.json();
  };
  const appendMessage = (who, text, markdown = false) => {
    const item = document.createElement("article"); item.className = `message ${who}`;
    const label = document.createElement("strong"); label.textContent = who === "user" ? "Você" : "Assistente";
    const content = document.createElement("div"); content.className = "message-content";
    if (markdown) content.innerHTML = PortalHelpers.renderMarkdown(text); else content.textContent = text;
    item.append(label, content); $("#chat-history").append(item); item.scrollIntoView({ block: "nearest" });
    return { item, content };
  };
  const appendTypingMessage = () => {
    const message = appendMessage("agent", "Digitando");
    message.item.classList.add("typing");
    message.content.insertAdjacentHTML("beforeend", '<span class="typing-dots" aria-hidden="true"><i></i><i></i><i></i></span>');
    message.content.setAttribute("aria-label", "Assistente está digitando");
    return message;
  };
  const formatDate = (value) => value ? new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : "—";
  const versionRows = (documentItem) => (documentItem.versions || []).map((version) => {
    const retry = PortalHelpers.isRetryableStatus(version.status) ? `<button type="button" data-republish="${documentItem.id}" data-version="${version.id}">Tentar novamente</button>` : "";
    return `<tr><td>${escapeHtml(version.version)}</td><td>${escapeHtml(version.environment)}</td><td class="status ${escapeHtml(version.status)}">${escapeHtml(version.status)}</td><td>${formatDate(version.indexingUpdatedAtUtc || version.publishedAtUtc || version.createdAtUtc)}</td><td>${version.lastError ? `<span class="error">${escapeHtml(version.lastError)}</span>` : ""}</td><td>${retry}</td></tr>`;
  }).join("");
  const escapeHtml = (value) => String(value ?? "").replace(/[&<>'"]/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", "\"": "&quot;" }[char]));
  async function refresh() {
    const list = $("#documentation-list"); list.textContent = "Carregando…";
    try {
      const documents = await request(`${apiUrl}/api/documentations`);
      list.innerHTML = documents.length ? documents.map((item) => `<article class="document"><h3>${escapeHtml(item.name)}</h3><p>${escapeHtml(item.apiId)}</p><table class="versions"><thead><tr><th>Versão</th><th>Ambiente</th><th>Status</th><th>Atualização</th><th>Erro</th><th>Ação</th></tr></thead><tbody>${versionRows(item)}</tbody></table></article>`).join("") : "Nenhuma documentação cadastrada.";
    } catch (error) { list.textContent = "Não foi possível carregar as documentações."; showNotice(error.message); }
  }
  $("#chat-form").addEventListener("submit", async (event) => {
    event.preventDefault(); const field = $("#chat-message"); const message = field.value.trim(); if (!message) return;
    appendMessage("user", message); field.value = ""; const button = event.submitter || event.currentTarget.querySelector("button"); button.disabled = true;
    const typing = appendTypingMessage(); const started = performance.now();
    try {
      const response = await request(`${agentUrl}/api/agents/chat`, { method: "POST", body: JSON.stringify({ message }) }, true);
      let answer = "";
      if (response.body) {
        const reader = response.body.getReader(); const decoder = new TextDecoder();
        for (;;) {
          const { done, value } = await reader.read();
          if (done) { answer += decoder.decode(); break; }
          answer += decoder.decode(value, { stream: true });
          typing.item.classList.remove("typing"); typing.content.removeAttribute("aria-label"); typing.content.textContent = answer; typing.item.scrollIntoView({ block: "nearest" });
        }
      } else answer = await response.text();
      if (!answer.trim()) throw new Error("O agente retornou uma resposta vazia.");
      typing.item.classList.remove("typing"); typing.content.removeAttribute("aria-label"); typing.content.innerHTML = PortalHelpers.renderMarkdown(answer);
      const elapsed = document.createElement("small"); elapsed.className = "message-meta"; elapsed.textContent = `Respondido em ${PortalHelpers.formatDuration(performance.now() - started)}`; typing.item.append(elapsed);
    } catch (error) {
      typing.item.classList.remove("typing"); typing.content.removeAttribute("aria-label"); typing.content.textContent = "Não foi possível consultar o agente."; showNotice(error.message);
    } finally { button.disabled = false; field.focus(); }
  });
  $("#openapi-file").addEventListener("change", async (event) => {
    const file = event.target.files[0]; if (!file) return; const format = PortalHelpers.formatFromFile(file.name);
    if (!format) { showNotice("Selecione um arquivo JSON, YAML ou YML."); event.target.value = ""; return; }
    $("#openapi-content").value = await file.text(); $("#file-status").textContent = `${file.name} carregado como ${format.toUpperCase()}.`;
  });
  $("#documentation-form").addEventListener("submit", async (event) => {
    event.preventDefault(); const form = new FormData(event.currentTarget); const content = String(form.get("content") || "").trim();
    if (!content) return showNotice("Informe uma especificação OpenAPI."); const file = $("#openapi-file").files[0];
    const payload = Object.fromEntries(form); payload.content = content; payload.format = PortalHelpers.formatFromFile(file?.name) || PortalHelpers.formatFromContent(content);
    const button = event.submitter; button.disabled = true;
    try { await request(`${apiUrl}/api/documentations`, { method: "POST", body: JSON.stringify(payload) }); showNotice("Documentação enviada para indexação.", true); event.currentTarget.reset(); $("#file-status").textContent = "JSON e YAML são aceitos."; await refresh(); } catch (error) { showNotice(error.message); } finally { button.disabled = false; }
  });
  $("#documentation-list").addEventListener("click", async (event) => {
    const button = event.target.closest("[data-republish]"); if (!button) return; button.disabled = true;
    try { await request(`${apiUrl}/api/documentations/${button.dataset.republish}/versions/${button.dataset.version}/republish`, { method: "POST" }); showNotice("Nova tentativa de indexação solicitada.", true); await refresh(); } catch (error) { showNotice(error.message); button.disabled = false; }
  });
  $("#refresh").addEventListener("click", refresh); refresh();
})();
