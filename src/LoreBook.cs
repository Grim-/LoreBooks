using BepInEx.Configuration;
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

        [XmlIgnoreAttribute]
        public Action<Character> OnBookOpened;
        [XmlIgnoreAttribute]
        public Action<Character, int> OnPageOpened;
        [XmlIgnoreAttribute]
        public Action<int, PageContent> OnPageAdded;
        [XmlIgnoreAttribute]
        public Action<int, PageContent> OnPageRemoved;

        [XmlIgnoreAttribute]
        public Func<Character, LoreBook, bool> CanOpenPredicate;

        public Action<LoreBook, int, Character> OnInteractKeyPressed;

        public int PageCount => PagesContent.Count;
        private Dictionary<int, PageContent> PagesContent = new Dictionary<int, PageContent>();

        public LoreBook(string bookUID, string titlePageTitle, string titlePageContent, Sprite titlePageImage, Action<Character> onBookOpened)
        {
            BookUID = bookUID;
            OnBookOpened = onBookOpened;
        }

        public void AddOrUpdatePageContent(int index, PageContent content)
        {
            if (!PagesContent.ContainsKey(index))
            {
                PagesContent.Add(index, content);
                OnPageAdded?.Invoke(index, content);
            }
            else PagesContent[index] = content;
        }

        public void RemovePage(int index)
        {
            if (!PagesContent.ContainsKey(index))
            {
                OnPageRemoved?.Invoke(index, PagesContent[index]);
                PagesContent.Remove(index);
            }
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
}
