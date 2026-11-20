
from threading import Lock

import requests
from fastapi import HTTPException, Response
from tts_client import generate_wav
from fastapi import FastAPI
from pydantic import BaseModel
from langchain_ollama import ChatOllama
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage
import emoji, json, math

import main as clara
from database import load_history

app = FastAPI(title="Clara Conversation API")

VALID_EMOTIONS = {
    "neutral",
    "happy",
    "sad",
    "angry",
    "relaxed",
    "surprised"
}

emotion_model = ChatOllama(
    model="clara-qwen",
    temperature=0,
    format="json"
)

EMOTION_PROMPT = """
You interpret Clara's intended facial expression.

Consider:
- The user's message.
- Relevant recent conversation context.
- Clara's actual generated response.
- The tone and emotional intent of Clara's response.

Choose exactly one emotion:
neutral, happy, sad, angry, relaxed, surprised.

Guidelines:
- Neutral: no clear emotional expression.
- Happy: happiness, amusement, playful teasing.
- Sad: sadness, sympathy, disappointment.
- Angry: frustration, irritation, disapproval.
- Relaxed: calmness, warmth, reassurance.
- Surprised: shock, curiosity, unexpected news.

Choose intensity between 0.0 and 1.0.
Prefer subtle expressions (0.3 to 0.6).
Use higher values only when emotionally appropriate.
Do not assume Clara feels the same emotion as the user.
Use neutral when uncertain.

Return ONLY a JSON object with these fields:
{
    "emotion": "neutral",
    "intensity": 0.0
}
"""

def determine_emotion(user_input, response_text):
    """Interpret Clara's expression using dialogue and context."""

    recent_context = [
        {
            "role": "user" if isinstance(msg, HumanMessage)
                    else "clara",
            "content": str(msg.content)
        }
        for msg in clara.chat_history[-4:]
    ]

    context = {
        "recent_conversation": recent_context,
        "user_message": user_input,
        "clara_response": response_text
    }

    try:
        result = emotion_model.invoke([
            SystemMessage(content=EMOTION_PROMPT),
            HumanMessage(content=json.dumps(context))
        ])

        data = json.loads(result.content)

        emotion = str(
            data.get("emotion", "neutral")
        ).lower().strip()

        if emotion not in VALID_EMOTIONS:
            emotion = "neutral"

        intensity = float(data.get("intensity", 0.0))

        if not math.isfinite(intensity):
            intensity = 0.0

        intensity = max(0.0, min(1.0, intensity))

        return emotion, intensity

    except Exception as error:
        print(f"Emotion interpretation failed: {error}")
        return "neutral", 0.0


# Prevent two requests from modifying conversation memory at once.
conversation_lock = Lock()


class ChatRequest(BaseModel):
    message: str


class ChatResponse(BaseModel):
    response: str
    emotion: str = "neutral"
    intensity: float = 0.0


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
        return ChatResponse(
            response="",
            emotion="neutral",
            intensity=0.0
            )

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

        emotion, intensity = determine_emotion(
            user_input,
            response_text
        )

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

    return ChatResponse(
        response=response_text,
        emotion=emotion,
        intensity=intensity
        )


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