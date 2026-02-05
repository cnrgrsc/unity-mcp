"""
Tool for managing Navigation (AI) components in Unity.
Supports NavMesh, NavMeshAgent, NavMeshObstacle, and OffMeshLink operations.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Navigation/AI components (NavMesh, NavMeshAgent, NavMeshObstacle, OffMeshLink). Read-only actions: navmesh_get_info, agent_get_info. Modifying actions: navmesh_bake, navmesh_clear, agent_add, agent_configure, agent_set_destination, agent_stop, obstacle_add, obstacle_configure, link_add, link_configure."
)
async def manage_navigation(
    ctx: Context,
    action: Annotated[
        Literal[
            "navmesh_bake", "navmesh_clear", "navmesh_get_info",
            "agent_add", "agent_configure", "agent_get_info",
            "agent_set_destination", "agent_stop",
            "obstacle_add", "obstacle_configure",
            "link_add", "link_configure"
        ],
        "Action to perform on navigation components"
    ],
    # Target GameObject
    target: Annotated[
        str | int,
        "Target GameObject - instance ID or name"
    ] | None = None,
    # NavMesh bake settings
    agent_radius: Annotated[float, "Agent radius for NavMesh baking"] | None = None,
    agent_height: Annotated[float, "Agent height for NavMesh baking"] | None = None,
    agent_slope: Annotated[float, "Maximum slope angle (degrees)"] | None = None,
    agent_climb: Annotated[float, "Maximum step height"] | None = None,
    # NavMeshAgent properties
    speed: Annotated[float, "Agent movement speed"] | None = None,
    angular_speed: Annotated[float, "Agent rotation speed (degrees/sec)"] | None = None,
    acceleration: Annotated[float, "Agent acceleration"] | None = None,
    stopping_distance: Annotated[float, "Distance at which agent stops"] | None = None,
    radius: Annotated[float, "Agent radius for avoidance"] | None = None,
    height: Annotated[float, "Agent height"] | None = None,
    base_offset: Annotated[float, "Agent base offset from ground"] | None = None,
    auto_braking: Annotated[bool, "Automatically brake when approaching destination"] | None = None,
    auto_repath: Annotated[bool, "Automatically recalculate path when invalid"] | None = None,
    avoidance_priority: Annotated[int, "Agent avoidance priority (0-99)"] | None = None,
    obstacle_avoidance_type: Annotated[
        Literal["None", "LowQuality", "MedQuality", "GoodQuality", "HighQuality"],
        "Obstacle avoidance quality"
    ] | None = None,
    destination: Annotated[
        list[float] | dict,
        "Destination position [x, y, z] for agent_set_destination"
    ] | None = None,
    # NavMeshObstacle properties
    shape: Annotated[
        Literal["box", "capsule"],
        "Obstacle shape"
    ] | None = None,
    carving: Annotated[bool, "Whether obstacle carves the NavMesh"] | None = None,
    carving_move_threshold: Annotated[float, "Movement threshold for carving update"] | None = None,
    carving_time_to_stationary: Annotated[float, "Time until considered stationary"] | None = None,
    carve_only_stationary: Annotated[bool, "Only carve when stationary"] | None = None,
    size: Annotated[list[float] | dict, "Obstacle size [x, y, z]"] | None = None,
    center: Annotated[list[float] | dict, "Obstacle center offset"] | None = None,
    # OffMeshLink properties
    start_transform: Annotated[str | int, "Start point GameObject"] | None = None,
    end_transform: Annotated[str | int, "End point GameObject"] | None = None,
    cost_override: Annotated[float, "Path cost override (-1 for default)"] | None = None,
    bidirectional: Annotated[bool, "Whether link is bidirectional"] | None = None,
    activated: Annotated[bool, "Whether link is active"] | None = None,
    auto_update_positions: Annotated[bool, "Auto-update link positions"] | None = None,
    area: Annotated[int, "NavMesh area index"] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Navigation/AI components.

    Actions:
    - navmesh_bake: Bake NavMesh for current scene
    - navmesh_clear: Clear baked NavMesh
    - navmesh_get_info: Get NavMesh information
    - agent_add: Add NavMeshAgent to GameObject
    - agent_configure: Configure NavMeshAgent properties
    - agent_get_info: Get NavMeshAgent information
    - agent_set_destination: Set agent destination (PlayMode only)
    - agent_stop: Stop agent movement
    - obstacle_add: Add NavMeshObstacle to GameObject
    - obstacle_configure: Configure NavMeshObstacle 
    - link_add: Add OffMeshLink to GameObject
    - link_configure: Configure OffMeshLink

    Examples:
    - Bake NavMesh: action="navmesh_bake", agent_radius=0.5, agent_height=2.0
    - Add Agent: action="agent_add", target="Enemy", speed=3.5, stopping_distance=1.0
    - Set Destination: action="agent_set_destination", target="Enemy", destination=[10, 0, 5]
    - Add Obstacle: action="obstacle_add", target="Wall", carving=true
    """
    unity_instance = get_unity_instance_from_context(ctx)

    gate = await preflight(ctx, wait_for_no_compile=True, refresh_if_dirty=True)
    if gate is not None:
        return gate.model_dump()

    if not action:
        return {
            "success": False,
            "message": "Missing required parameter 'action'."
        }

    try:
        params: dict[str, Any] = {"action": action}

        if target is not None:
            params["target"] = target

        # NavMesh bake settings
        if agent_radius is not None:
            params["agentRadius"] = agent_radius
        if agent_height is not None:
            params["agentHeight"] = agent_height
        if agent_slope is not None:
            params["agentSlope"] = agent_slope
        if agent_climb is not None:
            params["agentClimb"] = agent_climb

        # NavMeshAgent properties
        if speed is not None:
            params["speed"] = speed
        if angular_speed is not None:
            params["angularSpeed"] = angular_speed
        if acceleration is not None:
            params["acceleration"] = acceleration
        if stopping_distance is not None:
            params["stoppingDistance"] = stopping_distance
        if radius is not None:
            params["radius"] = radius
        if height is not None:
            params["height"] = height
        if base_offset is not None:
            params["baseOffset"] = base_offset
        if auto_braking is not None:
            params["autoBraking"] = auto_braking
        if auto_repath is not None:
            params["autoRepath"] = auto_repath
        if avoidance_priority is not None:
            params["avoidancePriority"] = avoidance_priority
        if obstacle_avoidance_type is not None:
            params["obstacleAvoidanceType"] = obstacle_avoidance_type
        if destination is not None:
            params["destination"] = destination

        # NavMeshObstacle properties
        if shape is not None:
            params["shape"] = shape
        if carving is not None:
            params["carving"] = carving
        if carving_move_threshold is not None:
            params["carvingMoveThreshold"] = carving_move_threshold
        if carving_time_to_stationary is not None:
            params["carvingTimeToStationary"] = carving_time_to_stationary
        if carve_only_stationary is not None:
            params["carveOnlyStationary"] = carve_only_stationary
        if size is not None:
            params["size"] = size
        if center is not None:
            params["center"] = center

        # OffMeshLink properties
        if start_transform is not None:
            params["startTransform"] = start_transform
        if end_transform is not None:
            params["endTransform"] = end_transform
        if cost_override is not None:
            params["costOverride"] = cost_override
        if bidirectional is not None:
            params["biDirectional"] = bidirectional
        if activated is not None:
            params["activated"] = activated
        if auto_update_positions is not None:
            params["autoUpdatePositions"] = auto_update_positions
        if area is not None:
            params["area"] = area

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_navigation",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Navigation {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing navigation: {e!s}"}
