"""
Tool for managing UI components in Unity.
Supports Canvas, UI Elements, EventSystem, and Layout operations.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity UI components (Canvas, UI Elements, EventSystem, Layout). Read-only actions: canvas_get_info, element_get_info, event_system_get_info. Modifying actions: canvas_create, canvas_configure, element_create, element_configure, event_system_add, layout_add, layout_configure."
)
async def manage_ui(
    ctx: Context,
    action: Annotated[
        Literal[
            "canvas_create", "canvas_configure", "canvas_get_info",
            "element_create", "element_configure", "element_get_info",
            "event_system_add", "event_system_get_info",
            "layout_add", "layout_configure"
        ],
        "Action to perform on UI components"
    ],
    # Target GameObject
    target: Annotated[
        str | int,
        "Target GameObject - instance ID or name"
    ] | None = None,
    # Canvas properties
    name: Annotated[str, "Name for Canvas or UI element"] | None = None,
    render_mode: Annotated[
        Literal["ScreenSpaceOverlay", "ScreenSpaceCamera", "WorldSpace"],
        "Canvas render mode"
    ] | None = None,
    scale_mode: Annotated[
        Literal["ConstantPixelSize", "ScaleWithScreenSize", "ConstantPhysicalSize"],
        "Canvas scaler mode"
    ] | None = None,
    reference_width: Annotated[float, "Reference resolution width"] | None = None,
    reference_height: Annotated[float, "Reference resolution height"] | None = None,
    match_width_or_height: Annotated[float, "Match width (0) to height (1)"] | None = None,
    sorting_order: Annotated[int, "Canvas sorting order"] | None = None,
    # UI Element creation
    element_type: Annotated[
        Literal["button", "text", "image", "panel", "inputfield", "slider", "toggle", "dropdown"],
        "Type of UI element to create"
    ] | None = None,
    parent: Annotated[str | int, "Parent GameObject for UI element"] | None = None,
    # Common UI properties
    text: Annotated[str, "Text content for button/text/toggle"] | None = None,
    font_size: Annotated[float, "Font size"] | None = None,
    color: Annotated[list[float] | dict | str, "Color [r,g,b,a] or hex"] | None = None,
    text_color: Annotated[list[float] | dict | str, "Text color"] | None = None,
    # RectTransform properties
    position: Annotated[list[float] | dict, "Anchored position [x, y]"] | None = None,
    width: Annotated[float, "Element width"] | None = None,
    height: Annotated[float, "Element height"] | None = None,
    anchor: Annotated[
        Literal["center", "topleft", "topright", "bottomleft", "bottomright", "stretch"],
        "Anchor preset"
    ] | None = None,
    pivot: Annotated[list[float] | dict, "Pivot point [x, y]"] | None = None,
    # Image/Sprite
    sprite: Annotated[str, "Path to sprite asset"] | None = None,
    # Slider properties
    min_value: Annotated[float, "Slider minimum value"] | None = None,
    max_value: Annotated[float, "Slider maximum value"] | None = None,
    value: Annotated[float | int, "Slider/Toggle value"] | None = None,
    whole_numbers: Annotated[bool, "Slider uses whole numbers only"] | None = None,
    # Toggle properties
    is_on: Annotated[bool, "Toggle state"] | None = None,
    label: Annotated[str, "Toggle label text"] | None = None,
    # Dropdown properties
    options: Annotated[list[str], "Dropdown options"] | None = None,
    # InputField properties
    placeholder: Annotated[str, "InputField placeholder text"] | None = None,
    # Layout properties
    layout_type: Annotated[
        Literal["horizontal", "vertical", "grid"],
        "Layout group type"
    ] | None = None,
    spacing: Annotated[float | list[float] | dict, "Layout spacing"] | None = None,
    padding: Annotated[int | dict, "Layout padding"] | None = None,
    child_force_expand_width: Annotated[bool, "Force expand child width"] | None = None,
    child_force_expand_height: Annotated[bool, "Force expand child height"] | None = None,
    child_control_width: Annotated[bool, "Control child width"] | None = None,
    child_control_height: Annotated[bool, "Control child height"] | None = None,
    cell_size: Annotated[list[float] | dict, "Grid cell size [x, y]"] | None = None,
    constraint: Annotated[
        Literal["Flexible", "FixedColumnCount", "FixedRowCount"],
        "Grid constraint"
    ] | None = None,
    constraint_count: Annotated[int, "Grid constraint count"] | None = None,
    # General
    interactable: Annotated[bool, "Whether element is interactable"] | None = None,
    stretch: Annotated[bool, "Stretch panel to fill parent"] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity UI components.

    Actions:
    - canvas_create: Create a new Canvas with EventSystem
    - canvas_configure: Configure existing Canvas
    - canvas_get_info: Get Canvas information
    - element_create: Create UI element (button, text, image, panel, inputfield, slider, toggle, dropdown)
    - element_configure: Configure UI element properties
    - element_get_info: Get UI element information
    - event_system_add: Add EventSystem to scene
    - event_system_get_info: Get EventSystem information
    - layout_add: Add layout group (horizontal, vertical, grid)
    - layout_configure: Configure layout group

    Examples:
    - Create Canvas: action="canvas_create", name="MainCanvas", scale_mode="ScaleWithScreenSize"
    - Create Button: action="element_create", element_type="button", text="Click Me", parent="Canvas"
    - Create Text: action="element_create", element_type="text", text="Hello World", font_size=32
    - Configure Slider: action="element_configure", target="HealthBar", min_value=0, max_value=100, value=75
    - Add Layout: action="layout_add", target="Panel", layout_type="vertical", spacing=10
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

        # Canvas properties
        if name is not None:
            params["name"] = name
        if render_mode is not None:
            params["renderMode"] = render_mode
        if scale_mode is not None:
            params["scaleMode"] = scale_mode
        if reference_width is not None:
            params["referenceWidth"] = reference_width
        if reference_height is not None:
            params["referenceHeight"] = reference_height
        if match_width_or_height is not None:
            params["matchWidthOrHeight"] = match_width_or_height
        if sorting_order is not None:
            params["sortingOrder"] = sorting_order

        # UI Element creation
        if element_type is not None:
            params["type"] = element_type
        if parent is not None:
            params["parent"] = parent

        # Common UI properties
        if text is not None:
            params["text"] = text
        if font_size is not None:
            params["fontSize"] = font_size
        if color is not None:
            params["color"] = color
        if text_color is not None:
            params["textColor"] = text_color

        # RectTransform
        if position is not None:
            params["position"] = position
        if width is not None:
            params["width"] = width
        if height is not None:
            params["height"] = height
        if anchor is not None:
            params["anchor"] = anchor
        if pivot is not None:
            params["pivot"] = pivot

        # Image/Sprite
        if sprite is not None:
            params["sprite"] = sprite

        # Slider
        if min_value is not None:
            params["minValue"] = min_value
        if max_value is not None:
            params["maxValue"] = max_value
        if value is not None:
            params["value"] = value
        if whole_numbers is not None:
            params["wholeNumbers"] = whole_numbers

        # Toggle
        if is_on is not None:
            params["isOn"] = is_on
        if label is not None:
            params["label"] = label

        # Dropdown
        if options is not None:
            params["options"] = options

        # InputField
        if placeholder is not None:
            params["placeholder"] = placeholder

        # Layout
        if layout_type is not None:
            params["type"] = layout_type
        if spacing is not None:
            params["spacing"] = spacing
        if padding is not None:
            params["padding"] = padding
        if child_force_expand_width is not None:
            params["childForceExpandWidth"] = child_force_expand_width
        if child_force_expand_height is not None:
            params["childForceExpandHeight"] = child_force_expand_height
        if child_control_width is not None:
            params["childControlWidth"] = child_control_width
        if child_control_height is not None:
            params["childControlHeight"] = child_control_height
        if cell_size is not None:
            params["cellSize"] = cell_size
        if constraint is not None:
            params["constraint"] = constraint
        if constraint_count is not None:
            params["constraintCount"] = constraint_count

        # General
        if interactable is not None:
            params["interactable"] = interactable
        if stretch is not None:
            params["stretch"] = stretch

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_ui",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"UI {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing UI: {e!s}"}
