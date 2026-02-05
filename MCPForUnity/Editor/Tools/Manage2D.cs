using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity 2D features:
    /// - Sprites and SpriteRenderer
    /// - 2D Physics (Rigidbody2D, Collider2D)
    /// - Tilemaps
    /// - Sorting layers
    /// </summary>
    [McpForUnityTool("manage_2d")]
    public static class Manage2D
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
                    "sprite_create" => SpriteCreate(@params),
                    "sprite_configure" => SpriteConfigure(@params),
                    "sprite_get_info" => SpriteGetInfo(@params),
                    "rigidbody2d_add" => Rigidbody2DAdd(@params),
                    "rigidbody2d_configure" => Rigidbody2DConfigure(@params),
                    "collider2d_add" => Collider2DAdd(@params),
                    "collider2d_configure" => Collider2DConfigure(@params),
                    "tilemap_create" => TilemapCreate(@params),
                    "tilemap_set_tile" => TilemapSetTile(@params),
                    "tilemap_clear" => TilemapClear(@params),
                    "sorting_layer_set" => SortingLayerSet(@params),
                    "sprite_animator_add" => SpriteAnimatorAdd(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[Manage2D] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        #region Sprite Actions

        private static object SpriteCreate(JObject @params)
        {
            string texturePath = ParamCoercion.CoerceString(@params["texturePath"], null);
            if (string.IsNullOrEmpty(texturePath))
            {
                return new ErrorResponse("'texturePath' is required.");
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null)
            {
                return new ErrorResponse($"Sprite not found at: {texturePath}");
            }

            Vector3 position = ParseVector3(@params["position"], Vector3.zero);

            GameObject go = new GameObject(sprite.name);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            ApplySpriteProperties(sr, @params);

            Undo.RegisterCreatedObjectUndo(go, "Create Sprite");
            EditorSceneManager.MarkSceneDirty(go.scene);

            return new SuccessResponse($"Sprite '{sprite.name}' created.", new
            {
                instanceID = go.GetInstanceID(),
                name = go.name,
                spriteName = sprite.name
            });
        }

        private static object SpriteConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                return new ErrorResponse($"'{target.name}' has no SpriteRenderer.");
            }

            Undo.RecordObject(sr, "Configure Sprite");
            ApplySpriteProperties(sr, @params);

            EditorUtility.SetDirty(sr);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Sprite configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                color = ColorUtility.ToHtmlStringRGBA(sr.color),
                flipX = sr.flipX,
                flipY = sr.flipY,
                sortingLayer = sr.sortingLayerName,
                sortingOrder = sr.sortingOrder
            });
        }

        private static object SpriteGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                return new ErrorResponse($"'{target.name}' has no SpriteRenderer.");
            }

            return new SuccessResponse($"Sprite info for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                spriteName = sr.sprite?.name,
                color = ColorUtility.ToHtmlStringRGBA(sr.color),
                flipX = sr.flipX,
                flipY = sr.flipY,
                sortingLayer = sr.sortingLayerName,
                sortingOrder = sr.sortingOrder,
                drawMode = sr.drawMode.ToString()
            });
        }

        private static void ApplySpriteProperties(SpriteRenderer sr, JObject @params)
        {
            if (@params["color"] != null)
            {
                if (ColorUtility.TryParseHtmlString(ParamCoercion.CoerceString(@params["color"], ""), out Color c))
                    sr.color = c;
            }

            if (@params["flipX"] != null)
                sr.flipX = ParamCoercion.CoerceBool(@params["flipX"], sr.flipX);

            if (@params["flipY"] != null)
                sr.flipY = ParamCoercion.CoerceBool(@params["flipY"], sr.flipY);

            if (@params["sortingLayer"] != null)
                sr.sortingLayerName = ParamCoercion.CoerceString(@params["sortingLayer"], sr.sortingLayerName);

            if (@params["sortingOrder"] != null)
                sr.sortingOrder = ParamCoercion.CoerceInt(@params["sortingOrder"], sr.sortingOrder);
        }

        #endregion

        #region Rigidbody2D Actions

        private static object Rigidbody2DAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<Rigidbody2D>() != null)
            {
                return new ErrorResponse($"'{target.name}' already has Rigidbody2D.");
            }

            Rigidbody2D rb = Undo.AddComponent<Rigidbody2D>(target);
            ApplyRigidbody2DProperties(rb, @params);

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Rigidbody2D added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                bodyType = rb.bodyType.ToString(),
                mass = rb.mass,
                gravityScale = rb.gravityScale
            });
        }

        private static object Rigidbody2DConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                return new ErrorResponse($"'{target.name}' has no Rigidbody2D.");
            }

            Undo.RecordObject(rb, "Configure Rigidbody2D");
            ApplyRigidbody2DProperties(rb, @params);

            EditorUtility.SetDirty(rb);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Rigidbody2D configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                bodyType = rb.bodyType.ToString(),
                mass = rb.mass,
                gravityScale = rb.gravityScale,
                linearDrag = rb.linearDamping,
                angularDrag = rb.angularDamping
            });
        }

        private static void ApplyRigidbody2DProperties(Rigidbody2D rb, JObject @params)
        {
            if (@params["bodyType"] != null)
            {
                string bt = ParamCoercion.CoerceString(@params["bodyType"], "");
                if (Enum.TryParse<RigidbodyType2D>(bt, true, out var bodyType))
                    rb.bodyType = bodyType;
            }

            if (@params["mass"] != null)
                rb.mass = ParamCoercion.CoerceFloat(@params["mass"], rb.mass);

            if (@params["gravityScale"] != null)
                rb.gravityScale = ParamCoercion.CoerceFloat(@params["gravityScale"], rb.gravityScale);

            if (@params["linearDrag"] != null)
                rb.linearDamping = ParamCoercion.CoerceFloat(@params["linearDrag"], rb.linearDamping);

            if (@params["angularDrag"] != null)
                rb.angularDamping = ParamCoercion.CoerceFloat(@params["angularDrag"], rb.angularDamping);

            if (@params["freezeRotation"] != null)
                rb.freezeRotation = ParamCoercion.CoerceBool(@params["freezeRotation"], rb.freezeRotation);
        }

        #endregion

        #region Collider2D Actions

        private static object Collider2DAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            string type = ParamCoercion.CoerceString(@params["colliderType"], "box")?.ToLowerInvariant();

            Collider2D collider = type switch
            {
                "box" => Undo.AddComponent<BoxCollider2D>(target),
                "circle" => Undo.AddComponent<CircleCollider2D>(target),
                "capsule" => Undo.AddComponent<CapsuleCollider2D>(target),
                "polygon" => Undo.AddComponent<PolygonCollider2D>(target),
                "edge" => Undo.AddComponent<EdgeCollider2D>(target),
                "composite" => Undo.AddComponent<CompositeCollider2D>(target),
                _ => null
            };

            if (collider == null)
            {
                return new ErrorResponse($"Unknown collider type: '{type}'");
            }

            ApplyCollider2DProperties(collider, @params);

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"{type} Collider2D added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                colliderType = collider.GetType().Name,
                isTrigger = collider.isTrigger
            });
        }

        private static object Collider2DConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Collider2D collider = target.GetComponent<Collider2D>();
            if (collider == null)
            {
                return new ErrorResponse($"'{target.name}' has no Collider2D.");
            }

            Undo.RecordObject(collider, "Configure Collider2D");
            ApplyCollider2DProperties(collider, @params);

            EditorUtility.SetDirty(collider);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Collider2D configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                colliderType = collider.GetType().Name,
                isTrigger = collider.isTrigger
            });
        }

        private static void ApplyCollider2DProperties(Collider2D collider, JObject @params)
        {
            if (@params["isTrigger"] != null)
                collider.isTrigger = ParamCoercion.CoerceBool(@params["isTrigger"], collider.isTrigger);

            if (@params["offset"] != null)
                collider.offset = ParseVector2(@params["offset"], collider.offset);

            if (collider is BoxCollider2D box && @params["size"] != null)
                box.size = ParseVector2(@params["size"], box.size);

            if (collider is CircleCollider2D circle && @params["radius"] != null)
                circle.radius = ParamCoercion.CoerceFloat(@params["radius"], circle.radius);

            if (collider is CapsuleCollider2D capsule)
            {
                if (@params["size"] != null)
                    capsule.size = ParseVector2(@params["size"], capsule.size);
            }
        }

        #endregion

        #region Tilemap Actions

        private static object TilemapCreate(JObject @params)
        {
            string name = ParamCoercion.CoerceString(@params["tilemapName"], "Tilemap");

            // Create Grid parent
            GameObject gridGo = new GameObject("Grid");
            Grid grid = gridGo.AddComponent<Grid>();

            // Create Tilemap child
            GameObject tilemapGo = new GameObject(name);
            tilemapGo.transform.SetParent(gridGo.transform);

            Tilemap tilemap = tilemapGo.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapGo.AddComponent<TilemapRenderer>();

            Undo.RegisterCreatedObjectUndo(gridGo, "Create Tilemap");
            EditorSceneManager.MarkSceneDirty(gridGo.scene);

            return new SuccessResponse($"Tilemap '{name}' created.", new
            {
                gridInstanceID = gridGo.GetInstanceID(),
                tilemapInstanceID = tilemapGo.GetInstanceID(),
                name
            });
        }

        private static object TilemapSetTile(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Tilemap tilemap = target.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                return new ErrorResponse($"'{target.name}' has no Tilemap.");
            }

            string tilePath = ParamCoercion.CoerceString(@params["tilePath"], null);
            if (string.IsNullOrEmpty(tilePath))
            {
                return new ErrorResponse("'tilePath' is required.");
            }

            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
            if (tile == null)
            {
                return new ErrorResponse($"Tile not found at: {tilePath}");
            }

            Vector3Int position = ParseVector3Int(@params["position"], Vector3Int.zero);

            Undo.RecordObject(tilemap, "Set Tile");
            tilemap.SetTile(position, tile);

            EditorUtility.SetDirty(tilemap);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Tile set at ({position.x}, {position.y}).", new
            {
                instanceID = target.GetInstanceID(),
                position = new { x = position.x, y = position.y, z = position.z },
                tileName = tile.name
            });
        }

        private static object TilemapClear(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Tilemap tilemap = target.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                return new ErrorResponse($"'{target.name}' has no Tilemap.");
            }

            Undo.RecordObject(tilemap, "Clear Tilemap");
            tilemap.ClearAllTiles();

            EditorUtility.SetDirty(tilemap);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Tilemap '{target.name}' cleared.", new
            {
                instanceID = target.GetInstanceID()
            });
        }

        #endregion

        #region Sorting & Animation

        private static object SortingLayerSet(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return new ErrorResponse($"'{target.name}' has no Renderer.");
            }

            Undo.RecordObject(renderer, "Set Sorting Layer");

            if (@params["sortingLayer"] != null)
                renderer.sortingLayerName = ParamCoercion.CoerceString(@params["sortingLayer"], renderer.sortingLayerName);

            if (@params["sortingOrder"] != null)
                renderer.sortingOrder = ParamCoercion.CoerceInt(@params["sortingOrder"], renderer.sortingOrder);

            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Sorting set on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                sortingLayer = renderer.sortingLayerName,
                sortingOrder = renderer.sortingOrder
            });
        }

        private static object SpriteAnimatorAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(target);
            }

            string controllerPath = ParamCoercion.CoerceString(@params["animationController"], null);
            if (!string.IsNullOrEmpty(controllerPath))
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                    animator.runtimeAnimatorController = controller;
            }

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Animator added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasController = animator.runtimeAnimatorController != null
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
                return GameObjectLookup.FindById(targetToken.Value<int>());
            }

            string targetStr = targetToken.ToString();
            if (int.TryParse(targetStr, out int id))
            {
                var go = GameObjectLookup.FindById(id);
                if (go != null) return go;
            }

            return GameObjectLookup.FindByTarget(targetToken, "by_name", true);
        }

        private static object TargetNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Target not found: '{@params["target"]}'");
        }

        private static Vector3 ParseVector3(JToken token, Vector3 def)
        {
            if (token == null) return def;
            if (token is JObject obj)
            {
                return new Vector3(
                    ParamCoercion.CoerceFloat(obj["x"], def.x),
                    ParamCoercion.CoerceFloat(obj["y"], def.y),
                    ParamCoercion.CoerceFloat(obj["z"], def.z)
                );
            }
            return def;
        }

        private static Vector2 ParseVector2(JToken token, Vector2 def)
        {
            if (token == null) return def;
            if (token is JObject obj)
            {
                return new Vector2(
                    ParamCoercion.CoerceFloat(obj["x"], def.x),
                    ParamCoercion.CoerceFloat(obj["y"], def.y)
                );
            }
            return def;
        }

        private static Vector3Int ParseVector3Int(JToken token, Vector3Int def)
        {
            if (token == null) return def;
            if (token is JObject obj)
            {
                return new Vector3Int(
                    ParamCoercion.CoerceInt(obj["x"], def.x),
                    ParamCoercion.CoerceInt(obj["y"], def.y),
                    ParamCoercion.CoerceInt(obj["z"], def.z)
                );
            }
            return def;
        }

        #endregion
    }
}
