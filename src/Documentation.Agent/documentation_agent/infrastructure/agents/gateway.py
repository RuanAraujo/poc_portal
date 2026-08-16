from collections.abc import AsyncIterator, Callable
from typing import Any

from langchain.agents import create_agent
from langchain.agents.middleware import ModelFallbackMiddleware
from langchain_openai import ChatOpenAI

from documentation_agent.application.errors import AgentInvocationFailed
from documentation_agent.application.services import KnowledgeSearchUseCase
from documentation_agent.infrastructure.config import Settings
from documentation_agent.infrastructure.tools import (
    create_consult_feature_integration_specialist,
    create_search_system_knowledge,
)
from documentation_agent.infrastructure.text import message_text


class ThoughtFilter:
    def __init__(self):
        self.buffer = ""
        self.thinking = False

    def filter(self, chunk: str) -> str:
        self.buffer += chunk
        output = []
        while self.buffer:
            tag = "</think>" if self.thinking else "<think>"
            index = self.buffer.find(tag)
            if index >= 0:
                if not self.thinking:
                    output.append(self.buffer[:index])
                self.buffer = self.buffer[index + len(tag):]
                self.thinking = not self.thinking
                continue
            keep = max((size for size in range(1, len(tag)) if self.buffer.endswith(tag[:size])), default=0)
            if not self.thinking:
                output.append(self.buffer[:-keep] if keep else self.buffer)
            if keep:
                self.buffer = self.buffer[-keep:]
                break
            self.buffer = ""
        return "".join(output)

    def finish(self) -> str:
        self.buffer = ""
        return ""


def chunk_text(chunk: Any) -> str:
    content = getattr(chunk, "content", chunk)
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return "".join(
            block.get("text", "")
            for block in content
            if isinstance(block, dict) and block.get("type", "text") == "text"
        )
    return ""


def specialist_completed(update: Any) -> bool:
    if not isinstance(update, dict):
        return False
    for value in update.values():
        messages = value.get("messages", []) if isinstance(value, dict) else []
        if any(getattr(message, "name", None) == "consult_feature_integration_specialist" for message in messages):
            return True
    return False



class LangChainAgentGateway:
    def __init__(self, settings: Settings, search: KnowledgeSearchUseCase, log: Callable[..., object]):
        self.settings = settings
        self.search = search
        self.log = log
        self.supervisor: Any | None = None

    def _models(self) -> tuple[ChatOpenAI, ChatOpenAI]:
        options = dict(
            api_key=self.settings.llm_api_key,
            base_url=self.settings.llm_base_url,
            max_tokens=self.settings.llm_max_tokens,
            reasoning_effort="none",
            streaming=True,
        )
        primary = ChatOpenAI(model=self.settings.agent_model, **options)
        fallback = ChatOpenAI(model=self.settings.agent_fallback_model, **options)
        return primary, fallback

    def _supervisor(self) -> Any:
        if self.supervisor is not None:
            return self.supervisor

        model, fallback = self._models()
        self.log(
            "agent_initialization",
            "started",
            PrimaryModel=self.settings.agent_model,
            FallbackModel=self.settings.agent_fallback_model,
        )
        knowledge_search = create_search_system_knowledge(self.search, self.log)
        feature = create_agent(
            model=model,
            tools=[knowledge_search],
            middleware=[ModelFallbackMiddleware(fallback)],
            name="feature_integration_agent",
            system_prompt=(
                "Você é um consultor técnico do Documentation Portal. Antes de responder, "
                "sempre use search_system_knowledge para consultar a documentação indexada. "
                "Não invente contratos ausentes. Responda em português com resumo, componentes "
                "impactados, contratos/fluxo e testes recomendados. Cite api_id, versão e ambiente "
                "dos chunks usados. O supervisor verá apenas sua resposta final."
            ),
        )
        specialist = create_consult_feature_integration_specialist(feature, self.log)
        self.supervisor = create_agent(
            model=model,
            tools=[specialist],
            middleware=[ModelFallbackMiddleware(fallback)],
            name="documentation_supervisor",
            system_prompt=(
                "Você coordena o consultor de integração do Documentation Portal. "
                "Você não deve tratar de assuntos fora do escopo de integração de recursos do Portal. "
                "Sempre delegue a solicitação recebida para consult_feature_integration_specialist "
                "e entregue ao usuário uma resposta concisa e fundamentada."
            ),
        )
        self.log("agent_initialization", "completed")
        return self.supervisor

    async def respond(self, message: str) -> AsyncIterator[str]:
        thought_filter = ThoughtFilter()
        specialist_done = False
        direct_answer = ""
        try:
            async for mode, event in self._supervisor().astream(
                {"messages": [{"role": "user", "content": message}]}, stream_mode=["updates", "messages"]
            ):
                if mode == "updates":
                    if not specialist_done and specialist_completed(event):
                        specialist_done = True
                        direct_answer = ""
                        thought_filter = ThoughtFilter()
                elif mode == "messages":
                    chunk, metadata = event
                    if metadata.get("langgraph_node") == "model":
                        text = thought_filter.filter(chunk_text(chunk))
                        if specialist_done and text:
                            yield text
                        elif text:
                            direct_answer += text
            text = thought_filter.finish()
            if specialist_done and text:
                yield text
            elif not specialist_done and direct_answer:
                yield direct_answer
        except Exception as exception:
            raise AgentInvocationFailed() from exception
