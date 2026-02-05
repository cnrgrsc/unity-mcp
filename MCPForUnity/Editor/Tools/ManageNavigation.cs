using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Navigation (AI) components:
    /// - NavMesh: baking, clearing, and info
    /// - NavMeshAgent: pathfinding agents
    /// - NavMeshObstacle: dynamic obstacles
    /// - OffMeshLink: manual navigation links
    /// 
    /// Actions:
    /// - navmesh_bake: Bake NavMesh for current scene
    /// - navmesh_clear: Clear baked NavMesh
    /// - navmesh_get_info: Get NavMesh information
    /// - agent_add: Add NavMeshAgent to GameObject
    /// - agent_configure: Configure NavMeshAgent properties
    /// - agent_get_info: Get NavMeshAgent information
    /// - agent_set_destination: Set agent destination (PlayMode)
    /// - obstacle_add: Add NavMeshObstacle to GameObject
    /// - obstacle_configure: Configure NavMeshObstacle properties
    /// - link_add: Add OffMeshLink to GameObject
    /// - link_configure: Configure OffMeshLink properties
    /// - surface_add: Add NavMeshSurface to GameObject
    /// - surface_bake: Bake specific NavMeshSurface
    /// </summary>
    [McpForUnityTool("manage_navigation")]
    public static class ManageNavigation
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
                    // NavMesh actions
                    "navmesh_bake" => NavMeshBake(@params),
                    "navmesh_clear" => NavMeshClear(@params),
                    "navmesh_get_info" => NavMeshGetInfo(@params),
                    
                    // NavMeshAgent actions
                    "agent_add" => AgentAdd(@params),
                    "agent_configure" => AgentConfigure(@params),
                    "agent_get_info" => AgentGetInfo(@params),
                    "agent_set_destination" => AgentSetDestination(@params),
                    "agent_stop" => AgentStop(@params),
                    
                    // NavMeshObstacle actions
                    "obstacle_add" => ObstacleAdd(@params),
                    "obstacle_configure" => ObstacleConfigure(@params),
                    
                    // OffMeshLink actions
                    "link_add" => LinkAdd(@params),
                    "link_configure" => LinkConfigure(@params),
                    
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: navmesh_bake, navmesh_clear, navmesh_get_info, agent_add, agent_configure, agent_get_info, agent_set_destination, agent_stop, obstacle_add, obstacle_configure, link_add, link_configure")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageNavigation] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region NavMesh Actions

        private static object NavMeshBake(JObject @params)
        {
            try
            {
                // Get bake settings
                NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
                
                // Apply custom settings if provided
                if (@params["agentRadius"] != null || @params["agent_radius"] != null)
                    settings.agentRadius = ParamCoercion.CoerceFloat(@params["agentRadius"] ?? @params["agent_radius"], settings.agentRadius);
                
                if (@params["agentHeight"] != null || @params["agent_height"] != null)
                    settings.agentHeight = ParamCoercion.CoerceFloat(@params["agentHeight"] ?? @params["agent_height"], settings.agentHeight);
                
                if (@params["agentSlope"] != null || @params["agent_slope"] != null || @params["maxSlope"] != null)
                    settings.agentSlope = ParamCoercion.CoerceFloat(@params["agentSlope"] ?? @params["agent_slope"] ?? @params["maxSlope"], settings.agentSlope);
                
                if (@params["agentClimb"] != null || @params["agent_climb"] != null || @params["stepHeight"] != null)
                    settings.agentClimb = ParamCoercion.CoerceFloat(@params["agentClimb"] ?? @params["agent_climb"] ?? @params["stepHeight"], settings.agentClimb);

                // Bake NavMesh using legacy API
                NavMeshBuilder.BuildNavMesh();
                
                // Mark scene as dirty
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                return new SuccessResponse("NavMesh baked successfully.", new
                {
                    agentRadius = settings.agentRadius,
                    agentHeight = settings.agentHeight,
                    agentSlope = settings.agentSlope,
                    agentClimb = settings.agentClimb
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to bake NavMesh: {e.Message}");
            }
        }

        private static object NavMeshClear(JObject @params)
        {
            try
            {
                NavMeshBuilder.ClearAllNavMeshes();
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                return new SuccessResponse("NavMesh cleared successfully.", new { });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to clear NavMesh: {e.Message}");
            }
        }

        private static object NavMeshGetInfo(JObject @params)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            
            // Get area names
            var areas = new List<object>();
            for (int i = 0; i < 32; i++)
            {
                string areaName = NavMesh.GetAreaFromName(GameObjectUtility.GetNavMeshAreaNames().Length > i 
                    ? GameObjectUtility.GetNavMeshAreaNames()[i] 
                    : $"Area {i}").ToString();
                
                float cost = NavMesh.GetAreaCost(i);
                if (!string.IsNullOrEmpty(areaName) || cost != 1f)
                {
                    areas.Add(new
                    {
                        index = i,
                        name = GameObjectUtility.GetNavMeshAreaNames().Length > i 
                            ? GameObjectUtility.GetNavMeshAreaNames()[i] 
                            : $"Area {i}",
                        cost = cost
                    });
                }
            }

            // Count agents and obstacles
            var agents = UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            var obstacles = UnityEngine.Object.FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None);
            var links = UnityEngine.Object.FindObjectsByType<OffMeshLink>(FindObjectsSortMode.None);

            return new SuccessResponse("NavMesh info retrieved.", new
            {
                hasNavMesh = triangulation.vertices.Length > 0,
                vertexCount = triangulation.vertices.Length,
                triangleCount = triangulation.indices.Length / 3,
                agentCount = agents.Length,
                obstacleCount = obstacles.Length,
                linkCount = links.Length,
                areas = areas.Take(10).ToArray() // Limit areas shown
            });
        }

        #endregion

        #region NavMeshAgent Actions

        private static object AgentAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<NavMeshAgent>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has a NavMeshAgent.");
            }

            NavMeshAgent agent = Undo.AddComponent<NavMeshAgent>(target);
            
            // Apply initial properties
            ApplyAgentProperties(agent, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"NavMeshAgent added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                speed = agent.speed,
                angularSpeed = agent.angularSpeed,
                stoppingDistance = agent.stoppingDistance,
                radius = agent.radius,
                height = agent.height
            });
        }

        private static object AgentConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a NavMeshAgent.");
            }

            Undo.RecordObject(agent, "Configure NavMeshAgent");
            ApplyAgentProperties(agent, @params);

            EditorUtility.SetDirty(agent);
            MarkSceneDirty(target);

            return new SuccessResponse($"NavMeshAgent configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                speed = agent.speed,
                angularSpeed = agent.angularSpeed,
                stoppingDistance = agent.stoppingDistance,
                acceleration = agent.acceleration
            });
        }

        private static object AgentGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a NavMeshAgent.");
            }

            var info = new Dictionary<string, object>
            {
                ["instanceID"] = target.GetInstanceID(),
                ["speed"] = agent.speed,
                ["angularSpeed"] = agent.angularSpeed,
                ["acceleration"] = agent.acceleration,
                ["stoppingDistance"] = agent.stoppingDistance,
                ["radius"] = agent.radius,
                ["height"] = agent.height,
                ["baseOffset"] = agent.baseOffset,
                ["autoBraking"] = agent.autoBraking,
                ["autoRepath"] = agent.autoRepath,
                ["obstacleAvoidanceType"] = agent.obstacleAvoidanceType.ToString(),
                ["avoidancePriority"] = agent.avoidancePriority
            };

            // Add runtime info if in PlayMode
            if (Application.isPlaying)
            {
                info["isOnNavMesh"] = agent.isOnNavMesh;
                info["hasPath"] = agent.hasPath;
                info["pathPending"] = agent.pathPending;
                info["isStopped"] = agent.isStopped;
                info["velocity"] = new { x = agent.velocity.x, y = agent.velocity.y, z = agent.velocity.z };
                info["remainingDistance"] = agent.remainingDistance;
                
                if (agent.hasPath)
                {
                    info["destination"] = new 
                    { 
                        x = agent.destination.x, 
                        y = agent.destination.y, 
                        z = agent.destination.z 
                    };
                    info["pathStatus"] = agent.pathStatus.ToString();
                }
            }

            return new SuccessResponse($"NavMeshAgent info for '{target.name}'.", info);
        }

        private static object AgentSetDestination(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a NavMeshAgent.");
            }

            Vector3 destination = ParseVector3(@params["destination"], Vector3.zero);
            
            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = $"Destination would be set in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        destination = new { x = destination.x, y = destination.y, z = destination.z },
                        warning = "SetDestination only works during PlayMode."
                    }
                };
            }

            if (!agent.isOnNavMesh)
            {
                return new ErrorResponse($"NavMeshAgent on '{target.name}' is not on a NavMesh.");
            }

            bool success = agent.SetDestination(destination);

            return new SuccessResponse($"Destination set for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                destination = new { x = destination.x, y = destination.y, z = destination.z },
                pathPending = agent.pathPending,
                setDestinationResult = success
            });
        }

        private static object AgentStop(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a NavMeshAgent.");
            }

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = "Agent would be stopped in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        warning = "Agent control only works during PlayMode."
                    }
                };
            }

            agent.isStopped = true;
            agent.ResetPath();

            return new SuccessResponse($"NavMeshAgent stopped on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                isStopped = agent.isStopped
            });
        }

        private static void ApplyAgentProperties(NavMeshAgent agent, JObject @params)
        {
            if (@params["speed"] != null)
                agent.speed = ParamCoercion.CoerceFloat(@params["speed"], agent.speed);
            
            if (@params["angularSpeed"] != null || @params["angular_speed"] != null)
                agent.angularSpeed = ParamCoercion.CoerceFloat(@params["angularSpeed"] ?? @params["angular_speed"], agent.angularSpeed);
            
            if (@params["acceleration"] != null)
                agent.acceleration = ParamCoercion.CoerceFloat(@params["acceleration"], agent.acceleration);
            
            if (@params["stoppingDistance"] != null || @params["stopping_distance"] != null)
                agent.stoppingDistance = ParamCoercion.CoerceFloat(@params["stoppingDistance"] ?? @params["stopping_distance"], agent.stoppingDistance);
            
            if (@params["radius"] != null)
                agent.radius = ParamCoercion.CoerceFloat(@params["radius"], agent.radius);
            
            if (@params["height"] != null)
                agent.height = ParamCoercion.CoerceFloat(@params["height"], agent.height);
            
            if (@params["baseOffset"] != null || @params["base_offset"] != null)
                agent.baseOffset = ParamCoercion.CoerceFloat(@params["baseOffset"] ?? @params["base_offset"], agent.baseOffset);
            
            if (@params["autoBraking"] != null || @params["auto_braking"] != null)
                agent.autoBraking = ParamCoercion.CoerceBool(@params["autoBraking"] ?? @params["auto_braking"], agent.autoBraking);
            
            if (@params["autoRepath"] != null || @params["auto_repath"] != null)
                agent.autoRepath = ParamCoercion.CoerceBool(@params["autoRepath"] ?? @params["auto_repath"], agent.autoRepath);
            
            if (@params["avoidancePriority"] != null || @params["priority"] != null)
                agent.avoidancePriority = ParamCoercion.CoerceInt(@params["avoidancePriority"] ?? @params["priority"], agent.avoidancePriority);
            
            if (@params["obstacleAvoidanceType"] != null)
            {
                string oat = ParamCoercion.CoerceString(@params["obstacleAvoidanceType"], "");
                if (Enum.TryParse<ObstacleAvoidanceType>(oat, true, out var avoidType))
                    agent.obstacleAvoidanceType = avoidType;
            }
        }

        #endregion

        #region NavMeshObstacle Actions

        private static object ObstacleAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<NavMeshObstacle>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has a NavMeshObstacle.");
            }

            NavMeshObstacle obstacle = Undo.AddComponent<NavMeshObstacle>(target);
            
            // Apply initial properties
            ApplyObstacleProperties(obstacle, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"NavMeshObstacle added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                shape = obstacle.shape.ToString(),
                carving = obstacle.carving,
                size = new { x = obstacle.size.x, y = obstacle.size.y, z = obstacle.size.z }
            });
        }

        private static object ObstacleConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            NavMeshObstacle obstacle = target.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a NavMeshObstacle.");
            }

            Undo.RecordObject(obstacle, "Configure NavMeshObstacle");
            ApplyObstacleProperties(obstacle, @params);

            EditorUtility.SetDirty(obstacle);
            MarkSceneDirty(target);

            return new SuccessResponse($"NavMeshObstacle configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                shape = obstacle.shape.ToString(),
                carving = obstacle.carving
            });
        }

        private static void ApplyObstacleProperties(NavMeshObstacle obstacle, JObject @params)
        {
            if (@params["shape"] != null)
            {
                string shape = ParamCoercion.CoerceString(@params["shape"], "").ToLowerInvariant();
                obstacle.shape = shape switch
                {
                    "capsule" => NavMeshObstacleShape.Capsule,
                    _ => NavMeshObstacleShape.Box
                };
            }

            if (@params["carving"] != null || @params["carve"] != null)
                obstacle.carving = ParamCoercion.CoerceBool(@params["carving"] ?? @params["carve"], obstacle.carving);

            if (@params["carvingMoveThreshold"] != null || @params["move_threshold"] != null)
                obstacle.carvingMoveThreshold = ParamCoercion.CoerceFloat(@params["carvingMoveThreshold"] ?? @params["move_threshold"], obstacle.carvingMoveThreshold);

            if (@params["carvingTimeToStationary"] != null || @params["time_to_stationary"] != null)
                obstacle.carvingTimeToStationary = ParamCoercion.CoerceFloat(@params["carvingTimeToStationary"] ?? @params["time_to_stationary"], obstacle.carvingTimeToStationary);

            if (@params["carveOnlyStationary"] != null)
                obstacle.carveOnlyStationary = ParamCoercion.CoerceBool(@params["carveOnlyStationary"], obstacle.carveOnlyStationary);

            if (@params["size"] != null)
                obstacle.size = ParseVector3(@params["size"], obstacle.size);

            if (@params["center"] != null)
                obstacle.center = ParseVector3(@params["center"], obstacle.center);

            if (@params["radius"] != null)
                obstacle.radius = ParamCoercion.CoerceFloat(@params["radius"], obstacle.radius);

            if (@params["height"] != null)
                obstacle.height = ParamCoercion.CoerceFloat(@params["height"], obstacle.height);
        }

        #endregion

        #region OffMeshLink Actions

        private static object LinkAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            OffMeshLink link = Undo.AddComponent<OffMeshLink>(target);

            // Set start and end transforms if provided
            JToken startToken = @params["startTransform"] ?? @params["start"];
            JToken endToken = @params["endTransform"] ?? @params["end"];

            if (startToken != null)
            {
                GameObject startGo = FindTargetFromToken(startToken);
                if (startGo != null)
                    link.startTransform = startGo.transform;
            }

            if (endToken != null)
            {
                GameObject endGo = FindTargetFromToken(endToken);
                if (endGo != null)
                    link.endTransform = endGo.transform;
            }

            // Apply other properties
            ApplyLinkProperties(link, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"OffMeshLink added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                costOverride = link.costOverride,
                biDirectional = link.biDirectional,
                activated = link.activated
            });
        }

        private static object LinkConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            OffMeshLink link = target.GetComponent<OffMeshLink>();
            if (link == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an OffMeshLink.");
            }

            Undo.RecordObject(link, "Configure OffMeshLink");
            ApplyLinkProperties(link, @params);

            EditorUtility.SetDirty(link);
            MarkSceneDirty(target);

            return new SuccessResponse($"OffMeshLink configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                costOverride = link.costOverride,
                biDirectional = link.biDirectional
            });
        }

        private static void ApplyLinkProperties(OffMeshLink link, JObject @params)
        {
            if (@params["costOverride"] != null || @params["cost"] != null)
                link.costOverride = ParamCoercion.CoerceFloat(@params["costOverride"] ?? @params["cost"], link.costOverride);

            if (@params["biDirectional"] != null || @params["bidirectional"] != null)
                link.biDirectional = ParamCoercion.CoerceBool(@params["biDirectional"] ?? @params["bidirectional"], link.biDirectional);

            if (@params["activated"] != null || @params["active"] != null)
                link.activated = ParamCoercion.CoerceBool(@params["activated"] ?? @params["active"], link.activated);

            if (@params["autoUpdatePositions"] != null)
                link.autoUpdatePositions = ParamCoercion.CoerceBool(@params["autoUpdatePositions"], link.autoUpdatePositions);

            if (@params["area"] != null)
                link.area = ParamCoercion.CoerceInt(@params["area"], link.area);
        }

        #endregion

        #region Helpers

        private static GameObject FindTarget(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null) return null;
            return FindTargetFromToken(targetToken);
        }

        private static GameObject FindTargetFromToken(JToken targetToken)
        {
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

        private static Vector3 ParseVector3(JToken token, Vector3 defaultValue)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 3)
            {
                return new Vector3(
                    ParamCoercion.CoerceFloat(arr[0], defaultValue.x),
                    ParamCoercion.CoerceFloat(arr[1], defaultValue.y),
                    ParamCoercion.CoerceFloat(arr[2], defaultValue.z)
                );
            }

            if (token is JObject obj)
            {
                return new Vector3(
                    ParamCoercion.CoerceFloat(obj["x"], defaultValue.x),
                    ParamCoercion.CoerceFloat(obj["y"], defaultValue.y),
                    ParamCoercion.CoerceFloat(obj["z"], defaultValue.z)
                );
            }

            return defaultValue;
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
