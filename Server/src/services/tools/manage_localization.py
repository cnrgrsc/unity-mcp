"""
manage_localization.py - Localization Tool

Handles:
- String tables
- Locale management
- Localized strings
- Asset localization
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_localization."""
    return {
        "name": "manage_localization",
        "description": """Manage Unity Localization for multi-language support.

Actions:
- locale_list: List available locales
- locale_add: Add a new locale
- locale_set_default: Set default locale
- table_create: Create a string table
- table_get_entries: Get string table entries
- entry_add: Add entry to string table
- entry_update: Update existing entry
- entry_remove: Remove entry from table
- asset_localize: Localize an asset for a locale
- get_localized_string: Get string for specific locale

Note: Requires Localization package (com.unity.localization).""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "locale_list",
                        "locale_add",
                        "locale_set_default",
                        "table_create",
                        "table_get_entries",
                        "entry_add",
                        "entry_update",
                        "entry_remove",
                        "asset_localize",
                        "get_localized_string"
                    ]
                },
                "localeCode": {
                    "type": "string",
                    "description": "Locale code (e.g., 'en', 'tr', 'de', 'fr', 'es', 'ja', 'zh')"
                },
                "tableName": {
                    "type": "string",
                    "description": "Name of the string table"
                },
                "tableCollectionName": {
                    "type": "string",
                    "description": "Name of the table collection"
                },
                "entryKey": {
                    "type": "string",
                    "description": "Key for the string entry"
                },
                "entryValue": {
                    "type": "string",
                    "description": "Localized string value"
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path to asset for localization"
                },
                "localizedAssetPath": {
                    "type": "string",
                    "description": "Path to localized version of asset"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_localization tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "localeCode", "tableName", "tableCollectionName", "entryKey",
        "entryValue", "assetPath", "localizedAssetPath"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_localization",
        "params": params
    })
