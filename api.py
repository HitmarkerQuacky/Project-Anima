
from threading import Lock

from fastapi import FastAPI
from pydantic import BaseModel
from langchain_core.messages import HumanMessage, AIMessage

import main as clara
from database import load_history

app = FastAPI(title="Clara Conversation API")

# Prevent two requests from modifying conversation memory at once.
conversation_lock = Lock()


class ChatRequest(BaseModel):
    message: str


class ChatResponse(BaseModel):
    response: str


@app.get("/health")
def health():
    return {"status": "online"}


@app.get("/history")
def history():
    messages = load_history(limit=50)

    return {
        "messages": [
            {
                "role": "user" if isinstance(msg, HumanMessage)
                        else "clara",
                "content": msg.content
            }
            for msg in messages
        ]
    }


@app.post("/chat", response_model=ChatResponse)
def chat(request: ChatRequest):
    user_input = request.message.strip()

    if not user_input:
        return ChatResponse(response="")

    with conversation_lock:
        # Reuse the existing LangChain prompt and Ollama model.
        result = clara.chain.invoke({
            "history": clara.chat_history,
            "user_input": user_input
        })

        response_text = result.content.strip()

        # Save both messages to SQLite.
        clara.save_message("human", user_input)
        clara.save_message("ai", response_text)

        # Update the in-memory LangChain history.
        clara.chat_history.append(
            HumanMessage(content=user_input)
        )
        clara.chat_history.append(
            AIMessage(content=response_text)
        )

    return ChatResponse(response=response_text)
