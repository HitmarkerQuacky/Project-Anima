
import pyaudio
import wave
from faster_whisper import WhisperModel
import keyboard

# Recording parameters
FORMAT = pyaudio.paInt16  # 16-bit resolution
CHANNELS = 2              # Mono audio
RATE = 44100              # Sample rate (44.1 kHz)
CHUNK = 1024              # Frames per buffer
OUTPUT_FILENAME = "user_audio.wav"


def record_audio(key):
    """Records input audio while key button is held.
    Returns: .wav file"""

    # init audio stream
    audio = pyaudio.PyAudio()
    stream = audio.open(format=FORMAT, channels=CHANNELS, rate=RATE, input=True, frames_per_buffer=CHUNK)
    frames = []

    # MAIN LOOP (ends if F key is pressed)
    try:
        while True:
            if keyboard.is_pressed(key):
                print("Finshed recording.")
                break
            data = stream.read(1024)
            frames.append(data)
    # user pressed CTRL+Z or CTRL+C
    except KeyboardInterrupt:
            pass
    
    # write to a wav file
    stream.stop_stream()
    stream.close()
    audio.terminate()

    soundFile = wave.open(OUTPUT_FILENAME, "wb")
    soundFile.setnchannels(CHANNELS)
    soundFile.setsampwidth(audio.get_sample_size(FORMAT))
    soundFile.setframerate(RATE)
    soundFile.writeframes(b''.join(frames))
    soundFile.close()

    return OUTPUT_FILENAME


def transcribe_audio(file):
    """Transcribes a audio file using openAI-Whisper.
    Returns: string"""

    model = WhisperModel(
    "small.en",
    device="cpu",
    compute_type="int8",
    )

    segments, info = model.transcribe(file, beam_size=5)

    transcription = " ".join(
        segment.text.strip() for segment in segments
    )

    return transcription