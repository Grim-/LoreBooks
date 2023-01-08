using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LoreBooks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class LoreBooksMod : BaseUnityPlugin
    {
        public const string GUID = "iggyandemo.lorebooks";
        public const string NAME = "LoreBooks";
        public const string VERSION = "1.0.0";

        internal static ManualLogSource Log;
        static LoreBooksMod Instance;

        private GameObject UIPrefab => GetFromAssetBundle<GameObject>("lorebooks", "lorebookui", "UIBookPanel");

        private Dictionary<Character, UIBookPanel> UIBookInstances = new Dictionary<Character, UIBookPanel>();

        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            Log = this.Logger;
            Instance = this;

            Log.LogMessage($"{NAME} Loaded.");
            // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
            new Harmony(GUID).PatchAll();
        }
        public void DelayDo(Action OnAfterDelay, float DelayTime)
        {
            StartCoroutine(DoAfterDelay(OnAfterDelay, DelayTime));
        }

        public IEnumerator DoAfterDelay(Action OnAfterDelay, float DelayTime)
        {
            yield return new WaitForSeconds(DelayTime);
            OnAfterDelay.Invoke();
            yield break;
        }

        public void CreateBookUIForCharacter(Character Character)
        {
            if (!UIBookInstances.ContainsKey(Character))
            {
                if (UIPrefab != null)
                {
                    if (Character.CharacterUI != null)
                    {
                        GameObject UIInstance = GameObject.Instantiate(UIPrefab);
                        ((RectTransform)UIInstance.transform).SetParent(Character.CharacterUI.transform, false);
                        UIBookPanel UIBookManager = UIInstance.AddComponent<UIBookPanel>();
                        UIBookManager.SetParentCharacter(Character);
                        UIBookManager.Hide();
                        UIBookInstances.Add(Character, UIBookManager);

                        Log.LogMessage($"Creating UI for{Character.Name}");
                    }
                    else
                    {
                        Log.LogMessage($"CharacterUI is null!");
                    }
                }
                else
                {
                    Log.LogMessage($"UIPrefab not found!");
                }
            }
            else
            {
                Log.LogMessage("Character already has a UI instance");
            }
        }

        public UIBookPanel GetBookManagerForCharacter(Character Character)
        {
            if (UIBookInstances.ContainsKey(Character))
            {
                return UIBookInstances[Character];
            }

            return null;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        public static class CharacterAwakePatch
        {
            static void Postfix(Character __instance)
            {
                LoreBooksMod.Instance.DelayDo(() =>
                {
                    if (__instance.IsLocalPlayer) LoreBooksMod.Instance.CreateBookUIForCharacter(__instance);
                }, 3f);
            }
        }

        public T GetFromAssetBundle<T>(string SLPackName, string AssetBundle, string key) where T : UnityEngine.Object
        {
            if (!SL.PacksLoaded)
            {
                return default(T);
            }

            return SL.GetSLPack(SLPackName).AssetBundles[AssetBundle].LoadAsset<T>(key);
        }
    }
}
