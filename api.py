
from threading import Lock

import requests
from fastapi import HTTPException, Response
from tts_client import generate_wav
from fastapi import FastAPI
from pydantic import BaseModel
from langchain_core.messages import HumanMessage, AIMessage
import emoji

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

        response_text = emoji.replace_emoji(
            result.content,
            replace=""
        ).strip()

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


@app.post("/tts")
def text_to_speech(request: ChatRequest):
    text = request.message.strip()

    if not text:
        raise HTTPException(
            status_code=400,
            detail="No text provided"
        )

    try:
        wav_data = generate_wav(text)

        return Response(
            content=wav_data,
            media_type="audio/wav"
        )

    except requests.RequestException as error:
        raise HTTPException(
            status_code=502,
            detail=f"Kokoro TTS failed: {error}"
        )