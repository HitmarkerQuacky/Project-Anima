import sqlite3
from langchain_core.messages import HumanMessage, AIMessage

DB_PATH = "clara.db"


def initialize_database():
    """Create the messages table if it doesn't exist."""
    with sqlite3.connect(DB_PATH) as connection:
        connection.execute("""
            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)


def save_message(role, content):
    """Save one message to the database."""
    with sqlite3.connect(DB_PATH) as connection:
        connection.execute(
            "INSERT INTO messages (role, content) VALUES (?, ?)",
            (role, content)
        )


def load_history():
    """Load saved messages as LangChain message objects."""
    with sqlite3.connect(DB_PATH) as connection:
        rows = connection.execute("""
            SELECT role, content
            FROM messages
            ORDER BY id
        """).fetchall()

    history = []

    for role, content in rows:
        if role == "human":
            history.append(HumanMessage(content=content))
        elif role == "ai":
            history.append(AIMessage(content=content))

    return history


def reset_history():
    with sqlite3.connect(DB_PATH) as connection:
        connection.execute("DELETE FROM messages")