using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.Quests;

namespace NugzzMenu.Services
{
    internal static class WelcomeQuestProgression
    {
        private static readonly HashSet<int> Recovering = new HashSet<int>();
        private static readonly HashSet<int> Recovered = new HashSet<int>();

        public static void RecoverPostNoteTransition(Quest_WelcomeToHylandPoint quest)
        {
            if (quest == null)
                return;

            int id;
            try
            {
                id = quest.GetInstanceID();
                if (quest.State != EQuestState.Active || Recovering.Contains(id) || Recovered.Contains(id))
                    return;
            }
            catch
            {
                return;
            }

            QuestEntry note = FindEntry(quest, "read the note");
            QuestEntry payphone = FindEntry(quest, "talk to uncle nelson at a payphone");
            if (note == null || payphone == null ||
                note.State != EQuestState.Completed || payphone.State != EQuestState.Inactive)
                return;

            Recovering.Add(id);
            try
            {
                QuestEntry investigate = FindEntry(quest, "investigate the explosion");
                if (investigate != null && investigate.State == EQuestState.Active)
                    investigate.Complete();

                if (payphone.State == EQuestState.Inactive)
                    payphone.SetActive(true);

                if (payphone.State == EQuestState.Active)
                    Recovered.Add(id);
            }
            catch { }
            finally
            {
                Recovering.Remove(id);
            }
        }

        private static QuestEntry FindEntry(Quest_WelcomeToHylandPoint quest, string title)
        {
            if (quest?.Entries == null)
                return null;

            string expected = Normalize(title);
            try
            {
                for (int i = 0; i < quest.Entries.Count; i++)
                {
                    QuestEntry entry = quest.Entries[i];
                    if (entry != null && Normalize(entry.Title) == expected)
                        return entry;
                }
            }
            catch { }

            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            char[] buffer = new char[value.Length];
            int length = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char character = char.ToLowerInvariant(value[i]);
                if (char.IsLetterOrDigit(character))
                    buffer[length++] = character;
            }

            return new string(buffer, 0, length);
        }
    }

    [HarmonyPatch(typeof(QuestEntry), nameof(QuestEntry.Complete))]
    internal static class WelcomeQuestEntryCompletePatch
    {
        private static void Postfix(QuestEntry __instance)
        {
            try
            {
                WelcomeQuestProgression.RecoverPostNoteTransition(
                    __instance?.ParentQuest as Quest_WelcomeToHylandPoint);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(QuestEntry), nameof(QuestEntry.SetState))]
    internal static class WelcomeQuestEntryStatePatch
    {
        private static void Postfix(QuestEntry __instance)
        {
            try
            {
                WelcomeQuestProgression.RecoverPostNoteTransition(
                    __instance?.ParentQuest as Quest_WelcomeToHylandPoint);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Quest_WelcomeToHylandPoint), "OnUncappedMinPass")]
    internal static class WelcomeQuestMinuteRecoveryPatch
    {
        private static void Postfix(Quest_WelcomeToHylandPoint __instance)
        {
            WelcomeQuestProgression.RecoverPostNoteTransition(__instance);
        }
    }
}
