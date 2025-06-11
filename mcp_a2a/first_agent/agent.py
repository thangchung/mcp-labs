import os
from google.adk.agents import Agent
from google.adk.tools import google_search


MODEL_GEMINI_PRO = os.getenv("MODEL_GEMINI_PRO")

first_agent = Agent(
    name="first_agent",
    model=MODEL_GEMINI_PRO,
    description="A simple agent to process messages",
    instruction="Respond to user queries about the weather.",
    tools=[google_search],
)

root_agent = first_agent
