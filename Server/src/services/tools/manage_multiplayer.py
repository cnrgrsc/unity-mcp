"""
manage_multiplayer.py - Multiplayer & Networking Tool

Handles:
- Unity Netcode for GameObjects
- Network objects
- RPCs and Client/Server communication
- Lobby and matchmaking
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_multiplayer."""
    return {
        "name": "manage_multiplayer",
        "description": """Manage Unity Netcode for GameObjects, networking, and multiplayer features.

Actions:
- setup_network_manager: Create and configure NetworkManager
- add_network_object: Add NetworkObject component to GameObject
- add_network_transform: Add NetworkTransform for sync
- add_network_rigidbody: Add NetworkRigidbody for physics sync
- add_network_animator: Add NetworkAnimator for animation sync
- add_network_variable: Add NetworkVariable to script
- create_rpc: Create ServerRpc or ClientRpc method
- create_lobby: Create multiplayer lobby
- join_lobby: Join existing lobby
- leave_lobby: Leave current lobby
- list_lobbies: List available lobbies
- start_host: Start as host
- start_server: Start as dedicated server
- start_client: Start as client
- stop_network: Stop networking
- get_connected_clients: Get list of connected clients
- kick_client: Kick a client from server
- spawn_networked: Spawn networked object
- despawn_networked: Despawn networked object
- relay_setup: Setup Unity Relay for NAT traversal""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "setup_network_manager",
                        "add_network_object",
                        "add_network_transform",
                        "add_network_rigidbody",
                        "add_network_animator",
                        "add_network_variable",
                        "create_rpc",
                        "create_lobby",
                        "join_lobby",
                        "leave_lobby",
                        "list_lobbies",
                        "start_host",
                        "start_server",
                        "start_client",
                        "stop_network",
                        "get_connected_clients",
                        "kick_client",
                        "spawn_networked",
                        "despawn_networked",
                        "relay_setup"
                    ]
                },
                "targetObject": {
                    "type": "string",
                    "description": "Target GameObject path"
                },
                "syncPositionX": {"type": "boolean", "description": "Sync X position"},
                "syncPositionY": {"type": "boolean", "description": "Sync Y position"},
                "syncPositionZ": {"type": "boolean", "description": "Sync Z position"},
                "syncRotation": {"type": "boolean", "description": "Sync rotation"},
                "syncScale": {"type": "boolean", "description": "Sync scale"},
                "interpolate": {"type": "boolean", "description": "Enable interpolation"},
                "variableName": {
                    "type": "string",
                    "description": "Network variable name"
                },
                "variableType": {
                    "type": "string",
                    "description": "Network variable type",
                    "enum": ["int", "float", "bool", "string", "Vector3", "Quaternion"]
                },
                "writePermission": {
                    "type": "string",
                    "description": "Write permission for variable",
                    "enum": ["Server", "Owner"]
                },
                "rpcName": {
                    "type": "string",
                    "description": "RPC method name"
                },
                "rpcType": {
                    "type": "string",
                    "description": "RPC type",
                    "enum": ["ServerRpc", "ClientRpc"]
                },
                "rpcParams": {
                    "type": "array",
                    "description": "RPC parameters",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string"},
                            "type": {"type": "string"}
                        }
                    }
                },
                "lobbyName": {
                    "type": "string",
                    "description": "Lobby name"
                },
                "lobbyId": {
                    "type": "string",
                    "description": "Lobby ID"
                },
                "maxPlayers": {
                    "type": "integer",
                    "description": "Maximum players in lobby"
                },
                "isPrivate": {
                    "type": "boolean",
                    "description": "Is lobby private"
                },
                "lobbyData": {
                    "type": "object",
                    "description": "Custom lobby data"
                },
                "serverAddress": {
                    "type": "string",
                    "description": "Server IP address"
                },
                "serverPort": {
                    "type": "integer",
                    "description": "Server port"
                },
                "clientId": {
                    "type": "integer",
                    "description": "Client ID"
                },
                "prefabPath": {
                    "type": "string",
                    "description": "Prefab path for spawning"
                },
                "spawnPosition": {
                    "type": "object",
                    "description": "Spawn position",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"}
                    }
                },
                "transportType": {
                    "type": "string",
                    "description": "Network transport type",
                    "enum": ["UnityTransport", "WebSocket"]
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_multiplayer tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "targetObject", "syncPositionX", "syncPositionY", "syncPositionZ",
        "syncRotation", "syncScale", "interpolate", "variableName", "variableType",
        "writePermission", "rpcName", "rpcType", "rpcParams", "lobbyName",
        "lobbyId", "maxPlayers", "isPrivate", "lobbyData", "serverAddress",
        "serverPort", "clientId", "prefabPath", "spawnPosition", "transportType"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_multiplayer",
        "params": params
    })
