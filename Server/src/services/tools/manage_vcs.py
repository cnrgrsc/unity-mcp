"""
manage_vcs.py - Version Control Tool

Handles:
- Git integration
- Plastic SCM / Unity Version Control
- Asset serialization
- Collaboration features
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_vcs."""
    return {
        "name": "manage_vcs",
        "description": """Manage version control integration with Git and Plastic SCM (Unity Version Control).

Actions:
- git_status: Get git status
- git_add: Stage files for commit
- git_commit: Commit staged changes
- git_push: Push to remote
- git_pull: Pull from remote
- git_branch_list: List branches
- git_branch_create: Create new branch
- git_branch_switch: Switch to branch
- git_stash: Stash changes
- git_stash_pop: Pop stashed changes
- git_diff: Show file differences
- git_log: Show commit history
- plastic_status: Get Plastic SCM status
- plastic_checkin: Check in changes
- plastic_checkout: Check out files
- plastic_update: Update workspace
- plastic_create_branch: Create branch
- plastic_switch_branch: Switch branch
- set_serialization: Set asset serialization mode
- set_line_endings: Configure line endings
- generate_gitignore: Generate Unity .gitignore
- set_vcs_mode: Set version control mode""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "git_status",
                        "git_add",
                        "git_commit",
                        "git_push",
                        "git_pull",
                        "git_branch_list",
                        "git_branch_create",
                        "git_branch_switch",
                        "git_stash",
                        "git_stash_pop",
                        "git_diff",
                        "git_log",
                        "plastic_status",
                        "plastic_checkin",
                        "plastic_checkout",
                        "plastic_update",
                        "plastic_create_branch",
                        "plastic_switch_branch",
                        "set_serialization",
                        "set_line_endings",
                        "generate_gitignore",
                        "set_vcs_mode"
                    ]
                },
                "files": {
                    "type": "array",
                    "description": "Files to operate on",
                    "items": {"type": "string"}
                },
                "message": {
                    "type": "string",
                    "description": "Commit message"
                },
                "branchName": {
                    "type": "string",
                    "description": "Branch name"
                },
                "remote": {
                    "type": "string",
                    "description": "Remote name (default: origin)"
                },
                "logCount": {
                    "type": "integer",
                    "description": "Number of log entries to show"
                },
                "serializationMode": {
                    "type": "string",
                    "description": "Asset serialization mode",
                    "enum": ["Mixed", "ForceBinary", "ForceText"]
                },
                "lineEndings": {
                    "type": "string",
                    "description": "Line ending mode",
                    "enum": ["OSNative", "Unix", "Windows"]
                },
                "vcsMode": {
                    "type": "string",
                    "description": "Version control mode",
                    "enum": ["Hidden", "Visible", "Git", "Plastic"]
                },
                "stashMessage": {
                    "type": "string",
                    "description": "Stash description"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_vcs tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "files", "message", "branchName", "remote", "logCount",
        "serializationMode", "lineEndings", "vcsMode", "stashMessage"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_vcs",
        "params": params
    })
