using System;
using System.Linq;
using System.Collections.Generic;
using xivModdingFramework.General.Enums;
using xivModdingFramework.General.DataContainers;
using xivModdingFramework.Items.Categories;
using xivModdingFramework.Items.DataContainers;
using FFXIV_Modding_Tool.Configuration;

namespace FFXIV_Modding_Tool.Search
{
    public class GameSearch
    {
        MainClass main = new MainClass();
        Config config = new Config();
        List<XivGear> gearList { get; set; }
        List<XivCharacter> characterList { get; set; }
        List<XivUi> uiList { get; set; }
        List<XivMount> mountList { get; set; }
        List<XivMinion> minionList { get; set; }
        List<XivPet> summonList { get; set; }
        List<XivFurniture> furnitureList { get; set; }
        
        public class ItemInfo
        {
            public string name { get; set; }
            public string slot { get; set; }
            public string body { get; set; }
            public int variant { get; set; }
            public string category { get; set; }
            public string itemCategory { get; set; }
            public string itemSubCategory { get; set; }
            public XivDataFile dataFile { get; set; }
            public XivModelInfo primaryModelInfo { get; set; }
            public int equipSlotCategory { get; set; }
            public uint iconNumber { get; set; }
            public int uiIconNumber { get; set; }
            public string uiPath { get; set; }
            
            public ItemInfo(){}
            public ItemInfo(SearchResults item, string modelId, string category)
            {
                name = modelId;
                this.category = category;
                slot = item.Slot;
                body = item.Body;
                variant = item.Variant;
            }
            public ItemInfo(XivGear item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;
                dataFile = item.DataFile;
                primaryModelInfo = item.ModelInfo;
                equipSlotCategory = item.EquipSlotCategory;
                iconNumber = item.IconNumber;
            }
            public ItemInfo(XivCharacter item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;;
                dataFile = item.DataFile;
                primaryModelInfo = item.ModelInfo;
            }
            public ItemInfo(XivUi item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;
                dataFile = item.DataFile;
                uiIconNumber = item.IconNumber;
                uiPath = item.UiPath;
            }
            public ItemInfo(XivMount item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;
                dataFile = item.DataFile;
                primaryModelInfo = item.ModelInfo;
            }
            public ItemInfo(XivMinion item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;
                dataFile = item.DataFile;
                primaryModelInfo = item.ModelInfo;
            }
            public ItemInfo(XivPet item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;
                dataFile = item.DataFile;
                primaryModelInfo = item.ModelInfo;
            }
            public ItemInfo(XivFurniture item)
            {
                name = item.Name;
                category = item.PrimaryCategory;
                itemCategory = item.SecondaryCategory;
                itemSubCategory = item.TertiaryCategory;
                dataFile = item.DataFile;
                primaryModelInfo = item.ModelInfo;
                iconNumber = item.IconNumber;
            }
        }

        public static ItemInfo itemInfo;

        /// <summary>
        /// Handles the search request by calling on the appropriate functions based on if the request is a (partial) name or model id
        /// </summary>
        /// <param name="request">The model being searched for</param>
        /// <returns>A list with the search results</returns>
        public List<ItemInfo> SearchForItem(string request)
        {
            main.PrintMessage($"Searching for {request}...");
            List<ItemInfo> searchResults = new List<ItemInfo>();
            if (int.TryParse(request, out int result))
                SearchById(result);
            else
                SearchByFullOrPartialName(request);
            foreach (var item in gearList)
                searchResults.Add(new ItemInfo(item));
            foreach (var item in characterList)
                searchResults.Add(new ItemInfo(item));
            foreach (var item in uiList)
                searchResults.Add(new ItemInfo(item));
            foreach (var item in minionList)
                searchResults.Add(new ItemInfo(item));
            foreach (var item in mountList)
                searchResults.Add(new ItemInfo(item));
            foreach (var item in summonList)
                searchResults.Add(new ItemInfo(item));
            foreach (var item in furnitureList)
                searchResults.Add(new ItemInfo(item));
            return searchResults;
        }

        /// <summary>
        /// Gets all the in game items
        /// </summary>
        void GetAllItems()
        {
            main.PrintMessage("Fetching all items...");
            Console.Write("\r0%");
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            var gear = new Gear(MainClass._indexDirectory, gameLanguage);
            var getGear = gear.GetUnCachedGearList();
            getGear.Wait();
            gearList = getGear.Result;
            Console.Write("\r7%");
            var character = new Character(MainClass._indexDirectory, gameLanguage);
            var getCharacter = character.GetUnCachedCharacterList();
            getCharacter.Wait();
            characterList = getCharacter.Result;
            var ui = new UI(MainClass._indexDirectory, gameLanguage);
            var getMaps = ui.GetMapList();
            getMaps.Wait();
            Console.Write("\r15%");
            var getMapSymbols = ui.GetMapSymbolList();
            getMapSymbols.Wait();
            Console.Write("\r24%");
            var getStatusEffects = ui.GetStatusList();
            getStatusEffects.Wait();
            Console.Write("\r32%");
            var getOnlineStatus = ui.GetOnlineStatusList();
            getOnlineStatus.Wait();
            Console.Write("\r40%");
            var getWeather = ui.GetWeatherList();
            getWeather.Wait();
            Console.Write("\r47%");
            var getLoadingScreen = ui.GetLoadingImageList();
            getLoadingScreen.Wait();
            Console.Write("\r55%");
            var getActions = ui.GetActionList();
            getActions.Wait();
            Console.Write("\r64%");
            var getHud = ui.GetUldList();
            getHud.Wait();
            uiList = getMaps.Result.Concat(getMapSymbols.Result).Concat(getStatusEffects.Result).Concat(getOnlineStatus.Result).Concat(getWeather.Result).Concat(getLoadingScreen.Result).Concat(getActions.Result).Concat(getHud.Result).ToList();
            Console.Write("\r72%");
            var companion = new Companions(MainClass._indexDirectory, gameLanguage);
            var getMounts = companion.GetUncachedMountList();
            getMounts.Wait();
            mountList = getMounts.Result;
            Console.Write("\r80%");
            var getMinions = companion.GetUncachedMinionList();
            getMinions.Wait();
            minionList = getMinions.Result;
            Console.Write("\r87%");
            var getSummons = companion.GetUncachedPetList();
            getSummons.Wait();
            summonList = getSummons.Result;
            Console.Write("\r95%");
            var furniture = new Housing(MainClass._indexDirectory, gameLanguage);
            var getFurniture = furniture.GetUncachedFurnitureList();
            getFurniture.Wait();
            furnitureList = getFurniture.Result;
            Console.Write("\r100%\n");
        }

        /// <summary>
        /// Searches for the item that was requested by name
        /// </summary>
        /// <param name="request">The (partial) name of the item being searched for</param>
        void SearchByFullOrPartialName(string request)
        {
            gearList = gearList.Where(gearPiece => gearPiece.Name.Contains(request)).ToList();
            characterList = characterList.Where(characterPiece => characterPiece.Name.Contains(request)).ToList();
            uiList = uiList.Where(uiElement => uiElement.Name.Contains(request)).ToList();
            mountList = mountList.Where(mount => mount.Name.Contains(request)).ToList();
            minionList = minionList.Where(minion => minion.Name.Contains(request)).ToList();
            summonList = summonList.Where(summon => summon.Name.Contains(request)).ToList();
            furnitureList = furnitureList.Where(furniturePiece => furniturePiece.Name.Contains(request)).ToList();
            
        }

        /// <summary>
        /// Searches for the item that was requested by model id
        /// </summary>
        /// <param name="request">The model id being searched for</param>
        void SearchById(int request)
        {
            gearList = gearList.Where(gearPiece => gearPiece.ModelInfo.PrimaryID.Equals(request)).ToList();
            characterList = characterList.Where(characterPiece => characterPiece.ModelInfo.PrimaryID.Equals(request)).ToList();
            uiList = uiList.Where(uiElement => uiElement.IconNumber.Equals(request)).ToList();
            mountList = mountList.Where(mount => mount.ModelInfo.PrimaryID.Equals(request)).ToList();
            minionList = minionList.Where(minion => minion.ModelInfo.PrimaryID.Equals(request)).ToList();
            summonList = summonList.Where(summon => summon.ModelInfo.PrimaryID.Equals(request)).ToList();
            furnitureList = furnitureList.Where(furniturePiece => furniturePiece.ModelInfo.PrimaryID.Equals(request)).ToList();
        }
    }
}
