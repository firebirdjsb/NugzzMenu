using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public static class QuestTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle buttonStyle,
            GUIStyle boxStyle, QuestService service)
        {
            if (service == null)
                return;

            service.EnsureFresh();

            DrawHeader(4f, y, w, "QUEST CONTROL");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 92f), boxStyle);

            float rowY = y + 6f;
            float halfW = (w - 18f) * 0.5f;
            if (GUIFit.Button(new Rect(6f, rowY, halfW, 24f),
                    "Check Welcome / RV Quest State", buttonStyle))
            {
                service.InspectWelcomeExplosionQuest();
            }
            if (GUIFit.Button(new Rect(12f + halfW, rowY, halfW, 24f),
                    "Refresh Quest List", buttonStyle))
            {
                service.Refresh();
            }

            rowY += 30f;
            DrawLabel(8f, rowY, w - 16f, 18f,
                "Quest inspection is read-only. Manual controls below are scene-specific.",
                LabelCategory.Label);
            rowY += 22f;
            DrawLabel(8f, rowY, w - 16f, 18f, "Status: " + service.LastStatus,
                LabelCategory.Status);

            y += 100f;

            DrawHeader(4f, y, w, "LIVE QUESTS");
            y += 20f;

            const int pageSize = 8;
            int pageCount = service.GetPageCount(pageSize);
            int pageItems = service.GetPageItemCount(pageSize);
            float panelHeight = 92f + Mathf.Max(1, pageItems) * 26f;
            GUIFit.Panel(new Rect(0f, y, w, panelHeight), boxStyle);

            rowY = y + 6f;
            float thirdW = (w - 24f) / 3f;
            if (GUIFit.Button(new Rect(6f, rowY, thirdW, 22f), "Prev Page", buttonStyle))
                service.PreviousPage();
            if (GUIFit.Button(new Rect(12f + thirdW, rowY, thirdW, 22f),
                    "Page " + (service.PageIndex + 1) + "/" + pageCount, buttonStyle))
            {
                service.Refresh();
            }
            if (GUIFit.Button(new Rect(18f + thirdW * 2f, rowY, thirdW, 22f), "Next Page", buttonStyle))
                service.NextPage(pageSize);

            rowY += 28f;

            if (service.QuestCount == 0)
            {
                DrawLabel(8f, rowY, w - 16f, 18f, "No quest objects found yet.", LabelCategory.Status);
                rowY += 26f;
            }
            else
            {
                for (int i = 0; i < pageItems; i++)
                {
                    int questIndex = service.GetPageQuestIndex(i, pageSize);
                    string label = service.GetQuestLabel(questIndex);
                    if (questIndex == service.SelectedIndex)
                        label = "> " + label;

                    if (GUIFit.Button(new Rect(6f, rowY, w - 12f, 22f), label, buttonStyle))
                        service.Select(questIndex);

                    rowY += 26f;
                }
            }

            y += panelHeight + 8f;

            DrawHeader(4f, y, w, "SELECTED QUEST");
            y += 20f;
            float detailsHeight = service.GetSelectedDetailsHeight(w - 16f);
            float selectedPanelHeight = detailsHeight + 74f;
            GUIFit.Panel(new Rect(0f, y, w, selectedPanelHeight), boxStyle);
            rowY = y + 6f;
            DrawLabel(8f, rowY, w - 16f, detailsHeight, service.GetSelectedDetails(), LabelCategory.Label, true);

            rowY += detailsHeight + 8f;
            float quarterW = (w - 30f) / 4f;
            if (GUIFit.Button(new Rect(6f, rowY, quarterW, 24f), "Start", buttonStyle))
                service.StartSelected();
            if (GUIFit.Button(new Rect(12f + quarterW, rowY, quarterW, 24f), "Complete", buttonStyle))
                service.CompleteSelected();
            if (GUIFit.Button(new Rect(18f + quarterW * 2f, rowY, quarterW, 24f), "End", buttonStyle))
                service.EndSelected();
            if (GUIFit.Button(new Rect(24f + quarterW * 3f, rowY, quarterW, 24f), "Reset", buttonStyle))
                service.ResetSelected();

            rowY += 30f;
            DrawLabel(8f, rowY, w - 16f, 18f,
                "Manual controls are host/scene sensitive. Use with saves backed up.",
                LabelCategory.Status);
            y += selectedPanelHeight + 8f;
        }

        private static void DrawHeader(float x, float y, float w, string text)
        {
            TMPHybridService.Instance.Label(x, y, w, 18f, text,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
        }

        private static void DrawLabel(float x, float y, float w, float h, string text,
            LabelCategory category, bool wordWrap = false)
        {
            TMPHybridService.Instance.Label(x, y, w, h, text ?? string.Empty,
                GUISystemService.Instance.GetColorForCategory(category),
                GUISystemService.Instance.GetFontSizeForCategory(category),
                GUISystemService.Instance.GetAlignmentForCategory(category),
                GUISystemService.Instance.GetStyleForCategory(category), wordWrap);
        }
    }
}
