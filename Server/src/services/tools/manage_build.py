"""
manage_build.py - Build & Deployment Tool

Handles:
- Build settings configuration
- Platform switching
- Player settings
- Build execution
- Scenes in build
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_build."""
    return {
        "name": "manage_build",
        "description": """Manage Unity build settings, player settings, and execute builds for all platforms including mobile (iOS/Android).

Actions:
- build_get_settings: Get current build settings
- build_set_platform: Switch target platform (Android, iOS, Windows, macOS, WebGL, etc.)
- build_add_scene: Add scene to build
- build_remove_scene: Remove scene from build
- build_set_scenes: Set all scenes in build
- build_execute: Execute build
- player_get_settings: Get player settings
- player_set_settings: Set player settings (product name, company, version, etc.)
- define_add: Add scripting define symbol
- define_remove: Remove scripting define symbol
- define_list: List all scripting define symbols
- android_get_settings: Get Android-specific settings
- android_set_settings: Set Android settings (minSdk, targetSdk, keystore, etc.)
- ios_get_settings: Get iOS-specific settings
- ios_set_settings: Set iOS settings (teamId, signing, deployment target, etc.)""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "build_get_settings",
                        "build_set_platform",
                        "build_add_scene",
                        "build_remove_scene",
                        "build_set_scenes",
                        "build_execute",
                        "player_get_settings",
                        "player_set_settings",
                        "define_add",
                        "define_remove",
                        "define_list",
                        "android_get_settings",
                        "android_set_settings",
                        "ios_get_settings",
                        "ios_set_settings"
                    ]
                },
                "platform": {
                    "type": "string",
                    "description": "Target platform",
                    "enum": [
                        "StandaloneWindows64",
                        "StandaloneOSX",
                        "StandaloneLinux64",
                        "Android",
                        "iOS",
                        "WebGL",
                        "PS4",
                        "PS5",
                        "XboxOne",
                        "Switch"
                    ]
                },
                "scenePath": {
                    "type": "string",
                    "description": "Scene path to add/remove"
                },
                "scenePaths": {
                    "type": "array",
                    "description": "List of scene paths",
                    "items": {"type": "string"}
                },
                "buildPath": {
                    "type": "string",
                    "description": "Output path for build"
                },
                "buildOptions": {
                    "type": "array",
                    "description": "Build options",
                    "items": {
                        "type": "string",
                        "enum": ["Development", "AutoRunPlayer", "ShowBuiltPlayer", "CleanBuildCache", "AllowDebugging"]
                    }
                },
                "productName": {
                    "type": "string",
                    "description": "Product name"
                },
                "companyName": {
                    "type": "string",
                    "description": "Company name"
                },
                "version": {
                    "type": "string",
                    "description": "Bundle version"
                },
                "bundleIdentifier": {
                    "type": "string",
                    "description": "Bundle identifier (e.g., com.company.product)"
                },
                "defineSymbol": {
                    "type": "string",
                    "description": "Scripting define symbol"
                },
                "icon": {
                    "type": "string",
                    "description": "Path to icon texture"
                },
                "splashScreen": {
                    "type": "string",
                    "description": "Path to splash screen texture"
                },
                # Android-specific settings
                "minSdkVersion": {
                    "type": "integer",
                    "description": "Android minimum SDK version (e.g., 24 for Android 7.0)"
                },
                "targetSdkVersion": {
                    "type": "integer",
                    "description": "Android target SDK version (e.g., 34 for Android 14)"
                },
                "keystorePath": {
                    "type": "string",
                    "description": "Path to Android keystore file"
                },
                "keystorePassword": {
                    "type": "string",
                    "description": "Keystore password"
                },
                "keyAliasName": {
                    "type": "string",
                    "description": "Key alias name in keystore"
                },
                "keyAliasPassword": {
                    "type": "string",
                    "description": "Key alias password"
                },
                "androidBuildSystem": {
                    "type": "string",
                    "description": "Android build system",
                    "enum": ["Gradle", "Internal"]
                },
                "exportAsGoogleAndroidProject": {
                    "type": "boolean",
                    "description": "Export as Android Studio project instead of APK/AAB"
                },
                "buildAppBundle": {
                    "type": "boolean",
                    "description": "Build Android App Bundle (AAB) instead of APK"
                },
                "targetArchitectures": {
                    "type": "array",
                    "description": "Target CPU architectures for Android",
                    "items": {
                        "type": "string",
                        "enum": ["ARMv7", "ARM64", "X86", "X86_64"]
                    }
                },
                # iOS-specific settings
                "appleTeamId": {
                    "type": "string",
                    "description": "Apple Developer Team ID for signing"
                },
                "appleDeveloperTeamID": {
                    "type": "string",
                    "description": "Apple Developer Team ID (same as appleTeamId)"
                },
                "iOSManualSigningProvisioningProfileID": {
                    "type": "string",
                    "description": "iOS Provisioning Profile ID for manual signing"
                },
                "iOSManualSigningProvisioningProfileType": {
                    "type": "string",
                    "description": "Provisioning profile type",
                    "enum": ["Development", "Distribution"]
                },
                "automaticSigning": {
                    "type": "boolean",
                    "description": "Use automatic signing for iOS"
                },
                "iOSTargetDeployment": {
                    "type": "string",
                    "description": "iOS deployment target version (e.g., 13.0)"
                },
                "targetOSVersionString": {
                    "type": "string",
                    "description": "Target iOS version string"
                },
                "iOSCameraUsageDescription": {
                    "type": "string",
                    "description": "Camera usage description for iOS privacy"
                },
                "iOSMicrophoneUsageDescription": {
                    "type": "string",
                    "description": "Microphone usage description for iOS privacy"
                },
                "iOSLocationUsageDescription": {
                    "type": "string",
                    "description": "Location usage description for iOS privacy"
                },
                "iOSAppInBackgroundBehavior": {
                    "type": "string",
                    "description": "iOS app behavior when in background",
                    "enum": ["Suspend", "Custom"]
                },
                "requiredDeviceCapabilities": {
                    "type": "array",
                    "description": "Required device capabilities for iOS (e.g., arm64, metal)",
                    "items": {"type": "string"}
                },
                "supportedOrientations": {
                    "type": "array",
                    "description": "Supported screen orientations",
                    "items": {
                        "type": "string",
                        "enum": ["Portrait", "PortraitUpsideDown", "LandscapeLeft", "LandscapeRight"]
                    }
                }
            },
            "required": ["action"]
        }
    }



async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_build tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        # General settings
        "platform", "scenePath", "scenePaths", "buildPath", "buildOptions",
        "productName", "companyName", "version", "bundleIdentifier",
        "defineSymbol", "icon", "splashScreen",
        # Android settings
        "minSdkVersion", "targetSdkVersion", "keystorePath", "keystorePassword",
        "keyAliasName", "keyAliasPassword", "androidBuildSystem",
        "exportAsGoogleAndroidProject", "buildAppBundle", "targetArchitectures",
        # iOS settings
        "appleTeamId", "appleDeveloperTeamID", "iOSManualSigningProvisioningProfileID",
        "iOSManualSigningProvisioningProfileType", "automaticSigning",
        "iOSTargetDeployment", "targetOSVersionString",
        "iOSCameraUsageDescription", "iOSMicrophoneUsageDescription",
        "iOSLocationUsageDescription", "iOSAppInBackgroundBehavior",
        "requiredDeviceCapabilities", "supportedOrientations"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_build",
        "params": params
    })

