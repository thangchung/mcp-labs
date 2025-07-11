import argparse
import logging
import uvicorn
from starlette.applications import Starlette
from starlette.requests import Request
from starlette.routing import Mount, Route

from mcp.server.sse import SseServerTransport
from weather_tools import mcp

# ─────────────────────────────────────────────
# Logging Configuration
# ─────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger("weather-mcp-server")


# ─────────────────────────────────────────────
# Starlette App Factory
# ─────────────────────────────────────────────
def create_starlette_app():
    """
    Create a Starlette app wired with the Weather MCP server over SSE transport.
    """
    logger.debug("Creating Starlette app with SSE transport for Weather MCP")
    sse = SseServerTransport("/messages/")

    async def handle_sse(request: Request) -> None:
        logger.info("🌤️  Weather MCP SSE connection established")
        async with sse.connect_sse(request.scope, request.receive, request._send) as (
            read_stream,
            write_stream,
        ):
            logger.debug("Starting Weather MCP server run loop over SSE")
            await mcp._mcp_server.run(
                read_stream,
                write_stream,
                mcp._mcp_server.create_initialization_options(),
            )
            logger.debug("Weather MCP run loop completed")

    return Starlette(
        debug=False,
        routes=[
            Route("/sse", endpoint=handle_sse),
            Mount("/messages/", app=sse.handle_post_message),
        ],
    )


# ─────────────────────────────────────────────
# CLI Entrypoint
# ─────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(description="Run a Weather MCP tool server.")
    parser.add_argument(
        "--server_type",
        choices=["stdio", "sse"],
        default="stdio",
        help="Transport to use",
    )
    parser.add_argument("--host", default="0.0.0.0", help="Host to bind to (SSE only)")
    parser.add_argument(
        "--port", type=int, default=3002, help="Port to bind to (SSE only)"
    )
    args = parser.parse_args()

    logger.info(f"🌤️  Starting Weather MCP server in {args.server_type.upper()} mode")

    if args.server_type == "stdio":
        mcp.run("stdio")
    else:
        app = create_starlette_app()
        logger.info(f"🌐 Running Weather MCP SSE server on {args.host}:{args.port}")
        uvicorn.run(app, host=args.host, port=args.port)


if __name__ == "__main__":
    main()