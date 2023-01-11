# LoreBooks
LoreBooks adds a new UI Element and a simple interface to add longer length books to Outward, it was made as for the Unbound mod but you can use this mod for your own mod without requiring Unbound.


### XML - A Example LoreBookDefinition.

Usage : 
Create a folder called "LoreBooks" in your root mod folder, create an xml file inside the LoreBooks folder, like the below example.

ItemID - The in-game Item created to allow the user to collect and open your book, you can use SideLoader to create this. The ID would be whatever your custom Items ID is.
BookUID - A Unique ID for your book, allowing you to reference it from c# if required.

-2105_Emonomicon.xml

```xml
<LoreBookDefinition>
<ItemID>-2105</ItemID>
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

If you want to use rich text formatting (<color=green>Text</color> <b>BOLD></b> etc you need to escape the entire text with 

such as in the above example 

```xml
    <TextContent>
<![CDATA[
     <color=red>Bleeding</color> Fixed!

  <i>You can only read this book while you have more than zero Current Mana. Or anything you choose, such as quest completion, items owned, or area the player is in.</i>

You can also press the default "Use" key (Space on PC) with certain books! Try it on this page.
]]>
   </TextContent>
```


![bookthing_pagetransition](https://user-images.githubusercontent.com/3288858/211231030-fa669afb-a5bc-45ee-a512-f1a644e366b1.gif)
