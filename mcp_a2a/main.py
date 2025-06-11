from starlette.applications import Starlette
from starlette.responses import JSONResponse
from starlette.routing import Route
from chainlit_app import app

async def homepage(request):
    """Homepage endpoint"""
    return JSONResponse({
        "message": "Hello from mcp-a2a!",
        "framework": "Starlette",
        "status": "running"
    })


async def health(request):
    """Health check endpoint"""
    return JSONResponse({"status": "healthy"})


# Define routes
routes = [
    Route("/", homepage),
    Route("/health", health),
]

# Create Starlette application
app = Starlette(debug=True, app=app, routes=routes)


def main():
    """Run the application with uvicorn"""
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)


if __name__ == "__main__":
    main()
