import json
from google.genai import types
import chainlit as cl
from google.adk.artifacts import InMemoryArtifactService
from google.adk.sessions import InMemorySessionService
from google.adk.runners import Runner
from dotenv import load_dotenv
from root_agent.agent import root_agent  # Import your agent class

load_dotenv()

session_service = InMemorySessionService()
artifacts_service = InMemoryArtifactService()

APP_NAME = "tutorial_app"
USER_ID = "user_1"
SESSION_ID = "session_001"

runner = None
session = None


async def call_agent_async(query: str, runner: Runner, session_id: str, user_id: str):
    """Sends a query to the agent and prints the final response."""
    print(f"\n>>> User Query: {query}")

    # Prepare the user's message in ADK format
    content = types.Content(role="user", parts=[types.Part(text=query)])

    final_response_text = "Agent did not produce a final response."

    async for event in runner.run_async(
        user_id=user_id, session_id=session_id, new_message=content
    ):
        # print(f"  [Event] Author: {event.author}, Type: {type(event).__name__}, Final: {event.is_final_response()}, Content: {event.content}")

        if event.is_final_response():
            if event.content and event.content.parts:
                # Assuming text response in the first part
                final_response_text = event.content.parts[0].text
            elif (
                event.actions and event.actions.escalate
            ):  # Handle potential errors/escalations
                final_response_text = (
                    f"Agent escalated: {event.error_message or 'No specific message.'}"
                )
            # Add more checks here if needed (e.g., specific error codes)
            break  # Stop processing events once the final response is found

    final_response = f"<<< ✅ Agent Response: {final_response_text}"
    print(final_response)

    return final_response


@cl.on_chat_start
async def on_chat_start():
    """Initialize the agent when the chat starts"""

    global runner, session

    cl.user_session.set("history", [])
    await cl.Message(
        content="Hello! I am support Agent. How can I help you today?"
    ).send()

    try:
        session = await session_service.create_session(
            app_name=APP_NAME, user_id=USER_ID, session_id=SESSION_ID
        )

        runner = Runner(
            agent=root_agent,  # The agent we want to run
            app_name=APP_NAME,  # Associates runs with our app
            artifact_service=artifacts_service,  # Uses our artifact manager
            session_service=session_service,  # Uses our session manager
        )
    except Exception as e:
        print(f"Error initializing agent: {e}")


@cl.on_message
async def on_message(message: cl.Message):
    """Handle incoming messages"""

    global runner, USER_ID, SESSION_ID

    history = cl.user_session.get("history")

    history.append({"role": "user", "content": message.content})

    # Process the message with the agent
    response = await call_agent_async(
        query=message.content, runner=runner, user_id=USER_ID, session_id=SESSION_ID
    )

    # Send the response back to the user
    await cl.Message(content=response).send()

    history.append({"role": "assistant", "content": response})

    cl.user_session.set("history", history)


@cl.on_chat_end
async def on_chat_end():
    history = cl.user_session.get("history") or []
    with open("chat_history.json", "w") as f:
        json.dump(history, f, indent=2)
    print("Chat history saved.")
