﻿using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HighSellPrice
{
    [HarmonyPatch(typeof(SettlementNameplateVM), "RefreshDynamicProperties")]
    public class HighSellPriceVM
    {
        public static int NumOfEventTypes => Enum.GetNames(typeof(SettlementNameplateEventItemVM.SettlementEventType)).Length;

        public static void Postfix(SettlementNameplateVM __instance)
        {
            if (__instance.IsInRange && __instance.Settlement.IsTown)
            {
                ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
                MBBindingList<SettlementNameplateEventItemVM> eventsList = __instance.SettlementEvents.EventsList;
                SettlementNameplateEventItemVM highSellPriceEvent = eventsList.FirstOrDefault(e => e.EventType == (SettlementNameplateEventItemVM.SettlementEventType)NumOfEventTypes);
                HighSellPriceSettings settings = HighSellPriceSettings.Instance;
                int numOfHighSellingItems = 0;

                for (int i = 0; i < itemRoster.Count; i++)
                {
                    ItemObject itemAtIndex = itemRoster.GetItemAtIndex(i);
                    int elementNumber = itemRoster.GetElementNumber(i);
                    bool isFood = itemAtIndex.IsFood, isCraftable = itemAtIndex.ItemCategory == DefaultItemCategories.Iron || itemAtIndex.ItemCategory == DefaultItemCategories.Wood, isOther = itemAtIndex.IsTradeGood && !isFood && !isCraftable;

                    if ((isFood && settings.ShouldCountFood && elementNumber >= settings.MinFoodCount) || (isCraftable && settings.ShouldCountCraftables && elementNumber >= settings.MinCraftableCount) || (isOther && settings.ShouldCountOthers && elementNumber >= settings.MinOtherCount))
                    {
                        ItemCategory itemCategory = itemAtIndex.ItemCategory;
                        float num = 0f, num2 = 0f;

                        if (Town.AllTowns != null)
                        {
                            foreach (Town town in Town.AllTowns)
                            {
                                num += town.GetItemCategoryPriceIndex(itemCategory);
                                num2 += 1f;
                            }
                        }

                        num /= num2 * 2f;

                        if (__instance.Settlement.Town.GetItemPrice(itemAtIndex, MobileParty.MainParty, true) / (float)itemAtIndex.Value > num * 1.3f)
                        {
                            numOfHighSellingItems++;
                        }
                    }
                }

                if (numOfHighSellingItems > 0)
                {
                    if (!eventsList.Contains(highSellPriceEvent))
                    {
                        // If the player has trade goods of more than the configurable number with high selling prices, add an icon to the city's nameplate. 
                        eventsList.Add(new SettlementNameplateEventItemVM((SettlementNameplateEventItemVM.SettlementEventType)NumOfEventTypes));
                    }
                }
                else
                {
                    // If not, remove the icon.
                    eventsList.Remove(highSellPriceEvent);
                }
            }
        }
    }
}
