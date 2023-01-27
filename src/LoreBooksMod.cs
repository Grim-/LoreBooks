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
using UnityEngine.UI;

namespace LoreBooks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class LoreBooksMod : BaseUnityPlugin
    {
        public const string GUID = "iggyandemo.lorebooks";
        public const string NAME = "LoreBooks";
        public const string VERSION = "1.0.0";

        //XML Folder Name
        public const string LoreBookFolderName = "LoreBooks";

        public static int MagicalTomeHair = -2920;
        public static int MagicalTomeHairColor = -2921;

        public static int EmonomiconID = -2905;
        //Geps note
        public static int GepsNoteID = 5601001;
        public static int TribalFavour = 5600050;

        public const string EXTRA_ACTION_KEY_MOD = "SeekingStone_Modifier";
        public const string EXTRA_ACTION_KEY = "SeekingStone__Key";

        public static GameObject BookButtonPrefab;

        internal static ManualLogSource Log;
        public static LoreBooksMod Instance;


        #region Config Entrys
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<float> PageTransitionSpeed;
        public static ConfigEntry<float> LineSpacing;
        public static ConfigEntry<int> FontMinSize;
        public static ConfigEntry<int> FontMaxSize;
        public static ConfigEntry<Color> TextColor;
        public static ConfigEntry<TextAnchor> TextAlignment;
        #endregion

        private GameObject UIPrefab => GetFromAssetBundle<GameObject>("lorebooks", "lorebookui", "UIBookPanel");

        //itemID, LorebookData
        public Dictionary<int, LoreBook> StoredBooks = new Dictionary<int, LoreBook>();


        /// <summary>
        /// You can use this event to find out when the mod is fully loaded, in order to add new Books.
        /// </summary>
        public Action OnReady;
        /// <summary>
        /// This event fires when all book definitions are deserialized. (You can use this to reference a book and add actions to the events, or add pages dynamically)
        /// </summary>
        public Action OnBooksLoaded;

        public Dictionary<Character, UIBookPanel> UIBookInstances = new Dictionary<Character, UIBookPanel>();

        internal void Awake()
        {
            Log = this.Logger;
            Instance = this;


            UIScale = Config.Bind(NAME, $"{NAME} UI Scale", 0.75f, "UI Scaling?");
            PageTransitionSpeed = Config.Bind(NAME, $"{NAME} Page Transition Speed", 1.6f, "Page Transition Speed");
            LineSpacing = Config.Bind(NAME, $"{NAME} Line Spacing", 0.75f, "Line Spacing");

            FontMinSize = Config.Bind(NAME, $"{NAME} Font mininmum size", 24, "Font Autoscaling minimum.");
            FontMaxSize = Config.Bind(NAME, $"{NAME} Font Maximum size", 30, "Font Autoscaling maximum.");

            TextColor = Config.Bind(NAME, $"{NAME} Font Color", new Color(0f, 0f, 0f) , "Font Color");
            TextAlignment = Config.Bind(NAME, $"{NAME} Font alignment", TextAnchor.UpperLeft, "Font alignment");


            Log.LogMessage($"{NAME} Loaded.");

            CustomKeybindings.AddAction(EXTRA_ACTION_KEY_MOD, KeybindingsCategory.CustomKeybindings, ControlType.Both, InputType.Button);
            CustomKeybindings.AddAction(EXTRA_ACTION_KEY, KeybindingsCategory.CustomKeybindings, ControlType.Both, InputType.Button);

            //this is called after all book definitions are loaded, so you can reference the book and register to c# events
            OnBooksLoaded += OnBookLoadingComplete;
            SL.OnPacksLoaded += SL_OnPacksLoaded;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            new Harmony(GUID).PatchAll();
        }

        private void SceneManager_sceneLoaded(Scene Scene, LoadSceneMode arg1)
        {
            if (Scene.name == "MainMenu_Empty")
            {
                foreach (var item in UIBookInstances)
                {
                    Destroy(item.Value.gameObject);
                }

                UIBookInstances.Clear();
            }
        }

        public void Start()
        {
            OnReady?.Invoke();
        }

        private void SL_OnPacksLoaded()
        {
            SL_Equipment SeekingStone = new SL_Equipment()
            {
                Target_ItemID = 5100500,
                New_ItemID = -110005,
                Name = "Seeking Stone",
                Description = "Slowly vibrates in your hand and glows when it has detected something.",
                ItemVisuals = new SL_ItemVisual()
                {
                    Prefab_SLPack = "lorebooks",
                    Prefab_AssetBundle = "emoseekingstone",
                    Prefab_Name = "SeekingStone",
                    Position = new Vector3(0.073f, -0.015f, 0.094f),
                    Rotation = new Vector3(67.56058f, 138.624161f, 119.654831f)
                },
                Tags = new string[]
                {
                    "Lexicon"
                }
            };

            SeekingStone.OnTemplateApplied += (item) =>
            {
                EmoSeekingStone emoSeekingStone = OutwardHelpers.CheckOrAddComponent<EmoSeekingStone>(item.gameObject);
            };

            SeekingStone.ApplyTemplate();

            FindXMLDefinitions();
            BookButtonPrefab = OutwardHelpers.GetFromAssetBundle<GameObject>("lorebooks", "lorebookui", "UIBookButton");
        }

        #region Custom Book Stuff

        private void OnBookLoadingComplete()
        {
            AddEmonomiconEffects();
            AddMagicalTomeHairEffects();
        }
        private void AddMagicalTomeHairEffects()
        {
            //style
            if (this.HasLoreBook("emo.magicaltome.hairstyle"))
            {
                LoreBook featureBook = this.GetLoreBook("emo.magicaltome.hairstyle");

                if (featureBook != null)
                {
                    for (int i = 0; i < CharacterManager.CharacterVisualsPresets.Hairs.Length; i++)
                    {
                        PageContent pageContent = new PageContent(null, $"Hair Style {i}", "Press the interact key to choose this hair style.");
                        featureBook.AddOrUpdatePageContent(i, pageContent);
                    }

                    featureBook.OnInteractKeyPressed += (LoreBook LoreBook, int PageIndex, Character Character) =>
                    {
                        Character.CharacterUI.NotificationPanel.ShowNotification($"Setting Hair to {PageIndex}");
                        Character.VisualData.HairStyleIndex = PageIndex;
                        GameObject.Destroy(Character.Visuals.DefaultHairVisuals.gameObject);
                        Character.Visuals.LoadCharacterCreationHair(PageIndex, Character.Visuals.VisualData.HairColorIndex);
                        Character.Inventory.RemoveItem(LoreBook.BookItemID, 1);

                        //return true to close book ui
                        return true;
                    };
                }
            }

            //color

            if (this.HasLoreBook("emo.magicaltome.haircolor"))
            {
                LoreBook featureBook = this.GetLoreBook("emo.magicaltome.haircolor");

                if (featureBook != null)
                {
                    //just triggering the property before the for loop so the HairMaterials array is filled
                    CharacterManager.CharacterVisualsPresets.ToString();

                    //there are 11 original hair colors
                    for (int i = 0; i < 10; i++)
                    {
                        PageContent pageContent = new PageContent(null, $"Hair Color {i}", "Press the interact key to choose this hair style.");
                        featureBook.AddOrUpdatePageContent(i, pageContent);
                    }

                    featureBook.OnInteractKeyPressed += (LoreBook LoreBook, int PageIndex, Character Character) =>
                    {
                        Character.CharacterUI.NotificationPanel.ShowNotification($"Setting Hair to {PageIndex}");
                        Character.VisualData.HairColorIndex = PageIndex;
                        Character.Visuals.LoadCharacterCreationHair(Character.Visuals.VisualData.HairStyleIndex, PageIndex);
                        Character.Inventory.RemoveItem(LoreBook.BookItemID, 1);
                        //return true to close book ui
                        return true;
                    };
                }
            }
        }
        private void AddEmonomiconEffects()
        {
            //do things with the emonomicon
            if (this.HasLoreBook("emo.emonomicon"))
            {
                //get the book reference
                LoreBook featureBook = this.GetLoreBook("emo.emonomicon");

                if (featureBook != null)
                {
                    Dictionary<AreaManager.AreaEnum, Area> Areas = new Dictionary<AreaManager.AreaEnum, Area>();

                    Area Harmattan = AreaManager.Instance.GetArea(AreaManager.AreaEnum.Harmattan);
                    Areas.Add(AreaManager.AreaEnum.Harmattan, Harmattan);

                    Area CierzoOutside = AreaManager.Instance.GetArea(AreaManager.AreaEnum.CierzoOutside);
                    Areas.Add(AreaManager.AreaEnum.CierzoOutside, CierzoOutside);

                    Area Berg = AreaManager.Instance.GetArea(AreaManager.AreaEnum.Berg);
                    Areas.Add(AreaManager.AreaEnum.Berg, Berg);

                    Area Monsoon = AreaManager.Instance.GetArea(AreaManager.AreaEnum.Monsoon);
                    Areas.Add(AreaManager.AreaEnum.Monsoon, Monsoon);


                    int StartingPageCount = featureBook.PageCount;
                    foreach (var area in Areas)
                    {
                        if (!featureBook.HasPage(StartingPageCount))
                        {
                            PageContent pagContent = new PageContent(area.Value.GetMapScreen(), $"{area.Value.GetName()}", area.Value.GetName());
                            pagContent.IsButtonPage = true;

                            PageContent Page = pagContent.AddButton($"Teleport To {area.Value.GetName()}", (UIBookPanel UIBookPanel, Character Character) =>
                            {
                                StartCoroutine(OutwardHelpers.TeleportToArea(Character, LoreBooksMod.Instance.GetBookManagerForCharacter(Character), area.Key));
                            });

                            featureBook.AddOrUpdatePageContent(StartingPageCount, pagContent);
                            featureBook.LockPage(StartingPageCount);
                            StartingPageCount++;
                        }

                    }


                    featureBook.OnPageOpened += (Character Character, int index) =>
                    {
                        if (index == 1)
                        {
                            Character.StatusEffectMngr.AddStatusEffect("Bleeding");
                        }
                        else if(index != 1 || index != 0)
                        {
                            if (Character.StatusEffectMngr.HasStatusEffect("Bleeding"))
                            {
                                Character.StatusEffectMngr.RemoveStatusWithIdentifierName("Bleeding");
                            }
                        }
                    };

                    featureBook.OnInteractKeyPressed += (LoreBook LoreBook, int page, Character Character) =>
                    {
                        //if on page 2
                        if (page == 2)
                        {
                            LoreBook.UnlockPages(new int[] { 3, 4, 5, 6 });
                        }
                        return false;
                    };

                    featureBook.CanOpenPredicate += (Character Character, LoreBook LoreBook) =>
                    {
                        if (Character.Mana <= 0)
                        {
                            Character.CharacterUI.NotificationPanel.ShowNotification("You cannot open the Emonomicon without a deeper understanding of magic.. find the source of the conflux.");
                            return false;
                        }

                        return true;
                    };
                }


            }
        }

        #endregion

        #region XML
        private void FindXMLDefinitions()
        {
            string[] directoriesInPluginsFolder = Directory.GetDirectories(Paths.PluginPath);
            foreach (var directory in directoriesInPluginsFolder)
            {
                string Path = $"{directory}/{LoreBookFolderName}";

                if (HasFolder(Path))
                {
                    //Log.LogMessage($"Found {LoreBookFolderName} folder at {directory}");
                    string[] filePaths = Directory.GetFiles(Path, "*.xml", SearchOption.AllDirectories);

                    foreach (var item in filePaths)
                    {
                        LoreBookDefinition bookDefinition = DeserializeFromXML<LoreBookDefinition>(item);

                        if (bookDefinition != null)
                        {
                            SL_Item bookItem = new SL_Item()
                            {
                                New_ItemID = bookDefinition.ItemID,
                                Target_ItemID = bookDefinition.TargetItemID != 0 ? bookDefinition.TargetItemID : GepsNoteID,
                                Name = bookDefinition.BookTitle,
                                Description = string.IsNullOrEmpty(bookDefinition.BookDescription) ? bookDefinition.BookUID : bookDefinition.BookDescription
                            };

                            bookItem.ApplyTemplate();

                            LoreBook loreBook = new LoreBook(bookDefinition.BookUID, bookDefinition.BookTitle, bookDefinition.BookTitlePageContent, null, null);
                            loreBook.VisualColor = new Color(bookDefinition.VisualColor.r, bookDefinition.VisualColor.g, bookDefinition.VisualColor.b, bookDefinition.VisualColor.a);
                            loreBook.UseVisual = bookDefinition.UseVisual;
                            loreBook.BookItemID = bookDefinition.ItemID;
                            loreBook.GenerateChapterPage = bookDefinition.GenerateChaptersPage;

                            loreBook.AddOrUpdatePageContent(0, new PageContent(null, bookDefinition.BookTitle, bookDefinition.BookTitlePageContent));


                            int StartIndex = 0;

                            if (bookDefinition.GenerateChaptersPage)
                            {
                                StartIndex = 2;
                            }
  
                            for (int i = StartIndex; i < bookDefinition.Pages.Count; i++)
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
        #endregion

        #region LoreBook
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
        public bool HasLoreBook(int bookItemID)
        {
           return StoredBooks.ContainsKey(bookItemID);
        }
        public bool HasLoreBook(string bookUID)
        {
            return StoredBooks.First(x=> x.Value.BookUID == bookUID).Value != null;
        }
        public LoreBook GetLoreBook(int bookItemID)
        {
            if (StoredBooks.ContainsKey(bookItemID))
            {
                return StoredBooks[bookItemID];
            }

            return null;
        }
        public LoreBook GetLoreBook(string bookUID)
        {
           return StoredBooks.First(x => x.Value.BookUID == bookUID).Value;
        }

        #endregion

        #region BookUI
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
                        UIBookManager.SetContentFontAutoSize(FontMinSize.Value, FontMaxSize.Value);
                        UIBookManager.SetTitleFontAutoSize(24, 35);
                        UIBookManager.SetContentFontColor(TextColor.Value);
                        UIBookManager.SetContentAlignment(TextAlignment.Value);
                        UIBookManager.SetLineSpace(LineSpacing.Value);
                        UIBookManager.PageTransitionSpeed = PageTransitionSpeed.Value;

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

        #endregion

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
