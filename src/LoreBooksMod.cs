using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoreBooks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    //[BepInDependency(GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class LoreBooksMod : BaseUnityPlugin
    {
        public const string GUID = "iggyandemo.lorebooks";
        public const string NAME = "LoreBooks";
        public const string VERSION = "1.0.0";
        public const string LoreBookFolderName = "LoreBooks";


        internal static ManualLogSource Log;
        public static LoreBooksMod Instance;

        public static ConfigEntry<float> UIScale;
        private GameObject UIPrefab => GetFromAssetBundle<GameObject>("lorebooks", "lorebookui", "UIBookPanel");

        private Dictionary<Character, UIBookPanel> UIBookInstances = new Dictionary<Character, UIBookPanel>();
        //itemID, LorebookData
        public Dictionary<int, LoreBook> StoredBooks = new Dictionary<int, LoreBook>();


        /// <summary>
        /// You can use this event to find out when the mod is fully loaded, in order to add new Books.
        /// </summary>
        public Action OnReady;
        public Action OnBooksLoaded;


        internal void Awake()
        {
            Log = this.Logger;
            Instance = this;
            UIScale = Config.Bind(NAME, $"{NAME} UI Scale", 0.75f, "UI Scaling?");
            Log.LogMessage($"{NAME} Loaded.");

            SL.OnPacksLoaded += SL_OnPacksLoaded;
            new Harmony(GUID).PatchAll();
        }

        private void SL_OnPacksLoaded()
        {
            FindXMLDefinitions();
        }

        public void Start()
        {
            OnReady?.Invoke();



            //OnBooksLoaded += () =>
            //{
            //    LoreBook featureBook = LoreBooksMod.Instance.StoredBooks[-2104];

            //    featureBook.OnBookOpened += (Character Character) =>
            //    {
            //        Character.CharacterUI.ShowInfoNotification("Book opened");
            //    };

            //    featureBook.OnPageOpened += (Character Character, int index) =>
            //    {
            //        Character.CharacterUI.ShowInfoNotification($"Page {index} opened");

            //        if (index == 0)
            //        {
            //            Character.StatusEffectMngr.AddStatusEffect("Health Recovery 4");
            //        }
            //        if (index == 1)
            //        {
            //            Character.StatusEffectMngr.AddStatusEffect("Health Recovery 4");
            //        }
            //    };


            //    featureBook.CanOpenPredicate += (Character Character, LoreBook LoreBook) =>
            //    {
            //        //internalized lexicon
            //        return Character.Inventory.SkillKnowledge.IsItemLearned(8205170);
            //    };


            //};

            //featureBook.AddOrUpdatePageContent(0, new PageContent(null, "Page 1", "<color=red>You can color text with the  </color> "));
            //featureBook.AddOrUpdatePageContent(1, new PageContent(null, "Page 2", "<color=green>PAGE TWO IS A PAGE OF HEALING</color>"));
            //AddLoreBook(-2104, "FeatureTest", featureBook);
        }
        

        private void FindXMLDefinitions()
        {
            string[] directoriesInPluginsFolder = Directory.GetDirectories(Paths.PluginPath);
            foreach (var directory in directoriesInPluginsFolder)
            {
                string Path = $"{directory}/{LoreBookFolderName}";

                if (HasFolder(Path))
                {
                    Log.LogMessage($"Found LoreBooks folder.");

                    string[] filePaths = Directory.GetFiles(Path, "*.xml");

                    foreach (var item in filePaths)
                    {
                        LoreBookDefinition bookDefinition = DeserializeFromXML<LoreBookDefinition>(item);

                        if (bookDefinition != null)
                        {
                            LoreBook loreBook = new LoreBook(bookDefinition.BookUID, bookDefinition.BookTitle, bookDefinition.BookTitlePageContent, null, null);

                            loreBook.AddOrUpdatePageContent(0, new PageContent(null, bookDefinition.BookTitle, bookDefinition.BookTitlePageContent));

                            for (int i = 0; i < bookDefinition.Pages.Count; i++)
                            {
                                int IPlus = i + 1;
                                loreBook.AddOrUpdatePageContent(IPlus, new PageContent(null, bookDefinition.Pages[i].PageTitle, bookDefinition.Pages[i].TextContent));
                            }


 
                            LoreBooksMod.Instance.AddLoreBook(bookDefinition.ItemID, bookDefinition.BookUID, loreBook);
                        }


                    }
                }
            }

            OnBooksLoaded?.Invoke();
        }

        public T DeserializeFromXML<T>(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StreamReader reader = new StreamReader(path);
            T deserialized = (T)serializer.Deserialize(reader.BaseStream);
            reader.Close();
            return deserialized;
        }

        private bool HasFolder(string FolderLocation)
        {
            return Directory.Exists(FolderLocation);
        }


        /// <summary>
        /// Adds a new LoreBook to the AvailableBooks dictionary
        /// </summary>
        /// <param name="bookItemID">The ItemID of the In-game book Item</param>
        /// <param name="bookUID">A Unique ID for your book to reference later with ShowBook(bookUID) </param>
        /// <param name="loreBook"></param>
        public void AddLoreBook(int bookItemID, string bookUID, LoreBook loreBook)
        {
            Log.LogMessage($"Adding Book UID : {bookUID} Page Count {loreBook.PageCount}");

            if (!StoredBooks.ContainsKey(bookItemID))
            {
                StoredBooks.Add(bookItemID, loreBook);
            }
            else StoredBooks[bookItemID] = loreBook;
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

                        RectTransform UIRect = (RectTransform)UIInstance.transform;
                        UIRect.SetParent(Character.CharacterUI.transform, false);
                        UIBookPanel UIBookManager = UIInstance.AddComponent<UIBookPanel>();
                        UIBookManager.SetParentCharacter(Character);
                        UIRect.localScale = new Vector3(LoreBooksMod.UIScale.Value, LoreBooksMod.UIScale.Value, LoreBooksMod.UIScale.Value);

                        //usual outward problems, need to delay it give it time to find references
                        DelayDo(() =>
                        {
                            UIBookManager.Hide();
                        }, 2f);

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
        public LoreBook GetLoreBook(int bookItemID)
        {
            if (StoredBooks.ContainsKey(bookItemID))
            {
                return StoredBooks[bookItemID];
            }

            return null;
        }

        #region Helpers
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
        public T GetFromAssetBundle<T>(string SLPackName, string AssetBundle, string key) where T : UnityEngine.Object
        {
            if (!SL.PacksLoaded)
            {
                return default(T);
            }

            return SL.GetSLPack(SLPackName).AssetBundles[AssetBundle].LoadAsset<T>(key);
        }
        #endregion
    }
}
