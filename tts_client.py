import requests
import winsound

TTS_URL = "http://127.0.0.1:8000/speak"


def speak(text):
    print("Sending text to Clara's voice server...")

    response = requests.post(
        TTS_URL,
        json={"text": text},
        timeout=300
    )

    response.raise_for_status()

    # Save the returned audio as a WAV file
    with open("clara_response.wav", "wb") as audio_file:
        audio_file.write(response.content)

    # Play the WAV file (Windows)
    winsound.PlaySound(
        "clara_response.wav",
        winsound.SND_FILENAME
    )