using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Physics components:
    /// - Rigidbody: mass, drag, angularDrag, useGravity, isKinematic, constraints
    /// - Collider: BoxCollider, SphereCollider, CapsuleCollider, MeshCollider
    /// - Joint: FixedJoint, HingeJoint, SpringJoint, ConfigurableJoint
    /// - Physics operations: add_force, add_torque, set_velocity, raycast
    /// 
    /// Actions:
    /// - rigidbody_add: Add Rigidbody to GameObject
    /// - rigidbody_configure: Configure Rigidbody properties
    /// - rigidbody_get_info: Get Rigidbody info
    /// - collider_add: Add Collider (box, sphere, capsule, mesh)
    /// - collider_configure: Configure Collider properties
    /// - collider_get_info: Get Collider info
    /// - joint_add: Add Joint (fixed, hinge, spring, configurable)
    /// - joint_configure: Configure Joint properties
    /// - add_force: Apply force to Rigidbody
    /// - add_torque: Apply torque to Rigidbody
    /// - set_velocity: Set Rigidbody velocity
    /// - raycast: Perform physics raycast
    /// </summary>
    [McpForUnityTool("manage_physics")]
    public static class ManagePhysics
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
                    // Rigidbody actions
                    "rigidbody_add" => RigidbodyAdd(@params),
                    "rigidbody_configure" => RigidbodyConfigure(@params),
                    "rigidbody_get_info" => RigidbodyGetInfo(@params),
                    
                    // Collider actions
                    "collider_add" => ColliderAdd(@params),
                    "collider_configure" => ColliderConfigure(@params),
                    "collider_get_info" => ColliderGetInfo(@params),
                    
                    // Joint actions
                    "joint_add" => JointAdd(@params),
                    "joint_configure" => JointConfigure(@params),
                    
                    // Physics operations
                    "add_force" => AddForce(@params),
                    "add_torque" => AddTorque(@params),
                    "set_velocity" => SetVelocity(@params),
                    "raycast" => Raycast(@params),
                    
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: rigidbody_add, rigidbody_configure, rigidbody_get_info, collider_add, collider_configure, collider_get_info, joint_add, joint_configure, add_force, add_torque, set_velocity, raycast")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManagePhysics] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region Rigidbody Actions

        private static object RigidbodyAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<Rigidbody>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has a Rigidbody.");
            }

            Rigidbody rb = Undo.AddComponent<Rigidbody>(target);
            
            // Apply initial properties if provided
            ApplyRigidbodyProperties(rb, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"Rigidbody added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                mass = rb.mass,
                useGravity = rb.useGravity,
                isKinematic = rb.isKinematic
            });
        }

        private static object RigidbodyConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Rigidbody.");
            }

            Undo.RecordObject(rb, "Configure Rigidbody");
            ApplyRigidbodyProperties(rb, @params);

            EditorUtility.SetDirty(rb);
            MarkSceneDirty(target);

            return new SuccessResponse($"Rigidbody configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                mass = rb.mass,
                drag = rb.linearDamping,
                angularDrag = rb.angularDamping,
                useGravity = rb.useGravity,
                isKinematic = rb.isKinematic
            });
        }

        private static object RigidbodyGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Rigidbody.");
            }

            return new SuccessResponse($"Rigidbody info for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                mass = rb.mass,
                drag = rb.linearDamping,
                angularDrag = rb.angularDamping,
                useGravity = rb.useGravity,
                isKinematic = rb.isKinematic,
                interpolation = rb.interpolation.ToString(),
                collisionDetection = rb.collisionDetectionMode.ToString(),
                constraints = rb.constraints.ToString(),
                velocity = new { x = rb.linearVelocity.x, y = rb.linearVelocity.y, z = rb.linearVelocity.z },
                angularVelocity = new { x = rb.angularVelocity.x, y = rb.angularVelocity.y, z = rb.angularVelocity.z }
            });
        }

        private static void ApplyRigidbodyProperties(Rigidbody rb, JObject @params)
        {
            if (@params["mass"] != null)
                rb.mass = ParamCoercion.CoerceFloat(@params["mass"], rb.mass);
            
            if (@params["drag"] != null)
                rb.linearDamping = ParamCoercion.CoerceFloat(@params["drag"], rb.linearDamping);
            
            if (@params["angularDrag"] != null || @params["angular_drag"] != null)
                rb.angularDamping = ParamCoercion.CoerceFloat(@params["angularDrag"] ?? @params["angular_drag"], rb.angularDamping);
            
            if (@params["useGravity"] != null || @params["use_gravity"] != null)
                rb.useGravity = ParamCoercion.CoerceBool(@params["useGravity"] ?? @params["use_gravity"], rb.useGravity);
            
            if (@params["isKinematic"] != null || @params["is_kinematic"] != null)
                rb.isKinematic = ParamCoercion.CoerceBool(@params["isKinematic"] ?? @params["is_kinematic"], rb.isKinematic);
            
            if (@params["interpolation"] != null)
            {
                string interp = ParamCoercion.CoerceString(@params["interpolation"], "");
                if (Enum.TryParse<RigidbodyInterpolation>(interp, true, out var interpValue))
                    rb.interpolation = interpValue;
            }
            
            if (@params["collisionDetection"] != null || @params["collision_detection"] != null)
            {
                string cd = ParamCoercion.CoerceString(@params["collisionDetection"] ?? @params["collision_detection"], "");
                if (Enum.TryParse<CollisionDetectionMode>(cd, true, out var cdValue))
                    rb.collisionDetectionMode = cdValue;
            }
            
            if (@params["constraints"] != null)
            {
                string cons = ParamCoercion.CoerceString(@params["constraints"], "");
                if (Enum.TryParse<RigidbodyConstraints>(cons, true, out var consValue))
                    rb.constraints = consValue;
            }
        }

        #endregion

        #region Collider Actions

        private static object ColliderAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            string colliderType = ParamCoercion.CoerceString(@params["colliderType"] ?? @params["collider_type"] ?? @params["type"], "box")?.ToLowerInvariant();

            Collider collider = colliderType switch
            {
                "box" => Undo.AddComponent<BoxCollider>(target),
                "sphere" => Undo.AddComponent<SphereCollider>(target),
                "capsule" => Undo.AddComponent<CapsuleCollider>(target),
                "mesh" => Undo.AddComponent<MeshCollider>(target),
                _ => null
            };

            if (collider == null)
            {
                return new ErrorResponse($"Unknown collider type: '{colliderType}'. Supported: box, sphere, capsule, mesh");
            }

            // Apply initial properties
            ApplyColliderProperties(collider, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"{colliderType} collider added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                colliderType = collider.GetType().Name,
                isTrigger = collider.isTrigger
            });
        }

        private static object ColliderConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            string colliderType = ParamCoercion.CoerceString(@params["colliderType"] ?? @params["collider_type"] ?? @params["type"], null);
            
            Collider collider;
            if (!string.IsNullOrEmpty(colliderType))
            {
                collider = colliderType.ToLowerInvariant() switch
                {
                    "box" => target.GetComponent<BoxCollider>(),
                    "sphere" => target.GetComponent<SphereCollider>(),
                    "capsule" => target.GetComponent<CapsuleCollider>(),
                    "mesh" => target.GetComponent<MeshCollider>(),
                    _ => target.GetComponent<Collider>()
                };
            }
            else
            {
                collider = target.GetComponent<Collider>();
            }

            if (collider == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Collider{(colliderType != null ? $" of type '{colliderType}'" : "")}.");
            }

            Undo.RecordObject(collider, "Configure Collider");
            ApplyColliderProperties(collider, @params);

            EditorUtility.SetDirty(collider);
            MarkSceneDirty(target);

            return new SuccessResponse($"Collider configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                colliderType = collider.GetType().Name,
                isTrigger = collider.isTrigger
            });
        }

        private static object ColliderGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Collider[] colliders = target.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have any Colliders.");
            }

            var colliderInfos = new List<object>();
            foreach (var col in colliders)
            {
                var info = new Dictionary<string, object>
                {
                    ["type"] = col.GetType().Name,
                    ["isTrigger"] = col.isTrigger,
                    ["enabled"] = col.enabled
                };

                if (col is BoxCollider box)
                {
                    info["center"] = new { x = box.center.x, y = box.center.y, z = box.center.z };
                    info["size"] = new { x = box.size.x, y = box.size.y, z = box.size.z };
                }
                else if (col is SphereCollider sphere)
                {
                    info["center"] = new { x = sphere.center.x, y = sphere.center.y, z = sphere.center.z };
                    info["radius"] = sphere.radius;
                }
                else if (col is CapsuleCollider capsule)
                {
                    info["center"] = new { x = capsule.center.x, y = capsule.center.y, z = capsule.center.z };
                    info["radius"] = capsule.radius;
                    info["height"] = capsule.height;
                    info["direction"] = capsule.direction;
                }
                else if (col is MeshCollider mesh)
                {
                    info["convex"] = mesh.convex;
                    info["sharedMesh"] = mesh.sharedMesh != null ? mesh.sharedMesh.name : null;
                }

                colliderInfos.Add(info);
            }

            return new SuccessResponse($"Found {colliders.Length} collider(s) on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                colliders = colliderInfos
            });
        }

        private static void ApplyColliderProperties(Collider collider, JObject @params)
        {
            if (@params["isTrigger"] != null || @params["is_trigger"] != null)
                collider.isTrigger = ParamCoercion.CoerceBool(@params["isTrigger"] ?? @params["is_trigger"], collider.isTrigger);

            if (@params["enabled"] != null)
                collider.enabled = ParamCoercion.CoerceBool(@params["enabled"], collider.enabled);

            // Type-specific properties
            if (collider is BoxCollider box)
            {
                if (@params["center"] != null)
                    box.center = ParseVector3(@params["center"], box.center);
                if (@params["size"] != null)
                    box.size = ParseVector3(@params["size"], box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                if (@params["center"] != null)
                    sphere.center = ParseVector3(@params["center"], sphere.center);
                if (@params["radius"] != null)
                    sphere.radius = ParamCoercion.CoerceFloat(@params["radius"], sphere.radius);
            }
            else if (collider is CapsuleCollider capsule)
            {
                if (@params["center"] != null)
                    capsule.center = ParseVector3(@params["center"], capsule.center);
                if (@params["radius"] != null)
                    capsule.radius = ParamCoercion.CoerceFloat(@params["radius"], capsule.radius);
                if (@params["height"] != null)
                    capsule.height = ParamCoercion.CoerceFloat(@params["height"], capsule.height);
                if (@params["direction"] != null)
                    capsule.direction = ParamCoercion.CoerceInt(@params["direction"], capsule.direction);
            }
            else if (collider is MeshCollider mesh)
            {
                if (@params["convex"] != null)
                    mesh.convex = ParamCoercion.CoerceBool(@params["convex"], mesh.convex);
            }
        }

        #endregion

        #region Joint Actions

        private static object JointAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            string jointType = ParamCoercion.CoerceString(@params["jointType"] ?? @params["joint_type"] ?? @params["type"], "fixed")?.ToLowerInvariant();

            Joint joint = jointType switch
            {
                "fixed" => Undo.AddComponent<FixedJoint>(target),
                "hinge" => Undo.AddComponent<HingeJoint>(target),
                "spring" => Undo.AddComponent<SpringJoint>(target),
                "configurable" => Undo.AddComponent<ConfigurableJoint>(target),
                "character" => Undo.AddComponent<CharacterJoint>(target),
                _ => null
            };

            if (joint == null)
            {
                return new ErrorResponse($"Unknown joint type: '{jointType}'. Supported: fixed, hinge, spring, configurable, character");
            }

            // Set connected body if provided
            if (@params["connectedBody"] != null || @params["connected_body"] != null)
            {
                JToken connectedToken = @params["connectedBody"] ?? @params["connected_body"];
                GameObject connectedGo = FindTargetFromToken(connectedToken);
                if (connectedGo != null)
                {
                    Rigidbody connectedRb = connectedGo.GetComponent<Rigidbody>();
                    if (connectedRb != null)
                        joint.connectedBody = connectedRb;
                }
            }

            ApplyJointProperties(joint, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"{jointType} joint added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                jointType = joint.GetType().Name,
                connectedBody = joint.connectedBody?.gameObject.name
            });
        }

        private static object JointConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Joint joint = target.GetComponent<Joint>();
            if (joint == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Joint.");
            }

            Undo.RecordObject(joint, "Configure Joint");
            ApplyJointProperties(joint, @params);

            EditorUtility.SetDirty(joint);
            MarkSceneDirty(target);

            return new SuccessResponse($"Joint configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                jointType = joint.GetType().Name
            });
        }

        private static void ApplyJointProperties(Joint joint, JObject @params)
        {
            if (@params["breakForce"] != null || @params["break_force"] != null)
                joint.breakForce = ParamCoercion.CoerceFloat(@params["breakForce"] ?? @params["break_force"], joint.breakForce);
            
            if (@params["breakTorque"] != null || @params["break_torque"] != null)
                joint.breakTorque = ParamCoercion.CoerceFloat(@params["breakTorque"] ?? @params["break_torque"], joint.breakTorque);

            if (@params["anchor"] != null)
                joint.anchor = ParseVector3(@params["anchor"], joint.anchor);

            if (@params["connectedAnchor"] != null || @params["connected_anchor"] != null)
                joint.connectedAnchor = ParseVector3(@params["connectedAnchor"] ?? @params["connected_anchor"], joint.connectedAnchor);

            if (@params["autoConfigureConnectedAnchor"] != null)
                joint.autoConfigureConnectedAnchor = ParamCoercion.CoerceBool(@params["autoConfigureConnectedAnchor"], joint.autoConfigureConnectedAnchor);

            // Hinge-specific
            if (joint is HingeJoint hinge)
            {
                if (@params["useSpring"] != null || @params["use_spring"] != null)
                    hinge.useSpring = ParamCoercion.CoerceBool(@params["useSpring"] ?? @params["use_spring"], hinge.useSpring);
                
                if (@params["useLimits"] != null || @params["use_limits"] != null)
                    hinge.useLimits = ParamCoercion.CoerceBool(@params["useLimits"] ?? @params["use_limits"], hinge.useLimits);

                if (@params["useMotor"] != null || @params["use_motor"] != null)
                    hinge.useMotor = ParamCoercion.CoerceBool(@params["useMotor"] ?? @params["use_motor"], hinge.useMotor);
            }

            // Spring-specific
            if (joint is SpringJoint spring)
            {
                if (@params["spring"] != null)
                    spring.spring = ParamCoercion.CoerceFloat(@params["spring"], spring.spring);
                
                if (@params["damper"] != null)
                    spring.damper = ParamCoercion.CoerceFloat(@params["damper"], spring.damper);
                
                if (@params["minDistance"] != null || @params["min_distance"] != null)
                    spring.minDistance = ParamCoercion.CoerceFloat(@params["minDistance"] ?? @params["min_distance"], spring.minDistance);
                
                if (@params["maxDistance"] != null || @params["max_distance"] != null)
                    spring.maxDistance = ParamCoercion.CoerceFloat(@params["maxDistance"] ?? @params["max_distance"], spring.maxDistance);
            }
        }

        #endregion

        #region Physics Operations

        private static object AddForce(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Rigidbody.");
            }

            Vector3 force = ParseVector3(@params["force"], Vector3.zero);
            string modeStr = ParamCoercion.CoerceString(@params["mode"] ?? @params["forceMode"], "Force");
            
            if (!Enum.TryParse<ForceMode>(modeStr, true, out var forceMode))
                forceMode = ForceMode.Force;

            // Note: AddForce only works in PlayMode
            if (Application.isPlaying)
            {
                rb.AddForce(force, forceMode);
                return new SuccessResponse($"Force applied to '{target.name}'.", new
                {
                    instanceID = target.GetInstanceID(),
                    force = new { x = force.x, y = force.y, z = force.z },
                    mode = forceMode.ToString()
                });
            }
            else
            {
                return new
                {
                    success = true,
                    message = $"Force would be applied to '{target.name}' in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        force = new { x = force.x, y = force.y, z = force.z },
                        mode = forceMode.ToString(),
                        warning = "AddForce only works during PlayMode."
                    }
                };
            }
        }

        private static object AddTorque(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Rigidbody.");
            }

            Vector3 torque = ParseVector3(@params["torque"], Vector3.zero);
            string modeStr = ParamCoercion.CoerceString(@params["mode"] ?? @params["forceMode"], "Force");
            
            if (!Enum.TryParse<ForceMode>(modeStr, true, out var forceMode))
                forceMode = ForceMode.Force;

            if (Application.isPlaying)
            {
                rb.AddTorque(torque, forceMode);
                return new SuccessResponse($"Torque applied to '{target.name}'.", new
                {
                    instanceID = target.GetInstanceID(),
                    torque = new { x = torque.x, y = torque.y, z = torque.z },
                    mode = forceMode.ToString()
                });
            }
            else
            {
                return new
                {
                    success = true,
                    message = $"Torque would be applied to '{target.name}' in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        torque = new { x = torque.x, y = torque.y, z = torque.z },
                        mode = forceMode.ToString(),
                        warning = "AddTorque only works during PlayMode."
                    }
                };
            }
        }

        private static object SetVelocity(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Rigidbody.");
            }

            if (@params["velocity"] != null)
            {
                Vector3 velocity = ParseVector3(@params["velocity"], rb.linearVelocity);
                if (Application.isPlaying)
                    rb.linearVelocity = velocity;
            }

            if (@params["angularVelocity"] != null || @params["angular_velocity"] != null)
            {
                Vector3 angVel = ParseVector3(@params["angularVelocity"] ?? @params["angular_velocity"], rb.angularVelocity);
                if (Application.isPlaying)
                    rb.angularVelocity = angVel;
            }

            return new SuccessResponse($"Velocity set on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                velocity = new { x = rb.linearVelocity.x, y = rb.linearVelocity.y, z = rb.linearVelocity.z },
                angularVelocity = new { x = rb.angularVelocity.x, y = rb.angularVelocity.y, z = rb.angularVelocity.z },
                warning = Application.isPlaying ? null : "Velocity changes only take effect in PlayMode."
            });
        }

        private static object Raycast(JObject @params)
        {
            Vector3 origin = ParseVector3(@params["origin"], Vector3.zero);
            Vector3 direction = ParseVector3(@params["direction"], Vector3.forward);
            float maxDistance = ParamCoercion.CoerceFloat(@params["maxDistance"] ?? @params["max_distance"], Mathf.Infinity);
            int layerMask = ParamCoercion.CoerceInt(@params["layerMask"] ?? @params["layer_mask"], -1);

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, maxDistance, layerMask))
            {
                return new SuccessResponse("Raycast hit.", new
                {
                    hit = true,
                    point = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                    normal = new { x = hit.normal.x, y = hit.normal.y, z = hit.normal.z },
                    distance = hit.distance,
                    collider = hit.collider?.name,
                    gameObject = hit.collider?.gameObject.name,
                    instanceID = hit.collider?.gameObject.GetInstanceID()
                });
            }
            else
            {
                return new SuccessResponse("Raycast did not hit anything.", new
                {
                    hit = false,
                    origin = new { x = origin.x, y = origin.y, z = origin.z },
                    direction = new { x = direction.x, y = direction.y, z = direction.z },
                    maxDistance = maxDistance
                });
            }
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
