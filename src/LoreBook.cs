using BepInEx.Configuration;
using SideLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace LoreBooks
{
    [System.Serializable]
    ///Used to define a book, its title and the page contents
    public class LoreBook
    {
        public string BookUID;
        public int BookItemID;


        public List<SL_Effect> EffectsOnOpen = new List<SL_Effect>();


        [XmlIgnoreAttribute]
        public Action<Character> OnBookOpened;
        [XmlIgnoreAttribute]
        public Action<Character, int> OnPageOpened;
        [XmlIgnoreAttribute]
        public Action<int, PageContent> OnPageAdded;
        [XmlIgnoreAttribute]
        public Action<int, PageContent> OnPageRemoved;

        public bool UseVisual = false;
        public bool GenerateChapterPage = false;
        public Color VisualColor = Color.cyan;


        [XmlIgnoreAttribute]
        public Func<Character, LoreBook, bool> CanOpenPredicate;
        [XmlIgnoreAttribute]
        public Func<LoreBook, int, Character, bool> OnInteractKeyPressed;

        [XmlIgnoreAttribute]
        public Action<LoreBook> OnBookPageInfoChange;

        [XmlIgnoreAttribute]
        public PageContent ChapterPage;
        public int PageCount => PagesContent.Count;
        public int UnlockedPageCount => PagesInfo.Where(x => x.Value.PageUnlocked).Count();
        private Dictionary<int, PageContent> PagesContent = new Dictionary<int, PageContent>();

        [XmlIgnoreAttribute]
        public Dictionary<int, PageInfo> PagesInfo = new Dictionary<int, PageInfo>();

        public LoreBook(string bookUID, string titlePageTitle, string titlePageContent, Sprite titlePageImage, Action<Character> onBookOpened)
        {
            BookUID = bookUID;
            OnBookOpened = onBookOpened;
        }


        public void AddNewPage(PageContent PageContent)
        {
            int CurrentCount = PageCount;

            if (!HasPage(CurrentCount))
            {
                AddOrUpdatePageContent(CurrentCount, PageContent);
            }
        }

        public void AddOrUpdatePageContent(int index, PageContent content)
        {
            if (!PagesContent.ContainsKey(index))
            {
                PagesContent.Add(index, content);
                OnPageAdded?.Invoke(index, content);
                content.ParentBook = this;

                if (!PagesInfo.ContainsKey(index))
                {
                    PagesInfo.Add(index, new PageInfo());
                }

                if(GenerateChapterPage) GenerateChapters();
            }
            else
            {
                PagesContent[index] = content;
                PagesContent[index].ParentBook = this;
            }
        }

        public void RemovePage(int index)
        {
            if (!PagesContent.ContainsKey(index))
            {
                PagesContent[index].ParentBook = null;
                OnPageRemoved?.Invoke(index, PagesContent[index]);
                PagesContent.Remove(index);

                if (GenerateChapterPage) GenerateChapters();
            }
        }



        public PageContent GenerateChapters()
        {
            PageContent ChapterPage = new PageContent(null, "Chapters", "");

            ChapterPage.IsButtonPage = true;

            foreach (var item in PagesContent)
            {
                if (item.Value.PageTitle == "Chapters")
                {
                    continue;
                }

                ChapterPage.AddButton(item.Value.PageTitle, (UIBookPanel UIBookPanel, Character Character) =>
                {
                    UIBookPanel.ChangeToPage(this, item.Key);
                });
            }

            AddOrUpdatePageContent(1, ChapterPage);
            return ChapterPage;
        }


        public bool HasViewedPage(int pageIndex)
        {
            if (PagesInfo.ContainsKey(pageIndex))
            {
                return PagesInfo[pageIndex].PageHasBeenViewed;
            }

            return false;
        }

        public void UnlockPage(int pageIndex)
        {
            if (HasPage(pageIndex) && PagesInfo.ContainsKey(pageIndex))
            {
                PagesInfo[pageIndex].PageUnlocked = true;
                OnBookPageInfoChange?.Invoke(this);
            }
        }

        public void UnlockPages(int[] pagesIndex)
        {
            foreach (var pageIndex in pagesIndex)
            {
                if (HasPage(pageIndex) && PagesInfo.ContainsKey(pageIndex))
                {
                    PagesInfo[pageIndex].PageUnlocked = true;
                }
            }
        }

        public void LockPage(int pageIndex)
        {
            if (HasPage(pageIndex) && PagesInfo.ContainsKey(pageIndex))
            {
                PagesInfo[pageIndex].PageUnlocked = false;
                OnBookPageInfoChange?.Invoke(this);
            }
        }

        public void LockPages(int[] pagesIndex)
        {
            foreach (var pageIndex in pagesIndex)
            {
                if (HasPage(pageIndex) && PagesInfo.ContainsKey(pageIndex))
                {
                    PagesInfo[pageIndex].PageUnlocked = false;
                }
            }
        }


        public bool HasUnlockedPage(int pageIndex)
        {
            if (PagesInfo.ContainsKey(pageIndex))
            {
                return PagesInfo[pageIndex].PageUnlocked;
            }

            return false;
        }

        public bool HasPage(int pageIndex)
        {
            return PagesContent.ContainsKey(pageIndex);
        }

        
        public PageContent GetPageContent(int pageIndex)
        {
            if (pageIndex <= PageCount)
            {
                return PagesContent[pageIndex];
            }

            return null;
        }

        public bool CanOpen(Character Character)
        {
            if (CanOpenPredicate != null)
            {
                return (bool)CanOpenPredicate?.Invoke(Character, this);
            }

            return true;
        }
    }

    [System.Serializable]
    public class PageInfo
    {
        public bool PageUnlocked = true;
        public bool PageHasBeenViewed = false;

        public PageInfo()
        {

        }
    }
}
