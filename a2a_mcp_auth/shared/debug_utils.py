"""Debug utilities for A2A MCP services."""

import json
import logging
import sys
import time
from datetime import datetime
from functools import wraps
from typing import Any, Dict, Optional

from fastapi import Request
from starlette.middleware.base import BaseHTTPMiddleware

from shared.config import settings

# Create debug loggers
auth_debug_logger = logging.getLogger("debug.auth")
a2a_debug_logger = logging.getLogger("debug.a2a")
mcp_debug_logger = logging.getLogger("debug.mcp")
requests_debug_logger = logging.getLogger("debug.requests")


def setup_debug_logging():
    """Setup debug logging configuration."""
    log_level = getattr(logging, settings.log_level.upper(), logging.INFO)
    
    # Configure root logger
    logging.basicConfig(
        level=log_level,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.StreamHandler(sys.stdout),
            logging.FileHandler('debug.log') if settings.debug else logging.NullHandler()
        ]
    )
    
    # Set debug loggers to DEBUG level if debug flags are enabled
    if settings.debug_auth:
        auth_debug_logger.setLevel(logging.DEBUG)
        auth_debug_logger.debug("Authentication debug logging enabled")
    
    if settings.debug_a2a:
        a2a_debug_logger.setLevel(logging.DEBUG)
        a2a_debug_logger.debug("A2A debug logging enabled")
    
    if settings.debug_mcp:
        mcp_debug_logger.setLevel(logging.DEBUG)
        mcp_debug_logger.debug("MCP debug logging enabled")
    
    if settings.debug_requests:
        requests_debug_logger.setLevel(logging.DEBUG)
        requests_debug_logger.debug("HTTP requests debug logging enabled")


def debug_function(category: str = "general"):
    """Decorator to add debug logging to functions."""
    def decorator(func):
        @wraps(func)
        async def async_wrapper(*args, **kwargs):
            logger = logging.getLogger(f"debug.{category}")
            if not logger.isEnabledFor(logging.DEBUG):
                return await func(*args, **kwargs)
            
            start_time = time.time()
            logger.debug(f"Entering {func.__name__} with args={args}, kwargs={kwargs}")
            
            try:
                result = await func(*args, **kwargs)
                elapsed = time.time() - start_time
                logger.debug(f"COMPLETED {func.__name__} in {elapsed:.3f}s")
                return result
            except Exception as e:
                elapsed = time.time() - start_time
                logger.debug(f"FAILED {func.__name__} after {elapsed:.3f}s: {str(e)}")
                raise
        
        @wraps(func)
        def sync_wrapper(*args, **kwargs):
            logger = logging.getLogger(f"debug.{category}")
            if not logger.isEnabledFor(logging.DEBUG):
                return func(*args, **kwargs)
            
            start_time = time.time()
            logger.debug(f"Entering {func.__name__} with args={args}, kwargs={kwargs}")
            
            try:
                result = func(*args, **kwargs)
                elapsed = time.time() - start_time
                logger.debug(f"COMPLETED {func.__name__} in {elapsed:.3f}s")
                return result
            except Exception as e:
                elapsed = time.time() - start_time
                logger.debug(f"FAILED {func.__name__} after {elapsed:.3f}s: {str(e)}")
                raise
        
        return async_wrapper if hasattr(func, '__code__') and func.__code__.co_flags & 0x80 else sync_wrapper
    return decorator


class DebugMiddleware(BaseHTTPMiddleware):
    """Middleware to log HTTP requests and responses for debugging."""
    
    async def dispatch(self, request: Request, call_next):
        if not settings.debug_requests:
            return await call_next(request)
        
        start_time = time.time()
        
        # Log request
        request_id = id(request)
        requests_debug_logger.debug(f"[{request_id}] {request.method} {request.url}")
        requests_debug_logger.debug(f"[{request_id}] Headers: {dict(request.headers)}")
        
        # Try to log request body
        try:
            if request.method in ["POST", "PUT", "PATCH"]:
                body = await request.body()
                if body:
                    try:
                        json_body = json.loads(body)
                        requests_debug_logger.debug(f"[{request_id}] Body: {json_body}")
                    except json.JSONDecodeError:
                        requests_debug_logger.debug(f"[{request_id}] Body: {body.decode()[:500]}...")
                
                # Recreate request for the next middleware
                async def receive():
                    return {"type": "http.request", "body": body}
                request._receive = receive
        except Exception as e:
            requests_debug_logger.debug(f"[{request_id}] Could not read body: {str(e)}")
        
        # Process request
        response = await call_next(request)
        
        # Log response
        elapsed = time.time() - start_time
        requests_debug_logger.debug(f"[{request_id}] {response.status_code} in {elapsed:.3f}s")
        
        return response


def log_auth_attempt(username: str, success: bool, details: Optional[Dict[str, Any]] = None):
    """Log authentication attempts."""
    if not settings.debug_auth:
        return
    
    status = "SUCCESS" if success else "FAILED"
    auth_debug_logger.debug(f"Auth attempt: {username} - {status}")
    
    if details:
        auth_debug_logger.debug(f"Auth Details: {details}")


def log_a2a_communication(direction: str, service: str, message: Dict[str, Any]):
    """Log A2A communication."""
    if not settings.debug_a2a:
        return
    
    arrow = "OUT" if direction == "outgoing" else "IN"
    a2a_debug_logger.debug(f"A2A {arrow} {direction} to/from {service}")
    a2a_debug_logger.debug(f"A2A Message: {message}")


def log_mcp_call(tool_name: str, args: Dict[str, Any], result: Any = None, error: Optional[str] = None):
    """Log MCP tool calls."""
    if not settings.debug_mcp:
        return
    
    mcp_debug_logger.debug(f"MCP call: {tool_name}")
    mcp_debug_logger.debug(f"MCP Args: {args}")
    
    if result is not None:
        mcp_debug_logger.debug(f"MCP Result: {result}")
    
    if error:
        mcp_debug_logger.debug(f"MCP Error: {error}")


def log_token_validation(token_preview: str, claims: Optional[Dict[str, Any]] = None, error: Optional[str] = None):
    """Log JWT token validation."""
    if not settings.debug_auth:
        return
    
    # Only show first/last few characters of token for security
    safe_token = f"{token_preview[:10]}...{token_preview[-10:]}" if len(token_preview) > 20 else "***"
    
    if error:
        auth_debug_logger.debug(f"[TOKEN] Token validation FAILED: {safe_token} - {error}")
    else:
        auth_debug_logger.debug(f"[TOKEN] Token validation SUCCESS: {safe_token}")
        if claims:
            safe_claims = {k: v for k, v in claims.items() if k not in ['aud', 'iss', 'sub']}
            auth_debug_logger.debug(f"[TOKEN] Claims: {safe_claims}")


def create_debug_endpoint():
    """Create debug information endpoint."""
    def debug_info():
        return {
            "debug_mode": settings.debug,
            "log_level": settings.log_level,
            "debug_flags": {
                "auth": settings.debug_auth,
                "a2a": settings.debug_a2a,
                "mcp": settings.debug_mcp,
                "requests": settings.debug_requests
            },
            "timestamp": datetime.now().isoformat(),
            "loggers": {
                "root": logging.getLogger().level,
                "auth": auth_debug_logger.level,
                "a2a": a2a_debug_logger.level,
                "mcp": mcp_debug_logger.level,
                "requests": requests_debug_logger.level
            }
        }
    return debug_info
