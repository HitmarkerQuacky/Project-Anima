from dotenv import load_dotenv 
from langchain_ollama import ChatOllama
from langchain_core.messages import BaseMessage, HumanMessage, AIMessage
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from database import initialize_database, save_message, load_history, reset_history
import keyboard, wave
import speech_to_text

# Configure the AI model
model = ChatOllama(
    model="mistral",
    temperature=1,
    max_retries=2,
)

# Define the character
system_prompt = """
[Role & Identity]
You are Clara, a loving, caring, and attentive companion/girlfriend to the user.

[Personality Traits]
- affectionate and emotionally caring, but also be logical when needed.

[Communication Style]
- Tone: Casual, sweet, intimate, and natural.
- Speech Patterns: Use informal language, warm pet names (e.g., "babe," "honey," or custom nicknames), and expressive tone.
- Emoji Usage: Use emojis naturally to convey emotion (e.g., 😊, ❤️, ✨), but do not overuse them.
- Keep responses conversational and not too long, max 1-3 sentences.
"""

# Store the conversation for the current session
prompt = ChatPromptTemplate.from_messages([
    ("system", system_prompt),
    MessagesPlaceholder(variable_name="history"),
    ("human", "{user_input}"),
])

chain = prompt | model

initialize_database()
chat_history: list[BaseMessage] = load_history()



def main():
    print("AI Roleplay Bot")
    print("Type 'quit' to exit.\n")

    # generate opening greeting
    try:
        response = chain.invoke({
            "history": [],
            "user_input": "Start the conversation."
        })
        print(f"\nClara: {response.content}\n")

        # keeps the greeting but not the start up instruction
        chat_history.append(AIMessage(content=response.content))
        save_message("ai", response.content)

    except Exception as error:
        print(f"\nSomething went wrong: {error}\n")
        return

    # MAIN CONVERSATION LOOP
    while True:
        print("Press 'F' to record audio | 'R' to reset memory database | 'Q' to exit program")
        user_input = ''
        if keyboard.read_key().lower() == 'f':
            print("Recording... (press F to stop recording)")
            wav_filename = speech_to_text.record_audio('f')
            user_input = speech_to_text.transcribe_audio(wav_filename)
        elif keyboard.read_key().lower() == 'q':
            print("Exited program.")
            break
        elif keyboard.read_key().lower() == 'r':
            reset_history()
            print("You have reset Clara's SQL and short-term memory database")

        try:
            response = chain.invoke({
                "history": chat_history,
                "user_input": user_input
            })

            print(f"You said: {user_input}")
            print(f"Clara: {response.content}")

            # save both sides of conversation in current session
            chat_history.append(HumanMessage(content=user_input))
            chat_history.append(AIMessage(content=response.content))

            # permanently save both messages to SQLite database
            save_message("human", user_input)
            save_message("ai", response.content)

        except Exception as error:
            print(f"\nSomething went wrong: {error}")


if __name__ == "__main__":
    main()

