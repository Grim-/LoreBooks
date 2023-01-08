using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoreBooks
{
    [System.Serializable]
    ///Used to define a book, its title and the page contents
    public class LoreBook
    {
        public string BookUID;

        public string TitlePageContent;
        public Sprite TitlePageImage;
        public int PageCount => PagesContent.Count;
        private Dictionary<int, PageContent> PagesContent = new Dictionary<int, PageContent>();

        public LoreBook(string bookUID, string titlePageContent, Sprite titlePageImage)
        {
            BookUID = bookUID;
            TitlePageContent = titlePageContent;
            TitlePageImage = titlePageImage;
        }

        public void AddOrUpdatePageContent(int index, PageContent content)
        {
            if (!PagesContent.ContainsKey(index))
            {
                PagesContent.Add(index, content);
            }
            else PagesContent[index] = content;
        }

        public void RemovePage(int index)
        {
            if (!PagesContent.ContainsKey(index))
            {
                PagesContent.Remove(index);
            }
        }

        public bool HasPage(int pageIndex)
        {
            return pageIndex <= PageCount;
        }

        public PageContent GetPageContent(int pageIndex)
        {
            if (pageIndex <= PageCount)
            {
                return PagesContent[pageIndex];
            }

            return null;
        }
    }
}
