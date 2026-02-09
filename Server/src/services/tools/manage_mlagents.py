"""
manage_mlagents.py - ML-Agents Tool

Handles:
- Unity ML-Agents setup
- Agent configuration
- Training settings
- Behavior parameters
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_mlagents."""
    return {
        "name": "manage_mlagents",
        "description": """Manage Unity ML-Agents for machine learning and AI training.

Actions:
- add_agent: Add Agent component to GameObject
- configure_behavior: Configure behavior parameters
- add_sensor: Add observation sensor
- add_actuator: Add action actuator
- set_reward: Configure reward signals
- create_training_config: Create training YAML config
- start_training: Start training session
- stop_training: Stop training
- load_model: Load trained model
- set_inference_mode: Set agent to inference mode
- set_training_mode: Set agent to training mode
- add_decision_requester: Add DecisionRequester component
- get_training_stats: Get training statistics
- export_onnx: Export model to ONNX format""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "add_agent",
                        "configure_behavior",
                        "add_sensor",
                        "add_actuator",
                        "set_reward",
                        "create_training_config",
                        "start_training",
                        "stop_training",
                        "load_model",
                        "set_inference_mode",
                        "set_training_mode",
                        "add_decision_requester",
                        "get_training_stats",
                        "export_onnx"
                    ]
                },
                "targetObject": {
                    "type": "string",
                    "description": "Target GameObject path"
                },
                "behaviorName": {
                    "type": "string",
                    "description": "Behavior name for agent"
                },
                "vectorObservationSize": {
                    "type": "integer",
                    "description": "Size of vector observations"
                },
                "stackedVectors": {
                    "type": "integer",
                    "description": "Number of stacked vector observations"
                },
                "actionType": {
                    "type": "string",
                    "description": "Action space type",
                    "enum": ["Discrete", "Continuous"]
                },
                "discreteBranches": {
                    "type": "array",
                    "description": "Discrete action branch sizes",
                    "items": {"type": "integer"}
                },
                "continuousActions": {
                    "type": "integer",
                    "description": "Number of continuous actions"
                },
                "sensorType": {
                    "type": "string",
                    "description": "Type of sensor to add",
                    "enum": ["RayPerception", "Camera", "RenderTexture", "GridSensor", "BufferSensor"]
                },
                "sensorConfig": {
                    "type": "object",
                    "description": "Sensor configuration",
                    "properties": {
                        "rayCount": {"type": "integer"},
                        "rayLength": {"type": "number"},
                        "detectableTags": {"type": "array", "items": {"type": "string"}},
                        "cameraWidth": {"type": "integer"},
                        "cameraHeight": {"type": "integer"},
                        "grayscale": {"type": "boolean"}
                    }
                },
                "decisionPeriod": {
                    "type": "integer",
                    "description": "Decision requester period"
                },
                "takeActionsBetweenDecisions": {
                    "type": "boolean",
                    "description": "Take actions between decisions"
                },
                "modelPath": {
                    "type": "string",
                    "description": "Path to trained model (.onnx)"
                },
                "trainingConfig": {
                    "type": "object",
                    "description": "Training hyperparameters",
                    "properties": {
                        "maxSteps": {"type": "integer"},
                        "learningRate": {"type": "number"},
                        "batchSize": {"type": "integer"},
                        "bufferSize": {"type": "integer"},
                        "hiddenUnits": {"type": "integer"},
                        "numLayers": {"type": "integer"},
                        "gamma": {"type": "number"}
                    }
                },
                "runId": {
                    "type": "string",
                    "description": "Training run ID"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_mlagents tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "targetObject", "behaviorName", "vectorObservationSize", "stackedVectors",
        "actionType", "discreteBranches", "continuousActions", "sensorType",
        "sensorConfig", "decisionPeriod", "takeActionsBetweenDecisions",
        "modelPath", "trainingConfig", "runId"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_mlagents",
        "params": params
    })
