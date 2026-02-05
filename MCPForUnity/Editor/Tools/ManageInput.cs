using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Input components:
    /// - Legacy Input: Input.GetKey, Input.GetAxis configuration
    /// - New Input System: PlayerInput, InputActionAsset
    /// 
    /// Actions:
    /// - get_input_system_info: Get information about available input systems
    /// - legacy_get_axes: Get legacy input axes
    /// - legacy_add_axis: Add a new legacy input axis
    /// - player_input_add: Add PlayerInput component (New Input System)
    /// - player_input_configure: Configure PlayerInput component
    /// - player_input_get_info: Get PlayerInput information
    /// - action_asset_create: Create InputActionAsset
    /// - action_asset_get_info: Get InputActionAsset information
    /// </summary>
    [McpForUnityTool("manage_input")]
    public static class ManageInput
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
                    // General info
                    "get_input_system_info" => GetInputSystemInfo(@params),
                    
                    // Legacy Input actions
                    "legacy_get_axes" => LegacyGetAxes(@params),
                    "legacy_add_axis" => LegacyAddAxis(@params),
                    
#if ENABLE_INPUT_SYSTEM
                    // New Input System actions
                    "player_input_add" => PlayerInputAdd(@params),
                    "player_input_configure" => PlayerInputConfigure(@params),
                    "player_input_get_info" => PlayerInputGetInfo(@params),
                    "action_asset_create" => ActionAssetCreate(@params),
                    "action_asset_get_info" => ActionAssetGetInfo(@params),
#else
                    "player_input_add" or "player_input_configure" or "player_input_get_info" or 
                    "action_asset_create" or "action_asset_get_info" => 
                        new ErrorResponse("New Input System is not enabled. Enable it in Player Settings or use Package Manager."),
#endif
                    
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: get_input_system_info, legacy_get_axes, legacy_add_axis, player_input_add, player_input_configure, player_input_get_info, action_asset_create, action_asset_get_info")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageInput] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region General Info

        private static object GetInputSystemInfo(JObject @params)
        {
            bool hasNewInputSystem = false;
            string activeInputHandling = "Unknown";

#if ENABLE_INPUT_SYSTEM
            hasNewInputSystem = true;
#endif

            // Check PlayerSettings for input handling mode
            try
            {
                var serializedSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
                var activeInputHandler = serializedSettings.FindProperty("activeInputHandler");
                if (activeInputHandler != null)
                {
                    activeInputHandling = activeInputHandler.intValue switch
                    {
                        0 => "InputManager (Legacy)",
                        1 => "InputSystem (New)",
                        2 => "Both",
                        _ => "Unknown"
                    };
                }
            }
            catch
            {
                // Ignore - can't read settings
            }

            return new SuccessResponse("Input system information.", new
            {
                newInputSystemEnabled = hasNewInputSystem,
                activeInputHandling = activeInputHandling,
                legacyInputAvailable = true, // Always available
#if ENABLE_INPUT_SYSTEM
                newInputSystemVersion = InputSystem.version
#endif
            });
        }

        #endregion

        #region Legacy Input

        private static object LegacyGetAxes(JObject @params)
        {
            try
            {
                var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
                var serializedObject = new SerializedObject(inputManager);
                var axesProperty = serializedObject.FindProperty("m_Axes");

                var axes = new List<object>();
                for (int i = 0; i < axesProperty.arraySize; i++)
                {
                    var axis = axesProperty.GetArrayElementAtIndex(i);
                    axes.Add(new
                    {
                        name = axis.FindPropertyRelative("m_Name").stringValue,
                        descriptiveName = axis.FindPropertyRelative("descriptiveName").stringValue,
                        negativeButton = axis.FindPropertyRelative("negativeButton").stringValue,
                        positiveButton = axis.FindPropertyRelative("positiveButton").stringValue,
                        altNegativeButton = axis.FindPropertyRelative("altNegativeButton").stringValue,
                        altPositiveButton = axis.FindPropertyRelative("altPositiveButton").stringValue,
                        type = axis.FindPropertyRelative("type").intValue, // 0=KeyOrMouseButton, 1=MouseMovement, 2=JoystickAxis
                        axis_index = axis.FindPropertyRelative("axis").intValue,
                        joyNum = axis.FindPropertyRelative("joyNum").intValue
                    });
                }

                return new SuccessResponse($"Found {axes.Count} input axes.", new
                {
                    axisCount = axes.Count,
                    axes = axes
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to read InputManager: {e.Message}");
            }
        }

        private static object LegacyAddAxis(JObject @params)
        {
            string axisName = ParamCoercion.CoerceString(@params["name"], null);
            if (string.IsNullOrEmpty(axisName))
            {
                return new ErrorResponse("'name' is required for adding an axis.");
            }

            try
            {
                var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
                var serializedObject = new SerializedObject(inputManager);
                var axesProperty = serializedObject.FindProperty("m_Axes");

                // Check if axis already exists
                for (int i = 0; i < axesProperty.arraySize; i++)
                {
                    var existingAxis = axesProperty.GetArrayElementAtIndex(i);
                    if (existingAxis.FindPropertyRelative("m_Name").stringValue == axisName)
                    {
                        return new ErrorResponse($"Axis '{axisName}' already exists.");
                    }
                }

                // Add new axis
                axesProperty.arraySize++;
                var newAxis = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);

                newAxis.FindPropertyRelative("m_Name").stringValue = axisName;
                newAxis.FindPropertyRelative("descriptiveName").stringValue = 
                    ParamCoercion.CoerceString(@params["descriptiveName"] ?? @params["description"], "");
                newAxis.FindPropertyRelative("negativeButton").stringValue = 
                    ParamCoercion.CoerceString(@params["negativeButton"] ?? @params["negative"], "");
                newAxis.FindPropertyRelative("positiveButton").stringValue = 
                    ParamCoercion.CoerceString(@params["positiveButton"] ?? @params["positive"], "");
                newAxis.FindPropertyRelative("altNegativeButton").stringValue = 
                    ParamCoercion.CoerceString(@params["altNegativeButton"], "");
                newAxis.FindPropertyRelative("altPositiveButton").stringValue = 
                    ParamCoercion.CoerceString(@params["altPositiveButton"], "");
                newAxis.FindPropertyRelative("gravity").floatValue = 
                    ParamCoercion.CoerceFloat(@params["gravity"], 3f);
                newAxis.FindPropertyRelative("dead").floatValue = 
                    ParamCoercion.CoerceFloat(@params["dead"] ?? @params["deadZone"], 0.001f);
                newAxis.FindPropertyRelative("sensitivity").floatValue = 
                    ParamCoercion.CoerceFloat(@params["sensitivity"], 3f);
                newAxis.FindPropertyRelative("snap").boolValue = 
                    ParamCoercion.CoerceBool(@params["snap"], false);
                newAxis.FindPropertyRelative("invert").boolValue = 
                    ParamCoercion.CoerceBool(@params["invert"], false);
                
                // Type: 0 = Key or Mouse Button, 1 = Mouse Movement, 2 = Joystick Axis
                string typeStr = ParamCoercion.CoerceString(@params["type"], "key")?.ToLowerInvariant();
                int typeValue = typeStr switch
                {
                    "mouse" or "mousemovement" => 1,
                    "joystick" or "gamepad" => 2,
                    _ => 0
                };
                newAxis.FindPropertyRelative("type").intValue = typeValue;
                
                newAxis.FindPropertyRelative("axis").intValue = 
                    ParamCoercion.CoerceInt(@params["axis"], 0);
                newAxis.FindPropertyRelative("joyNum").intValue = 
                    ParamCoercion.CoerceInt(@params["joyNum"] ?? @params["joystick"], 0);

                serializedObject.ApplyModifiedProperties();

                return new SuccessResponse($"Input axis '{axisName}' added.", new
                {
                    name = axisName,
                    type = typeValue,
                    positiveButton = newAxis.FindPropertyRelative("positiveButton").stringValue,
                    negativeButton = newAxis.FindPropertyRelative("negativeButton").stringValue
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to add axis: {e.Message}");
            }
        }

        #endregion

#if ENABLE_INPUT_SYSTEM
        #region New Input System

        private static object PlayerInputAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<PlayerInput>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has a PlayerInput component.");
            }

            PlayerInput playerInput = Undo.AddComponent<PlayerInput>(target);

            // Set actions asset if provided
            string actionsPath = ParamCoercion.CoerceString(@params["actions"] ?? @params["actionsAsset"], null);
            if (!string.IsNullOrEmpty(actionsPath))
            {
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
                if (actions != null)
                {
                    playerInput.actions = actions;
                }
            }

            ApplyPlayerInputProperties(playerInput, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"PlayerInput added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasActions = playerInput.actions != null,
                actionsName = playerInput.actions?.name,
                defaultScheme = playerInput.defaultControlScheme,
                defaultActionMap = playerInput.defaultActionMap
            });
        }

        private static object PlayerInputConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayerInput playerInput = target.GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a PlayerInput component.");
            }

            Undo.RecordObject(playerInput, "Configure PlayerInput");

            // Set actions asset if provided
            string actionsPath = ParamCoercion.CoerceString(@params["actions"] ?? @params["actionsAsset"], null);
            if (!string.IsNullOrEmpty(actionsPath))
            {
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
                if (actions != null)
                {
                    playerInput.actions = actions;
                }
            }

            ApplyPlayerInputProperties(playerInput, @params);

            EditorUtility.SetDirty(playerInput);
            MarkSceneDirty(target);

            return new SuccessResponse($"PlayerInput configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                defaultScheme = playerInput.defaultControlScheme,
                defaultActionMap = playerInput.defaultActionMap
            });
        }

        private static object PlayerInputGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayerInput playerInput = target.GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a PlayerInput component.");
            }

            var actionMaps = playerInput.actions?.actionMaps.Select(m => new
            {
                name = m.name,
                enabled = m.enabled,
                actionCount = m.actions.Count
            }).ToArray();

            var controlSchemes = playerInput.actions?.controlSchemes.Select(s => s.name).ToArray();

            return new SuccessResponse($"PlayerInput info for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasActions = playerInput.actions != null,
                actionsName = playerInput.actions?.name,
                actionsPath = playerInput.actions != null ? AssetDatabase.GetAssetPath(playerInput.actions) : null,
                defaultControlScheme = playerInput.defaultControlScheme,
                defaultActionMap = playerInput.defaultActionMap,
                notificationBehavior = playerInput.notificationBehavior.ToString(),
                actionMaps = actionMaps,
                controlSchemes = controlSchemes
            });
        }

        private static object ActionAssetCreate(JObject @params)
        {
            string path = ParamCoercion.CoerceString(@params["path"], null);
            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' is required for InputActionAsset creation (e.g., 'Assets/Input/PlayerControls.inputactions').");
            }

            if (!path.EndsWith(".inputactions"))
                path += ".inputactions";

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

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);

            // Add default action map if specified
            string defaultMapName = ParamCoercion.CoerceString(@params["defaultActionMap"] ?? @params["actionMap"], null);
            if (!string.IsNullOrEmpty(defaultMapName))
            {
                var map = asset.AddActionMap(defaultMapName);
                
                // Add default actions if specified
                JArray actionsArray = @params["actions"] as JArray;
                if (actionsArray != null)
                {
                    foreach (JToken actionToken in actionsArray)
                    {
                        if (actionToken is JObject actionObj)
                        {
                            string actionName = ParamCoercion.CoerceString(actionObj["name"], null);
                            if (!string.IsNullOrEmpty(actionName))
                            {
                                string actionType = ParamCoercion.CoerceString(actionObj["type"], "Value");
                                InputActionType inputActionType = actionType.ToLowerInvariant() switch
                                {
                                    "button" => InputActionType.Button,
                                    "passthrough" => InputActionType.PassThrough,
                                    _ => InputActionType.Value
                                };
                                
                                var inputAction = map.AddAction(actionName, inputActionType);
                                
                                // Add binding if specified
                                string binding = ParamCoercion.CoerceString(actionObj["binding"], null);
                                if (!string.IsNullOrEmpty(binding))
                                {
                                    inputAction.AddBinding(binding);
                                }
                            }
                        }
                    }
                }
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"InputActionAsset created at '{path}'.", new
            {
                path = path,
                guid = AssetDatabase.AssetPathToGUID(path),
                actionMapCount = asset.actionMaps.Count
            });
        }

        private static object ActionAssetGetInfo(JObject @params)
        {
            string assetPath = ParamCoercion.CoerceString(@params["path"] ?? @params["asset"], null);
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("'path' is required.");
            }

            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (asset == null)
            {
                return new ErrorResponse($"InputActionAsset not found at '{assetPath}'.");
            }

            var actionMaps = asset.actionMaps.Select(map => new
            {
                name = map.name,
                actions = map.actions.Select(a => new
                {
                    name = a.name,
                    type = a.type.ToString(),
                    bindingCount = a.bindings.Count
                }).ToArray()
            }).ToArray();

            var controlSchemes = asset.controlSchemes.Select(s => new
            {
                name = s.name,
                devices = s.deviceRequirements.Select(d => d.controlPath).ToArray()
            }).ToArray();

            return new SuccessResponse($"InputActionAsset info for '{asset.name}'.", new
            {
                path = assetPath,
                name = asset.name,
                actionMapCount = actionMaps.Length,
                actionMaps = actionMaps,
                controlSchemeCount = controlSchemes.Length,
                controlSchemes = controlSchemes
            });
        }

        private static void ApplyPlayerInputProperties(PlayerInput playerInput, JObject @params)
        {
            if (@params["defaultControlScheme"] != null || @params["controlScheme"] != null)
                playerInput.defaultControlScheme = ParamCoercion.CoerceString(
                    @params["defaultControlScheme"] ?? @params["controlScheme"], playerInput.defaultControlScheme);

            if (@params["defaultActionMap"] != null || @params["actionMap"] != null)
                playerInput.defaultActionMap = ParamCoercion.CoerceString(
                    @params["defaultActionMap"] ?? @params["actionMap"], playerInput.defaultActionMap);

            if (@params["notificationBehavior"] != null)
            {
                string behavior = ParamCoercion.CoerceString(@params["notificationBehavior"], "");
                if (Enum.TryParse<PlayerNotifications>(behavior, true, out var notifications))
                    playerInput.notificationBehavior = notifications;
            }

            if (@params["neverAutoSwitchControlSchemes"] != null || @params["autoSwitch"] != null)
            {
                bool autoSwitch = ParamCoercion.CoerceBool(@params["autoSwitch"], true);
                playerInput.neverAutoSwitchControlSchemes = !autoSwitch;
            }
        }

        #endregion
#endif

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
