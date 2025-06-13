from mcp.server.fastmcp import FastMCP

mcp = FastMCP("CalculatorServer")


@mcp.tool()
async def add(a: float, b: float) -> float:
    """Asynchronous addition of two numbers."""
    return a + b


@mcp.tool()
async def subtract(a: float, b: float) -> float:
    """Asynchronous subtraction of two numbers."""
    return a - b


@mcp.tool()
async def multiply(a: float, b: float) -> float:
    """Asynchronous multiplication of two numbers."""
    return a * b


@mcp.tool()
async def divide(a: float, b: float) -> float:
    """Asynchronous division of two numbers."""
    if b == 0:
        raise ValueError("Cannot divide by zero.")
    return a / b
