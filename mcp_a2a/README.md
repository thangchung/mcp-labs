# MCP A2A - Starlette Web Application

A simple web application built with [Starlette](https://www.starlette.io/) framework using [uv](https://github.com/astral-sh/uv) for Python project management.

## Getting Started

### Prerequisites

- Python 3.12 or higher
- `uv` package manager (already available in this dev container)

### Installation

#### Option 1: Using uv (Recommended)
1. **Install dependencies:**
   ```bash
   uv sync
   ```

2. **Activate the virtual environment (optional):**
   ```bash
   source .venv/bin/activate
   ```

#### Option 2: Using pip (Alternative)
If you encounter issues with `uv sync`, you can use pip as an alternative:

1. **Create a virtual environment:**
   ```bash
   python -m venv .venv
   source .venv/bin/activate  # On Windows: .venv\Scripts\activate
   ```

2. **Install dependencies:**
   ```bash
   pip install chainlit google-adk starlette uvicorn[standard]
   ```

**Note:** The `google-adk` package (Google Application Development Kit) has been successfully added! This package provides tools for Google Cloud Platform development including AI Platform, Storage, Speech, and other Google services.

### Running the Application

#### Option 1: Direct execution
```bash
python main.py
chainlit run chainlit_app.py
```

#### Option 2: Using uv
```bash
uv run python main.py
```

#### Option 3: Using uvicorn directly
```bash
uv run uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

The application will start on `http://0.0.0.0:8000`

### Available Endpoints

- **GET /** - Homepage with welcome message
- **GET /health** - Health check endpoint

### Example Requests

```bash
# Homepage
curl http://localhost:8000/

# Expected response:
# {"message":"Hello from mcp-a2a!","framework":"Starlette","status":"running"}

# Health check
curl http://localhost:8000/health

# Expected response:
# {"status":"healthy"}
```

### Testing the Application

You can test that the application is working by:

1. **Starting the server** (using any of the methods above)
2. **Opening another terminal** and running the curl commands shown in the examples
3. **Opening in browser** - navigate to `http://localhost:8000` to see the JSON response

### Project Structure

```
mcp_a2a/
├── .devcontainer/          # Dev container configuration
├── .venv/                  # Virtual environment (created after installation)
├── .editorconfig          # Editor configuration
├── .gitignore             # Git ignore rules for Python projects
├── .python-version        # Python version specification
├── main.py               # Main application file
├── pyproject.toml        # Project configuration and dependencies
├── requirements.txt      # Alternative pip requirements file
├── uv.lock              # Lock file for reproducible builds
└── README.md            # This file
```

### Development

The application runs in debug mode with auto-reload enabled, so any changes to the code will automatically restart the server.

### Dependencies

- **Chainlit** (>=2.5.0) - Framework for building conversational AI interfaces
- **Google ADK** (>=1.2.0) - Google Application Development Kit for Google Cloud Platform services
- **Starlette** (>=0.37.0) - Lightweight ASGI framework for building async web services
- **Uvicorn** (>=0.24.0) - Lightning-fast ASGI server implementation

### Browser Access

If you're running this in a dev container or remote environment, you can open the application in your host browser using:

```bash
"$BROWSER" http://localhost:8000
```

## Next Steps

### Google ADK Integration
With `google-adk` now installed, you can integrate Google Cloud Platform services:

```python
# Example: Using Google ADK (add to main.py)
from google_adk import GoogleADK

# Initialize Google ADK client
# gcp_client = GoogleADK()

# Available services include:
# - Google AI Platform
# - Google Cloud Storage  
# - Google Cloud Speech
# - Google Cloud Secret Manager
# - And many more Google services
```

### General Development
- Add more routes and endpoints
- Implement middleware for logging, CORS, etc.
- Add database integration
- Implement authentication
- Add API documentation with OpenAPI/Swagger
- Integrate Chainlit for conversational AI interfaces
