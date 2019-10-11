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
            List<XivGear> gearList { get; set; }
            List<XivCharacter> characterList { get; set; }
            List<XivUi> uiList { get; set; }
            List<XivMount> mountList { get; set; }
            List<XivMinion> minionList { get; set; }
            List<XivPet> summonList { get; set; }
            List<XivFurniture> furnitureList { get; set; }
            List<SearchResults> modelIdList { get; set; }

            /// <summary>
            /// Handles the search request by calling on the appropriate functions based on if the request is a (partial) name or model id
            /// </summary>
            /// <param name="request">The model being searched for</param>
            /// <returns>A dictionary with the search results, sorted by their categories</returns>
            public Dictionary<string, List<string>> SearchForItem(string request)
            {
                main.PrintMessage($"Searching for {request}...");
                Dictionary<string, List<string>> searchResults = new Dictionary<string, List<string>>();
                if (int.TryParse(request, out int result))
                {
                    SearchById(result);
                    foreach (var item in modelIdList)
                        searchResults = AddSearchResult(searchResults, item.Slot, $"{request}, Body: {item.Body}, Variant: {item.Variant}");
                }
                else
                {
                    SearchByFullOrPartialName(request);
                    foreach (var item in gearList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
                    foreach (var item in characterList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
                    foreach (var item in uiList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
                    foreach (var item in minionList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
                    foreach (var item in mountList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
                    foreach (var item in summonList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
                    foreach (var item in furnitureList)
                        searchResults = AddSearchResult(searchResults, item.Category, item.Name);
            }
            return searchResults;
            }

            /// <summary>
            /// Gets all the in game items and searches for the requested item
            /// </summary>
            /// <param name="request">The (partial) name of the item being searched for</param>
            void SearchByFullOrPartialName(string request)
            {
                // Dictionary<string, List<string>> searchResults = new Dictionary<string, List<string>>();
                Config config = new Config();
                XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
                var gear = new Gear(MainClass._indexDirectory, gameLanguage);
                var getGear = gear.GetGearList();
                getGear.Wait();
                gearList.AddRange(getGear.Result.Where(gearPiece => gearPiece.Name.Contains(request)));
                var character = new Character(MainClass._indexDirectory, gameLanguage);
                var getCharacter = character.GetCharacterList();
                getCharacter.Wait();
                characterList.AddRange(getCharacter.Result.Where(characterPiece => characterPiece.Name.Contains(request)));
                var ui = new UI(MainClass._indexDirectory, gameLanguage);
                var getMaps = ui.GetMapList();
                getMaps.Wait();
                var getMapSymbols = ui.GetMapSymbolList();
                getMapSymbols.Wait();
                var getStatusEffects = ui.GetStatusList();
                getStatusEffects.Wait();
                var getOnlineStatus = ui.GetOnlineStatusList();
                getOnlineStatus.Wait();
                var getWeather = ui.GetWeatherList();
                getWeather.Wait();
                var getLoadingScreen = ui.GetLoadingImageList();
                getLoadingScreen.Wait();
                var getActions = ui.GetActionList();
                getActions.Wait();
                var getHud = ui.GetUldList();
                getHud.Wait();
                List<XivUi> tmpuiList = getMaps.Result.Concat(getMapSymbols.Result).Concat(getStatusEffects.Result).Concat(getOnlineStatus.Result).Concat(getWeather.Result).Concat(getLoadingScreen.Result).Concat(getActions.Result).Concat(getHud.Result).ToList();
                uiList.AddRange(tmpuiList.Where(uiElement => uiElement.Name.Contains(request)));
                var companion = new Companions(MainClass._indexDirectory, gameLanguage);
                var getMounts = companion.GetMountList();
                getMounts.Wait();
                mountList.AddRange(getMounts.Result.Where(mount => mount.Name.Contains(request)));
                var getMinions = companion.GetMinionList();
                getMinions.Wait();
                minionList.AddRange(getMinions.Result.Where(minion => minion.Name.Contains(request)));
                var getSummons = companion.GetPetList();
                getSummons.Wait();
                summonList.AddRange(getSummons.Result.Where(summon => summon.Name.Contains(request)));
                var furniture = new Housing(MainClass._indexDirectory, gameLanguage);
                var getFurniture = furniture.GetFurnitureList();
                getFurniture.Wait();
                furnitureList.AddRange(getFurniture.Result.Where(furniturePiece => furniturePiece.Name.Contains(request)));
            }

            /// <summary>
            /// Searches the game files for the model being requested
            /// </summary>
            /// <param name="request">The model id being searched for</param>
            void SearchById(int request)
            {
                Config config = new Config();
                XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
                var gear = new Gear(MainClass._indexDirectory, gameLanguage);
                var getEquipment = gear.SearchGearByModelID(request, "Equipment");
                getEquipment.Wait();
                var getWeapons = gear.SearchGearByModelID(request, "Weapon");
                getWeapons.Wait();
                var getAccesories = gear.SearchGearByModelID(request, "Accessory");
                getAccesories.Wait();
                var companion = new Companions(MainClass._indexDirectory, gameLanguage);
                var getMonsters = companion.SearchMonstersByModelID(request, XivItemType.monster);
                getMonsters.Wait();
                var getDemiHumans = companion.SearchMonstersByModelID(request, XivItemType.demihuman);
                getDemiHumans.Wait();
                var housing = new Housing(MainClass._indexDirectory, gameLanguage);
                var getFurniture = housing.SearchHousingByModelID(request, XivItemType.furniture);
                getFurniture.Wait();
                modelIdList = getEquipment.Result.Concat(getWeapons.Result).Concat(getAccesories.Result).Concat(getMonsters.Result).Concat(getDemiHumans.Result).Concat(getFurniture.Result).ToList();
            }

            /// <summary>
            /// Adds the latest search result to the dictionary
            /// </summary>
            /// <remarks>
            /// Has to check if a key already exists or not, as items can't be added to a nonexistant list
            /// </remarks>
            /// <param name="searchResults">Dictionary to add a search result to</param>
            /// <param name="category">The dictionary key to check</param>
            /// <param name="entry">The entry to add to the appropriate search result list</param>
            /// <returns>The given dictionary with the latest search result added</returns>
            Dictionary<string, List<string>> AddSearchResult(Dictionary<string, List<string>> searchResults, string category, string entry)
            {
                category = $"[{category}]";
                if (!searchResults.ContainsKey(category))
                    searchResults.Add(category, new List<string>{ entry });
                else
                    searchResults[category].Add(entry);
                return searchResults;
            }
        }
}