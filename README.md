# LoreBooks
LoreBooks adds a new UI Element and a simple interface to add longer length books to Outward, it was made as for the Unbound mod but you can use this mod for your own mod without requiring Unbound.

![LoreBooksGitGif 1](https://github.com/Grim-/LoreBooks/blob/main/LoreBooksGitGif.gif)

### XML - A Example LoreBookDefinition.

Usage : 
Create a folder called "LoreBooks" in your root mod folder, create an xml file inside the LoreBooks folder, like the below example.

ItemID - The in-game Item created to allow the user to collect and open your book, you can use SideLoader to create this. The ID would be whatever your custom Items ID is.
BookUID - A Unique ID for your book, allowing you to reference it from c# if required.

-2905_Emonomicon.xml

```xml
<LoreBookDefinition>
<ItemID>-2905</ItemID>
<BookUID>emonomicon</BookUID>
<BookTitle>
<![CDATA[
<size=100>Emonomicon</size>
]]>
</BookTitle>
<BookTitlePageContent>
<![CDATA[
  <color=red>
  This book contains information on the features available to modders.
  </color>
]]>
</BookTitlePageContent>
<Pages>
  <PageContent>
    <PageTitle>LoreBooks Features (Events)</PageTitle>
    <TextContent>
<![CDATA[
  <b>Events</b>
   You can trigger events on certain actions, such as applying a status effect to the character opening the book.
   (you are now bleeding, go to the next page).
]]>
  </TextContent>
  </PageContent>
  <PageContent>
    <PageTitle>LoreBooks Features(Interactive)</PageTitle>
    <TextContent>
<![CDATA[
     <color=red>Bleeding</color> Fixed!

  <i>You can only read this book while you have more than zero Current Mana. Or anything you choose, such as quest completion, items owned, or area the player is in.</i>

You can also press the default "Use" key (Space on PC) with certain books! Try it on this page.
]]>
   </TextContent>
  </PageContent>
</Pages>
</LoreBookDefinition>
```

If you want to use rich text formatting (<color=green>Text</color> <b>BOLD></b> etc you need to escape the entire text with...

```xml
<![CDATA[
```
then close it with 
```xml
]]>
```
As shown in the example.

```xml
    <TextContent>
<![CDATA[
     <color=red>Bleeding</color> Fixed!

  <i>You can only read this book while you have more than zero Current Mana. Or anything you choose, such as quest completion, items owned, or area the player is in.</i>

You can also press the default "Use" key (Space on PC) with certain books! Try it on this page.
]]>
   </TextContent>
```


# CSharp
You can also Define LoreBooks via code

```csharp
        //your mods Awake method
        internal void Awake()
        {
            Log = this.Logger;
            Log.LogMessage($"{NAME} Loaded.");


            //register to the mods on ready event, you can add books once this is called
            LoreBooksMod.Instance.OnReady += OnReadyEvent;
            //this is called after all book definitions are loaded, so you can reference the book and register to c# events
            //LoreBooksMod.Instance.OnBooksLoaded += OnBookLoadingComplete;
        }

        private void OnReadyEvent()
        {
            //mod is ready to add books
            DefineABook();
        }

        private void DefineABook()
        {
            //an image loaded elsewhere
            Sprite SomeHeaderImageForTitle = null;

            LoreBook Newbook = new LoreBook("SOMEGUID", "SOME TITLE", "SOME TITLE PAGE CONTENT", SomeHeaderImageForTitle, null);

            //an image loaded elsewhere
            Sprite SomeHeaderImageForPage = null;

            //first (0) is always Title page
            Newbook.AddOrUpdatePageContent(0, new PageContent(SomeHeaderImageForPage, "SOME TITLE", "SOME TITLE PAGE CONTENT"));

            //add another
            Newbook.AddOrUpdatePageContent(1, new PageContent(SomeHeaderImageForPage, "SOME TITLE PAGE 1", "SOME TITLE PAGE 1"));

            //add the book to the main mod so it can be shown on the BookUI
            //itemID, BookUID, LoreBook
            LoreBooksMod.Instance.AddLoreBook(-9999, "SOMEGUID", Newbook);
        }
```


You can also reference books once the OnBooksLoaded event has fired, allowing you to react to certain events. The Emonomicon (ItemID -2105) contains an example of each of the features, spawn it in and read it to learn a bit more otherwise here is a c# example of that same book.


```csharp
        internal void Awake()
        {
            //this is called after all book definitions are loaded, so you can reference the book and register to c# events
            LoreBooksMod.Instance.OnBooksLoaded += OnBookLoadingComplete;

            //SL.OnPacksLoaded += SL_OnPacksLoaded;
            //new Harmony(GUID).PatchAll();
        }

        private void OnBookLoadingComplete()
        {
            //do things with the emonomicon
            if (LoreBooksMod.Instance.HasLoreBook("emo.emonomicon"))
            {
                //get the book reference
                LoreBook featureBook = LoreBooksMod.Instance.GetLoreBook("emo.emonomicon");

                if (featureBook != null)
                {
                    Dictionary<AreaManager.AreaEnum, Area> Areas = new Dictionary<AreaManager.AreaEnum, Area>();

                    Area Harmattan = AreaManager.Instance.GetArea(AreaManager.AreaEnum.Harmattan);
                    Areas.Add(AreaManager.AreaEnum.Harmattan, Harmattan);

                    Area CierzoOutside = AreaManager.Instance.GetArea(AreaManager.AreaEnum.CierzoOutside);
                    Areas.Add(AreaManager.AreaEnum.CierzoOutside, CierzoOutside);

                    Area Berg = AreaManager.Instance.GetArea(AreaManager.AreaEnum.Berg);
                    Areas.Add(AreaManager.AreaEnum.Berg, Berg);

                    Area Monsoon = AreaManager.Instance.GetArea(AreaManager.AreaEnum.Monsoon);
                    Areas.Add(AreaManager.AreaEnum.Monsoon, Monsoon);


                    int StartingPageCount = featureBook.PageCount;
                    foreach (var area in Areas)
                    {
                        if (!featureBook.HasPage(StartingPageCount))
                        {
                            PageContent pagContent = new PageContent(area.Value.GetMapScreen(), $"{area.Value.GetName()}", area.Value.GetName());
                            pagContent.IsButtonPage = true;

                            PageContent Page = pagContent.AddButton($"Teleport To {area.Value.GetName()}", (UIBookPanel UIBookPanel, Character Character) =>
                            {
                                StartCoroutine(OutwardHelpers.TeleportToArea(Character, LoreBooksMod.Instance.GetBookManagerForCharacter(Character), area.Key));
                            });

                            featureBook.AddOrUpdatePageContent(StartingPageCount, pagContent);
                            featureBook.LockPage(StartingPageCount);
                            StartingPageCount++;
                        }

                    }


                    featureBook.OnPageOpened += (Character Character, int index) =>
                    {
                        if (index == 1)
                        {
                            Character.StatusEffectMngr.AddStatusEffect("Bleeding");
                        }
                        else if(index != 1 || index != 0)
                        {
                            if (Character.StatusEffectMngr.HasStatusEffect("Bleeding"))
                            {
                                Character.StatusEffectMngr.RemoveStatusWithIdentifierName("Bleeding");
                            }
                        }
                    };

                    featureBook.OnInteractKeyPressed += (LoreBook LoreBook, int page, Character Character) =>
                    {
                        //if on page 2
                        if (page == 2)
                        {
                            LoreBook.UnlockPages(new int[] { 3, 4, 5, 6 });
                        }
                        return false;
                    };

                    featureBook.CanOpenPredicate += (Character Character, LoreBook LoreBook) =>
                    {
                        if (Character.Mana <= 0)
                        {
                            Character.CharacterUI.NotificationPanel.ShowNotification("You cannot open the Emonomicon without a deeper understanding of magic.. find the source of the conflux.");
                            return false;
                        }

                        return true;
                    };
                }


            }
        }
```
