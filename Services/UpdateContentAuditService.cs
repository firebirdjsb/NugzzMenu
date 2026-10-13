using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Core.Avatar;
using Il2CppScheduleOne.Property;
using UnityEngine;

namespace NugzzMenu.Services
{
    public static class UpdateContentAuditService
    {
        private static readonly string[] Items = { "blackrubbermat_1x1", "blackrubbermat_2x2", "blackrubbermat_4x4", "persianrug",
            "goldaviators", "silveraviators", "browlineglasses", "roundtintedglasses", "beerhat", "jethelmet", "panzerhelmet",
            "paisleybandana", "usabandana", "bluetie", "redtie", "greentie", "browntie", "leatherjacket", "aquatiedyeshirt",
            "purpletiedyeshirt", "yellowtiedyeshirt", "dirtytanktop", "hotshirt", "moleratsjersey", "tubetop", "meshtop" };
        private static readonly string[] Cosmetics = { "batemanhair", "waveyhair", "longwaveyhair", "chevronmustache",
            "swirlymustache", "beardsoulpatch", "beardbushymoustache", "beardhandlebar", "beardmuttonchops", "beardfull" };
        private static readonly string[] Locations = { "southoverpass", "suburbia", "park", "westville", "casino" };
        private static readonly Dictionary<string, int> Groups = new Dictionary<string, int>
            { { "hippies", 8 }, { "partybus", 9 }, { "businessmen", 6 }, { "bikers", 8 } };
        public static string Status { get; private set; } = "Run this in a loaded save to check native registrations.";

        public static void Run()
        {
            try
            {
                var registry = ManagerCacheService.Instance.Registry;
                if (registry == null || ManagerCacheService.Instance.LocalPlayer == null)
                {
                    Status = "Load a save before auditing the update content.";
                    NotificationService.Instance.Notify(Status);
                    return;
                }
                var missing = new List<string>();
                foreach (string id in Items)
                    if (registry._GetItem(id, false) == null || !ItemService.Instance.TryFindItem(id, out _))
                        missing.Add("Item: " + id);
                SpecialCustomerService.Instance.Refresh();
                foreach (var expected in Groups)
                {
                    SpecialCustomerService.Group match = null;
                    foreach (var group in SpecialCustomerService.Instance.Groups)
                        if (group.Id == expected.Key) match = group;
                    if (match == null || match.Members.Count < expected.Value)
                        missing.Add("Roster: " + expected.Key + " (expected " + expected.Value + ")");
                }
                var objectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (AvatarObject obj in Resources.FindObjectsOfTypeAll<AvatarObject>())
                    if (obj != null) objectIds.Add(obj.Id);
                foreach (string id in Cosmetics)
                    if (!objectIds.Contains(id)) missing.Add("Cosmetic: " + id);
                Transform points = Il2CppScheduleOne.Console.Instance?.TeleportPointsContainer;
                var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; points != null && i < points.childCount; i++) locations.Add(points.GetChild(i).name);
                foreach (string id in Locations)
                    if (!locations.Contains(id)) missing.Add("Teleport: " + id);
                bool medical = false;
                var businesses = Business.Businesses;
                for (int i = 0; businesses != null && i < businesses.Count; i++)
                    if (businesses[i] != null && businesses[i].PropertyName.IndexOf("Hyland", StringComparison.OrdinalIgnoreCase) >= 0)
                        medical = true;
                if (!medical) missing.Add("Business: Hyland Medical");
                Status = missing.Count == 0
                    ? "Registered: 4 groups / 31 visitors, 26 items, 10 cosmetics, 5 teleports, Hyland Medical."
                    : "Unavailable registrations (see log): " + missing.Count;
                foreach (string entry in missing) MelonLoader.MelonLogger.Warning("[Nugzz content audit] " + entry);
                NotificationService.Instance.Notify(Status);
            }
            catch (Exception ex)
            {
                Status = "Content audit failed: " + ex.GetType().Name;
                DebugLogService.Instance.VerboseException("Update content audit", ex);
                NotificationService.Instance.Notify(Status);
            }
        }
    }
}
