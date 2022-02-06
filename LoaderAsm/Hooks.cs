using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using Loader = LoaderAsm.Loader;
using System.Reflection;
using System.IO;
using UML;
using System.Security.Cryptography;
using System.Diagnostics;

namespace KarlsonLoader
{
    class MonoHooks : MonoBehaviour
    {
        public static MonoHooks instance { get; private set; }

        public void Start()
        {
            instance = this;
            StartCoroutine(InitPrefabs());
            StartCoroutine(OnAfterPrefabs());
        }

        private IEnumerator InitPrefabs()
        {
            yield return new WaitForSeconds(1f);
            Prefabs.InitializePrefabs();
        }

        public static List<Mod> mods = new List<Mod>();

        private IEnumerator OnAfterPrefabs()
        {
            yield return new WaitForSeconds(1.3f);
            Loader.loadingStatusString = "Preloading mods..";
            Loader.Log("Preloading mods..");
            foreach (string s in from x in Directory.GetFiles(Path.Combine(Loader.KarlsonLoaderDir, "UML", "mods"))
                                 where x.EndsWith(".dll")
                                 select x)
            {
                Loader.Log($"Loading file {Path.GetFileName(s)}");
                Assembly assembly = Assembly.LoadFrom(s);
                Loader.Log("  Loaded file.");
                //Loader.Log(assembly.GetTypes().Length + "");
                bool loaded = false;
                int num = 0;
                try
                {
                    foreach (Type type2 in assembly.GetTypes())
                    {
                        ModInfo customAttribute = type2.GetCustomAttribute<ModInfo>();
                        if (customAttribute == null && num != 2)
                        {
                            num = 1;
                        }
                        else
                        {
                            loaded = true;
                            mods.Add(new Mod(assembly, type2, customAttribute.name, customAttribute.author, customAttribute.version, customAttribute.guid, customAttribute.dependencies, customAttribute.modType, SHA256CheckSum(s)));
                            ResourceManager.RegisterAssetBundle(customAttribute.guid);
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Loader.Log(ex.ToString());
                    Loader.Log(ex.Message);
                    foreach (Exception iex in ex.LoaderExceptions)
                    {
                        Loader.Log(iex.ToString());
                        Loader.Log(iex.Message);
                    }
                }
                if (!loaded)
                {
                    Loader.Log(" Failed to load mod (No Type has an attribute of ModInfo set)");
                }
            }
            Loader.loadingStatusString = "Loading mods..";
            Loader.Log("Loading mods..");
            int lastRemMods = 0;
            int remainingMods = mods.Count;
            while (remainingMods > 0)
            {
                foreach (Mod mod in from x in mods
                                    where !x.isLoaded && x.dependecies.Count == 0
                                    select x)
                {
                    Loader.Log($"Loading mod {mod.name} {mod.version} ({mod.version}) - {mod.modType}");
                    object obj = Activator.CreateInstance(mod.type, null);
                    if (!(obj is IMod mod1))
                    {
                        Loader.Log(" Failed to load mod (The Type with an attribute of ModInfo is not inhereting from IMod)");
                    }
                    else
                    {
                        mod.StartInstance(mod1);
                        foreach (Mod mod2 in from x in mods
                                             where !x.isLoaded && x.dependecies.Contains(mod.guid)
                                             select x)
                        {
                            mod2.dependecies.Remove(mod.guid);
                        }
                        remainingMods--;
                    }
                }
                if (lastRemMods == remainingMods && remainingMods > 0)
                {
                    Loader.Log("Some mods could not be loaded, because they are missing a dependency:");
                    foreach (Mod mod in from x in mods
                                        where !x.isLoaded && x.dependecies.Count != 0
                                        select x)
                    {
                        Loader.Log($" Missing deps in mod {mod.name}:");
                        foreach (string dep in mod.dependecies)
                        {
                            Loader.Log($"  {dep}");
                        }
                    }
                    break;
                }
                lastRemMods = remainingMods;
            }
            Loader.Log("Loaded all mods");
            Loader.loading = false;
        }

        public static string SHA256CheckSum(string filePath)
        {
            string result;
            using (SHA256 sha = SHA256.Create())
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                result = BitConverter.ToString(sha.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }
            return result;
        }

        private float elapsedLoadingTime = 0f;
        private bool modlistShown = false;

        public void Update()
        {
            if (Loader.loading)
            {
                elapsedLoadingTime += Time.deltaTime;
            }
            if (Input.GetKeyDown(KeyCode.RightControl))
            {
                modlistShown = !modlistShown;
            }
            foreach (Mod m in from x in mods
                              where x.isLoaded
                              select x)
            {
                m.mod.Update(Time.deltaTime);
            }
        }

        public void OnGUI()
        {
            if (Loader.loading)
            {
                Texture2D blackTx = new Texture2D(1, 1);
                blackTx.SetPixel(0, 0, new Color(35f / 255f, 31f / 255f, 32f / 255f));
                blackTx.Apply();
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), blackTx, new Rect(0, 0, 1, 1));
                GUIStyle statusFont = new GUIStyle
                {
                    fontSize = 30,
                    fontStyle = FontStyle.Bold
                };
                statusFont.normal.textColor = Color.white;
                GUI.Label(new Rect((Screen.width - statusFont.CalcSize(new GUIContent(Loader.loadingStatusString)).x) / 2, 600f, 1000f, 1000f), Loader.loadingStatusString, statusFont);
                if (elapsedLoadingTime <= 1.5f)
                {
                    float a = 1f;
                    if (elapsedLoadingTime >= 0.5f)
                    {
                        a = 1.5f - elapsedLoadingTime;
                    }
                    blackTx.SetPixel(0, 0, new Color(35f / 255f, 31f / 255f, 32f / 255f, a));
                    blackTx.Apply();
                    GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), blackTx, new Rect(0, 0, 1, 1));
                }
            }
            else
            {
                GUIStyle infoFont = new GUIStyle
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.LowerLeft
                };
                infoFont.normal.textColor = Color.white;
                GUI.Label(new Rect(10f, 0f, 1000f, Screen.height), $"{mods.FindAll((m) => m.isLoaded).Count} mods loaded (RCTRL).\nKarlsonLoader (ASM v1.0)", infoFont);
            }
            if (modlistShown)
            {
                GUI.Box(new Rect(Screen.width - 305f, Screen.height - 305f, 300f, 300f), "Modlist");
                GUI.BeginGroup(new Rect(Screen.width - 305f, Screen.height - 305f, 300f, 300f));
                float yOffset = 18f;
                foreach (Mod m in from x in mods
                                  where x.isLoaded
                                  select x)
                {
                    GUI.Label(new Rect(5f, yOffset, 295f, 20f), $"{m.name} (v {m.version}) by {m.author}");
                    yOffset += 16f;
                }
                if ((from x in mods
                     where !x.isLoaded
                     select x).Count() > 0)
                {
                    GUI.Label(new Rect(5f, yOffset, 295f, 20f), $"Mods that didn't load: ({(from x in mods where !x.isLoaded select x).Count()})");
                    yOffset += 16f;
                    foreach (Mod m in from x in mods
                                      where !x.isLoaded
                                      select x)
                    {
                        GUI.Label(new Rect(5f, yOffset, 295f, 20f), $"{m.name} (v {m.version}) by {m.author}");
                        yOffset += 16f;
                    }
                }
                GUI.EndGroup();
            }
            foreach (Mod m in from x in mods
                              where x.isLoaded
                              select x)
            {
                m.mod.OnGUI();
            }
        }

        public void FixedUpdate()
        {

            foreach (Mod m in from x in mods
                              where x.isLoaded
                              select x)
            {
                m.mod.FixedUpdate(Time.fixedDeltaTime);
            }
        }

        public void OnApplicationQuit()
        {

            foreach (Mod m in from x in mods
                              where x.isLoaded
                              select x)
            {
                m.mod.OnApplicationQuit();
            }
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0); // fail safe
        }
    }

    public class Mod
    {
        public void StartInstance(IMod _mod)
        {
            mod = _mod;
            mod.Start();
            isLoaded = true;
        }

        public IMod mod { get; private set; }
        public bool isLoaded { get; private set; }

        public Mod(Assembly _asm, Type _type, string _name, string _author, string _version, string _guid, string[] _dependencies, ModType _modType, string _sha256)
        {
            asm = _asm;
            type = _type;
            name = _name;
            author = _author;
            version = _version;
            guid = _guid;
            dependecies = _dependencies.ToList();
            modType = _modType;
            sha256 = _sha256;
        }

        public readonly Assembly asm;
        public readonly string name;
        public readonly string author;
        public readonly string version;
        public readonly string guid;
        public List<string> dependecies;
        public readonly Type type;
        public readonly ModType modType;
        public readonly string sha256;
    }
}
