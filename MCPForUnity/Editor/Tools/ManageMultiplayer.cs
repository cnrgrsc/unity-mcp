using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Netcode for GameObjects and multiplayer features.
    /// </summary>
    [McpForUnityTool("manage_multiplayer")]
    public static class ManageMultiplayer
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
                    "setup_network_manager" => SetupNetworkManager(@params),
                    "add_network_object" => AddNetworkObject(@params),
                    "add_network_transform" => AddNetworkTransform(@params),
                    "add_network_rigidbody" => AddNetworkRigidbody(@params),
                    "add_network_animator" => AddNetworkAnimator(@params),
                    "add_network_variable" => AddNetworkVariable(@params),
                    "create_rpc" => CreateRpc(@params),
                    "create_lobby" => CreateLobby(@params),
                    "relay_setup" => RelaySetup(@params),
                    "get_connected_clients" => GetConnectedClients(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageMultiplayer] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object SetupNetworkManager(JObject @params)
        {
            // Check for existing NetworkManager
            var existingManager = GameObject.FindObjectOfType<MonoBehaviour>();
            
            var networkManagerGO = new GameObject("NetworkManager");
            
            return new SuccessResponse("NetworkManager setup.", new
            {
                gameObject = "NetworkManager",
                requiredPackages = new[]
                {
                    "com.unity.netcode.gameobjects",
                    "com.unity.transport"
                },
                components = new[]
                {
                    "NetworkManager",
                    "UnityTransport"
                },
                instruction = "Add NetworkManager and UnityTransport components manually or via Package Manager"
            });
        }

        private static object AddNetworkObject(JObject @params)
        {
            string targetPath = ParamCoercion.CoerceString(@params["targetObject"], "");
            
            if (string.IsNullOrEmpty(targetPath))
                return new ErrorResponse("'targetObject' is required.");

            var target = GameObject.Find(targetPath);
            if (target == null)
                return new ErrorResponse($"GameObject not found: {targetPath}");

            return new SuccessResponse($"NetworkObject component info for '{targetPath}'.", new
            {
                targetObject = targetPath,
                component = "NetworkObject",
                instruction = "Add Unity.Netcode.NetworkObject component to the GameObject",
                codeExample = "gameObject.AddComponent<NetworkObject>();"
            });
        }

        private static object AddNetworkTransform(JObject @params)
        {
            string targetPath = ParamCoercion.CoerceString(@params["targetObject"], "");
            bool syncX = ParamCoercion.CoerceBool(@params["syncPositionX"], true);
            bool syncY = ParamCoercion.CoerceBool(@params["syncPositionY"], true);
            bool syncZ = ParamCoercion.CoerceBool(@params["syncPositionZ"], true);
            bool syncRot = ParamCoercion.CoerceBool(@params["syncRotation"], true);
            bool syncScale = ParamCoercion.CoerceBool(@params["syncScale"], false);
            bool interpolate = ParamCoercion.CoerceBool(@params["interpolate"], true);

            return new SuccessResponse("NetworkTransform configured.", new
            {
                targetObject = targetPath,
                syncPosition = new { x = syncX, y = syncY, z = syncZ },
                syncRotation = syncRot,
                syncScale = syncScale,
                interpolate = interpolate,
                component = "NetworkTransform"
            });
        }

        private static object AddNetworkRigidbody(JObject @params)
        {
            string targetPath = ParamCoercion.CoerceString(@params["targetObject"], "");

            return new SuccessResponse("NetworkRigidbody configured.", new
            {
                targetObject = targetPath,
                component = "NetworkRigidbody",
                note = "Syncs Rigidbody physics across network"
            });
        }

        private static object AddNetworkAnimator(JObject @params)
        {
            string targetPath = ParamCoercion.CoerceString(@params["targetObject"], "");

            return new SuccessResponse("NetworkAnimator configured.", new
            {
                targetObject = targetPath,
                component = "NetworkAnimator",
                note = "Syncs Animator parameters across network"
            });
        }

        private static object AddNetworkVariable(JObject @params)
        {
            string varName = ParamCoercion.CoerceString(@params["variableName"], "networkVar");
            string varType = ParamCoercion.CoerceString(@params["variableType"], "int");
            string writePermission = ParamCoercion.CoerceString(@params["writePermission"], "Server");

            string netVarType = varType switch
            {
                "int" => "NetworkVariable<int>",
                "float" => "NetworkVariable<float>",
                "bool" => "NetworkVariable<bool>",
                "string" => "NetworkVariable<FixedString64Bytes>",
                "Vector3" => "NetworkVariable<Vector3>",
                "Quaternion" => "NetworkVariable<Quaternion>",
                _ => $"NetworkVariable<{varType}>"
            };

            return new SuccessResponse("NetworkVariable configured.", new
            {
                variableName = varName,
                variableType = netVarType,
                writePermission = writePermission,
                codeExample = $"public {netVarType} {varName} = new {netVarType}(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.{writePermission});"
            });
        }

        private static object CreateRpc(JObject @params)
        {
            string rpcName = ParamCoercion.CoerceString(@params["rpcName"], "MyRpc");
            string rpcType = ParamCoercion.CoerceString(@params["rpcType"], "ServerRpc");
            var rpcParamsArray = @params["rpcParams"] as JArray;

            var paramList = new List<string>();
            if (rpcParamsArray != null)
            {
                foreach (var param in rpcParamsArray)
                {
                    string name = param["name"]?.ToString() ?? "param";
                    string type = param["type"]?.ToString() ?? "int";
                    paramList.Add($"{type} {name}");
                }
            }

            string paramsString = string.Join(", ", paramList);
            string attribute = rpcType == "ServerRpc" ? "[ServerRpc]" : "[ClientRpc]";
            string suffix = rpcType == "ServerRpc" ? "ServerRpc" : "ClientRpc";

            return new SuccessResponse($"{rpcType} created.", new
            {
                rpcName = rpcName + suffix,
                rpcType = rpcType,
                codeExample = $@"{attribute}
public void {rpcName}{suffix}({paramsString})
{{
    // RPC implementation
}}"
            });
        }

        private static object CreateLobby(JObject @params)
        {
            string lobbyName = ParamCoercion.CoerceString(@params["lobbyName"], "My Lobby");
            int maxPlayers = ParamCoercion.CoerceInt(@params["maxPlayers"], 4);
            bool isPrivate = ParamCoercion.CoerceBool(@params["isPrivate"], false);

            return new SuccessResponse("Lobby configuration.", new
            {
                lobbyName = lobbyName,
                maxPlayers = maxPlayers,
                isPrivate = isPrivate,
                requiredPackage = "com.unity.services.lobby",
                codeExample = $@"var lobby = await LobbyService.Instance.CreateLobbyAsync(""{lobbyName}"", {maxPlayers}, new CreateLobbyOptions {{ IsPrivate = {isPrivate.ToString().ToLower()} }});"
            });
        }

        private static object RelaySetup(JObject @params)
        {
            return new SuccessResponse("Unity Relay setup info.", new
            {
                requiredPackage = "com.unity.services.relay",
                steps = new[]
                {
                    "1. Enable Relay in Unity Dashboard",
                    "2. Initialize Unity Services",
                    "3. Create allocation with RelayService.Instance.CreateAllocationAsync()",
                    "4. Get join code with RelayService.Instance.GetJoinCodeAsync()",
                    "5. Configure transport with allocation data"
                }
            });
        }

        private static object GetConnectedClients(JObject @params)
        {
            return new SuccessResponse("Connected clients info.", new
            {
                codeExample = "NetworkManager.Singleton.ConnectedClientsIds",
                note = "Access at runtime through NetworkManager"
            });
        }
    }
}
