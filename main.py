
import os

from dotenv import load_dotenv
#from langchain_mistralai import 
from langchain_ollama import ChatOllama
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.chat_history import InMemoryChatMessageHistory
from langchain_core.runnables.history import RunnableWithMessageHistory

# Load the API key from .env
# load_dotenv()

# api_key = os.getenv("MISTRAL_API_KEY")

# if not api_key:
#     raise ValueError(
#         "MISTRAL_API_KEY was not found. Check your .env file."
#     )

# Configure the AI model
model = ChatOllama(
    model="mistral",
    temperature=1,
    max_retries=2,
)

# Define the character
system_prompt = """
    You are Clara and the user is Michael, here is a detailed description about you:

    Name: Clara
    Age: 23
    Occupation: University student-visual arts major
    Personality: cute, introverted, caring, awkward, clingy
    Likes: anime, video games, mango pudding, ice cream, stir fry dishes, scented candles, cosplay
    Dislikes: rude people, crowded spaces, pickles, embarrassment
    Appearance: black shoulder length hair, brown eyes, slightly curvy body, wears glasses, a sweater, checkered red and black plaid skirt, black leggings, winter boots

    Clara prefers staying indoors and loves reading book and playing video games, Clara also gets very clingy to people close to her.
    She has been thinking of becoming more confident in herself and try new things as of recent.

    Only say name when meeting or introducing yourself.
    Can agree or disagree with the user's statements or actions.
    Responses are less than 1 paragraph.
    Can use emoticons.
    Do not send long lists.
"""

# Store the conversation for the current session
prompt = ChatPromptTemplate.from_messages([
    ("system", system_prompt),
    MessagesPlaceholder(variable_name="history"),
    ("human", "{user_input}"),
])

chain = prompt | model

chat_histories = {}

def get_history(session_id):
    if session_id not in chat_histories:
        chat_histories[session_id] = InMemoryChatMessageHistory()
    return chat_histories[session_id]

chatbot = RunnableWithMessageHistory(
    chain,
    get_history,
    input_messages_key="user_input",
    history_messages_key="history"
)


def main():
    print("AI Roleplay Bot")
    print("Type 'quit' to exit.\n")

    while True:
        user_input = input("You: ").strip()

        if user_input.lower() in {"quit", "exit"}:
            print("Goodbye!")
            break

        if not user_input:
            continue

        try:
            response = chatbot.invoke(
                {"user_input": user_input},
                config={"configurable": {"session_id": "my_chat"}}
            )
            print(f"\n{response.content}\n")
        except Exception as error:
            print(f"\nSomething went wrong: {error}\n")


if __name__ == "__main__":
    main()

