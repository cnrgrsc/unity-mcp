"""
manage_cloud.py - Cloud Services Tool

Handles:
- Unity Cloud Save
- Remote Config
- Unity Authentication
- Cloud Code
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_cloud."""
    return {
        "name": "manage_cloud",
        "description": """Manage Unity Cloud Services including Cloud Save, Remote Config, and Authentication.

Actions:
- auth_initialize: Initialize Unity Authentication
- auth_sign_in_anonymous: Sign in anonymously
- auth_sign_in_email: Sign in with email/password
- auth_sign_up: Sign up new user
- auth_sign_out: Sign out current user
- auth_get_user: Get current user info
- auth_link_account: Link anonymous to email account
- save_initialize: Initialize Cloud Save
- save_data: Save data to cloud
- load_data: Load data from cloud
- delete_data: Delete cloud data
- list_keys: List all saved keys
- config_initialize: Initialize Remote Config
- config_fetch: Fetch remote config
- config_get_value: Get config value
- config_get_all: Get all config values
- config_set_environment: Set config environment
- cloud_code_run: Run cloud code function
- cloud_code_list: List available cloud functions""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "auth_initialize",
                        "auth_sign_in_anonymous",
                        "auth_sign_in_email",
                        "auth_sign_up",
                        "auth_sign_out",
                        "auth_get_user",
                        "auth_link_account",
                        "save_initialize",
                        "save_data",
                        "load_data",
                        "delete_data",
                        "list_keys",
                        "config_initialize",
                        "config_fetch",
                        "config_get_value",
                        "config_get_all",
                        "config_set_environment",
                        "cloud_code_run",
                        "cloud_code_list"
                    ]
                },
                "email": {
                    "type": "string",
                    "description": "User email"
                },
                "password": {
                    "type": "string",
                    "description": "User password"
                },
                "saveKey": {
                    "type": "string",
                    "description": "Key for cloud save data"
                },
                "saveData": {
                    "type": "object",
                    "description": "Data to save (JSON object)"
                },
                "saveType": {
                    "type": "string",
                    "description": "Type of save",
                    "enum": ["Default", "Public", "Private"]
                },
                "configKey": {
                    "type": "string",
                    "description": "Remote config key"
                },
                "configEnvironment": {
                    "type": "string",
                    "description": "Remote config environment",
                    "enum": ["development", "staging", "production"]
                },
                "cloudFunctionName": {
                    "type": "string",
                    "description": "Cloud Code function name"
                },
                "cloudFunctionParams": {
                    "type": "object",
                    "description": "Parameters for cloud function"
                },
                "projectId": {
                    "type": "string",
                    "description": "Unity Project ID"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_cloud tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "email", "password", "saveKey", "saveData", "saveType",
        "configKey", "configEnvironment", "cloudFunctionName",
        "cloudFunctionParams", "projectId"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_cloud",
        "params": params
    })
