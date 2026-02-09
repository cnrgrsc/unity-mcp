using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity ML-Agents for AI training.
    /// </summary>
    [McpForUnityTool("manage_mlagents")]
    public static class ManageMLAgents
    {
        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
                return new ErrorResponse("Parameters cannot be null.");

            string action = ParamCoercion.CoerceString(@params["action"], null)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
                return new ErrorResponse("'action' parameter is required.");

            try
            {
                return action switch
                {
                    "add_agent" => AddAgent(@params),
                    "configure_behavior" => ConfigureBehavior(@params),
                    "add_sensor" => AddSensor(@params),
                    "add_decision_requester" => AddDecisionRequester(@params),
                    "create_training_config" => CreateTrainingConfig(@params),
                    "load_model" => LoadModel(@params),
                    "set_inference_mode" => SetInferenceMode(@params),
                    "export_onnx" => ExportOnnx(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageMLAgents] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object AddAgent(JObject @params)
        {
            string targetPath = ParamCoercion.CoerceString(@params["targetObject"], "");

            return new SuccessResponse("ML-Agent setup.", new
            {
                targetObject = targetPath,
                package = "com.unity.ml-agents",
                components = new[] { "Agent (inherit)", "BehaviorParameters", "DecisionRequester" },
                steps = new[]
                {
                    "1. Install ML-Agents package",
                    "2. Create script inheriting from Agent",
                    "3. Override CollectObservations(), OnActionReceived(), Heuristic()",
                    "4. Add BehaviorParameters component",
                    "5. Add DecisionRequester component"
                }
            });
        }

        private static object ConfigureBehavior(JObject @params)
        {
            string behaviorName = ParamCoercion.CoerceString(@params["behaviorName"], "MyBehavior");
            int vectorObsSize = ParamCoercion.CoerceInt(@params["vectorObservationSize"], 8);
            int stackedVectors = ParamCoercion.CoerceInt(@params["stackedVectors"], 1);
            string actionType = ParamCoercion.CoerceString(@params["actionType"], "Discrete");
            int continuousActions = ParamCoercion.CoerceInt(@params["continuousActions"], 0);
            var discreteBranches = @params["discreteBranches"] as JArray;

            var branches = new List<int>();
            if (discreteBranches != null)
            {
                foreach (var branch in discreteBranches)
                    branches.Add(branch.ToObject<int>());
            }

            return new SuccessResponse("Behavior parameters configured.", new
            {
                behaviorName = behaviorName,
                vectorObservationSize = vectorObsSize,
                stackedVectors = stackedVectors,
                actionType = actionType,
                continuousActions = continuousActions,
                discreteBranches = branches,
                instruction = "Configure these in BehaviorParameters component"
            });
        }

        private static object AddSensor(JObject @params)
        {
            string targetPath = ParamCoercion.CoerceString(@params["targetObject"], "");
            string sensorType = ParamCoercion.CoerceString(@params["sensorType"], "RayPerception");

            var sensorComponents = new Dictionary<string, string>
            {
                { "RayPerception", "RayPerceptionSensorComponent3D or RayPerceptionSensorComponent2D" },
                { "Camera", "CameraSensorComponent" },
                { "RenderTexture", "RenderTextureSensorComponent" },
                { "GridSensor", "GridSensorComponent" },
                { "BufferSensor", "BufferSensorComponent" }
            };

            return new SuccessResponse($"Sensor '{sensorType}' configuration.", new
            {
                sensorType = sensorType,
                component = sensorComponents.GetValueOrDefault(sensorType, sensorType),
                targetObject = targetPath,
                rayPerceptionExample = new
                {
                    rayCount = 11,
                    rayLength = 20f,
                    detectableTags = new[] { "Enemy", "Wall", "Goal" },
                    sphereCastRadius = 0.5f
                }
            });
        }

        private static object AddDecisionRequester(JObject @params)
        {
            int decisionPeriod = ParamCoercion.CoerceInt(@params["decisionPeriod"], 5);
            bool takeActionsBetween = ParamCoercion.CoerceBool(@params["takeActionsBetweenDecisions"], true);

            return new SuccessResponse("DecisionRequester configured.", new
            {
                decisionPeriod = decisionPeriod,
                takeActionsBetweenDecisions = takeActionsBetween,
                note = "Lower period = more frequent decisions (more compute)"
            });
        }

        private static object CreateTrainingConfig(JObject @params)
        {
            string behaviorName = ParamCoercion.CoerceString(@params["behaviorName"], "MyBehavior");
            var config = @params["trainingConfig"];

            int maxSteps = 500000;
            float learningRate = 0.0003f;
            int batchSize = 1024;
            int bufferSize = 10240;

            if (config != null)
            {
                maxSteps = ParamCoercion.CoerceInt(config["maxSteps"], maxSteps);
                learningRate = (float)ParamCoercion.CoerceDouble(config["learningRate"], learningRate);
                batchSize = ParamCoercion.CoerceInt(config["batchSize"], batchSize);
                bufferSize = ParamCoercion.CoerceInt(config["bufferSize"], bufferSize);
            }

            string yamlConfig = $@"behaviors:
  {behaviorName}:
    trainer_type: ppo
    hyperparameters:
      batch_size: {batchSize}
      buffer_size: {bufferSize}
      learning_rate: {learningRate}
      beta: 0.005
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
      learning_rate_schedule: linear
    network_settings:
      normalize: false
      hidden_units: 128
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: {maxSteps}
    time_horizon: 64
    summary_freq: 10000";

            return new SuccessResponse("Training config generated.", new
            {
                behaviorName = behaviorName,
                yaml = yamlConfig,
                savePath = $"config/{behaviorName}.yaml",
                trainingCommand = $"mlagents-learn config/{behaviorName}.yaml --run-id={behaviorName}_run"
            });
        }

        private static object LoadModel(JObject @params)
        {
            string modelPath = ParamCoercion.CoerceString(@params["modelPath"], "");

            return new SuccessResponse("Model loading configuration.", new
            {
                modelPath = modelPath,
                steps = new[]
                {
                    "1. Place .onnx model in Assets folder",
                    "2. Select agent GameObject",
                    "3. Set BehaviorParameters > Model to your .onnx file",
                    "4. Set BehaviorType to 'Inference Only'"
                }
            });
        }

        private static object SetInferenceMode(JObject @params)
        {
            return new SuccessResponse("Inference mode configuration.", new
            {
                instruction = "Set BehaviorParameters.BehaviorType = BehaviorType.InferenceOnly",
                codeExample = "behaviorParams.BehaviorType = BehaviorType.InferenceOnly;"
            });
        }

        private static object ExportOnnx(JObject @params)
        {
            string runId = ParamCoercion.CoerceString(@params["runId"], "");

            return new SuccessResponse("ONNX export info.", new
            {
                modelLocation = $"results/{runId}/{runId}/model.onnx",
                instruction = "Models are automatically saved as .onnx during training",
                copyTo = "Assets/Models/"
            });
        }
    }
}
