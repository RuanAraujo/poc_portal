from typing import Any


def message_text(message: Any) -> str:
    text = getattr(message, "text", "")
    if isinstance(text, str) and text:
        return text.rsplit("</think>", 1)[-1].strip()

    content = getattr(message, "content", "")
    if isinstance(content, str):
        return content.rsplit("</think>", 1)[-1].strip()
    if isinstance(content, list):
        text = "\n".join(
            block.get("text", "")
            for block in content
            if isinstance(block, dict) and block.get("text")
        )
        return text.rsplit("</think>", 1)[-1].strip()
    return str(content)
