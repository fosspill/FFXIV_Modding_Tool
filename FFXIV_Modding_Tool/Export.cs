using System.Collections.Generic;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Items.Enums;
using xivModdingFramework.Items.Categories;
using xivModdingFramework.Items.DataContainers;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Search;

namespace FFXIV_Modding_Tool.Exporting
{
    public class Export
    {
        MainClass main = new MainClass();
        Config config = new Config();
        
        public void GetExportInfo(GameSearch.ItemInfo item)
        {
            if (int.TryParse(item.name, out int modelId))
                item = LoadModelData(modelId, item);
            Dictionary<string, List<XivRace>> availableRaces = new Dictionary<string, List<XivRace>>();
            main.PrintMessage("Retrieving racial data...");
            availableRaces["[Textures]"] = GetTextureRaces(item);
            availableRaces["[Model]"] = GetModelRaces(item);
            foreach (KeyValuePair<string, List<XivRace>> allRaces in availableRaces)
            {
                main.PrintMessage(allRaces.Key);
                if (allRaces.Value.Count == 0)
                    main.PrintMessage("N/A");
                else
                {
                    foreach (XivRace race in allRaces.Value)
                        main.PrintMessage(race.GetDisplayName());
                }
            }
        }

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
                        ModelID = modelId,
                        Body = body,
                        Variant = variant
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
                        ModelID = modelId,
                        Body = body,
                        Variant = variant,
                        ModelType = XivItemType.monster
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
                        ModelID = modelId,
                        Body = body,
                        Variant = variant,
                        ModelType = XivItemType.demihuman
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
                        ModelID = modelId
                    }
                };
            }
            return itemInfo;
        }

        List<XivRace> GetTextureRaces(GameSearch.ItemInfo item)
        {
            List<XivRace> races = new List<XivRace>();
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            if (item.category.Equals("Gear"))
            {
                Gear gear = new Gear(MainClass._indexDirectory, gameLanguage);
                var gearItem = new XivGear{
                    Name = item.name,
                    Category = item.category,
                    ItemCategory = item.itemCategory,
                    ItemSubCategory = item.itemSubCategory,
                    DataFile = item.dataFile,
                    ModelInfo = item.primaryModelInfo,
                    SecondaryModelInfo = item.secondaryModelInfo,
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
                        Category = item.category,
                        ItemCategory = item.itemCategory,
                        ItemSubCategory = item.itemSubCategory,
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

        List<XivRace> GetModelRaces(GameSearch.ItemInfo item)
        {
            List<XivRace> races = new List<XivRace>();
            XivLanguage gameLanguage = XivLanguages.GetXivLanguage(config.ReadConfig("Language"));
            if (item.category.Equals("Gear"))
            {
                Gear gear = new Gear(MainClass._indexDirectory, gameLanguage);
                var gearItem = new XivGear{
                    Name = item.name,
                    Category = item.category,
                    ItemCategory = item.itemCategory,
                    ItemSubCategory = item.itemSubCategory,
                    DataFile = item.dataFile,
                    ModelInfo = item.primaryModelInfo,
                    SecondaryModelInfo = item.secondaryModelInfo,
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
                    Category = item.category,
                    ItemCategory = item.itemCategory,
                    ItemSubCategory = item.itemSubCategory,
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
    }
}