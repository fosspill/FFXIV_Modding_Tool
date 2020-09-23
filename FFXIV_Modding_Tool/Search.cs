using System;
using System.Linq;
using System.Collections.Generic;
using xivModdingFramework.General.Enums;
using xivModdingFramework.General.DataContainers;
using xivModdingFramework.Items.Categories;
using xivModdingFramework.Items.Enums;
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
        Dictionary<string, List<SearchResults>> modelIdList { get; set; }
        
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
            {
                SearchById(result);
                foreach (var itemList in modelIdList)
                {
                    foreach (var item in itemList.Value)
                        searchResults.Add(new ItemInfo(item, request, itemList.Key));
                }
            }
            else
            {
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
        }
        return searchResults;
        }

        /// <summary>
        /// Gets all the in game items and searches for the requested item
        /// </summary>
        /// <param name="request">The (partial) name of the item being searched for</param>
        void SearchByFullOrPartialName(string request)
        {
            Console.Write("\r0%");
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            var gear = new Gear(MainClass._indexDirectory, gameLanguage);
            var getGear = gear.GetGearList();
            getGear.Wait();
            gearList = getGear.Result.Where(gearPiece => gearPiece.Name.Contains(request)).ToList();
            Console.Write("\r7%");
            var character = new Character(MainClass._indexDirectory, gameLanguage);
            var getCharacter = character.GetCharacterList();
            getCharacter.Wait();
            characterList = getCharacter.Result.Where(characterPiece => characterPiece.Name.Contains(request)).ToList();
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
            List<XivUi> tmpuiList = getMaps.Result.Concat(getMapSymbols.Result).Concat(getStatusEffects.Result).Concat(getOnlineStatus.Result).Concat(getWeather.Result).Concat(getLoadingScreen.Result).Concat(getActions.Result).Concat(getHud.Result).ToList();
            uiList = tmpuiList.Where(uiElement => uiElement.Name.Contains(request)).ToList();
            Console.Write("\r72%");
            var companion = new Companions(MainClass._indexDirectory, gameLanguage);
            var getMounts = companion.GetMountList();
            getMounts.Wait();
            mountList = getMounts.Result.Where(mount => mount.Name.Contains(request)).ToList();
            Console.Write("\r80%");
            var getMinions = companion.GetMinionList();
            getMinions.Wait();
            minionList = getMinions.Result.Where(minion => minion.Name.Contains(request)).ToList();
            Console.Write("\r87%");
            var getSummons = companion.GetPetList();
            getSummons.Wait();
            summonList = getSummons.Result.Where(summon => summon.Name.Contains(request)).ToList();
            Console.Write("\r95%");
            var furniture = new Housing(MainClass._indexDirectory, gameLanguage);
            var getFurniture = furniture.GetFurnitureList();
            getFurniture.Wait();
            furnitureList = getFurniture.Result.Where(furniturePiece => furniturePiece.Name.Contains(request)).ToList();
            Console.Write("\r100%\n");
        }

        /// <summary>
        /// Searches the game files for the model being requested
        /// </summary>
        /// <param name="request">The model id being searched for</param>
        void SearchById(int request)
        {
            modelIdList = new Dictionary<string, List<SearchResults>>();
            Console.Write("\r0%");
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            var gear = new Gear(MainClass._indexDirectory, gameLanguage);
            var getEquipment = gear.SearchGearByModelID(request, "Equipment");
            getEquipment.Wait();
            modelIdList["Equipment"] = getEquipment.Result;
            Console.Write("\r15%");
            var getWeapons = gear.SearchGearByModelID(request, "Weapon");
            getWeapons.Wait();
            modelIdList["Weapon"] = getWeapons.Result;
            Console.Write("\r33%");
            var getAccesories = gear.SearchGearByModelID(request, "Accessory");
            getAccesories.Wait();
            modelIdList["Accesory"] = getAccesories.Result;
            Console.Write("\r50%");
            var companion = new Companions(MainClass._indexDirectory, gameLanguage);
            var getMonsters = companion.SearchMonstersByModelID(request, XivItemType.monster);
            getMonsters.Wait();
            modelIdList["Monster"] = getMonsters.Result;
            Console.Write("\r67%");
            var getDemiHumans = companion.SearchMonstersByModelID(request, XivItemType.demihuman);
            getDemiHumans.Wait();
            modelIdList["DemiHuman"] = getDemiHumans.Result;
            Console.Write("\r85%");
            var housing = new Housing(MainClass._indexDirectory, gameLanguage);
            var getFurniture = housing.SearchHousingByModelID(request, XivItemType.furniture);
            getFurniture.Wait();
            modelIdList["Furniture"] = getFurniture.Result;
            Console.Write("\r100%\n");
        }
    }
}
