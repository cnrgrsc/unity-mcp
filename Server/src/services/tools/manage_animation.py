"""
Tool for managing Animation components in Unity.
Supports Animator, AnimatorController, and AnimationClip operations.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Animation components (Animator, AnimatorController, AnimationClip). Read-only actions: animator_get_info, animator_get_current_state, controller_get_info, clip_get_info. Modifying actions: animator_add, animator_configure, animator_set_parameter, animator_play, controller_create, controller_add_parameter, controller_add_state, controller_add_transition, clip_create, clip_add_curve."
)
async def manage_animation(
    ctx: Context,
    action: Annotated[
        Literal[
            "animator_add", "animator_configure", "animator_get_info",
            "animator_set_parameter", "animator_play", "animator_get_current_state",
            "controller_create", "controller_add_parameter", "controller_add_state",
            "controller_add_transition", "controller_get_info",
            "clip_create", "clip_add_curve", "clip_get_info"
        ],
        "Action to perform on animation components"
    ],
    # Target GameObject (for animator actions)
    target: Annotated[
        str | int,
        "Target GameObject - instance ID or name (for animator actions)"
    ] | None = None,
    # Animator properties
    controller: Annotated[str, "Path to AnimatorController asset"] | None = None,
    apply_root_motion: Annotated[bool, "Whether to apply root motion"] | None = None,
    update_mode: Annotated[
        Literal["Normal", "AnimatePhysics", "UnscaledTime"],
        "Animator update mode"
    ] | None = None,
    culling_mode: Annotated[
        Literal["AlwaysAnimate", "CullUpdateTransforms", "CullCompletely"],
        "Animator culling mode"
    ] | None = None,
    speed: Annotated[float, "Animator playback speed multiplier"] | None = None,
    # Animator parameter operations
    parameter: Annotated[str, "Parameter name for set_parameter"] | None = None,
    parameter_type: Annotated[
        Literal["bool", "int", "float", "trigger"],
        "Parameter type"
    ] | None = None,
    value: Annotated[
        bool | int | float,
        "Parameter value for set_parameter"
    ] | None = None,
    # Animator play
    state: Annotated[str, "State name to play"] | None = None,
    layer: Annotated[int, "Animator layer index (default: 0)"] | None = None,
    normalized_time: Annotated[float, "Normalized time to start playing from"] | None = None,
    # Controller creation
    path: Annotated[str, "Asset path for controller/clip creation"] | None = None,
    parameters: Annotated[
        list[dict],
        "Parameter definitions [{name, type}] for controller creation"
    ] | None = None,
    # State creation
    name: Annotated[str, "Name for state/parameter"] | None = None,
    clip: Annotated[str, "Path to AnimationClip for state motion"] | None = None,
    is_default: Annotated[bool, "Set as default state"] | None = None,
    # Transition creation
    from_state: Annotated[str, "Source state name for transition"] | None = None,
    to_state: Annotated[str, "Destination state name for transition"] | None = None,
    duration: Annotated[float, "Transition duration"] | None = None,
    has_exit_time: Annotated[bool, "Whether transition waits for exit time"] | None = None,
    exit_time: Annotated[float, "Normalized exit time"] | None = None,
    conditions: Annotated[
        list[dict],
        "Transition conditions [{parameter, mode, threshold}]"
    ] | None = None,
    # Clip properties
    loop: Annotated[bool, "Whether clip should loop"] | None = None,
    frame_rate: Annotated[float, "Clip frame rate"] | None = None,
    # Curve properties
    property_name: Annotated[str, "Property name for animation curve"] | None = None,
    component_type: Annotated[str, "Component type for curve (e.g., Transform)"] | None = None,
    relative_path: Annotated[str, "Path to target object relative to animator"] | None = None,
    keyframes: Annotated[
        list[dict] | list[list],
        "Keyframes [{time, value, inTangent, outTangent}] or [[time, value], ...]"
    ] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Animation components.

    Actions:
    - animator_add: Add Animator component to GameObject
    - animator_configure: Configure Animator properties
    - animator_get_info: Get Animator information
    - animator_set_parameter: Set Animator parameter (bool, int, float, trigger)
    - animator_play: Play animation state
    - animator_get_current_state: Get current animation state info
    - controller_create: Create AnimatorController asset
    - controller_add_parameter: Add parameter to controller
    - controller_add_state: Add state to controller
    - controller_add_transition: Add transition between states
    - controller_get_info: Get controller information
    - clip_create: Create AnimationClip asset
    - clip_add_curve: Add animation curve to clip
    - clip_get_info: Get clip information

    Examples:
    - Add Animator: action="animator_add", target="Character", controller="Assets/Animations/Character.controller"
    - Create Controller: action="controller_create", path="Assets/Animations/Player.controller"
    - Add State: action="controller_add_state", controller="Assets/Animations/Player.controller", name="Idle", clip="Assets/Animations/Idle.anim"
    - Create Clip: action="clip_create", path="Assets/Animations/Walk.anim", loop=true
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

        # Target for animator actions
        if target is not None:
            params["target"] = target

        # Animator properties
        if controller is not None:
            params["controller"] = controller
        if apply_root_motion is not None:
            params["applyRootMotion"] = apply_root_motion
        if update_mode is not None:
            params["updateMode"] = update_mode
        if culling_mode is not None:
            params["cullingMode"] = culling_mode
        if speed is not None:
            params["speed"] = speed

        # Parameter operations
        if parameter is not None:
            params["parameter"] = parameter
        if parameter_type is not None:
            params["type"] = parameter_type
        if value is not None:
            params["value"] = value

        # Play state
        if state is not None:
            params["state"] = state
        if layer is not None:
            params["layer"] = layer
        if normalized_time is not None:
            params["normalizedTime"] = normalized_time

        # Asset paths
        if path is not None:
            params["path"] = path
        if parameters is not None:
            params["parameters"] = parameters

        # State creation
        if name is not None:
            params["name"] = name
        if clip is not None:
            params["clip"] = clip
        if is_default is not None:
            params["isDefault"] = is_default

        # Transition
        if from_state is not None:
            params["from"] = from_state
        if to_state is not None:
            params["to"] = to_state
        if duration is not None:
            params["duration"] = duration
        if has_exit_time is not None:
            params["hasExitTime"] = has_exit_time
        if exit_time is not None:
            params["exitTime"] = exit_time
        if conditions is not None:
            params["conditions"] = conditions

        # Clip properties
        if loop is not None:
            params["loop"] = loop
        if frame_rate is not None:
            params["frameRate"] = frame_rate

        # Curve properties
        if property_name is not None:
            params["property"] = property_name
        if component_type is not None:
            params["componentType"] = component_type
        if relative_path is not None:
            params["relativePath"] = relative_path
        if keyframes is not None:
            params["keyframes"] = keyframes

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_animation",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Animation {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing animation: {e!s}"}
