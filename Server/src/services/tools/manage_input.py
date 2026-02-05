"""
Tool for managing Input components in Unity.
Supports Legacy Input Manager and New Input System.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Input systems (Legacy Input Manager and New Input System). Read-only actions: get_input_system_info, legacy_get_axes, player_input_get_info, action_asset_get_info. Modifying actions: legacy_add_axis, player_input_add, player_input_configure, action_asset_create."
)
async def manage_input(
    ctx: Context,
    action: Annotated[
        Literal[
            "get_input_system_info",
            "legacy_get_axes", "legacy_add_axis",
            "player_input_add", "player_input_configure", "player_input_get_info",
            "action_asset_create", "action_asset_get_info"
        ],
        "Action to perform on input components"
    ],
    # Target GameObject (for PlayerInput)
    target: Annotated[
        str | int,
        "Target GameObject - instance ID or name"
    ] | None = None,
    # Legacy Input axis properties
    name: Annotated[str, "Name for new input axis"] | None = None,
    description: Annotated[str, "Descriptive name for axis"] | None = None,
    positive: Annotated[str, "Positive button (e.g., 'right', 'd')"] | None = None,
    negative: Annotated[str, "Negative button (e.g., 'left', 'a')"] | None = None,
    alt_positive: Annotated[str, "Alternative positive button"] | None = None,
    alt_negative: Annotated[str, "Alternative negative button"] | None = None,
    gravity: Annotated[float, "How fast axis returns to 0"] | None = None,
    dead_zone: Annotated[float, "Dead zone size"] | None = None,
    sensitivity: Annotated[float, "Axis sensitivity"] | None = None,
    snap: Annotated[bool, "Snap to 0 when opposite direction pressed"] | None = None,
    invert: Annotated[bool, "Invert axis direction"] | None = None,
    input_type: Annotated[
        Literal["key", "mouse", "joystick"],
        "Input type (key/mouse button, mouse movement, joystick)"
    ] | None = None,
    axis_index: Annotated[int, "Joystick axis index (0-27)"] | None = None,
    joystick_num: Annotated[int, "Joystick number (0=all)"] | None = None,
    # PlayerInput properties
    actions_asset: Annotated[str, "Path to InputActionAsset"] | None = None,
    default_control_scheme: Annotated[str, "Default control scheme name"] | None = None,
    default_action_map: Annotated[str, "Default action map name"] | None = None,
    notification_behavior: Annotated[
        Literal["SendMessages", "BroadcastMessages", "InvokeUnityEvents", "InvokeCSharpEvents"],
        "How input events are broadcast"
    ] | None = None,
    auto_switch: Annotated[bool, "Auto-switch control schemes"] | None = None,
    # InputActionAsset creation
    path: Annotated[str, "Asset path for InputActionAsset creation"] | None = None,
    action_map: Annotated[str, "Default action map name"] | None = None,
    actions: Annotated[
        list[dict],
        "Actions to create [{name, type, binding}]"
    ] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Input systems.

    Actions:
    - get_input_system_info: Get info about available input systems
    - legacy_get_axes: Get legacy Input Manager axes
    - legacy_add_axis: Add new legacy input axis
    - player_input_add: Add PlayerInput component (New Input System)
    - player_input_configure: Configure PlayerInput
    - player_input_get_info: Get PlayerInput information
    - action_asset_create: Create InputActionAsset
    - action_asset_get_info: Get InputActionAsset information

    Examples:
    - Get system info: action="get_input_system_info"
    - Add legacy axis: action="legacy_add_axis", name="Horizontal2", positive="d", negative="a"
    - Add PlayerInput: action="player_input_add", target="Player", actions_asset="Assets/Input/Controls.inputactions"
    - Create ActionAsset: action="action_asset_create", path="Assets/Input/PlayerControls.inputactions"
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

        # Legacy Input axis properties
        if name is not None:
            params["name"] = name
        if description is not None:
            params["descriptiveName"] = description
        if positive is not None:
            params["positiveButton"] = positive
        if negative is not None:
            params["negativeButton"] = negative
        if alt_positive is not None:
            params["altPositiveButton"] = alt_positive
        if alt_negative is not None:
            params["altNegativeButton"] = alt_negative
        if gravity is not None:
            params["gravity"] = gravity
        if dead_zone is not None:
            params["dead"] = dead_zone
        if sensitivity is not None:
            params["sensitivity"] = sensitivity
        if snap is not None:
            params["snap"] = snap
        if invert is not None:
            params["invert"] = invert
        if input_type is not None:
            params["type"] = input_type
        if axis_index is not None:
            params["axis"] = axis_index
        if joystick_num is not None:
            params["joyNum"] = joystick_num

        # PlayerInput properties
        if actions_asset is not None:
            params["actions"] = actions_asset
        if default_control_scheme is not None:
            params["defaultControlScheme"] = default_control_scheme
        if default_action_map is not None:
            params["defaultActionMap"] = default_action_map
        if notification_behavior is not None:
            params["notificationBehavior"] = notification_behavior
        if auto_switch is not None:
            params["autoSwitch"] = auto_switch

        # InputActionAsset creation
        if path is not None:
            params["path"] = path
        if action_map is not None:
            params["actionMap"] = action_map
        if actions is not None:
            params["actions"] = actions

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_input",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Input {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing input: {e!s}"}
