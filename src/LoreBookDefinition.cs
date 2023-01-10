using LoreBooks;
using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class LoreBookDefinition
{
    public int ItemID;
    public string BookUID;
    public string BookTitle;
    public string BookTitlePageContent;
    public List<PageContent> Pages;
}