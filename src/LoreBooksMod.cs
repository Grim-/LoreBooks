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
    //[BepInDependency(GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class LoreBooksMod : BaseUnityPlugin
    {
        public const string GUID = "iggyandemo.lorebooks";
        public const string NAME = "LoreBooks";
        public const string VERSION = "1.0.0";
        public const string LoreBookFolderName = "LoreBooks";
        public static int EmonomiconID = -2905;


        public static GameObject BookButtonPrefab;

        internal static ManualLogSource Log;
        public static LoreBooksMod Instance;

        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<float> PageTransitionSpeed;
        public static ConfigEntry<float> LineSpacing;
        public static ConfigEntry<int> FontMinSize;
        public static ConfigEntry<int> FontMaxSize;
        public static ConfigEntry<Color> TextColor;
        public static ConfigEntry<TextAnchor> TextAlignment;

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
            PageTransitionSpeed = Config.Bind(NAME, $"{NAME} Page Transition Speed", 1.6f, "Page Transition Speed");
            LineSpacing = Config.Bind(NAME, $"{NAME} Line Spacing", 0.75f, "Line Spacing");

            FontMinSize = Config.Bind(NAME, $"{NAME} Font mininmum size", 24, "Font Autoscaling minimum.");
            FontMaxSize = Config.Bind(NAME, $"{NAME} Font Maximum size", 30, "Font Autoscaling maximum.");

            TextColor = Config.Bind(NAME, $"{NAME} Font Color", new Color(0f, 0f, 0f) , "Font Color");
            TextAlignment = Config.Bind(NAME, $"{NAME} Font alignment", TextAnchor.UpperLeft, "Font alignment");


            Log.LogMessage($"{NAME} Loaded.");


            //this is called after all book definitions are loaded, so you can reference the book and register to c# events
            OnBooksLoaded += OnBookLoadingComplete;

            SL.OnPacksLoaded += SL_OnPacksLoaded;
            new Harmony(GUID).PatchAll();
        }

        private void OnBookLoadingComplete()
        {
            //do things with the emonomicon
            if (this.HasLoreBook(EmonomiconID))
            {
                //get the book reference
                LoreBook featureBook = this.GetLoreBook(EmonomiconID);


                if (featureBook != null)
                {
                    featureBook.EffectsOnOpen.Add(new SL_AddStatusEffect()
                    {
                        StatusEffect = "Bleeding"
                    });

                    featureBook.EffectsOnOpen.Add(new SL_Puke()
                    {
                        ChanceToTrigger = 100,
                    });

                    featureBook.OnBookOpened += (Character Character) =>
                    {
                        Character.CharacterUI.NotificationPanel.ShowNotification("Book opened");
                    };

                    featureBook.OnPageOpened += (Character Character, int index) =>
                    {
                        if (index == 1)
                        {
                            Character.StatusEffectMngr.AddStatusEffect("Bleeding");
                        }

                        if (index == 2)
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
                            LoreBook cached = LoreBook;
                            Character.CharacterUI.NotificationPanel.ShowNotification("Interact key pressed!");

                            //if page 3 doesn't exist
                            if (!cached.HasPage(3))
                            {
                                UIBookPanel bookPanel = LoreBooksMod.Instance.GetBookManagerForCharacter(Character);
                                bookPanel.ShowEffect();
                                Character.CharacterUI.NotificationPanel.ShowNotification("You unlocked the hidden page!");

                                PageContent pagContent = new PageContent(null, $"{Character.Name}", "You unlocked the hidden page!");


                                pagContent.IsButtonPage = true;

                                pagContent.AddButton("Mini Teleport", (Character Character) =>
                                {
                                    Character.Teleport(Character.transform.position + Character.transform.forward * 2f, Character.transform.rotation);

                                });

                                pagContent.AddButton("Teleport to Harmattan", (Character Character) =>
                                {
                                    StartCoroutine(TeleportToArea(Character, bookPanel, AreaManager.AreaEnum.Harmattan));                                
                                });

                                pagContent.AddButton("Teleport to Cierzo", (Character Character) =>
                                {
                                    StartCoroutine(TeleportToArea(Character, bookPanel, AreaManager.AreaEnum.CierzoOutside));
                                });

                                pagContent.AddButton("Teleport to Berg", (Character Character) =>
                                {
                                    StartCoroutine(TeleportToArea(Character, bookPanel, AreaManager.AreaEnum.Berg));
                                });

                                pagContent.AddButton("Teleport to Monsoon", (Character Character) =>
                                {
                                    StartCoroutine(TeleportToArea(Character, bookPanel, AreaManager.AreaEnum.Monsoon));
                                });

                                cached.AddOrUpdatePageContent(3, pagContent);
                                bookPanel.ChangeToPage(cached, 3);



                            }
                        }

      
                    };

                    featureBook.CanOpenPredicate += (Character Character, LoreBook LoreBook) =>
                    {

                        if (Character.Mana <= 0)
                        {
                            Character.CharacterUI.NotificationPanel.ShowNotification("You cannot open the Emonomicon without a deeper understanding of magic, you plebian.");
                            Character.StatusEffectMngr.AddStatusEffect("Doomed");
                            return false;
                        }

                        return true;
                    };
                }


            }
        }


        private IEnumerator TeleportToArea(Character Character, UIBookPanel bookPanel, AreaManager.AreaEnum Area)
        {
            if (!Character.InLocomotion || !Character.NextIsLocomotion || Character.PreparingToSleep)
            {
                yield break;
            }

            if (Character && Character.IsLocalPlayer)
            {
                //yield return bookPanel.FadeEffect(0, 1, 1f);

                yield return new WaitForSeconds(1f);

                Area target = AreaManager.Instance.GetArea(Area);

                if (target != null)
                {
                    bookPanel.Hide();
                    //yield return bookPanel.FadeEffect(1, 0, 0.001f);

                    CharacterManager.Instance.RequestAreaSwitch(Character, target, 0, 0, 0, "");
                }
            }

            yield break;
        }

        private void SL_OnPacksLoaded()
        {
            BookButtonPrefab = OutwardHelpers.GetFromAssetBundle<GameObject>("lorebooks", "lorebookui", "UIBookButton");
            FindXMLDefinitions();

        }

        public void Start()
        {
            OnReady?.Invoke();
        }
        
        private void FindXMLDefinitions()
        {
            string[] directoriesInPluginsFolder = Directory.GetDirectories(Paths.PluginPath);
            foreach (var directory in directoriesInPluginsFolder)
            {
                string Path = $"{directory}/{LoreBookFolderName}";

                if (HasFolder(Path))
                {
                    Log.LogMessage($"Found {LoreBookFolderName} folder at {directory}");
                    string[] filePaths = Directory.GetFiles(Path, "*.xml");

                    foreach (var item in filePaths)
                    {
                        LoreBookDefinition bookDefinition = DeserializeFromXML<LoreBookDefinition>(item);

                        if (bookDefinition != null)
                        {
                            LoreBook loreBook = new LoreBook(bookDefinition.BookUID, bookDefinition.BookTitle, bookDefinition.BookTitlePageContent, null, null);
                            //LoreBooksMod.Log.LogMessage("COLOR : " + bookDefinition.VisualColor);
                            loreBook.VisualColor = new Color32(((byte)bookDefinition.VisualColor.r), ((byte)bookDefinition.VisualColor.g), ((byte)bookDefinition.VisualColor.b), ((byte)bookDefinition.VisualColor.a));
                            loreBook.UseVisual = bookDefinition.UseVisual;

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
        public bool HasLoreBook(int bookItemID)
        {
           return StoredBooks.ContainsKey(bookItemID);
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
