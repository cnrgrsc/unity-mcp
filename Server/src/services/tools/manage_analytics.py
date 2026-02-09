"""
manage_analytics.py - Analytics & Tracking Tool

Handles:
- Unity Analytics
- Firebase Analytics
- Custom events
- User properties
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_analytics."""
    return {
        "name": "manage_analytics",
        "description": """Manage Unity Analytics, Firebase Analytics, and custom event tracking.

Actions:
- initialize: Initialize analytics system
- send_event: Send custom analytics event
- set_user_property: Set user property
- set_user_id: Set user ID for tracking
- enable: Enable analytics
- disable: Disable analytics
- flush: Flush pending events
- get_settings: Get current analytics settings
- firebase_initialize: Initialize Firebase Analytics
- firebase_log_event: Log Firebase event
- firebase_set_screen: Set current screen name
- session_start: Start analytics session
- session_end: End analytics session
- revenue_event: Track revenue/purchase event
- level_event: Track level start/complete
- tutorial_event: Track tutorial progress""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "initialize",
                        "send_event",
                        "set_user_property",
                        "set_user_id",
                        "enable",
                        "disable",
                        "flush",
                        "get_settings",
                        "firebase_initialize",
                        "firebase_log_event",
                        "firebase_set_screen",
                        "session_start",
                        "session_end",
                        "revenue_event",
                        "level_event",
                        "tutorial_event"
                    ]
                },
                "eventName": {
                    "type": "string",
                    "description": "Name of the analytics event"
                },
                "eventParams": {
                    "type": "object",
                    "description": "Event parameters as key-value pairs"
                },
                "propertyName": {
                    "type": "string",
                    "description": "User property name"
                },
                "propertyValue": {
                    "type": "string",
                    "description": "User property value"
                },
                "userId": {
                    "type": "string",
                    "description": "User ID for tracking"
                },
                "screenName": {
                    "type": "string",
                    "description": "Screen name for tracking"
                },
                "screenClass": {
                    "type": "string",
                    "description": "Screen class for tracking"
                },
                "revenue": {
                    "type": "number",
                    "description": "Revenue amount"
                },
                "currency": {
                    "type": "string",
                    "description": "Currency code (e.g., USD, EUR)"
                },
                "productId": {
                    "type": "string",
                    "description": "Product ID for revenue tracking"
                },
                "levelName": {
                    "type": "string",
                    "description": "Level name"
                },
                "levelIndex": {
                    "type": "integer",
                    "description": "Level index/number"
                },
                "success": {
                    "type": "boolean",
                    "description": "Whether level/tutorial was successful"
                },
                "tutorialStep": {
                    "type": "string",
                    "description": "Tutorial step name"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_analytics tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "eventName", "eventParams", "propertyName", "propertyValue",
        "userId", "screenName", "screenClass", "revenue", "currency",
        "productId", "levelName", "levelIndex", "success", "tutorialStep"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_analytics",
        "params": params
    })
