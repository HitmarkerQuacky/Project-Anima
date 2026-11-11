from dotenv import load_dotenv 
from langchain_ollama import ChatOllama
from langchain_core.messages import BaseMessage, HumanMessage, AIMessage
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder, FewShotChatMessagePromptTemplate
from database import initialize_database, save_message, load_history, reset_history
from tts_client import speak
import keyboard, wave, emoji
import speech_to_text
import char_personality

# Configure the AI model
model = ChatOllama(
    model="clara-qwen",
    temperature=0.8,
    max_retries=2
)

# Define the character
system_prompt = char_personality.character_profile

# Define the format for each example conversation
example_prompt = ChatPromptTemplate.from_messages([
    ("human", "{input}"),
    ("ai", "{output}")
])

# Convert the examples into actual chat messages
few_shot_prompt = FewShotChatMessagePromptTemplate(
    examples=char_personality.example_dialogue,
    example_prompt=example_prompt
)

# Store the conversation for the current session
prompt = ChatPromptTemplate.from_messages([
    ("system", system_prompt),
    few_shot_prompt,
    MessagesPlaceholder(variable_name="history"),
    ("human", "{user_input}"),
])

chain = prompt | model

initialize_database()
chat_history: list[BaseMessage] = load_history()


def main():
    print("AI Roleplay Bot")
    print("Type 'quit' to exit.\n")

    # generate opening greeting ONLY if database is empty
    if not chat_history:
        try:
            response = chain.invoke({
                "history": [],
                "user_input": "Start the conversation."
            })
            print(f"\nClara: {response.content.strip()}\n")
            raw_text = emoji.replace_emoji(response.content, replace="").strip()
            raw_text = " ".join(raw_text.split())
            speak(raw_text)

            # keeps the greeting but not the start up instruction
            chat_history.append(AIMessage(content=response.content))
            save_message("ai", response.content)

        except Exception as error:
            print(f"\nSomething went wrong: {error}\n")
            return
    else:
        # DB already has history - prints last message
        last_msg = chat_history[-1].content
        print(f"\n[Loaded existing memory: {len(last_msg)} messages]")

    # MAIN CONVERSATION LOOP
    while True:
        # print("Press 'F' to record audio | 'R' to reset memory database | 'Q' to exit program")
        # user_input = ''
        # if keyboard.read_key().lower() == 'f':
        #     print("Recording... (press F to stop recording)")
        #     wav_filename = speech_to_text.record_audio('f')
        #     user_input = speech_to_text.transcribe_audio(wav_filename)
        # elif keyboard.read_key().lower() == 'q':
        #     print("Exited program.")
        #     break
        # elif keyboard.read_key().lower() == 'r':
        #     reset_history()
        #     print("You have reset Clara's SQL and short-term memory database")

        # IF JUST TYPING NOT USING MICROPHONE
        user_input = input("You: ").strip()
        if user_input.lower() in ["quit", "exit"]:
            print("Exited program.")
            break
        if not user_input:
            continue

        try:
            response = chain.invoke({
                "history": chat_history,
                "user_input": user_input
            })

            #print(f"You said: {user_input}")
            print(f"Clara: {response.content.strip()}")
            raw_text = emoji.replace_emoji(response.content, replace="").strip()
            raw_text = " ".join(raw_text.split())
            speak(raw_text)

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

