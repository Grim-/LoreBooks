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
    public class PageContent
    {
        public Sprite HeaderImage = null;
        public string PageTitle = string.Empty;
        public string TextContent = string.Empty;

        [XmlIgnore]
        public Func<Character, LoreBook, bool> CanOpenPredicate;

        public PageContent()
        {

        }

        public PageContent(Sprite headerImage, string pageTitle, string textContent)
        {
            HeaderImage = headerImage;
            PageTitle = pageTitle;
            TextContent = textContent;
        }
    }

}
