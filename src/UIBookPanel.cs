using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoreBooks
{
    [DisallowMultipleComponent]
    public class UIBookPanel : MonoBehaviour
    {
        public float PageTransitionSpeed = 2f;


        public Image Background;
        public Text TitleLabel;
        public Text ContentLabel;
        public Text CurrentPageLabel;
        public Text PreviousPageLabel;
        public Text NextPageLabel;
        public Image HeaderImage;
        public Button NextButton;
        public Button PrevButton;
        public Button CloseButton;

        public GameObject ButtonContainer;
        public GameObject TextContainer;
        
        

        public CanvasGroup CanvasGroup => GetComponent<CanvasGroup>();
        //again cant use editor set references, I will cache it if it becomes a problem, 
        //but anyone opening enough books per frame to see the results of that problem, have another problem entirely.
        public CanvasGroup ContentCanvasGroup => transform.Find("Panel/Scroll View/Viewport/Content").gameObject.GetComponent<CanvasGroup>();
        public Character ParentCharacter;

        private LoreBook CurrentBook = null;
        private int CurrentPageIndex = 0;
        private List<GameObject> Buttons = new List<GameObject>();

        public bool IsVisible
        {
            get
            {
                return IsShown;
            }
        }
        private bool IsShown = true;
        private bool IsPageTransitioning = false;

        public List<object> PlayerVariables = new List<object>();


        private Coroutine TransitionRoutine;


        private void Awake()
        {
            FindUIReferences();
        }

        public void Start()
        {
            Font Philsopher = Resources.FindObjectsOfTypeAll<Font>().First(it => it.name.Contains("Philosopher"));


            if (Philsopher != null)
            {
                SetTitleFont(Philsopher);
                SetContentFont(Philsopher);
            }
          
        }

        public void SetParentCharacter(Character Character)
        {
            ParentCharacter = Character;

            UpdatePlayerVariables(Character);
        }


        private void UpdatePlayerVariables(Character Character)
        {
            PlayerVariables.Clear();


            PlayerVariables.Add(Character.Name);
            PlayerVariables.Add(Character.PlayerStats.CurrentHealth);
            PlayerVariables.Add(Character.PlayerStats.MaxHealth);
            PlayerVariables.Add(Character.PlayerStats.CurrentStamina);
            PlayerVariables.Add(Character.PlayerStats.MaxStamina);
            PlayerVariables.Add(OutwardHelpers.GetActiveRegionFromSceneName(SceneManager.GetActiveScene().name));
        }

        private void Update()
        {
            if (ParentCharacter != null)
            {
                if (IsShown)
                {

                    if (ControlsInput.MenuCancel(ParentCharacter.OwnerPlayerSys.PlayerID))
                    {
                        Hide();
                    }

                    if (ControlsInput.GoToPreviousFilterTab(ParentCharacter.OwnerPlayerSys.PlayerID))
                    {
                        GoToPrevPage();
                    }


                    if (ControlsInput.GoToNextFilterTab(ParentCharacter.OwnerPlayerSys.PlayerID))
                    {
                        GoToNextPage();
                    }

                    if (ControlsInput.MenuQuickAction(ParentCharacter.OwnerPlayerSys.PlayerID))
                    {
                        if (CurrentBook != null)
                        {
                            CurrentBook?.OnInteractKeyPressed?.Invoke(CurrentBook, CurrentPageIndex, ParentCharacter);
                        }
                    }
                }

            }
        }


        //Cant use editor set references, so the slow way it is.
        private void FindUIReferences()
        {
            HeaderImage = transform.Find("Panel/Scroll View/Image").gameObject.GetComponent<Image>();
            Background = GetComponent<Image>();

            TitleLabel = transform.Find("Panel/Title").gameObject.GetComponent<Text>();
            TitleLabel.verticalOverflow = VerticalWrapMode.Overflow;

            NextButton = transform.Find("NextPage").gameObject.GetComponent<Button>();
            NextPageLabel = transform.Find("NextPageLabel").gameObject.GetComponent<Text>();

            if (NextButton != null)
            {
                NextButton.onClick.AddListener(GoToNextPage);
            }


            PrevButton = transform.Find("PreviousPage").gameObject.GetComponent<Button>();
            PreviousPageLabel = transform.Find("PreviousPageLabel").gameObject.GetComponent<Text>();


            if (PrevButton != null)
            {
                PrevButton.onClick.AddListener(GoToPrevPage);
            }


            ButtonContainer = transform.Find("Panel/ButtonList").gameObject;
            TextContainer = transform.Find("Panel/Scroll View/Viewport/Content").gameObject;

            CurrentPageLabel = transform.Find("CurrentPageLabel").gameObject.GetComponent<Text>();

            ContentLabel = transform.Find("Panel/Scroll View/Viewport/Content").gameObject.GetComponent<Text>();
        }

        
        public void ShowBook(LoreBook loreBook)
        {
            if (loreBook != null)
            {
                if (loreBook.CanOpen(ParentCharacter))
                {
                    if (!IsShown)
                    {
                        Show();
                    }

                    if (loreBook.UseVisual)
                    {
                        //this is just for now, I need to move it 

                        if (Background!= null)
                        {
                            Background.material.SetFloat("_NoiseScrollY", -0.1f);
                        }

                        SetEffectColor(loreBook.VisualColor);
                        ShowEffect();
                    }
                    else
                    {
                        HideEffect();
                    }

                    SetCurrentLoreBook(loreBook);
                }   
            }
        }

        public void SetCurrentLoreBook(LoreBook NewBook)
        {
            CurrentBook = NewBook;


            SetPageContent(CurrentBook, 0);

            if (ParentCharacter != null)
            {
                CurrentBook.OnBookOpened?.Invoke(ParentCharacter);


                //if I can get this working properly then people can trigger things from SL_Effects instead of just code, lot more options.

                if (CurrentBook.EffectsOnOpen.Count > 0)
                {
                    GameObject tmpEffect = new GameObject();

                    SL_EffectTransform effectTransform = new SL_EffectTransform()
                    {
                        TransformName = "Normal"
                    };


                    effectTransform.Effects = CurrentBook.EffectsOnOpen.ToArray();
                    Transform actual = effectTransform.ApplyToTransform(tmpEffect.transform, EditBehaviours.Override);

                    Effect[] effects = actual.GetComponentsInChildren<Effect>();

                    foreach (var item in effects)
                    {
                        object[] infos = null;
                        item.ProcessAffectInfos(ParentCharacter, ParentCharacter.transform.position, ParentCharacter.transform.forward, ref infos);
                        LoreBooksMod.Log.LogMessage($"Triggering effect {item.name} on char");
                        item.Affect(ParentCharacter, ParentCharacter.transform.position, ParentCharacter.transform.forward);
                    }
                }
            }
        }

        public void SetTitleFont(Font Font)
        {
            if (TitleLabel != null && Font != null)
            {
                TitleLabel.font = Font;
            }
        }

        public void SetTitleFontAutoSize(int minSize, int maxSize)
        {
            if (TitleLabel != null)
            {
                TitleLabel.resizeTextForBestFit = true;
                TitleLabel.resizeTextMinSize = minSize;
                TitleLabel.resizeTextMaxSize = maxSize;
            }
        }


        public void SetContentFont(Font Font)
        {
            if (ContentLabel != null && Font != null)
            {
                ContentLabel.font = Font;
            }
        }

        public void SetContentFontAutoSize(int minSize, int maxSize)
        {
            if (ContentLabel != null)
            {
                ContentLabel.resizeTextForBestFit = true;
                ContentLabel.resizeTextMinSize = minSize;
                ContentLabel.resizeTextMaxSize = maxSize;
            }
        }

        public void SetContentFontColor(Color color)
        {
            if (ContentLabel != null)
            {
                ContentLabel.color = color;
            }
        }

        public void SetContentAlignment(TextAnchor textAlignment)
        {
            if (ContentLabel != null)
            {
                ContentLabel.alignment = textAlignment;
            }
        }



        public void ChangeToPage(LoreBook Book, int pageIndex)
        {
            if (IsPageTransitioning)
            {
                StopCoroutine(TransitionRoutine);
            }

            if (Book.HasPage(pageIndex))
            {
                TransitionRoutine = StartCoroutine(FadePage(Book, pageIndex));
            }

        }

        public void SetLineSpace(float spacing)
        {
            if (ContentLabel != null)
            {
                ContentLabel.lineSpacing = spacing;
            }
        }


        private void SetCurrentPageLabel(string CurrentPage)
        {
            if (CurrentPageLabel != null)
            {
                CurrentPageLabel.text = CurrentPage;
            }
        }

        private void SetPreviousPageLabelKey(string value)
        {
            if (PreviousPageLabel != null)
            {
                PreviousPageLabel.text = value;
            }
        }

        private void SetNextPageLabelKey(string value)
        {
            if (NextPageLabel != null)
            {
                NextPageLabel.text = value;
            }
        }


        //this one actually sets all the various UI elements to pageIndex of the Book passed
        private void SetPageContent(LoreBook Book, int pageIndex)
        {
            if (Book.HasPage(pageIndex))
            {
                PageContent pageContent = Book.GetPageContent(pageIndex);

                if (pageContent != null)
                {
                    PageContent CurrentPageContent = pageContent;

                    if (!String.IsNullOrEmpty(CurrentPageContent.PageTitle))
                    {
                        TitleLabel.gameObject.SetActive(true);
                        SetTitleTextContent(CurrentPageContent.PageTitle);
                    }
                    else
                    {
                        TitleLabel.gameObject.SetActive(false);
                    }


                    if (pageContent.IsButtonPage)
                    {
                        TextContainer.gameObject.SetActive(false);
                        ButtonContainer.gameObject.SetActive(true);


                        if (Buttons.Count > 0)
                        {
                            foreach (var button in Buttons)
                            {
                                Destroy(button.gameObject);
                            }

                            Buttons.Clear();
                        }

                        foreach (var button in pageContent.Buttons)
                        {
                            GameObject tmp = Instantiate(LoreBooksMod.BookButtonPrefab);
                            Button but = tmp.GetComponent<Button>();
                            Text buttonText = tmp.GetComponentInChildren<Text>();
                            buttonText.text = button.ButtonText;

                            but.onClick.AddListener(() =>
                            {
                                button.ButtonAction?.Invoke(ParentCharacter);
                            });

                            ((RectTransform)tmp.transform).SetParent(ButtonContainer.transform, false);
                            Buttons.Add(tmp);
                        }

                    }
                    else
                    {
                        TextContainer.gameObject.SetActive(true);
                        ButtonContainer.gameObject.SetActive(false);

                        SetTextContent(CurrentPageContent.TextContent);

                        if (CurrentPageContent.HeaderImage != null)
                        {
                            SetHeaderImage(CurrentPageContent.HeaderImage);
                        }
                    }




                    Book.OnPageOpened?.Invoke(ParentCharacter, pageIndex);
                    SetCurrentPageIndex(pageIndex);
                    SetCurrentPageLabel($"{CurrentPageIndex+1} / { Book.PageCount}");
                }
            }
        }

        private IEnumerator FadePage(LoreBook Book, int pageIndex)
        {
            if (pageIndex < 0 || pageIndex > Book.PageCount)
            {
                LoreBooksMod.Log.LogMessage($"PageIndex is less than 0, or greater than Book.PageCount {pageIndex}");
                yield break;
            }

            if (ContentCanvasGroup == null)
            {
                //cant find canvasgroup, just do it instantly and break out of the routine
                SetPageContent(Book, pageIndex);
                yield break;
            }


            IsPageTransitioning = true;

            //fade it out
            while (ContentCanvasGroup.alpha != 0)
            {
                ContentCanvasGroup.alpha -= PageTransitionSpeed * Time.deltaTime;
                yield return null;
            }

            SetPageContent(Book, pageIndex);

            while (ContentCanvasGroup.alpha != 1)
            {
                ContentCanvasGroup.alpha += PageTransitionSpeed * Time.deltaTime;
                yield return null;
            }

            IsPageTransitioning = false;
            yield break;
        }


        public void Show()
        {
            if (CanvasGroup)
            {
                CanvasGroup.alpha = 1;
                CanvasGroup.interactable = true;
            }

            IsShown = true;
        }
        public void Hide()
        {
            if (CanvasGroup)
            {
                CanvasGroup.alpha = 0;
                CanvasGroup.interactable = false;
            }

            IsShown = false;
        }
        public void ToggleShowHide()
        {
            if (IsShown)
                Hide();
            else
                Show();
        }


        private void GoToPrevPage()
        {
            if (CurrentBook != null)
            {
                if (CurrentPageIndex < 0)
                {
                    return;
                }

                int PrevPage = CurrentPageIndex - 1;

                if (PrevPage >= 0)
                {
                    ChangeToPage(CurrentBook, PrevPage);
                }
            }

        }

        private void GoToNextPage()
        {
            if (CurrentBook != null)
            {
                //already at the last page DO NOT DO ANYTHING
                if (CurrentPageIndex > CurrentBook.PageCount)
                {
                    return;
                }

                int NextPage = CurrentPageIndex + 1;
                //Currently less than or equal to last page
                if (NextPage <= CurrentBook.PageCount)
                {
                    ChangeToPage(CurrentBook, NextPage);
                }
            }
        }

        private void SetTitleTextContent(string content)
        {
            if (TitleLabel != null)
            {
                UpdatePlayerVariables(ParentCharacter);
                TitleLabel.text = string.Format(content, PlayerVariables.ToArray());
            }
        }
        private void SetTextContent(string content)
        {
            if (ContentLabel != null)
            {
                UpdatePlayerVariables(ParentCharacter);
                ContentLabel.text = string.Format(content, PlayerVariables.ToArray());
            }
        }

        private void SetHeaderImage(Sprite headerImage)
        {
            if (headerImage != null)
            {
                HeaderImage.gameObject.SetActive(true);
                HeaderImage.sprite = headerImage;
            }
            else HeaderImage.gameObject.SetActive(false);
        }

        private void SetCurrentPageIndex(int newPageIndex)
        {
            CurrentPageIndex = newPageIndex;
        }

        public virtual bool CanCharacterOpenBook(LoreBook LoreBook, Character Character)
        {
            return true;
        }


        public void ShowEffect()
        {
            if (Background != null)
            {
                Background.material.SetFloat("_ShowEffect", 1);
            }

        }
        public void SetEffectColor(Color color)
        {
            if (Background != null)
            {
                Background.material.SetColor("_EffectTint", color);
            }
        }
        public void HideEffect()
        {
            if (Background != null)
            {
                Background.material.SetFloat("_ShowEffect", 0);
            }

   
        }
    }
}
