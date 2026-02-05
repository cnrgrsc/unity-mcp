using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Animation components:
    /// - Animator: controller, parameters, states, layers
    /// - AnimationClip: creation, curves, events
    /// - Avatar configuration
    /// 
    /// Actions:
    /// - animator_add: Add Animator component to GameObject
    /// - animator_configure: Configure Animator properties
    /// - animator_get_info: Get Animator state and parameters
    /// - animator_set_parameter: Set Animator parameter value
    /// - animator_play: Play animation state
    /// - animator_get_current_state: Get current state info
    /// - controller_create: Create AnimatorController asset
    /// - controller_add_parameter: Add parameter to controller
    /// - controller_add_state: Add state to controller layer
    /// - controller_add_transition: Add transition between states
    /// - clip_create: Create AnimationClip asset
    /// - clip_add_curve: Add animation curve to clip
    /// </summary>
    [McpForUnityTool("manage_animation")]
    public static class ManageAnimation
    {
        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new ErrorResponse("Parameters cannot be null.");
            }

            string action = ParamCoercion.CoerceString(@params["action"], null)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("'action' parameter is required.");
            }

            try
            {
                return action switch
                {
                    // Animator component actions
                    "animator_add" => AnimatorAdd(@params),
                    "animator_configure" => AnimatorConfigure(@params),
                    "animator_get_info" => AnimatorGetInfo(@params),
                    "animator_set_parameter" => AnimatorSetParameter(@params),
                    "animator_play" => AnimatorPlay(@params),
                    "animator_get_current_state" => AnimatorGetCurrentState(@params),
                    
                    // AnimatorController asset actions
                    "controller_create" => ControllerCreate(@params),
                    "controller_add_parameter" => ControllerAddParameter(@params),
                    "controller_add_state" => ControllerAddState(@params),
                    "controller_add_transition" => ControllerAddTransition(@params),
                    "controller_get_info" => ControllerGetInfo(@params),
                    
                    // AnimationClip actions
                    "clip_create" => ClipCreate(@params),
                    "clip_add_curve" => ClipAddCurve(@params),
                    "clip_get_info" => ClipGetInfo(@params),
                    
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: animator_add, animator_configure, animator_get_info, animator_set_parameter, animator_play, animator_get_current_state, controller_create, controller_add_parameter, controller_add_state, controller_add_transition, controller_get_info, clip_create, clip_add_curve, clip_get_info")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageAnimation] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region Animator Component Actions

        private static object AnimatorAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<Animator>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has an Animator.");
            }

            Animator animator = Undo.AddComponent<Animator>(target);

            // Set controller if provided
            string controllerPath = ParamCoercion.CoerceString(@params["controller"] ?? @params["controllerPath"], null);
            if (!string.IsNullOrEmpty(controllerPath))
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
            }

            // Apply other properties
            ApplyAnimatorProperties(animator, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"Animator added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasController = animator.runtimeAnimatorController != null,
                controllerName = animator.runtimeAnimatorController?.name
            });
        }

        private static object AnimatorConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an Animator.");
            }

            Undo.RecordObject(animator, "Configure Animator");

            // Set controller if provided
            string controllerPath = ParamCoercion.CoerceString(@params["controller"] ?? @params["controllerPath"], null);
            if (!string.IsNullOrEmpty(controllerPath))
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
            }

            ApplyAnimatorProperties(animator, @params);

            EditorUtility.SetDirty(animator);
            MarkSceneDirty(target);

            return new SuccessResponse($"Animator configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                applyRootMotion = animator.applyRootMotion,
                updateMode = animator.updateMode.ToString(),
                cullingMode = animator.cullingMode.ToString()
            });
        }

        private static object AnimatorGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an Animator.");
            }

            var parameters = new List<object>();
            if (animator.runtimeAnimatorController != null)
            {
                foreach (var param in animator.parameters)
                {
                    var paramInfo = new Dictionary<string, object>
                    {
                        ["name"] = param.name,
                        ["type"] = param.type.ToString()
                    };

                    // Get current value if in play mode
                    if (Application.isPlaying)
                    {
                        paramInfo["value"] = param.type switch
                        {
                            AnimatorControllerParameterType.Bool => animator.GetBool(param.name),
                            AnimatorControllerParameterType.Int => animator.GetInteger(param.name),
                            AnimatorControllerParameterType.Float => animator.GetFloat(param.name),
                            AnimatorControllerParameterType.Trigger => "trigger",
                            _ => null
                        };
                    }
                    parameters.Add(paramInfo);
                }
            }

            return new SuccessResponse($"Animator info for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasController = animator.runtimeAnimatorController != null,
                controllerName = animator.runtimeAnimatorController?.name,
                applyRootMotion = animator.applyRootMotion,
                updateMode = animator.updateMode.ToString(),
                cullingMode = animator.cullingMode.ToString(),
                speed = animator.speed,
                layerCount = animator.layerCount,
                parameters = parameters,
                isPlayMode = Application.isPlaying
            });
        }

        private static object AnimatorSetParameter(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an Animator.");
            }

            string paramName = ParamCoercion.CoerceString(@params["parameter"] ?? @params["parameterName"] ?? @params["name"], null);
            if (string.IsNullOrEmpty(paramName))
            {
                return new ErrorResponse("'parameter' name is required.");
            }

            string paramType = ParamCoercion.CoerceString(@params["type"] ?? @params["parameterType"], null)?.ToLowerInvariant();
            JToken valueToken = @params["value"];

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = $"Parameter '{paramName}' would be set in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        parameter = paramName,
                        warning = "Animator parameters can only be set during PlayMode."
                    }
                };
            }

            try
            {
                // Auto-detect type if not specified
                if (string.IsNullOrEmpty(paramType))
                {
                    var param = animator.parameters.FirstOrDefault(p => p.name == paramName);
                    if (param != null)
                    {
                        paramType = param.type.ToString().ToLowerInvariant();
                    }
                }

                switch (paramType)
                {
                    case "bool":
                        animator.SetBool(paramName, ParamCoercion.CoerceBool(valueToken, false));
                        break;
                    case "int":
                    case "integer":
                        animator.SetInteger(paramName, ParamCoercion.CoerceInt(valueToken, 0));
                        break;
                    case "float":
                        animator.SetFloat(paramName, ParamCoercion.CoerceFloat(valueToken, 0f));
                        break;
                    case "trigger":
                        animator.SetTrigger(paramName);
                        break;
                    default:
                        return new ErrorResponse($"Unknown parameter type: '{paramType}'. Use: bool, int, float, trigger");
                }

                return new SuccessResponse($"Parameter '{paramName}' set on '{target.name}'.", new
                {
                    instanceID = target.GetInstanceID(),
                    parameter = paramName,
                    type = paramType,
                    value = valueToken?.ToString()
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to set parameter '{paramName}': {e.Message}");
            }
        }

        private static object AnimatorPlay(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an Animator.");
            }

            string stateName = ParamCoercion.CoerceString(@params["state"] ?? @params["stateName"], null);
            if (string.IsNullOrEmpty(stateName))
            {
                return new ErrorResponse("'state' name is required.");
            }

            int layer = ParamCoercion.CoerceInt(@params["layer"], 0);
            float normalizedTime = ParamCoercion.CoerceFloat(@params["normalizedTime"] ?? @params["time"], 0f);

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = $"State '{stateName}' would play in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        state = stateName,
                        layer = layer,
                        warning = "Animator.Play only works during PlayMode."
                    }
                };
            }

            animator.Play(stateName, layer, normalizedTime);

            return new SuccessResponse($"Playing state '{stateName}' on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                state = stateName,
                layer = layer,
                normalizedTime = normalizedTime
            });
        }

        private static object AnimatorGetCurrentState(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an Animator.");
            }

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = "State info only available in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        warning = "Animator state info only available during PlayMode."
                    }
                };
            }

            var layerInfos = new List<object>();
            for (int i = 0; i < animator.layerCount; i++)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(i);

                layerInfos.Add(new
                {
                    layer = i,
                    layerName = animator.GetLayerName(i),
                    normalizedTime = stateInfo.normalizedTime,
                    length = stateInfo.length,
                    speed = stateInfo.speed,
                    isLooping = stateInfo.loop,
                    clips = clipInfos.Select(c => new { name = c.clip.name, weight = c.weight }).ToArray()
                });
            }

            return new SuccessResponse($"Current state info for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                layers = layerInfos
            });
        }

        private static void ApplyAnimatorProperties(Animator animator, JObject @params)
        {
            if (@params["applyRootMotion"] != null || @params["apply_root_motion"] != null)
                animator.applyRootMotion = ParamCoercion.CoerceBool(@params["applyRootMotion"] ?? @params["apply_root_motion"], animator.applyRootMotion);

            if (@params["updateMode"] != null || @params["update_mode"] != null)
            {
                string mode = ParamCoercion.CoerceString(@params["updateMode"] ?? @params["update_mode"], "");
                if (Enum.TryParse<AnimatorUpdateMode>(mode, true, out var updateMode))
                    animator.updateMode = updateMode;
            }

            if (@params["cullingMode"] != null || @params["culling_mode"] != null)
            {
                string mode = ParamCoercion.CoerceString(@params["cullingMode"] ?? @params["culling_mode"], "");
                if (Enum.TryParse<AnimatorCullingMode>(mode, true, out var cullingMode))
                    animator.cullingMode = cullingMode;
            }

            if (@params["speed"] != null)
                animator.speed = ParamCoercion.CoerceFloat(@params["speed"], animator.speed);
        }

        #endregion

        #region AnimatorController Actions

        private static object ControllerCreate(JObject @params)
        {
            string path = ParamCoercion.CoerceString(@params["path"], null);
            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' is required for controller creation (e.g., 'Assets/Animations/MyController.controller').");
            }

            if (!path.EndsWith(".controller"))
                path += ".controller";

            // Create parent directories if needed
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                string[] folders = directory.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            
            // Add default parameters if specified
            JArray paramArray = @params["parameters"] as JArray;
            if (paramArray != null)
            {
                foreach (JToken paramToken in paramArray)
                {
                    if (paramToken is JObject paramObj)
                    {
                        string name = ParamCoercion.CoerceString(paramObj["name"], null);
                        string type = ParamCoercion.CoerceString(paramObj["type"], "float")?.ToLowerInvariant();
                        
                        if (!string.IsNullOrEmpty(name))
                        {
                            AnimatorControllerParameterType paramType = type switch
                            {
                                "bool" => AnimatorControllerParameterType.Bool,
                                "int" or "integer" => AnimatorControllerParameterType.Int,
                                "trigger" => AnimatorControllerParameterType.Trigger,
                                _ => AnimatorControllerParameterType.Float
                            };
                            controller.AddParameter(name, paramType);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();

            return new SuccessResponse($"AnimatorController created at '{path}'.", new
            {
                path = path,
                guid = AssetDatabase.AssetPathToGUID(path),
                layerCount = controller.layers.Length,
                parameterCount = controller.parameters.Length
            });
        }

        private static object ControllerAddParameter(JObject @params)
        {
            string controllerPath = ParamCoercion.CoerceString(@params["controller"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(controllerPath))
            {
                return new ErrorResponse("'controller' path is required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return new ErrorResponse($"AnimatorController not found at '{controllerPath}'.");
            }

            string paramName = ParamCoercion.CoerceString(@params["name"] ?? @params["parameterName"], null);
            if (string.IsNullOrEmpty(paramName))
            {
                return new ErrorResponse("'name' is required for parameter.");
            }

            string typeStr = ParamCoercion.CoerceString(@params["type"], "float")?.ToLowerInvariant();
            AnimatorControllerParameterType paramType = typeStr switch
            {
                "bool" => AnimatorControllerParameterType.Bool,
                "int" or "integer" => AnimatorControllerParameterType.Int,
                "trigger" => AnimatorControllerParameterType.Trigger,
                _ => AnimatorControllerParameterType.Float
            };

            Undo.RecordObject(controller, "Add Parameter");
            controller.AddParameter(paramName, paramType);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Parameter '{paramName}' added to controller.", new
            {
                controller = controllerPath,
                parameter = paramName,
                type = paramType.ToString()
            });
        }

        private static object ControllerAddState(JObject @params)
        {
            string controllerPath = ParamCoercion.CoerceString(@params["controller"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(controllerPath))
            {
                return new ErrorResponse("'controller' path is required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return new ErrorResponse($"AnimatorController not found at '{controllerPath}'.");
            }

            string stateName = ParamCoercion.CoerceString(@params["name"] ?? @params["stateName"], null);
            if (string.IsNullOrEmpty(stateName))
            {
                return new ErrorResponse("'name' is required for state.");
            }

            int layerIndex = ParamCoercion.CoerceInt(@params["layer"], 0);
            if (layerIndex >= controller.layers.Length)
            {
                return new ErrorResponse($"Layer index {layerIndex} is out of range.");
            }

            AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;
            
            Undo.RecordObject(stateMachine, "Add State");
            AnimatorState state = stateMachine.AddState(stateName);

            // Set motion/clip if provided
            string clipPath = ParamCoercion.CoerceString(@params["clip"] ?? @params["motion"], null);
            if (!string.IsNullOrEmpty(clipPath))
            {
                Motion motion = AssetDatabase.LoadAssetAtPath<Motion>(clipPath);
                if (motion != null)
                {
                    state.motion = motion;
                }
            }

            // Set as default if specified
            bool isDefault = ParamCoercion.CoerceBool(@params["isDefault"] ?? @params["default"], false);
            if (isDefault)
            {
                stateMachine.defaultState = state;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"State '{stateName}' added to controller.", new
            {
                controller = controllerPath,
                state = stateName,
                layer = layerIndex,
                hasMotion = state.motion != null,
                isDefault = stateMachine.defaultState == state
            });
        }

        private static object ControllerAddTransition(JObject @params)
        {
            string controllerPath = ParamCoercion.CoerceString(@params["controller"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(controllerPath))
            {
                return new ErrorResponse("'controller' path is required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return new ErrorResponse($"AnimatorController not found at '{controllerPath}'.");
            }

            string fromState = ParamCoercion.CoerceString(@params["from"] ?? @params["sourceState"], null);
            string toState = ParamCoercion.CoerceString(@params["to"] ?? @params["destinationState"], null);
            
            if (string.IsNullOrEmpty(fromState) || string.IsNullOrEmpty(toState))
            {
                return new ErrorResponse("'from' and 'to' state names are required.");
            }

            int layerIndex = ParamCoercion.CoerceInt(@params["layer"], 0);
            AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;

            AnimatorState sourceState = FindState(stateMachine, fromState);
            AnimatorState destState = FindState(stateMachine, toState);

            if (sourceState == null)
            {
                return new ErrorResponse($"Source state '{fromState}' not found.");
            }
            if (destState == null)
            {
                return new ErrorResponse($"Destination state '{toState}' not found.");
            }

            Undo.RecordObject(sourceState, "Add Transition");
            AnimatorStateTransition transition = sourceState.AddTransition(destState);

            // Configure transition
            if (@params["duration"] != null)
                transition.duration = ParamCoercion.CoerceFloat(@params["duration"], transition.duration);
            
            if (@params["hasExitTime"] != null || @params["has_exit_time"] != null)
                transition.hasExitTime = ParamCoercion.CoerceBool(@params["hasExitTime"] ?? @params["has_exit_time"], transition.hasExitTime);
            
            if (@params["exitTime"] != null || @params["exit_time"] != null)
                transition.exitTime = ParamCoercion.CoerceFloat(@params["exitTime"] ?? @params["exit_time"], transition.exitTime);

            // Add conditions if specified
            JArray conditions = @params["conditions"] as JArray;
            if (conditions != null)
            {
                foreach (JToken condToken in conditions)
                {
                    if (condToken is JObject cond)
                    {
                        string paramName = ParamCoercion.CoerceString(cond["parameter"], null);
                        string modeStr = ParamCoercion.CoerceString(cond["mode"], "Equals")?.Replace("_", "");
                        float threshold = ParamCoercion.CoerceFloat(cond["threshold"] ?? cond["value"], 0f);

                        if (!string.IsNullOrEmpty(paramName) && Enum.TryParse<AnimatorConditionMode>(modeStr, true, out var mode))
                        {
                            transition.AddCondition(mode, threshold, paramName);
                        }
                    }
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Transition added from '{fromState}' to '{toState}'.", new
            {
                controller = controllerPath,
                from = fromState,
                to = toState,
                duration = transition.duration,
                hasExitTime = transition.hasExitTime,
                conditionCount = transition.conditions.Length
            });
        }

        private static object ControllerGetInfo(JObject @params)
        {
            string controllerPath = ParamCoercion.CoerceString(@params["controller"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(controllerPath))
            {
                return new ErrorResponse("'controller' path is required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return new ErrorResponse($"AnimatorController not found at '{controllerPath}'.");
            }

            var parameters = controller.parameters.Select(p => new
            {
                name = p.name,
                type = p.type.ToString(),
                defaultFloat = p.defaultFloat,
                defaultInt = p.defaultInt,
                defaultBool = p.defaultBool
            }).ToArray();

            var layers = controller.layers.Select((layer, index) => new
            {
                index = index,
                name = layer.name,
                stateCount = layer.stateMachine.states.Length,
                defaultState = layer.stateMachine.defaultState?.name,
                states = layer.stateMachine.states.Select(s => s.state.name).ToArray()
            }).ToArray();

            return new SuccessResponse($"Controller info for '{controllerPath}'.", new
            {
                path = controllerPath,
                parameterCount = parameters.Length,
                parameters = parameters,
                layerCount = layers.Length,
                layers = layers
            });
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                    return childState.state;
            }
            return null;
        }

        #endregion

        #region AnimationClip Actions

        private static object ClipCreate(JObject @params)
        {
            string path = ParamCoercion.CoerceString(@params["path"], null);
            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' is required for clip creation (e.g., 'Assets/Animations/MyClip.anim').");
            }

            if (!path.EndsWith(".anim"))
                path += ".anim";

            // Create parent directories if needed
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                string[] folders = directory.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            AnimationClip clip = new AnimationClip();
            clip.name = System.IO.Path.GetFileNameWithoutExtension(path);

            // Set clip settings
            if (@params["loop"] != null || @params["isLooping"] != null)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = ParamCoercion.CoerceBool(@params["loop"] ?? @params["isLooping"], false);
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            if (@params["frameRate"] != null || @params["frame_rate"] != null)
            {
                clip.frameRate = ParamCoercion.CoerceFloat(@params["frameRate"] ?? @params["frame_rate"], 60f);
            }

            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"AnimationClip created at '{path}'.", new
            {
                path = path,
                guid = AssetDatabase.AssetPathToGUID(path),
                frameRate = clip.frameRate,
                length = clip.length
            });
        }

        private static object ClipAddCurve(JObject @params)
        {
            string clipPath = ParamCoercion.CoerceString(@params["clip"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("'clip' path is required.");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"AnimationClip not found at '{clipPath}'.");
            }

            string propertyName = ParamCoercion.CoerceString(@params["property"] ?? @params["propertyName"], null);
            if (string.IsNullOrEmpty(propertyName))
            {
                return new ErrorResponse("'property' name is required.");
            }

            string relativePath = ParamCoercion.CoerceString(@params["relativePath"] ?? @params["path_to_target"], "");
            Type componentType = typeof(Transform); // Default to Transform
            
            string componentTypeName = ParamCoercion.CoerceString(@params["componentType"] ?? @params["component"], null);
            if (!string.IsNullOrEmpty(componentTypeName))
            {
                componentType = UnityTypeResolver.ResolveComponent(componentTypeName) ?? typeof(Transform);
            }

            // Parse keyframes
            JArray keyframesArray = @params["keyframes"] as JArray;
            if (keyframesArray == null || keyframesArray.Count == 0)
            {
                return new ErrorResponse("'keyframes' array is required with at least one keyframe.");
            }

            var keyframes = new List<Keyframe>();
            foreach (JToken kfToken in keyframesArray)
            {
                if (kfToken is JObject kf)
                {
                    float time = ParamCoercion.CoerceFloat(kf["time"], 0f);
                    float value = ParamCoercion.CoerceFloat(kf["value"], 0f);
                    float inTangent = ParamCoercion.CoerceFloat(kf["inTangent"], 0f);
                    float outTangent = ParamCoercion.CoerceFloat(kf["outTangent"], 0f);
                    
                    keyframes.Add(new Keyframe(time, value, inTangent, outTangent));
                }
                else if (kfToken is JArray kfArr && kfArr.Count >= 2)
                {
                    // Simple [time, value] format
                    float time = ParamCoercion.CoerceFloat(kfArr[0], 0f);
                    float value = ParamCoercion.CoerceFloat(kfArr[1], 0f);
                    keyframes.Add(new Keyframe(time, value));
                }
            }

            AnimationCurve curve = new AnimationCurve(keyframes.ToArray());
            
            Undo.RecordObject(clip, "Add Curve");
            clip.SetCurve(relativePath, componentType, propertyName, curve);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Curve added to clip '{clip.name}'.", new
            {
                clip = clipPath,
                property = propertyName,
                componentType = componentType.Name,
                keyframeCount = keyframes.Count,
                duration = clip.length
            });
        }

        private static object ClipGetInfo(JObject @params)
        {
            string clipPath = ParamCoercion.CoerceString(@params["clip"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("'clip' path is required.");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"AnimationClip not found at '{clipPath}'.");
            }

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            var curves = bindings.Select(b => new
            {
                path = b.path,
                propertyName = b.propertyName,
                type = b.type.Name
            }).ToArray();

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

            return new SuccessResponse($"Clip info for '{clip.name}'.", new
            {
                path = clipPath,
                name = clip.name,
                length = clip.length,
                frameRate = clip.frameRate,
                isLooping = settings.loopTime,
                isLegacy = clip.legacy,
                curveCount = curves.Length,
                curves = curves
            });
        }

        #endregion

        #region Helpers

        private static GameObject FindTarget(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null) return null;

            if (targetToken.Type == JTokenType.Integer)
            {
                int instanceId = targetToken.Value<int>();
                return GameObjectLookup.FindById(instanceId);
            }

            string targetStr = targetToken.ToString();

            if (int.TryParse(targetStr, out int parsedId))
            {
                var byId = GameObjectLookup.FindById(parsedId);
                if (byId != null) return byId;
            }

            return GameObjectLookup.FindByTarget(targetToken, "by_name", true);
        }

        private static object TargetNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Target GameObject '{@params["target"]}' not found.");
        }

        private static void MarkSceneDirty(GameObject go)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }

        #endregion
    }
}
