# Introduction

A conversational agent over unstructured documents with Chainlit on AzureOpenAI

# Get starting

```sh
# echo .env
AZURE_OPENAI_API_KEY=<your key>
AZURE_OPENAI_ENDPOINT=https://<your project>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_API_VERSION=2024-08-01-preview
```

```sh
# Create a virtual environment
python -m venv .venv

# Activate the virtual environment 
# On Windows:
.venv\Scripts\activate
# On macOS/Linux:
source .venv/bin/activate

# Install dependencies
uv pip install -r requirements.txt

# Initialize Dapr
dapr init
```


```sh
dapr run --app-id doc-agent --resources-path ./components -- chainlit run app.py -w
```

Access to [`http://localhost:8000`](http://localhost:8000), and play around.

## Troubleshooting

If you get `ImportError: libGL.so.1: cannot open shared object file: No such file or directory`, then run:

```sh
sudo apt update
sudo apt install libgl1
```

## Refenrences

- https://github.com/dapr/dapr-agents/blob/8741289e7dd484102392d5e36526099977e2b3c3/quickstarts/06-document-agent-chainlit/README.md
