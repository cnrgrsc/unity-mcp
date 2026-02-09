"""
manage_ads.py - Ads & Monetization Tool

Handles:
- Unity Ads integration
- AdMob/Google Ads
- In-App Purchases (IAP)
- Rewarded videos, banners, interstitials
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_ads."""
    return {
        "name": "manage_ads",
        "description": """Manage Unity Ads, AdMob, and In-App Purchases for game monetization.

Actions:
- ads_initialize: Initialize Unity Ads with game ID
- ads_show_banner: Show banner ad
- ads_hide_banner: Hide banner ad
- ads_show_interstitial: Show interstitial ad
- ads_show_rewarded: Show rewarded video ad
- ads_check_ready: Check if ad is ready to show
- admob_initialize: Initialize Google AdMob
- admob_set_ids: Set AdMob unit IDs
- iap_initialize: Initialize In-App Purchases
- iap_add_product: Add IAP product
- iap_get_products: Get all IAP products
- iap_purchase: Initiate purchase
- iap_restore: Restore purchases
- get_settings: Get current ads/IAP settings
- setup_mediation: Configure ad mediation""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "ads_initialize",
                        "ads_show_banner",
                        "ads_hide_banner",
                        "ads_show_interstitial",
                        "ads_show_rewarded",
                        "ads_check_ready",
                        "admob_initialize",
                        "admob_set_ids",
                        "iap_initialize",
                        "iap_add_product",
                        "iap_get_products",
                        "iap_purchase",
                        "iap_restore",
                        "get_settings",
                        "setup_mediation"
                    ]
                },
                "gameId": {
                    "type": "string",
                    "description": "Unity Ads game ID"
                },
                "testMode": {
                    "type": "boolean",
                    "description": "Enable test mode for ads"
                },
                "placementId": {
                    "type": "string",
                    "description": "Ad placement ID"
                },
                "adType": {
                    "type": "string",
                    "description": "Type of ad",
                    "enum": ["Banner", "Interstitial", "Rewarded", "Native"]
                },
                "bannerPosition": {
                    "type": "string",
                    "description": "Banner position on screen",
                    "enum": ["Top", "Bottom", "TopLeft", "TopRight", "BottomLeft", "BottomRight", "Center"]
                },
                "admobAppId": {
                    "type": "string",
                    "description": "AdMob App ID"
                },
                "admobBannerId": {
                    "type": "string",
                    "description": "AdMob Banner unit ID"
                },
                "admobInterstitialId": {
                    "type": "string",
                    "description": "AdMob Interstitial unit ID"
                },
                "admobRewardedId": {
                    "type": "string",
                    "description": "AdMob Rewarded unit ID"
                },
                "productId": {
                    "type": "string",
                    "description": "IAP product ID"
                },
                "productType": {
                    "type": "string",
                    "description": "IAP product type",
                    "enum": ["Consumable", "NonConsumable", "Subscription"]
                },
                "productTitle": {
                    "type": "string",
                    "description": "IAP product display title"
                },
                "productPrice": {
                    "type": "string",
                    "description": "IAP product price (e.g., '0.99')"
                },
                "storeIds": {
                    "type": "object",
                    "description": "Store-specific product IDs",
                    "properties": {
                        "googlePlay": {"type": "string"},
                        "appleAppStore": {"type": "string"}
                    }
                },
                "mediationNetwork": {
                    "type": "string",
                    "description": "Ad mediation network",
                    "enum": ["IronSource", "AppLovin", "AdColony", "Vungle", "Unity"]
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_ads tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "gameId", "testMode", "placementId", "adType", "bannerPosition",
        "admobAppId", "admobBannerId", "admobInterstitialId", "admobRewardedId",
        "productId", "productType", "productTitle", "productPrice", "storeIds",
        "mediationNetwork"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_ads",
        "params": params
    })
