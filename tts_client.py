import requests
import winsound

TTS_URL = "http://127.0.0.1:8000/speak"

def speak(text):
    # Split text into sentences to stream playback faster
    #sentences = [s.strip() for s in text.replace('!', '.').replace('?', '.').split('.') if s.strip()]
    if not text.strip():
        return

    # for sentence in sentences:
    #     response = requests.post(
    #         TTS_URL,
    #         json={"text": sentence + "."},
    #         timeout=300
    #     )

    response = requests.post(
        TTS_URL,
        json={"text": text},
        timeout=300
    )
    response.raise_for_status()

    # Save and play sentence by sentence asynchronously
    with open("clara_response.wav", "wb") as audio_file:
        audio_file.write(response.content)

    winsound.PlaySound(
        "clara_response.wav",
        winsound.SND_FILENAME | winsound.SND_ASYNC
    )