using LoreBooks;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class LoreBookDefinition
{
    public int ItemID;
    public string BookUID;
    public string BookTitle;
    public string BookTitlePageContent;
    public bool UseVisual = false;
    public Color VisualColor = Color.cyan;
    public List<PageContent> Pages;
}