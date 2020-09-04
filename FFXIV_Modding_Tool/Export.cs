using System.Linq;
using System.Collections.Generic;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Items.Enums;
using xivModdingFramework.Items.Categories;
using xivModdingFramework.Items.DataContainers;
using xivModdingFramework.Textures.FileTypes;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Search;

namespace FFXIV_Modding_Tool.Exporting
{
    public class Export
    {
        MainClass main = new MainClass();
        Config config = new Config();
        
        /// <summary>
        /// Retrieves information on what can be exported based on the user's chosen item
        /// </summary>
        /// <param name="item">The item the user wishes to export</param>
        public void GetExportInfo(GameSearch.ItemInfo item)
        {
            if (int.TryParse(item.name, out int modelId))
                item = LoadModelData(modelId, item);
            Dictionary<string, List<XivRace>> availableRaces = new Dictionary<string, List<XivRace>>();
            main.PrintMessage("Retrieving racial data...");
            availableRaces["Textures"] = GetTextureRaces(item);
            availableRaces["Model"] = GetModelRaces(item);
            foreach (KeyValuePair<string, List<XivRace>> allRaces in availableRaces)
            {
                main.PrintMessage($"[{allRaces.Key}]");
                if (allRaces.Value.Count == 0)
                    main.PrintMessage("N/A");
                else
                {
                    foreach (XivRace race in allRaces.Value)
                        main.PrintMessage(race.GetDisplayName());
                }
            }
            GetTextures(item, availableRaces["Textures"][0]);
        }

        /// <summary>
        /// Load the actual model data of an item that was searched for by its model id
        /// </summary>
        /// <param name="modelId">The model id of the requested item</param>
        /// <param name="item">The requested item</param>
        /// <returns></returns>
        GameSearch.ItemInfo LoadModelData(int modelId, GameSearch.ItemInfo item)
        {
            GameSearch.ItemInfo itemInfo = new GameSearch.ItemInfo();
            int.TryParse(item.body, out var body);
            var variant = item.variant;

            if (item.category.Equals("Equipment") || item.category.Equals("Accessory") ||
                item.category.Equals("Weapon"))
            {
                itemInfo = new GameSearch.ItemInfo
                {
                    name = $"{item.category.ToLower()[0]}{modelId.ToString().PadLeft(4, '0')}",
                    category = "Gear",
                    itemCategory = item.slot,
                    dataFile = XivDataFile._04_Chara,
                    primaryModelInfo = new XivModelInfo
                    {
                        PrimaryID = modelId,
                        SecondaryID = body,
                        ImcSubsetID = variant
                    }
                };
            }
            else if (item.category.Equals("Monster"))
            {
                itemInfo = new GameSearch.ItemInfo
                {
                    name = $"{item.category.ToLower()[0]}{modelId.ToString().PadLeft(4, '0')}",
                    category = "Companions",
                    itemCategory = "Monster",
                    dataFile = XivDataFile._04_Chara,
                    primaryModelInfo = new XivModelInfo
                    {
                        PrimaryID = modelId,
                        SecondaryID = body
                    }
                };
            }
            else if (item.category.Equals("DemiHuman"))
            {
                itemInfo = new GameSearch.ItemInfo
                {
                    name = $"{item.category.ToLower()[0]}{modelId.ToString().PadLeft(4, '0')}",
                    category = "Companions",
                    itemCategory = "Monster",
                    dataFile = XivDataFile._04_Chara,
                    primaryModelInfo = new XivModelInfo
                    {
                        PrimaryID = modelId,
                        SecondaryID = body
                    }
                };
            }
            else if (item.category.Equals("Furniture"))
            {
                itemInfo = new GameSearch.ItemInfo
                {
                    name = $"{item.category.ToLower()[0]}{modelId.ToString().PadLeft(4, '0')}",
                    category = "Housing",
                    itemCategory = item.slot,
                    dataFile = XivDataFile._01_Bgcommon,
                    primaryModelInfo = new XivModelInfo
                    {
                        PrimaryID = modelId
                    }
                };
            }
            return itemInfo;
        }

        /// <summary>
        /// Retrieve all the races that have unique textures
        /// </summary>
        /// <param name="item">The item that textures are being requested of</param>
        /// <returns>A list of races with unique textures</returns>
        List<XivRace> GetTextureRaces(GameSearch.ItemInfo item)
        {
            List<XivRace> races = new List<XivRace>();
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            if (item.category.Equals("Gear"))
            {
                Gear gear = new Gear(MainClass._indexDirectory, gameLanguage);
                var gearItem = new XivGear{
                    Name = item.name,
                    PrimaryCategory = item.category,
                    SecondaryCategory = item.itemCategory,
                    TertiaryCategory = item.itemSubCategory,
                    DataFile = item.dataFile,
                    ModelInfo = item.primaryModelInfo,
                    IconNumber = item.iconNumber,
                    EquipSlotCategory = item.equipSlotCategory
                };
                var getRaces = gear.GetRacesForTextures(gearItem, item.dataFile);
                getRaces.Wait();
                races.AddRange(getRaces.Result);
            }
            else if (item.category.Equals("Companions"))
                races.Add(XivRace.DemiHuman);
            else if (item.category.Equals("Character"))
            {
                if (item.itemCategory.Equals("Face_Paint") || item.itemCategory.Equals("Equip_Decals"))
                    races.Add(XivRace.All_Races);
                else
                {
                    Character character = new Character(MainClass._indexDirectory, gameLanguage);
                    var charaItem = new XivCharacter{
                        Name = item.name,
                        PrimaryCategory = item.category,
                        SecondaryCategory = item.itemCategory,
                        TertiaryCategory = item.itemSubCategory,
                        DataFile = item.dataFile,
                        ModelInfo = item.primaryModelInfo
                    };
                    var charaRaceAndNumberDictionary = character.GetRacesAndNumbersForTextures(charaItem);
                    foreach (var racesAndNumber in charaRaceAndNumberDictionary.Result)
                        races.Add(racesAndNumber.Key);
                }
            }
            else if (item.category.Equals("UI"))
                races.Add(XivRace.All_Races);
            else if (item.category.Equals("Housing"))
                races.Add(XivRace.All_Races);
            return races;
        }

        /// <summary>
        /// Retrieve all the races that have unique models
        /// </summary>
        /// <param name="item">The item that models are being requested of</param>
        /// <returns>A list of races with unique models</returns>
        List<XivRace> GetModelRaces(GameSearch.ItemInfo item)
        {
            List<XivRace> races = new List<XivRace>();
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            if (item.category.Equals("Gear"))
            {
                Gear gear = new Gear(MainClass._indexDirectory, gameLanguage);
                var gearItem = new XivGear{
                    Name = item.name,
                    PrimaryCategory = item.category,
                    SecondaryCategory = item.itemCategory,
                    TertiaryCategory = item.itemSubCategory,
                    DataFile = item.dataFile,
                    ModelInfo = item.primaryModelInfo,
                    IconNumber = item.iconNumber,
                    EquipSlotCategory = item.equipSlotCategory
                };
                var getRaces = gear.GetRacesForModels(gearItem, item.dataFile);
                getRaces.Wait();
                races.AddRange(getRaces.Result);
            }
            else if (item.category.Equals("Companions"))
                races.Add(XivRace.DemiHuman);
            else if (item.category.Equals("Character"))
            {
                Character character = new Character(MainClass._indexDirectory, gameLanguage);
                var charaItem = new XivCharacter{
                    Name = item.name,
                    PrimaryCategory = item.category,
                    SecondaryCategory = item.itemCategory,
                    TertiaryCategory = item.itemSubCategory,
                    DataFile = item.dataFile,
                    ModelInfo = item.primaryModelInfo
                    };
                var getRaces = character.GetRacesAndNumbersForModels(charaItem);
                getRaces.Wait();
                foreach (var racesAndNumber in getRaces.Result)
                    races.Add(racesAndNumber.Key);
            }
            else if (item.category.Equals("Housing"))
                races.Add(XivRace.All_Races);
            return races;
        }

        /// <summary>
        /// Gets all the different types of textures an item has
        /// </summary>
        /// <param name="item">The item we want the textures of</param>
        /// <param name="race">A race the item has textures for to use to retrieve texture data</param>
        void GetTextures(GameSearch.ItemInfo item, XivRace race)
        {
            List<string> parts = getTextureParts(item, race);
            foreach (string part in parts)
                main.PrintMessage(part);
        }

        /// <summary>
        /// Gets the texture parts for the given item
        /// </summary>
        /// <param name="item">The item we want the parts of</param>
        /// <param name="race">A race the item has textures for to use to request texture parts</param>
        /// <returns>A list with the different texture parts of the item</returns>
        List<string> getTextureParts(GameSearch.ItemInfo item, XivRace race)
        {
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            List<string> partList = new List<string>();
            if (item.category.Equals("Character"))
                {
                    Character character = new Character(MainClass._indexDirectory, gameLanguage);
                    var charaItem = new XivCharacter{
                        Name = item.name,
                        PrimaryCategory = item.category,
                        SecondaryCategory = item.itemCategory,
                        TertiaryCategory = item.itemSubCategory,
                        DataFile = item.dataFile,
                        ModelInfo = item.primaryModelInfo
                    };
                    if (item.itemCategory.Equals("Face_Paint") || item.itemCategory.Equals("Equip_Decals"))
                    {
                        var getPartList = character.GetDecalNums(charaItem);
                        getPartList.Wait();
                        partList = getPartList.Result.Select(part => part.ToString()).ToList();
                        if (item.itemCategory.Equals("Equip_Decals"))
                            partList.Add("_stigma");
                    }
                    else
                    {
                        var getRaces = character.GetRacesAndNumbersForModels(charaItem);
                        getRaces.Wait();
                        partList = getRaces.Result[race].Select(part => part.ToString()).ToList();
                    }
                }
                else if ((item.itemCategory.Equals("Mounts") || item.itemCategory.Equals("Monster")) && item.primaryModelInfo.ModelType == XivItemType.demihuman)
                {
                    Companions companions = new Companions(MainClass._indexDirectory, gameLanguage);
                    var mountItem = new XivMount{
                        Name = item.name,
                        PrimaryCategory = item.category,
                        SecondaryCategory = item.itemCategory,
                        TertiaryCategory = item.itemSubCategory,
                        DataFile = item.dataFile,
                        ModelInfo = item.primaryModelInfo
                    };
                    var equipParts = companions.GetDemiHumanMountTextureEquipPartList(mountItem);
                    equipParts.Wait();
                    foreach (var equipPart in equipParts.Result)
                        partList.Add(equipPart.Key);
                }
                else if (item.category.Equals("Gear"))
                {
                    partList.Add("Primary");
                    if (item.secondaryModelInfo != null && item.secondaryModelInfo.ModelID > 0)
                        partList.Add("Secondary");
                }
                else
                {
                    Tex tex = new Tex(MainClass._indexDirectory, item.dataFile);
                    var anItem = new XivGenericItemModel{
                        Name = item.name,
                        PrimaryCategory = item.category,
                        SecondaryCategory = item.itemCategory,
                        TertiaryCategory = item.itemSubCategory,
                        DataFile = item.dataFile,
                        ModelInfo = item.primaryModelInfo
                    };
                    var getParts = tex.GetTexturePartList(anItem, race, item.dataFile);
                    getParts.Wait();
                    partList = getParts.Result;
                }
            return partList;
        }
    }
}