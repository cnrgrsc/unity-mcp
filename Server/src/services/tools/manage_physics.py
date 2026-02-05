"""
Tool for managing Physics components in Unity.
Supports Rigidbody, Collider, Joint and physics operations.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.utils import normalize_properties
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Physics components (Rigidbody, Collider, Joint) and physics operations (raycast, add_force). Read-only actions: rigidbody_get_info, collider_get_info, raycast. Modifying actions: rigidbody_add, rigidbody_configure, collider_add, collider_configure, joint_add, joint_configure, add_force, add_torque, set_velocity."
)
async def manage_physics(
    ctx: Context,
    action: Annotated[
        Literal[
            "rigidbody_add", "rigidbody_configure", "rigidbody_get_info",
            "collider_add", "collider_configure", "collider_get_info",
            "joint_add", "joint_configure",
            "add_force", "add_torque", "set_velocity", "raycast"
        ],
        "Action to perform on physics components"
    ],
    target: Annotated[
        str | int,
        "Target GameObject - instance ID (preferred) or name/path"
    ] | None = None,
    # Rigidbody properties
    mass: Annotated[float, "Rigidbody mass (default: 1)"] | None = None,
    drag: Annotated[float, "Rigidbody linear drag"] | None = None,
    angular_drag: Annotated[float, "Rigidbody angular drag"] | None = None,
    use_gravity: Annotated[bool, "Whether gravity affects this Rigidbody"] | None = None,
    is_kinematic: Annotated[bool, "Whether physics affects this Rigidbody"] | None = None,
    interpolation: Annotated[
        Literal["None", "Interpolate", "Extrapolate"],
        "Rigidbody interpolation mode"
    ] | None = None,
    collision_detection: Annotated[
        Literal["Discrete", "Continuous", "ContinuousDynamic", "ContinuousSpeculative"],
        "Collision detection mode"
    ] | None = None,
    constraints: Annotated[
        str,
        "Rigidbody constraints (e.g., 'FreezePositionX', 'FreezeRotation', 'FreezeAll')"
    ] | None = None,
    # Collider properties
    collider_type: Annotated[
        Literal["box", "sphere", "capsule", "mesh"],
        "Type of collider to add"
    ] | None = None,
    is_trigger: Annotated[bool, "Whether collider is a trigger"] | None = None,
    center: Annotated[
        list[float] | dict,
        "Collider center position [x, y, z] or {x, y, z}"
    ] | None = None,
    size: Annotated[
        list[float] | dict,
        "BoxCollider size [x, y, z]"
    ] | None = None,
    radius: Annotated[float, "Sphere/Capsule radius"] | None = None,
    height: Annotated[float, "Capsule height"] | None = None,
    convex: Annotated[bool, "MeshCollider convex flag"] | None = None,
    # Joint properties
    joint_type: Annotated[
        Literal["fixed", "hinge", "spring", "configurable", "character"],
        "Type of joint to add"
    ] | None = None,
    connected_body: Annotated[
        str | int,
        "Connected Rigidbody GameObject"
    ] | None = None,
    break_force: Annotated[float, "Force needed to break the joint"] | None = None,
    break_torque: Annotated[float, "Torque needed to break the joint"] | None = None,
    anchor: Annotated[list[float] | dict, "Joint anchor position"] | None = None,
    connected_anchor: Annotated[list[float] | dict, "Connected anchor position"] | None = None,
    use_spring: Annotated[bool, "HingeJoint use spring"] | None = None,
    use_limits: Annotated[bool, "HingeJoint use limits"] | None = None,
    use_motor: Annotated[bool, "HingeJoint use motor"] | None = None,
    spring: Annotated[float, "SpringJoint spring force"] | None = None,
    damper: Annotated[float, "SpringJoint damper"] | None = None,
    min_distance: Annotated[float, "SpringJoint min distance"] | None = None,
    max_distance: Annotated[float, "SpringJoint max distance"] | None = None,
    # Physics operations
    force: Annotated[list[float] | dict, "Force vector [x, y, z] for add_force"] | None = None,
    torque: Annotated[list[float] | dict, "Torque vector [x, y, z] for add_torque"] | None = None,
    velocity: Annotated[list[float] | dict, "Velocity vector [x, y, z]"] | None = None,
    angular_velocity: Annotated[list[float] | dict, "Angular velocity [x, y, z]"] | None = None,
    mode: Annotated[
        Literal["Force", "Acceleration", "Impulse", "VelocityChange"],
        "Force mode for add_force/add_torque"
    ] | None = None,
    # Raycast parameters
    origin: Annotated[list[float] | dict, "Raycast origin [x, y, z]"] | None = None,
    direction: Annotated[list[float] | dict, "Raycast direction [x, y, z]"] | None = None,
    max_distance: Annotated[float, "Maximum raycast distance"] | None = None,
    layer_mask: Annotated[int, "Layer mask for raycast"] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Physics components.

    Actions:
    - rigidbody_add: Add Rigidbody to a GameObject
    - rigidbody_configure: Configure Rigidbody properties (mass, drag, gravity, etc.)
    - rigidbody_get_info: Get Rigidbody information
    - collider_add: Add a Collider (box, sphere, capsule, mesh)
    - collider_configure: Configure Collider properties
    - collider_get_info: Get Collider information
    - joint_add: Add a Joint (fixed, hinge, spring, configurable)
    - joint_configure: Configure Joint properties
    - add_force: Apply force to a Rigidbody (PlayMode only)
    - add_torque: Apply torque to a Rigidbody (PlayMode only)
    - set_velocity: Set Rigidbody velocity
    - raycast: Perform a physics raycast

    Examples:
    - Add Rigidbody: action="rigidbody_add", target="Cube", mass=2.0, use_gravity=true
    - Add BoxCollider: action="collider_add", target="Player", collider_type="box", is_trigger=false
    - Raycast: action="raycast", origin=[0,1,0], direction=[0,-1,0], max_distance=100
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

    # Raycast doesn't need target
    if action != "raycast" and not target:
        return {
            "success": False,
            "message": "Missing required parameter 'target'. Specify GameObject instance ID or name."
        }

    try:
        params: dict[str, Any] = {"action": action}
        
        if target is not None:
            params["target"] = target

        # Rigidbody properties
        if mass is not None:
            params["mass"] = mass
        if drag is not None:
            params["drag"] = drag
        if angular_drag is not None:
            params["angularDrag"] = angular_drag
        if use_gravity is not None:
            params["useGravity"] = use_gravity
        if is_kinematic is not None:
            params["isKinematic"] = is_kinematic
        if interpolation is not None:
            params["interpolation"] = interpolation
        if collision_detection is not None:
            params["collisionDetection"] = collision_detection
        if constraints is not None:
            params["constraints"] = constraints

        # Collider properties
        if collider_type is not None:
            params["colliderType"] = collider_type
        if is_trigger is not None:
            params["isTrigger"] = is_trigger
        if center is not None:
            params["center"] = center
        if size is not None:
            params["size"] = size
        if radius is not None:
            params["radius"] = radius
        if height is not None:
            params["height"] = height
        if convex is not None:
            params["convex"] = convex

        # Joint properties
        if joint_type is not None:
            params["jointType"] = joint_type
        if connected_body is not None:
            params["connectedBody"] = connected_body
        if break_force is not None:
            params["breakForce"] = break_force
        if break_torque is not None:
            params["breakTorque"] = break_torque
        if anchor is not None:
            params["anchor"] = anchor
        if connected_anchor is not None:
            params["connectedAnchor"] = connected_anchor
        if use_spring is not None:
            params["useSpring"] = use_spring
        if use_limits is not None:
            params["useLimits"] = use_limits
        if use_motor is not None:
            params["useMotor"] = use_motor
        if spring is not None:
            params["spring"] = spring
        if damper is not None:
            params["damper"] = damper
        if min_distance is not None:
            params["minDistance"] = min_distance
        if max_distance is not None and action != "raycast":
            params["maxDistance"] = max_distance

        # Physics operations
        if force is not None:
            params["force"] = force
        if torque is not None:
            params["torque"] = torque
        if velocity is not None:
            params["velocity"] = velocity
        if angular_velocity is not None:
            params["angularVelocity"] = angular_velocity
        if mode is not None:
            params["mode"] = mode

        # Raycast
        if origin is not None:
            params["origin"] = origin
        if direction is not None:
            params["direction"] = direction
        if action == "raycast" and max_distance is not None:
            params["maxDistance"] = max_distance
        if layer_mask is not None:
            params["layerMask"] = layer_mask

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_physics",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Physics {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing physics: {e!s}"}
