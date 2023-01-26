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
        public bool IsButtonPage = false;

        public List<ButtonPageContent> Buttons = new List<ButtonPageContent>();

        [XmlIgnore]
        public Func<Character, LoreBook, bool> CanOpenPredicate;

        [XmlIgnoreAttribute]
        public LoreBook ParentBook;

        public PageContent()
        {

        }

        public PageContent(Sprite headerImage, string pageTitle, string textContent)
        {
            HeaderImage = headerImage;
            PageTitle = pageTitle;
            TextContent = textContent;
        }


        public PageContent AddButton(string buttonLabel, Action<UIBookPanel, Character> OnButtonPress)
        {
            if (Buttons.Find(x=> x.ButtonText == buttonLabel) == null)
            {
                var newButton = new ButtonPageContent(buttonLabel, OnButtonPress);
                Buttons.Add(newButton);
                return this;
            }

            return null;
        }
    }

    [System.Serializable]
    public class ButtonPageContent
    {
        public string ButtonText = string.Empty;
        [XmlIgnore]
        public Action<UIBookPanel, Character> ButtonAction;

        public ButtonPageContent()
        {

        }

        public ButtonPageContent(string buttonText, Action<UIBookPanel, Character> buttonAction)
        {
            ButtonText = buttonText;
            ButtonAction = buttonAction;
        }

    }

}
