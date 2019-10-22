using xivModdingFramework.General.Enums;
using xivModdingFramework.Items.Enums;
using xivModdingFramework.Items.DataContainers;
using FFXIV_Modding_Tool.Search;

namespace FFXIV_Modding_Tool.Exporting
{
    public class Export
    {
        MainClass main = new MainClass();
        public void GetExportInfo(GameSearch.ItemInfo item)
        {
            if (int.TryParse(item.name, out int modelId))
                item = LoadModelData(modelId, item);
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
    }
}