﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LoreBooks
{
    [DisallowMultipleComponent]
    public class UIBookPanel : MonoBehaviour
    {
        public float PageTransitionSpeed = 1.2f;

        public Text TitleLabel;
        public Text ContentLabel;
        public Image HeaderImage;
        public Button NextButton;
        public Button PrevButton;
        public Button CloseButton;

        public CanvasGroup CanvasGroup => GetComponent<CanvasGroup>();
        //again cant use editor set references, I will cache it if it becomes a problem, 
        //but anyone opening enough books per frame to see the results of that problem, have another problem entirely.
        public CanvasGroup ContentCanvasGroup => transform.Find("Panel/Scroll View/Viewport/Content").gameObject.GetComponent<CanvasGroup>();

        //bookUID, LoreBook
        private Dictionary<string, LoreBook> AvailableBooks = new Dictionary<string, LoreBook>();


        public Character ParentCharacter;

        private LoreBook CurrentBook = null;
        private int CurrentPageIndex = 0;

        private bool IsShown = true;
        private bool IsPageTransitioning = false;

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
                }

            }
        }


        //Cant use editor set references, so the slow way it is.
        private void FindUIReferences()
        {
            ContentLabel = transform.Find("Panel/Scroll View/Viewport/Content").gameObject.GetComponent<Text>();

            TitleLabel = transform.Find("Panel/Title").gameObject.GetComponent<Text>();

            NextButton = transform.Find("NextPage").gameObject.GetComponent<Button>();

            if (NextButton != null)
            {
                NextButton.onClick.AddListener(GoToNextPage);
            }


            PrevButton = transform.Find("PreviousPage").gameObject.GetComponent<Button>();


            if (PrevButton != null)
            {
                PrevButton.onClick.AddListener(GoToPrevPage);
            }

            CloseButton = transform.Find("CloseButton").gameObject.GetComponent<Button>();

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(() =>
                {
                    Hide();
                });
            }

            HeaderImage = transform.Find("Panel/Scroll View/Image").gameObject.GetComponent<Image>();
        }

        
        public void ShowBook(LoreBook loreBook)
        {
            if (loreBook != null)
            {
                if (!IsShown)
                {
                    Show();
                }

                SetCurrentLoreBook(loreBook);
            }
        }
        public void SetCurrentLoreBook(LoreBook NewBook)
        {
            CurrentBook = NewBook;
            SetPageContent(CurrentBook, 0);
        }
        public void SetTitleFont(Font Font)
        {
            if (TitleLabel != null && Font != null)
            {
                TitleLabel.font = Font;
            }
        }
        public void SetContentFont(Font Font)
        {
            if (ContentLabel != null && Font != null)
            {
                ContentLabel.font = Font;
            }
        }

        public void ChangeToPage(LoreBook Book, int pageIndex)
        {
            if (!IsPageTransitioning && Book.HasPage(pageIndex))
            {
               StartCoroutine(FadePage(Book, pageIndex));
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

                    //if its the first page use the book title, else use the page title
                    if (pageIndex == 0)
                    {
                        if (!String.IsNullOrEmpty(Book.TitlePageContent))
                        {
                            TitleLabel.gameObject.SetActive(true);
                            SetTitleTextContent(Book.TitlePageContent);
                        }
                        else
                        {
                            TitleLabel.gameObject.SetActive(false);
                        }
                    }
                    else if (pageIndex > 0)
                    {
                        if (!String.IsNullOrEmpty(CurrentPageContent.PageTitle))
                        {
                            TitleLabel.gameObject.SetActive(true);
                            SetTitleTextContent(CurrentPageContent.PageTitle);
                        }
                        else
                        {
                            TitleLabel.gameObject.SetActive(false);
                        }
                    }

                    SetTextContent(CurrentPageContent.TextContent);

                    if (CurrentPageContent.HeaderImage != null)
                    {
                        SetHeaderImage(CurrentPageContent.HeaderImage);
                    }

                    SetCurrentPageIndex(pageIndex);
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
                IsShown = true;
            }
        }
        public void Hide()
        {
            if (CanvasGroup)
            {
                CanvasGroup.alpha = 0;
                CanvasGroup.interactable = false;
                IsShown = false;
            }
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
                TitleLabel.text = content;
            }
        }
        private void SetTextContent(string content)
        {
            if (ContentLabel != null)
            {
                ContentLabel.text = content;
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
    }
}
