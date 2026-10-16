// Consolidated from: SpecialOrderInfiniteDuration.cs, SpecialOrderNoTicket.cs
// Original class implementations preserved.
using HarmonyLib;
using StardewModdingAPI.Utilities;
using StardewValley.GameData.SpecialOrders;
using StardewValley.SpecialOrders;
using StardewValley;

// ===== Original file: SpecialOrderInfiniteDuration.cs =====
namespace MagicStardew
{
    [HarmonyPatch]
    internal static class SpecialOrderInfiniteDuration
    {
        private const string InfiniteKey = "MagicStardew.InfiniteDuration";

        private static bool IsInfinite(SpecialOrder order)
        {
            SpecialOrderData data = order.GetData();
            return data?.CustomFields != null && data.CustomFields.ContainsKey(InfiniteKey);
        }

        [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.IsTimedQuest))]
        [HarmonyPostfix]
        public static void IsTimedQuest_Postfix(SpecialOrder __instance, ref bool __result)
        {
            // Jeśli to nasze nieskończone zadanie, nadpisujemy wynik na false
            if (IsInfinite(__instance))
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.GetDaysLeft))]
        [HarmonyPrefix]
        public static bool GetDaysLeft_Prefix(SpecialOrder __instance, ref int __result)
        {
            if (IsInfinite(__instance))
            {
                // Udajemy, że zawsze zostało 99 dni
                __result = 99;
                return false; // false blokuje wykonanie oryginalnej metody z gry
            }
            return true; // true pozwala grze wykonać jej własną metodę
        }

        [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.SetDuration))]
        [HarmonyPrefix]
        public static bool SetDuration_Prefix(SpecialOrder __instance)
        {
            if (IsInfinite(__instance))
            {
                // Czysty zapis do zmiennej Netcode, bez koszmarnego rzutowania
                __instance.dueDate.Value = SDate.Now().AddDays(99).DaysSinceStart;

                return false; // Blokujemy oryginalne ustalanie czasu
            }
            return true;
        }
    }
}

// ===== Original file: SpecialOrderNoTicket.cs =====
namespace MagicStardew
{
    [HarmonyPatch]
    internal static class SpecialOrderNoTicket
    {
        private const string NoTicketKey = "MagicStardew.NoTicket";

        private static bool IsNoTicket(SpecialOrder order)
        {
            SpecialOrderData data = order.GetData();
            return data?.CustomFields != null && data.CustomFields.ContainsKey(NoTicketKey);
        }

        [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.Update))]
        [HarmonyPrefix]
        public static void Update_Prefix(SpecialOrder __instance, out string __state)
        {
            __state = null;

            if (IsNoTicket(__instance))
            {
                if (__instance.questState.Value == SpecialOrderStatus.Complete &&
                    __instance.unclaimedRewards.ContainsKey(Game1.player.UniqueMultiplayerID))
                {
                    // Zapisujemy oryginalny orderType do zmiennej __state
                    __state = __instance.orderType.Value;

                    // Zmieniamy typ na chwilę, aby gra ominęła przyznanie biletu
                    __instance.orderType.Value = "MagicStardew_NoTicket";
                }
            }
        }

        [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(SpecialOrder __instance, string __state)
        {
            // Przywracamy oryginalny orderType
            if (__state != null)
            {
                __instance.orderType.Value = __state;
            }
        }
    }
}
