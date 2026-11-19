import requests
import winsound

TTS_URL = "http://127.0.0.1:8000/speak"

def generate_wav(text: str) -> bytes:
    """Generate speech thorugh Kokoro and return WAV bytes."""

    if not text.strip():
        return b""

    response = requests.post(
        TTS_URL,
        json={"text": text},
        timeout=300
    )

    response.raise_for_status()

    return response.content


def speak(text: str):
    """Legacy playback for Python terminal application."""

    wav_data = generate_wav(text)

    if not wav_data:
        return
    
    with open("clara_response.wav", "wb") as audio_file:
        audio_file.write(wav_data)

    winsound.PlaySound(
        "clara_response.wav",
        winsound.SND_FILENAME | winsound.SND_ASYNC
    )