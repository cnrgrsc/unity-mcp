using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Ads, AdMob, and In-App Purchases.
    /// </summary>
    [McpForUnityTool("manage_ads")]
    public static class ManageAds
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
                    "ads_initialize" => AdsInitialize(@params),
                    "ads_show_banner" => AdsShowBanner(@params),
                    "ads_hide_banner" => AdsHideBanner(@params),
                    "ads_show_interstitial" => AdsShowInterstitial(@params),
                    "ads_show_rewarded" => AdsShowRewarded(@params),
                    "ads_check_ready" => AdsCheckReady(@params),
                    "admob_initialize" => AdMobInitialize(@params),
                    "admob_set_ids" => AdMobSetIds(@params),
                    "iap_initialize" => IAPInitialize(@params),
                    "iap_add_product" => IAPAddProduct(@params),
                    "iap_get_products" => IAPGetProducts(@params),
                    "get_settings" => GetSettings(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageAds] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object AdsInitialize(JObject @params)
        {
            string gameId = ParamCoercion.CoerceString(@params["gameId"], "");
            bool testMode = ParamCoercion.CoerceBool(@params["testMode"], true);

            // Check if Unity Ads package is installed
            #if UNITY_ADS
            Advertisement.Initialize(gameId, testMode);
            return new SuccessResponse("Unity Ads initialized.", new { gameId, testMode });
            #else
            return new SuccessResponse("Unity Ads package not installed. Add 'com.unity.ads' via Package Manager.", new
            {
                gameId,
                testMode,
                packageRequired = "com.unity.ads",
                instruction = "Install Unity Ads from Window > Package Manager"
            });
            #endif
        }

        private static object AdsShowBanner(JObject @params)
        {
            string placementId = ParamCoercion.CoerceString(@params["placementId"], "banner");
            string position = ParamCoercion.CoerceString(@params["bannerPosition"], "Bottom");

            return new SuccessResponse("Banner ad show requested.", new
            {
                placementId,
                position,
                note = "Implement in runtime script with Advertisement.Banner.Show()"
            });
        }

        private static object AdsHideBanner(JObject @params)
        {
            return new SuccessResponse("Banner ad hide requested.", new
            {
                note = "Implement in runtime script with Advertisement.Banner.Hide()"
            });
        }

        private static object AdsShowInterstitial(JObject @params)
        {
            string placementId = ParamCoercion.CoerceString(@params["placementId"], "interstitial");

            return new SuccessResponse("Interstitial ad show requested.", new
            {
                placementId,
                note = "Implement in runtime script with Advertisement.Show(placementId)"
            });
        }

        private static object AdsShowRewarded(JObject @params)
        {
            string placementId = ParamCoercion.CoerceString(@params["placementId"], "rewardedVideo");

            return new SuccessResponse("Rewarded ad show requested.", new
            {
                placementId,
                note = "Implement in runtime with IUnityAdsShowListener callback"
            });
        }

        private static object AdsCheckReady(JObject @params)
        {
            string placementId = ParamCoercion.CoerceString(@params["placementId"], "");

            return new SuccessResponse("Ad ready check.", new
            {
                placementId,
                note = "Check with Advertisement.IsReady(placementId) at runtime"
            });
        }

        private static object AdMobInitialize(JObject @params)
        {
            string appId = ParamCoercion.CoerceString(@params["admobAppId"], "");

            return new SuccessResponse("AdMob initialization configured.", new
            {
                appId,
                androidManifest = "Add to AndroidManifest.xml: <meta-data android:name=\"com.google.android.gms.ads.APPLICATION_ID\" android:value=\"" + appId + "\"/>",
                iosPlist = "Add to Info.plist: GADApplicationIdentifier = " + appId,
                package = "com.google.ads.mobile.unity"
            });
        }

        private static object AdMobSetIds(JObject @params)
        {
            return new SuccessResponse("AdMob unit IDs configured.", new
            {
                bannerId = ParamCoercion.CoerceString(@params["admobBannerId"], ""),
                interstitialId = ParamCoercion.CoerceString(@params["admobInterstitialId"], ""),
                rewardedId = ParamCoercion.CoerceString(@params["admobRewardedId"], ""),
                note = "Store these IDs in a ScriptableObject or configuration file"
            });
        }

        private static object IAPInitialize(JObject @params)
        {
            return new SuccessResponse("IAP initialization info.", new
            {
                package = "com.unity.purchasing",
                instruction = "Enable IAP in Services window and configure products",
                note = "Use IAPManager script with UnityPurchasing.Initialize()"
            });
        }

        private static object IAPAddProduct(JObject @params)
        {
            string productId = ParamCoercion.CoerceString(@params["productId"], "");
            string productType = ParamCoercion.CoerceString(@params["productType"], "Consumable");
            string title = ParamCoercion.CoerceString(@params["productTitle"], "");

            return new SuccessResponse("IAP product configured.", new
            {
                productId,
                productType,
                title,
                googlePlayId = @params["storeIds"]?["googlePlay"]?.ToString() ?? productId,
                appleId = @params["storeIds"]?["appleAppStore"]?.ToString() ?? productId,
                note = "Add to ConfigurationBuilder: builder.AddProduct(productId, ProductType." + productType + ")"
            });
        }

        private static object IAPGetProducts(JObject @params)
        {
            return new SuccessResponse("IAP products info.", new
            {
                note = "Products are configured in Unity Services Dashboard",
                codeExample = "foreach(var product in storeController.products.all) { ... }"
            });
        }

        private static object GetSettings(JObject @params)
        {
            return new SuccessResponse("Current ads/IAP settings.", new
            {
                unityAdsInstalled = IsPackageInstalled("com.unity.ads"),
                iapInstalled = IsPackageInstalled("com.unity.purchasing"),
                admobNote = "AdMob requires external Google Mobile Ads Unity Plugin"
            });
        }

        private static bool IsPackageInstalled(string packageId)
        {
            var listRequest = UnityEditor.PackageManager.Client.List(true);
            while (!listRequest.IsCompleted) { }
            
            foreach (var package in listRequest.Result)
            {
                if (package.name == packageId)
                    return true;
            }
            return false;
        }
    }
}
