using SideLoader.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoreBooks
{

    public class CustomSaveData : PlayerSaveExtension
    {

        public List<WrapperClass> CharacterInts;

        public override void ApplyLoadedSave(Character character, bool isWorldHost)
        {
            //YourInt is now filled with whatever value it had last time it saved

            if (CharacterIntsContains(character.UID))
            {
                WrapperClass data = GetCharacterData(character.UID);

                //do stuff
            }
        }

        public override void Save(Character character, bool isWorldHost)
        {
            //Set this value as the game saves
            //YourInt = 

            if (CharacterIntsContains(character.UID))
            {
                UpdateCharacterIntValue(character.UID, 0001);
            }
            else
            {
                AddCharacterIntValue(character.UID, 0001);
            }
        }


        public void AddCharacterIntValue(string CharacterUID, int Value)
        {
            if (!CharacterIntsContains(CharacterUID))
            {
                WrapperClass data = new WrapperClass();
                data.CharacterUID = CharacterUID;
                data.CharacterIntValue = Value;

                CharacterInts.Add(data)/*)*/;
            }
        }

        public void UpdateCharacterIntValue(string CharacterUID, int Value)
        {
            if (CharacterIntsContains(CharacterUID))
            {
                WrapperClass data = GetCharacterData(CharacterUID);
                data.CharacterUID = CharacterUID;
                data.CharacterIntValue = Value;
            }
        }

        public bool CharacterIntsContains(string CharacterUID)
        {
            return CharacterInts.Where(x => x.CharacterUID == CharacterUID) != null;
        }

        public WrapperClass GetCharacterData(string CharacterUID)
        {
            return CharacterInts.Where(x => x.CharacterUID == CharacterUID).First();
        }
    }

    [System.Serializable]
    public class WrapperClass
    {
        public string CharacterUID;
        public int CharacterIntValue;
    }


    public class BookSaveData : PlayerSaveExtension
    {
        public List<BookData> BookMemory;


        public override void ApplyLoadedSave(Character character, bool isWorldHost)
        {
            if (LoreBooksMod.Instance.StoredBooks != null && LoreBooksMod.Instance.StoredBooks.Count > 0)
            {
                //loop each of the currently stored books
                foreach (var storedBook in LoreBooksMod.Instance.StoredBooks)
                {
                    //if the book memory variable has a key for the book UID
                    if (BookMemoryHasUID(storedBook.Value.BookUID))
                    {
                        //get that book
                        LoreBook loreBook = LoreBooksMod.Instance.GetLoreBook(storedBook.Value.BookUID);

                        //update the page info
                        loreBook.PagesInfo = CreatePageInfoDataFrom(GetBookData(storedBook.Value.BookUID).PageInfo);
                    }
                }
            }
        }

        public override void Save(Character character, bool isWorldHost)
        {
            if (LoreBooksMod.Instance.StoredBooks != null && LoreBooksMod.Instance.StoredBooks.Count > 0)
            {
                //loop each of the currently stored books
                foreach (var storedBook in LoreBooksMod.Instance.StoredBooks)
                {
                    //if the book memory variable has a key for the book UID
                    if (!BookMemoryHasUID(storedBook.Value.BookUID))
                    {
                        //get that book
                        LoreBook loreBook = LoreBooksMod.Instance.GetLoreBook(storedBook.Value.BookUID);
                        AddBookData(storedBook.Value.BookUID, CreatePageInfoDataFrom(loreBook.PagesInfo));
                    }
                    else
                    {
                        LoreBook loreBook = LoreBooksMod.Instance.GetLoreBook(storedBook.Value.BookUID);
                        UpdateBookData(storedBook.Value.BookUID, CreatePageInfoDataFrom(loreBook.PagesInfo));
                    }
                }
            }


        }


        public Dictionary<int, PageInfo> CreatePageInfoDataFrom(List<PageInfoData> PagesInfo)
        {
            Dictionary<int, PageInfo> pageInfoDatas = new Dictionary<int, PageInfo>();

            foreach (var item in PagesInfo)
            {
                pageInfoDatas.Add(item.PageIndex, item.PageInfo);
            }

            return pageInfoDatas;
        }

        public List<PageInfoData> CreatePageInfoDataFrom(Dictionary<int, PageInfo> PagesInfo)
        {
            List<PageInfoData> pageInfoDatas = new List<PageInfoData>();

            foreach (var item in PagesInfo)
            {
                pageInfoDatas.Add(new PageInfoData(item.Key, item.Value));
            }

            return pageInfoDatas;
        }


        public bool BookMemoryHasUID(string BookUID)
        {
            foreach (var item in BookMemory)
            {
                if (item.BookItemUID == BookUID)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddBookData(string BookUID, List<PageInfoData> pageInfo)
        {
            BookMemory.Add(new BookData(BookUID, pageInfo));
        }

        public void UpdateBookData(string BookUID, List<PageInfoData> pageInfo)
        {
            if (BookMemoryHasUID(BookUID))
            {
                BookData bookData = GetBookData(BookUID);
                bookData.PageInfo = pageInfo;
            }
        }

        public BookData GetBookData(string BookUID)
        {
            foreach (var item in BookMemory)
            {
                if (item.BookItemUID == BookUID)
                {
                    return item;
                }
            }

            return null;
        }
    }


    public class PlayerJourneryData
    {
        public string CharacterUID;
        public string CharacterName;

        public int TimesCharacterDied;

        public float TimeSpentInGame;

        public int QuestsCompleted;
        public int QuestsFailed;
    }


    [System.Serializable]
    public class BookData
    {
        public string BookItemUID;
        public bool BookOpenedOnce = false;
        public List<PageInfoData> PageInfo;

        public BookData()
        {

        }

        public BookData(string bookItemUID, List<PageInfoData> pageEnabled)
        {
            BookItemUID = bookItemUID;
            PageInfo = pageEnabled;
        }
    }


    [System.Serializable]
    public class PageInfoData
    {
        public int PageIndex;
        public PageInfo PageInfo;

        public PageInfoData()
        {

        }

        public PageInfoData(int pageIndex, PageInfo pageInfo)
        {
            PageIndex = pageIndex;
            PageInfo = pageInfo;
        }
    }
}


