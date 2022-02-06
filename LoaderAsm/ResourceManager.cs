using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KarlsonLoader
{
    public class ResourceManager
    {
        private static Dictionary<string, AssetBundle> assetBundles = new Dictionary<string, AssetBundle>();

        public static void RegisterAssetBundle(string guid)
        {
            if (!File.Exists(Path.Combine(LoaderAsm.Loader.KarlsonLoaderDir, "UML", "res", guid)))
                return;
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(LoaderAsm.Loader.KarlsonLoaderDir, "UML", "res", guid));
            assetBundles.Add(guid, bundle);
        }

        public static T Load<T>(string guid, string name) where T : UnityEngine.Object
        {
            // get calling guid
            if (!assetBundles.ContainsKey(guid))
            {
                LoaderAsm.Loader.Log("[ERR] ResourceManager.Load(): Could not find the asset bundle (not existing / not loaded)");
                return null;
            }
            return assetBundles[guid].LoadAsset<T>(name);
        }
    }
}