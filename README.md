# LoreBooks
LoreBooks adds a new UI Element and a simple interface to add longer length books to Outward, it was made as for the Unbound mod but you can use this mod for your own mod without requiring Unbound.

### CSharp

```csharp
        //use start so its had to time to load
        public void Start()
        {
            //register to the OnReady event
            LoreBooksMod.Instance.OnReady += OnLoreBooksReady;
        }

        //add your custom books
        private void OnLoreBooksReady()
        {
            //create a book, give it an ID, a Title and optionally a Sprite Image
            LoreBook TestBook = new LoreBook("EMONOMICON", "Emo-nomincon a beginners guide to eldritch horrors.", null);
            //add a page, first is page index, second is page content, page content takes an optional sprite, a title and the body text
            TestBook.AddOrUpdatePageContent(0, new PageContent(null, "FIRST PAGE TITLE", "SHIBBLY DIBBLY DOO."));
            TestBook.AddOrUpdatePageContent(1, new PageContent(null, "BOOZU MILK", "It was the second age of man when Boozu milk first found its way to our shores, " +
                "everyone was like 'yeah but how did he know doing that to a cow would produce milk? Does that not warrant its own line of questioning? No one else find it strange?'"));
            TestBook.AddOrUpdatePageContent(2, new PageContent(null, "THE THIRD PAGE", "Nobody knows what the third page contains as it is lost to time."));

            //itemID, bookID, Book, add the book to the manager
            //now when the item with ID -2103 is right clicked it will show an option to Open 'UnboundExplainer' book.
            LoreBooksMod.Instance.AddLoreBook(-2103, "UnboundExplainer", TestBook);
        }
```


### XML

Usage : 
Create a folder called "LoreBooks" in your root mod folder, create an xml file inside the LoreBooks folder, like the below example.


ItemID - The in-game created to allow the user to collect and open your book.
BookUID - A Unique ID for your book, allowing you to reference it from c# if required.

somefile.xml

```xml
<LoreBookDefinition>
<ItemID>-2105</ItemID>
<BookUID>some.uid.</BookUID>
<BookTitle>XML Book Test</BookTitle>
<BookTitlePageContent>BOdy Content</BookTitlePageContent>
<Pages>
  <PageContent>
    <PageTitle>Page 1</PageTitle>
    <TextContent>Page 1 text content</TextContent>
  </PageContent>
  <PageContent>
    <PageTitle>Page 2</PageTitle>
    <TextContent>Page 2 text content</TextContent>
  </PageContent>
  <PageContent>
    <PageTitle>Page 3</PageTitle>
    <TextContent>Page 3 text content</TextContent>
  </PageContent>
</Pages>
</LoreBookDefinition>
```


![bookthing_pagetransition](https://user-images.githubusercontent.com/3288858/211231030-fa669afb-a5bc-45ee-a512-f1a644e366b1.gif)
