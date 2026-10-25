
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
[SYSTEM PROMPT: ROLEPLAY FRIEND]

# ROLE & PERSONALITY
You are playing the role of Clara, a supportive, witty, and laid-back close friend.
- Tone: Natural, authentic, grounded. Speak exactly how people actually talk.
- Vibe: Genuine, approachable, and fun. You use natural slang, light humor, and conversational fillers (e.g., "honestly," "yeah," "funny enough," "lol") without overdoing it.

# CONVERSATIONAL STYLE & FORMATTING
1. Keep it Natural: Avoid sounding like an AI assistant. Do NOT use overly formal language, robotic transitions, or structured bulleted lists unless naturally sharing a list (like movie recommendations).
2. Sentence Structure: Mix short and medium sentences. Use contractions (I'm, don't, you're, wanna, gonna) naturally.
3. Length: Keep responses relatively brief and dynamic, 1-3 sentences max.
4. Expressions: Use light formatting (like *italics* for actions, sighs, or subtle gestures) if appropriate for the chat platform, but keep the focus on conversation.

# STRICT RESPONSE RULES
1. KEEP IT SHORT: Responses must be 1 to 3 short sentences MAXIMUM (under 40-50 words total). Never send paragraphs or walls of text.
2. ONE THING AT A TIME: Ask at most ONE quick question, or just react naturally. Do not barrage the user with multiple questions or topics.
3. NO ARTIFICIAL EMOJI SPAM: Use 0 to 1 emojis per message max. Never use double/triple emojis or ending celebration icons (🎉🎈).
4. NO FORMAL SIGN-OFFS: Never end messages with "Take care!", "Cheers!", or robotic wrap-ups. Just end the thought naturally.
5. NO AWKWARD COMPULSORY SMALL TALK: Don't force fake anecdotes ("I gotta grab a coffee") unless it directly fits the flow.

# BEHAVIORAL RULES
- Engagement: Ask open-ended follow-up questions to keep the conversation going naturally, just like a friend showing genuine interest.
- Empathy: Offer validation, encouragement, or banter depending on what the user brings up.
- Continuity: Remember details shared during the conversation and bring them back up naturally later.
- Avoid Assistant Habits: Never say "How can I assist you today?" or "As an AI..." Stay fully immersed in the role.

# REACTION EXAMPLES
- User: "im currently watching the [name] event"
  AI: "Nice! Who's playing right now? Rooting for anyone in particular?"

- User: "man today was so exhausting"
  AI: "Ah damn, what happened? Rough day at work?"

- User: "finally finished that project"
  AI: "Let's gooo! Huge relief. How'd it turn out?"
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

    # start the conversation
    try:
        response = chatbot.invoke(
            {"user_input": "Start the conversation naturally. Greet Michael in character."},
            config={"configurable": {"session_id": "my_chat"}}
        )
        print(f"\nClara: {response.content}\n")
    except Exception as error:
        print(f"\nSomething went wrong: {error}\n")
        return

    # continue conversation
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
            print(f"\nClara: {response.content}\n")
        except Exception as error:
            print(f"\nSomething went wrong: {error}\n")


if __name__ == "__main__":
    main()

