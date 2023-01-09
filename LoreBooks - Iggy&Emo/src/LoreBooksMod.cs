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
        public static LoreBooksMod Instance;

        private GameObject UIPrefab => GetFromAssetBundle<GameObject>("lorebooks", "lorebookui", "UIBookPanel");

        private Dictionary<Character, UIBookPanel> UIBookInstances = new Dictionary<Character, UIBookPanel>();
        //itemID, LorebookData
        public Dictionary<int, LoreBook> StoredBooks = new Dictionary<int, LoreBook>();

        public Action OnReady;

        internal void Awake()
        {
            Log = this.Logger;
            Instance = this;

            Log.LogMessage($"{NAME} Loaded.");

            SL.BeforePacksLoaded += SL_BeforePacksLoaded;

            new Harmony(GUID).PatchAll();
        }

        public void Start()
        {
            OnReady?.Invoke();
            AddDummyBooks();
        }
        
        private void AddDummyBooks()
        {
            LoreBook TestBook = new LoreBook("EMONOMICON", "Emo-nomincon a beginners guide to eldritch horrors.", null);
            TestBook.AddOrUpdatePageContent(0, new PageContent(null, "FIRST PAGE TITLE", "SHIBBLY DIBBLY DOO."));
            TestBook.AddOrUpdatePageContent(1, new PageContent(null, "BOOZU MILK", "It was the second age of man when Boozu milk first found its way to our shores, " +
                "everyone was like 'yeah but how did he know doing that to a cow would produce milk? Does that not warrant its own line of questioning? No one else find it strange?'"));

            TestBook.AddOrUpdatePageContent(2, new PageContent(null, "THE THIRD PAGE", "Nobody knows what the third page contains as it is lost to time."));

            AddLoreBook(-2104, "EMONOMICON", TestBook);

            LoreBook Anotherbook = new LoreBook("EmoTwo", "A truly awful book.", null);

            Anotherbook.AddOrUpdatePageContent(0, new PageContent(null, "HAHAHA", "*fart*"));
            Anotherbook.AddOrUpdatePageContent(1, new PageContent(null, "AGAIN", "*fart* *fart*"));
            Anotherbook.AddOrUpdatePageContent(2, new PageContent(null, "RUDE", "Nobody knows what the third page contains as it is lost to time."));

            AddLoreBook(-2105, "EmoTwo", Anotherbook);
        }

        private void SL_BeforePacksLoaded()
        {
            SL_Item Emonomicon = new SL_Item()
            {
                Target_ItemID = 5601001,
                New_ItemID = -2104,
                Name = "THE EMONOMICON",
                Description = "A Profane book."
            };

            Emonomicon.ApplyTemplate();

            SL_Item OtherBook = new SL_Item()
            {
                Target_ItemID = 5601001,
                New_ItemID = -2105,
                Name = "ANOTHER BOOK!",
                Description = "A book."
            };

            OtherBook.ApplyTemplate();
        }

        public void AddLoreBook(int bookItemID, string bookUID, LoreBook loreBook)
        {
            if (!StoredBooks.ContainsKey(bookItemID))
            {
                StoredBooks.Add(bookItemID, loreBook);
            }
            else StoredBooks[bookItemID] = loreBook;
        }
        public LoreBook GetLoreBook(int bookItemID)
        {
            if (StoredBooks.ContainsKey(bookItemID))
            {
                return StoredBooks[bookItemID];
            }

            return null;
        }




        //Creates an instance of the BookUI for each Character, it is parented to the Characters.CharacterUI Canvas.
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

                        //usual outward problems, need to delay it give it time to find references
                        DelayDo(() =>
                        {
                            UIBookManager.Hide();
                        }, 1f);

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
