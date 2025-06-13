import os
from google.adk.agents import Agent
from google.adk.tools import google_search, agent_tool
from google.adk.tools.mcp_tool.mcp_toolset import MCPToolset, SseServerParams


MODEL_GEMINI_PRO = os.getenv("MODEL_GEMINI_PRO")


class RootAgent:
    def __init__(self):
        self.agent = None
        self.create_agent()

    def create_agent(self):
        toolset = MCPToolset(
            connection_params=SseServerParams(url="http://localhost:8080/sse"),
        )

        Agent_Search = Agent(
            model=MODEL_GEMINI_PRO,
            name="SearchAgent",
            instruction="""
            You're a specialist in Google Search
            """,
            tools=[google_search],
        )

        Agent_Calculator = Agent(
            model=MODEL_GEMINI_PRO,
            name="CalculatorAgent",
            instruction="""
            You're a specialist in calculating mathematical expressions.
            """,
            tools=[toolset],
        )

        self.agent = Agent(
            name="AgentOrchestrator",
            model=MODEL_GEMINI_PRO,
            description="Agent responsible for orchestrating multiple agents.",
            instruction="You're an agent orchestrator, you can use your agents to get to the end result and serve the user.",
            # sub_agents=[
            #     Agent_Calculator,
            # ],
            # https://github.com/google/adk-python/issues/53#issuecomment-2798906767
            # tools=[agent_tool.AgentTool(agent=Agent_Search)],
            tools=[
                agent_tool.AgentTool(agent=Agent_Search),
                agent_tool.AgentTool(agent=Agent_Calculator),
            ],  # chainlit works
        )


root_agent = RootAgent().agent
