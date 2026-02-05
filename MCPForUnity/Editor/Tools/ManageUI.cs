using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity UI components:
    /// - Canvas: UI canvas configuration
    /// - UI Elements: Button, Text, Image, Panel, Input Field, etc.
    /// - EventSystem: Event system setup
    /// - Layout: Layout groups and content size fitters
    /// 
    /// Actions:
    /// - canvas_create: Create a new Canvas with EventSystem
    /// - canvas_configure: Configure existing Canvas
    /// - canvas_get_info: Get Canvas information
    /// - element_create: Create UI element (Button, Text, Image, etc.)
    /// - element_configure: Configure UI element properties
    /// - element_get_info: Get UI element information
    /// - event_system_add: Add EventSystem to scene
    /// - event_system_get_info: Get EventSystem information
    /// - layout_add: Add layout group to UI element
    /// - layout_configure: Configure layout group
    /// </summary>
    [McpForUnityTool("manage_ui")]
    public static class ManageUI
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
                    // Canvas actions
                    "canvas_create" => CanvasCreate(@params),
                    "canvas_configure" => CanvasConfigure(@params),
                    "canvas_get_info" => CanvasGetInfo(@params),
                    
                    // UI Element actions
                    "element_create" => ElementCreate(@params),
                    "element_configure" => ElementConfigure(@params),
                    "element_get_info" => ElementGetInfo(@params),
                    
                    // EventSystem actions
                    "event_system_add" => EventSystemAdd(@params),
                    "event_system_get_info" => EventSystemGetInfo(@params),
                    
                    // Layout actions
                    "layout_add" => LayoutAdd(@params),
                    "layout_configure" => LayoutConfigure(@params),
                    
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: canvas_create, canvas_configure, canvas_get_info, element_create, element_configure, element_get_info, event_system_add, event_system_get_info, layout_add, layout_configure")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageUI] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region Canvas Actions

        private static object CanvasCreate(JObject @params)
        {
            string name = ParamCoercion.CoerceString(@params["name"], "Canvas");
            
            // Create Canvas GameObject
            GameObject canvasGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = canvasGo.AddComponent<GraphicRaycaster>();

            // Set render mode
            string renderMode = ParamCoercion.CoerceString(@params["renderMode"] ?? @params["render_mode"], "ScreenSpaceOverlay")?.ToLowerInvariant();
            canvas.renderMode = renderMode switch
            {
                "screenspacecamera" or "screen_space_camera" => RenderMode.ScreenSpaceCamera,
                "worldspace" or "world_space" => RenderMode.WorldSpace,
                _ => RenderMode.ScreenSpaceOverlay
            };

            // Configure CanvasScaler
            string scaleMode = ParamCoercion.CoerceString(@params["scaleMode"] ?? @params["scale_mode"], "ConstantPixelSize")?.ToLowerInvariant();
            scaler.uiScaleMode = scaleMode switch
            {
                "scalewithscreensize" or "scale_with_screen_size" => CanvasScaler.ScaleMode.ScaleWithScreenSize,
                "constantphysicalsize" or "constant_physical_size" => CanvasScaler.ScaleMode.ConstantPhysicalSize,
                _ => CanvasScaler.ScaleMode.ConstantPixelSize
            };

            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                float refWidth = ParamCoercion.CoerceFloat(@params["referenceWidth"] ?? @params["refWidth"], 1920f);
                float refHeight = ParamCoercion.CoerceFloat(@params["referenceHeight"] ?? @params["refHeight"], 1080f);
                scaler.referenceResolution = new Vector2(refWidth, refHeight);
                scaler.matchWidthOrHeight = ParamCoercion.CoerceFloat(@params["matchWidthOrHeight"] ?? @params["match"], 0.5f);
            }

            // Ensure EventSystem exists
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                CreateEventSystem();
            }

            MarkSceneDirty(canvasGo);

            return new SuccessResponse($"Canvas '{name}' created.", new
            {
                instanceID = canvasGo.GetInstanceID(),
                name = name,
                renderMode = canvas.renderMode.ToString(),
                scaleMode = scaler.uiScaleMode.ToString(),
                referenceResolution = new { x = scaler.referenceResolution.x, y = scaler.referenceResolution.y }
            });
        }

        private static object CanvasConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Canvas component.");
            }

            Undo.RecordObject(canvas, "Configure Canvas");

            // Render mode
            string renderMode = ParamCoercion.CoerceString(@params["renderMode"] ?? @params["render_mode"], null)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(renderMode))
            {
                canvas.renderMode = renderMode switch
                {
                    "screenspacecamera" or "screen_space_camera" => RenderMode.ScreenSpaceCamera,
                    "worldspace" or "world_space" => RenderMode.WorldSpace,
                    _ => RenderMode.ScreenSpaceOverlay
                };
            }

            // Sort order
            if (@params["sortingOrder"] != null || @params["sortOrder"] != null)
                canvas.sortingOrder = ParamCoercion.CoerceInt(@params["sortingOrder"] ?? @params["sortOrder"], canvas.sortingOrder);

            // CanvasScaler
            CanvasScaler scaler = target.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Configure CanvasScaler");
                
                if (@params["referenceWidth"] != null || @params["referenceHeight"] != null)
                {
                    float refWidth = ParamCoercion.CoerceFloat(@params["referenceWidth"] ?? @params["refWidth"], scaler.referenceResolution.x);
                    float refHeight = ParamCoercion.CoerceFloat(@params["referenceHeight"] ?? @params["refHeight"], scaler.referenceResolution.y);
                    scaler.referenceResolution = new Vector2(refWidth, refHeight);
                }

                if (@params["matchWidthOrHeight"] != null || @params["match"] != null)
                    scaler.matchWidthOrHeight = ParamCoercion.CoerceFloat(@params["matchWidthOrHeight"] ?? @params["match"], scaler.matchWidthOrHeight);
            }

            EditorUtility.SetDirty(canvas);
            MarkSceneDirty(target);

            return new SuccessResponse($"Canvas configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                renderMode = canvas.renderMode.ToString(),
                sortingOrder = canvas.sortingOrder
            });
        }

        private static object CanvasGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null)
            {
                // List all canvases in scene
                Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                var canvasInfos = canvases.Select(c => new
                {
                    name = c.gameObject.name,
                    instanceID = c.gameObject.GetInstanceID(),
                    renderMode = c.renderMode.ToString(),
                    sortingOrder = c.sortingOrder
                }).ToArray();

                return new SuccessResponse($"Found {canvases.Length} Canvas(es) in scene.", new
                {
                    canvasCount = canvases.Length,
                    canvases = canvasInfos
                });
            }

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a Canvas component.");
            }

            CanvasScaler scaler = target.GetComponent<CanvasScaler>();

            return new SuccessResponse($"Canvas info for '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                renderMode = canvas.renderMode.ToString(),
                sortingOrder = canvas.sortingOrder,
                pixelPerfect = canvas.pixelPerfect,
                scaleMode = scaler?.uiScaleMode.ToString(),
                referenceResolution = scaler != null ? new { x = scaler.referenceResolution.x, y = scaler.referenceResolution.y } : null,
                matchWidthOrHeight = scaler?.matchWidthOrHeight
            });
        }

        #endregion

        #region UI Element Actions

        private static object ElementCreate(JObject @params)
        {
            string elementType = ParamCoercion.CoerceString(@params["type"] ?? @params["elementType"], null)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(elementType))
            {
                return new ErrorResponse("'type' is required. Options: button, text, image, panel, inputfield, slider, toggle, dropdown, scrollview");
            }

            string name = ParamCoercion.CoerceString(@params["name"], elementType);
            
            // Find parent Canvas or UI element
            GameObject parent = null;
            JToken parentToken = @params["parent"];
            if (parentToken != null)
            {
                parent = FindTargetFromToken(parentToken);
            }

            // If no parent specified, find or create a Canvas
            if (parent == null)
            {
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    // Create canvas first
                    var canvasResult = CanvasCreate(new JObject { ["name"] = "Canvas" });
                    if (canvasResult is SuccessResponse sr && sr.Data is object data)
                    {
                        var dataDict = data as dynamic;
                        int canvasId = ((dynamic)data).instanceID;
                        parent = GameObjectLookup.FindById(canvasId);
                    }
                }
                else
                {
                    parent = canvas.gameObject;
                }
            }

            if (parent == null)
            {
                return new ErrorResponse("Could not find or create a parent Canvas.");
            }

            GameObject uiElement = null;

            switch (elementType)
            {
                case "button":
                    uiElement = CreateButton(name, parent, @params);
                    break;
                case "text":
                case "textmeshpro":
                case "tmp":
                    uiElement = CreateText(name, parent, @params);
                    break;
                case "image":
                    uiElement = CreateImage(name, parent, @params);
                    break;
                case "panel":
                    uiElement = CreatePanel(name, parent, @params);
                    break;
                case "inputfield":
                case "input":
                    uiElement = CreateInputField(name, parent, @params);
                    break;
                case "slider":
                    uiElement = CreateSlider(name, parent, @params);
                    break;
                case "toggle":
                    uiElement = CreateToggle(name, parent, @params);
                    break;
                case "dropdown":
                    uiElement = CreateDropdown(name, parent, @params);
                    break;
                default:
                    return new ErrorResponse($"Unknown element type: '{elementType}'.");
            }

            if (uiElement == null)
            {
                return new ErrorResponse($"Failed to create UI element of type '{elementType}'.");
            }

            // Apply common RectTransform properties
            ApplyRectTransformProperties(uiElement.GetComponent<RectTransform>(), @params);

            MarkSceneDirty(uiElement);

            return new SuccessResponse($"UI element '{name}' created.", new
            {
                instanceID = uiElement.GetInstanceID(),
                name = name,
                type = elementType,
                parent = parent.name
            });
        }

        private static object ElementConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            Undo.RecordObject(target, "Configure UI Element");

            // Configure based on component types present
            Button button = target.GetComponent<Button>();
            if (button != null)
            {
                ConfigureButton(button, @params);
            }

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                ConfigureImage(image, @params);
            }

            Text text = target.GetComponent<Text>();
            if (text != null)
            {
                ConfigureText(text, @params);
            }

            TextMeshProUGUI tmpText = target.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                ConfigureTMPText(tmpText, @params);
            }

            Slider slider = target.GetComponent<Slider>();
            if (slider != null)
            {
                ConfigureSlider(slider, @params);
            }

            Toggle toggle = target.GetComponent<Toggle>();
            if (toggle != null)
            {
                ConfigureToggle(toggle, @params);
            }

            // Apply RectTransform properties
            RectTransform rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                ApplyRectTransformProperties(rectTransform, @params);
            }

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"UI element '{target.name}' configured.", new
            {
                instanceID = target.GetInstanceID(),
                name = target.name
            });
        }

        private static object ElementGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            var info = new Dictionary<string, object>
            {
                ["instanceID"] = target.GetInstanceID(),
                ["name"] = target.name,
                ["active"] = target.activeSelf
            };

            // RectTransform info
            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt != null)
            {
                info["rectTransform"] = new
                {
                    anchoredPosition = new { x = rt.anchoredPosition.x, y = rt.anchoredPosition.y },
                    sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y },
                    pivot = new { x = rt.pivot.x, y = rt.pivot.y },
                    anchorMin = new { x = rt.anchorMin.x, y = rt.anchorMin.y },
                    anchorMax = new { x = rt.anchorMax.x, y = rt.anchorMax.y }
                };
            }

            // Component-specific info
            Button button = target.GetComponent<Button>();
            if (button != null)
            {
                info["button"] = new { interactable = button.interactable };
            }

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                info["image"] = new
                {
                    hasSprite = image.sprite != null,
                    spriteName = image.sprite?.name,
                    color = new { r = image.color.r, g = image.color.g, b = image.color.b, a = image.color.a },
                    raycastTarget = image.raycastTarget
                };
            }

            Text text = target.GetComponent<Text>();
            if (text != null)
            {
                info["text"] = new
                {
                    content = text.text,
                    fontSize = text.fontSize,
                    fontStyle = text.fontStyle.ToString(),
                    alignment = text.alignment.ToString()
                };
            }

            TextMeshProUGUI tmpText = target.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                info["textMeshPro"] = new
                {
                    content = tmpText.text,
                    fontSize = tmpText.fontSize,
                    fontStyle = tmpText.fontStyle.ToString(),
                    alignment = tmpText.alignment.ToString()
                };
            }

            return new SuccessResponse($"UI element info for '{target.name}'.", info);
        }

        #region Element Creators

        private static GameObject CreateButton(string name, GameObject parent, JObject @params)
        {
            GameObject buttonGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Button");
            buttonGo.transform.SetParent(parent.transform, false);

            // Add Image for background
            Image image = buttonGo.AddComponent<Image>();
            image.color = ParseColor(@params["color"], new Color(1, 1, 1, 1));

            // Add Button component
            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            // Add text child (TMP preferred)
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(buttonGo.transform, false);
            
            TextMeshProUGUI tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = ParamCoercion.CoerceString(@params["text"], "Button");
            tmpText.fontSize = ParamCoercion.CoerceFloat(@params["fontSize"], 24f);
            tmpText.color = ParseColor(@params["textColor"], Color.black);
            tmpText.alignment = TextAlignmentOptions.Center;

            // Set text RectTransform to fill button
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Set default size
            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 160f),
                ParamCoercion.CoerceFloat(@params["height"], 40f)
            );

            return buttonGo;
        }

        private static GameObject CreateText(string name, GameObject parent, JObject @params)
        {
            GameObject textGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(textGo, "Create Text");
            textGo.transform.SetParent(parent.transform, false);

            TextMeshProUGUI tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = ParamCoercion.CoerceString(@params["text"], "Text");
            tmpText.fontSize = ParamCoercion.CoerceFloat(@params["fontSize"], 24f);
            tmpText.color = ParseColor(@params["color"], Color.white);
            
            string alignment = ParamCoercion.CoerceString(@params["alignment"], "center")?.ToLowerInvariant();
            tmpText.alignment = alignment switch
            {
                "left" => TextAlignmentOptions.Left,
                "right" => TextAlignmentOptions.Right,
                "topleft" => TextAlignmentOptions.TopLeft,
                "topright" => TextAlignmentOptions.TopRight,
                _ => TextAlignmentOptions.Center
            };

            RectTransform rect = textGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 200f),
                ParamCoercion.CoerceFloat(@params["height"], 50f)
            );

            return textGo;
        }

        private static GameObject CreateImage(string name, GameObject parent, JObject @params)
        {
            GameObject imageGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(imageGo, "Create Image");
            imageGo.transform.SetParent(parent.transform, false);

            Image image = imageGo.AddComponent<Image>();
            image.color = ParseColor(@params["color"], Color.white);

            // Load sprite if provided
            string spritePath = ParamCoercion.CoerceString(@params["sprite"] ?? @params["spritePath"], null);
            if (!string.IsNullOrEmpty(spritePath))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null) image.sprite = sprite;
            }

            RectTransform rect = imageGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 100f),
                ParamCoercion.CoerceFloat(@params["height"], 100f)
            );

            return imageGo;
        }

        private static GameObject CreatePanel(string name, GameObject parent, JObject @params)
        {
            GameObject panelGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(panelGo, "Create Panel");
            panelGo.transform.SetParent(parent.transform, false);

            Image image = panelGo.AddComponent<Image>();
            image.color = ParseColor(@params["color"], new Color(1, 1, 1, 0.4f));

            RectTransform rect = panelGo.GetComponent<RectTransform>();
            bool stretch = ParamCoercion.CoerceBool(@params["stretch"], false);
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = new Vector2(
                    ParamCoercion.CoerceFloat(@params["width"], 200f),
                    ParamCoercion.CoerceFloat(@params["height"], 200f)
                );
            }

            return panelGo;
        }

        private static GameObject CreateInputField(string name, GameObject parent, JObject @params)
        {
            // Create using TMP_InputField for better features
            GameObject inputGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(inputGo, "Create Input Field");
            inputGo.transform.SetParent(parent.transform, false);

            Image image = inputGo.AddComponent<Image>();
            image.color = ParseColor(@params["backgroundColor"], Color.white);

            TMP_InputField inputField = inputGo.AddComponent<TMP_InputField>();

            // Create text area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputGo.transform, false);
            RectMask2D mask = textArea.AddComponent<RectMask2D>();
            
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);

            // Create placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = ParamCoercion.CoerceString(@params["placeholder"], "Enter text...");
            placeholderText.fontSize = ParamCoercion.CoerceFloat(@params["fontSize"], 14f);
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.enableWordWrapping = false;
            
            RectTransform phRect = placeholder.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            // Create text object
            GameObject text = new GameObject("Text");
            text.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = ParamCoercion.CoerceFloat(@params["fontSize"], 14f);
            textComponent.color = ParseColor(@params["textColor"], Color.black);
            textComponent.enableWordWrapping = false;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;
            inputField.text = ParamCoercion.CoerceString(@params["text"], "");

            RectTransform rect = inputGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 200f),
                ParamCoercion.CoerceFloat(@params["height"], 40f)
            );

            return inputGo;
        }

        private static GameObject CreateSlider(string name, GameObject parent, JObject @params)
        {
            // Create basic slider structure
            GameObject sliderGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(sliderGo, "Create Slider");
            sliderGo.transform.SetParent(parent.transform, false);

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = ParamCoercion.CoerceFloat(@params["minValue"] ?? @params["min"], 0f);
            slider.maxValue = ParamCoercion.CoerceFloat(@params["maxValue"] ?? @params["max"], 1f);
            slider.value = ParamCoercion.CoerceFloat(@params["value"], 0.5f);
            slider.wholeNumbers = ParamCoercion.CoerceBool(@params["wholeNumbers"], false);

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderGo.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = ParseColor(@params["backgroundColor"], new Color(0.2f, 0.2f, 0.2f, 1f));
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            // Fill Area  
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = ParseColor(@params["fillColor"], new Color(0.3f, 0.7f, 1f, 1f));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;

            // Handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGo.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            RectTransform rect = sliderGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 160f),
                ParamCoercion.CoerceFloat(@params["height"], 20f)
            );

            return sliderGo;
        }

        private static GameObject CreateToggle(string name, GameObject parent, JObject @params)
        {
            GameObject toggleGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(toggleGo, "Create Toggle");
            toggleGo.transform.SetParent(parent.transform, false);

            Toggle toggle = toggleGo.AddComponent<Toggle>();
            toggle.isOn = ParamCoercion.CoerceBool(@params["isOn"] ?? @params["value"], false);

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleGo.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = Color.white;
            toggle.targetGraphic = bgImage;
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.anchoredPosition = new Vector2(10, -10);
            bgRect.sizeDelta = new Vector2(20, 20);

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = ParseColor(@params["checkmarkColor"], new Color(0.2f, 0.2f, 0.2f, 1f));
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(2, 2);
            checkRect.offsetMax = new Vector2(-2, -2);
            toggle.graphic = checkImage;

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(toggleGo.transform, false);
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = ParamCoercion.CoerceString(@params["label"] ?? @params["text"], "Toggle");
            labelText.fontSize = ParamCoercion.CoerceFloat(@params["fontSize"], 14f);
            labelText.color = ParseColor(@params["textColor"], Color.white);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(25, 0);
            labelRect.offsetMax = Vector2.zero;

            RectTransform rect = toggleGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 160f),
                ParamCoercion.CoerceFloat(@params["height"], 20f)
            );

            return toggleGo;
        }

        private static GameObject CreateDropdown(string name, GameObject parent, JObject @params)
        {
            GameObject dropdownGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(dropdownGo, "Create Dropdown");
            dropdownGo.transform.SetParent(parent.transform, false);

            Image image = dropdownGo.AddComponent<Image>();
            image.color = Color.white;

            TMP_Dropdown dropdown = dropdownGo.AddComponent<TMP_Dropdown>();

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(dropdownGo.transform, false);
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "Option A";
            labelText.fontSize = 14f;
            labelText.color = Color.black;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);
            dropdown.captionText = labelText;

            // Add default options
            JArray optionsArray = @params["options"] as JArray;
            if (optionsArray != null)
            {
                dropdown.options.Clear();
                foreach (JToken optToken in optionsArray)
                {
                    string optText = optToken.Type == JTokenType.String 
                        ? optToken.ToString() 
                        : ParamCoercion.CoerceString(((JObject)optToken)["text"], "Option");
                    dropdown.options.Add(new TMP_Dropdown.OptionData(optText));
                }
            }
            else
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("Option A"));
                dropdown.options.Add(new TMP_Dropdown.OptionData("Option B"));
                dropdown.options.Add(new TMP_Dropdown.OptionData("Option C"));
            }

            dropdown.value = ParamCoercion.CoerceInt(@params["value"], 0);
            dropdown.RefreshShownValue();

            RectTransform rect = dropdownGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                ParamCoercion.CoerceFloat(@params["width"], 160f),
                ParamCoercion.CoerceFloat(@params["height"], 30f)
            );

            return dropdownGo;
        }

        #endregion

        #region Element Configurators

        private static void ConfigureButton(Button button, JObject @params)
        {
            if (@params["interactable"] != null)
                button.interactable = ParamCoercion.CoerceBool(@params["interactable"], button.interactable);
        }

        private static void ConfigureImage(Image image, JObject @params)
        {
            if (@params["color"] != null)
                image.color = ParseColor(@params["color"], image.color);

            if (@params["sprite"] != null || @params["spritePath"] != null)
            {
                string spritePath = ParamCoercion.CoerceString(@params["sprite"] ?? @params["spritePath"], null);
                if (!string.IsNullOrEmpty(spritePath))
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite != null) image.sprite = sprite;
                }
            }

            if (@params["raycastTarget"] != null)
                image.raycastTarget = ParamCoercion.CoerceBool(@params["raycastTarget"], image.raycastTarget);
        }

        private static void ConfigureText(Text text, JObject @params)
        {
            if (@params["text"] != null)
                text.text = ParamCoercion.CoerceString(@params["text"], text.text);

            if (@params["fontSize"] != null)
                text.fontSize = ParamCoercion.CoerceInt(@params["fontSize"], text.fontSize);

            if (@params["color"] != null)
                text.color = ParseColor(@params["color"], text.color);
        }

        private static void ConfigureTMPText(TextMeshProUGUI tmpText, JObject @params)
        {
            if (@params["text"] != null)
                tmpText.text = ParamCoercion.CoerceString(@params["text"], tmpText.text);

            if (@params["fontSize"] != null)
                tmpText.fontSize = ParamCoercion.CoerceFloat(@params["fontSize"], tmpText.fontSize);

            if (@params["color"] != null)
                tmpText.color = ParseColor(@params["color"], tmpText.color);

            if (@params["alignment"] != null)
            {
                string alignment = ParamCoercion.CoerceString(@params["alignment"], "")?.ToLowerInvariant();
                tmpText.alignment = alignment switch
                {
                    "left" => TextAlignmentOptions.Left,
                    "right" => TextAlignmentOptions.Right,
                    "center" => TextAlignmentOptions.Center,
                    "topleft" => TextAlignmentOptions.TopLeft,
                    "topright" => TextAlignmentOptions.TopRight,
                    _ => tmpText.alignment
                };
            }
        }

        private static void ConfigureSlider(Slider slider, JObject @params)
        {
            if (@params["minValue"] != null || @params["min"] != null)
                slider.minValue = ParamCoercion.CoerceFloat(@params["minValue"] ?? @params["min"], slider.minValue);

            if (@params["maxValue"] != null || @params["max"] != null)
                slider.maxValue = ParamCoercion.CoerceFloat(@params["maxValue"] ?? @params["max"], slider.maxValue);

            if (@params["value"] != null)
                slider.value = ParamCoercion.CoerceFloat(@params["value"], slider.value);

            if (@params["wholeNumbers"] != null)
                slider.wholeNumbers = ParamCoercion.CoerceBool(@params["wholeNumbers"], slider.wholeNumbers);
        }

        private static void ConfigureToggle(Toggle toggle, JObject @params)
        {
            if (@params["isOn"] != null || @params["value"] != null)
                toggle.isOn = ParamCoercion.CoerceBool(@params["isOn"] ?? @params["value"], toggle.isOn);

            if (@params["interactable"] != null)
                toggle.interactable = ParamCoercion.CoerceBool(@params["interactable"], toggle.interactable);
        }

        #endregion

        #endregion

        #region EventSystem Actions

        private static object EventSystemAdd(JObject @params)
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                return new SuccessResponse("EventSystem already exists in scene.", new
                {
                    instanceID = existing.gameObject.GetInstanceID(),
                    name = existing.gameObject.name,
                    alreadyExisted = true
                });
            }

            GameObject eventSystemGo = CreateEventSystem();

            return new SuccessResponse("EventSystem created.", new
            {
                instanceID = eventSystemGo.GetInstanceID(),
                name = eventSystemGo.name
            });
        }

        private static object EventSystemGetInfo(JObject @params)
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                return new SuccessResponse("No EventSystem found in scene.", new
                {
                    exists = false
                });
            }

            return new SuccessResponse("EventSystem info.", new
            {
                exists = true,
                instanceID = eventSystem.gameObject.GetInstanceID(),
                name = eventSystem.gameObject.name,
                firstSelectedGameObject = eventSystem.firstSelectedGameObject?.name,
                sendNavigationEvents = eventSystem.sendNavigationEvents,
                pixelDragThreshold = eventSystem.pixelDragThreshold
            });
        }

        private static GameObject CreateEventSystem()
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
            MarkSceneDirty(eventSystemGo);
            return eventSystemGo;
        }

        #endregion

        #region Layout Actions

        private static object LayoutAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            string layoutType = ParamCoercion.CoerceString(@params["type"] ?? @params["layoutType"], null)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(layoutType))
            {
                return new ErrorResponse("'type' is required. Options: horizontal, vertical, grid");
            }

            Component layout = null;
            switch (layoutType)
            {
                case "horizontal":
                    layout = Undo.AddComponent<HorizontalLayoutGroup>(target);
                    break;
                case "vertical":
                    layout = Undo.AddComponent<VerticalLayoutGroup>(target);
                    break;
                case "grid":
                    layout = Undo.AddComponent<GridLayoutGroup>(target);
                    break;
                default:
                    return new ErrorResponse($"Unknown layout type: '{layoutType}'.");
            }

            ApplyLayoutProperties(layout, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"Layout '{layoutType}' added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                layoutType = layoutType
            });
        }

        private static object LayoutConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            LayoutGroup layout = target.GetComponent<LayoutGroup>();
            if (layout == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have a LayoutGroup component.");
            }

            Undo.RecordObject(layout, "Configure Layout");
            ApplyLayoutProperties(layout, @params);

            EditorUtility.SetDirty(layout);
            MarkSceneDirty(target);

            return new SuccessResponse($"Layout configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID()
            });
        }

        private static void ApplyLayoutProperties(Component layout, JObject @params)
        {
            if (layout is HorizontalOrVerticalLayoutGroup hvLayout)
            {
                if (@params["spacing"] != null)
                    hvLayout.spacing = ParamCoercion.CoerceFloat(@params["spacing"], hvLayout.spacing);

                if (@params["childForceExpandWidth"] != null)
                    hvLayout.childForceExpandWidth = ParamCoercion.CoerceBool(@params["childForceExpandWidth"], hvLayout.childForceExpandWidth);

                if (@params["childForceExpandHeight"] != null)
                    hvLayout.childForceExpandHeight = ParamCoercion.CoerceBool(@params["childForceExpandHeight"], hvLayout.childForceExpandHeight);

                if (@params["childControlWidth"] != null)
                    hvLayout.childControlWidth = ParamCoercion.CoerceBool(@params["childControlWidth"], hvLayout.childControlWidth);

                if (@params["childControlHeight"] != null)
                    hvLayout.childControlHeight = ParamCoercion.CoerceBool(@params["childControlHeight"], hvLayout.childControlHeight);
            }

            if (layout is GridLayoutGroup gridLayout)
            {
                if (@params["cellSize"] != null)
                    gridLayout.cellSize = ParseVector2(@params["cellSize"], gridLayout.cellSize);

                if (@params["spacing"] != null)
                    gridLayout.spacing = ParseVector2(@params["spacing"], gridLayout.spacing);

                if (@params["constraint"] != null)
                {
                    string constraint = ParamCoercion.CoerceString(@params["constraint"], "")?.ToLowerInvariant();
                    gridLayout.constraint = constraint switch
                    {
                        "fixedcolumncount" or "columns" => GridLayoutGroup.Constraint.FixedColumnCount,
                        "fixedrowcount" or "rows" => GridLayoutGroup.Constraint.FixedRowCount,
                        _ => GridLayoutGroup.Constraint.Flexible
                    };
                }

                if (@params["constraintCount"] != null)
                    gridLayout.constraintCount = ParamCoercion.CoerceInt(@params["constraintCount"], gridLayout.constraintCount);
            }

            // Padding (common to all layout groups)
            if (layout is LayoutGroup lg)
            {
                if (@params["padding"] != null)
                {
                    JToken paddingToken = @params["padding"];
                    if (paddingToken.Type == JTokenType.Integer)
                    {
                        int p = ParamCoercion.CoerceInt(paddingToken, 0);
                        lg.padding = new RectOffset(p, p, p, p);
                    }
                    else if (paddingToken is JObject padObj)
                    {
                        lg.padding = new RectOffset(
                            ParamCoercion.CoerceInt(padObj["left"], lg.padding.left),
                            ParamCoercion.CoerceInt(padObj["right"], lg.padding.right),
                            ParamCoercion.CoerceInt(padObj["top"], lg.padding.top),
                            ParamCoercion.CoerceInt(padObj["bottom"], lg.padding.bottom)
                        );
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private static void ApplyRectTransformProperties(RectTransform rt, JObject @params)
        {
            if (rt == null) return;

            Undo.RecordObject(rt, "Configure RectTransform");

            if (@params["position"] != null || @params["anchoredPosition"] != null)
                rt.anchoredPosition = ParseVector2(@params["position"] ?? @params["anchoredPosition"], rt.anchoredPosition);

            if (@params["width"] != null)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ParamCoercion.CoerceFloat(@params["width"], rt.rect.width));

            if (@params["height"] != null)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ParamCoercion.CoerceFloat(@params["height"], rt.rect.height));

            if (@params["sizeDelta"] != null)
                rt.sizeDelta = ParseVector2(@params["sizeDelta"], rt.sizeDelta);

            if (@params["pivot"] != null)
                rt.pivot = ParseVector2(@params["pivot"], rt.pivot);

            if (@params["anchorMin"] != null)
                rt.anchorMin = ParseVector2(@params["anchorMin"], rt.anchorMin);

            if (@params["anchorMax"] != null)
                rt.anchorMax = ParseVector2(@params["anchorMax"], rt.anchorMax);

            // Preset anchors
            string anchor = ParamCoercion.CoerceString(@params["anchor"], null)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(anchor))
            {
                switch (anchor)
                {
                    case "center":
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                        break;
                    case "topleft":
                        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                        break;
                    case "topright":
                        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                        break;
                    case "bottomleft":
                        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                        break;
                    case "bottomright":
                        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
                        break;
                    case "stretch":
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = rt.offsetMax = Vector2.zero;
                        break;
                }
            }
        }

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

        private static Color ParseColor(JToken token, Color defaultColor)
        {
            if (token == null) return defaultColor;

            if (token is JArray arr)
            {
                return new Color(
                    arr.Count > 0 ? ParamCoercion.CoerceFloat(arr[0], defaultColor.r) : defaultColor.r,
                    arr.Count > 1 ? ParamCoercion.CoerceFloat(arr[1], defaultColor.g) : defaultColor.g,
                    arr.Count > 2 ? ParamCoercion.CoerceFloat(arr[2], defaultColor.b) : defaultColor.b,
                    arr.Count > 3 ? ParamCoercion.CoerceFloat(arr[3], defaultColor.a) : defaultColor.a
                );
            }

            if (token is JObject obj)
            {
                return new Color(
                    ParamCoercion.CoerceFloat(obj["r"], defaultColor.r),
                    ParamCoercion.CoerceFloat(obj["g"], defaultColor.g),
                    ParamCoercion.CoerceFloat(obj["b"], defaultColor.b),
                    ParamCoercion.CoerceFloat(obj["a"], defaultColor.a)
                );
            }

            // Try parsing as hex color
            string colorStr = token.ToString();
            if (ColorUtility.TryParseHtmlString(colorStr, out Color parsedColor))
            {
                return parsedColor;
            }

            return defaultColor;
        }

        private static Vector2 ParseVector2(JToken token, Vector2 defaultValue)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 2)
            {
                return new Vector2(
                    ParamCoercion.CoerceFloat(arr[0], defaultValue.x),
                    ParamCoercion.CoerceFloat(arr[1], defaultValue.y)
                );
            }

            if (token is JObject obj)
            {
                return new Vector2(
                    ParamCoercion.CoerceFloat(obj["x"], defaultValue.x),
                    ParamCoercion.CoerceFloat(obj["y"], defaultValue.y)
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
