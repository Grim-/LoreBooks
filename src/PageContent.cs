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

        public PageContent()
        {

        }

        public PageContent(Sprite headerImage, string pageTitle, string textContent)
        {
            HeaderImage = headerImage;
            PageTitle = pageTitle;
            TextContent = textContent;
        }


        public void AddButton(string buttonLabel, Action<Character> OnButtonPress)
        {
            if (Buttons.Find(x=> x.ButtonText == buttonLabel) == null)
            {
                Buttons.Add(new ButtonPageContent(buttonLabel, OnButtonPress));
            }

        }
    }

    [System.Serializable]
    public class ButtonPageContent
    {
        public string ButtonText = string.Empty;
        [XmlIgnore]
        public Action<Character> ButtonAction;

        public ButtonPageContent()
        {

        }

        public ButtonPageContent(string buttonText, Action<Character> buttonAction)
        {
            ButtonText = buttonText;
            ButtonAction = buttonAction;
        }

    }

}
