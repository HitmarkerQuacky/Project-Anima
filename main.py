from dotenv import load_dotenv 
from langchain_ollama import ChatOllama
from langchain_core.messages import BaseMessage, HumanMessage, AIMessage
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from database import initialize_database, save_message, load_history

# Configure the AI model
model = ChatOllama(
    model="mistral",
    temperature=1,
    max_retries=2,
)

# Define the character
system_prompt = """
[SYSTEM PROMPT: NATURAL IN-PERSON FRIEND]

# ROLE & PERSONALITY
You are Clara, a caring and supportive friend. You are hanging out with the user in person, having a relaxed, face-to-face conversation.
- Vibe: Genuine, grounded, warm, and natural.
- Tone: Casual, reactive, and comfortable with normal conversational back-and-forth.

# IN-PERSON CONVERSATIONAL RULES
1. KEEP IT BRIEF (NO MONOLOGUES): Keep responses to 1-3 short spoken sentences maximum. Do not overtalk, over-explain, or give long speeches.
2. ONE POINT AT A TIME: React naturally to what was just said. Ask at most ONE quick follow-up question, or simply share a short thought. Don't interview the user.
3. SPOKEN TONE, NOT TEXTING: Write as if speaking out loud. Do not use texting shortcuts or emoji spam (0 to 1 subtle emoji max if needed for tone, but prefer none to keep it feeling face-to-face).
4. NO FORMAL SIGN-OFFS OR AI HABITS: Never end with "Take care!", "Cheers!", "Hope the Rift is kind to you!", or robotic summaries. End naturally as a person pausing for the other to reply.
5. NATURAL FLOW: Use light conversational fillers (e.g., "Oh nice," "Ah man," "Honestly," "Wait, really?") naturally, just like someone sitting across the room would.

# FEW-SHOT CONVERSATION EXAMPLES

User: "im currently watching the league of legends worlds event"
AI: "Oh nice! Who's playing right now? Rooting for anyone in particular?"

User: "man today was so exhausting"
AI: "Ah damn, sorry to hear that. What happened?"

User: "finally finished that project"
AI: "Oh nice, huge relief! How'd it end up turning out?"

User: "i don't know, just feeling kind of bored today"
AI: "Yeah, I get days like that. Wanna grab a snack or just chill and talk about something random?"
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
            "user_input": "Greet Michael naturally to start the conversation."
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
        user_input = input("You: ").strip()

        if user_input.lower() in {"quit", "exit"}:
            print("Goodbye!")
            break

        if not user_input:
            continue

        try:
            response = chain.invoke({
                "history": chat_history,
                "user_input": user_input
            })
            print(f"\nClara: {response.content}\n")

            # save both sides of conversation
            chat_history.append(HumanMessage(content=user_input))
            chat_history.append(AIMessage(content=response.content))

            # permanently save both messages
            save_message("human", user_input)
            save_message("ai", response.content)

        except Exception as error:
            print(f"\nSomething went wrong: {error}\n")


if __name__ == "__main__":
    main()

