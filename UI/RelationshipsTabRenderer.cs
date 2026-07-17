using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public static class RelationshipsTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            RelationshipService service)
        {
            service.EnsureFresh();

            DrawHeader(4f, y, w, "NPC / CLIENT RELATIONSHIPS");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 66f), boxStyle);
            DrawLabel(8f, y + 7f, 58f, 18f, "Search", LabelCategory.Label);
            string search = GUIFit.TextField(new Rect(66f, y + 5f, w - 274f, 22f),
                service.SearchText, 48, "relationships.search");
            if (search != service.SearchText)
                service.SetSearchText(search);
            if (GUIFit.Button(new Rect(w - 202f, y + 5f, 94f, 22f), "Clear", buttonStyle))
                service.SetSearchText(string.Empty);
            if (GUIFit.Button(new Rect(w - 102f, y + 5f, 96f, 22f), "Refresh", buttonStyle))
                service.Refresh();
            DrawLabel(8f, y + 37f, w - 16f, 20f, "Status: " + service.Status,
                LabelCategory.Status);
            y += 74f;

            DrawHeader(4f, y, w, "PEOPLE");
            y += 20f;
            int pageItems = service.GetPageItemCount();
            float listHeight = 40f + Mathf.Max(1, pageItems) * 26f;
            GUIFit.Panel(new Rect(0f, y, w, listHeight), boxStyle);
            float rowY = y + 6f;
            float third = (w - 24f) / 3f;
            if (GUIFit.Button(new Rect(6f, rowY, third, 22f), "Prev Page", buttonStyle))
                service.PreviousPage();
            GUIFit.Button(new Rect(12f + third, rowY, third, 22f),
                "Page " + (service.PageIndex + 1) + "/" + service.GetPageCount(), buttonStyle);
            if (GUIFit.Button(new Rect(18f + third * 2f, rowY, third, 22f), "Next Page", buttonStyle))
                service.NextPage();
            rowY += 28f;
            if (pageItems == 0)
            {
                DrawLabel(8f, rowY, w - 16f, 18f, "No matching NPCs or clients.",
                    LabelCategory.Status);
            }
            else
            {
                for (int i = 0; i < pageItems; i++)
                {
                    if (GUIFit.Button(new Rect(6f, rowY, w - 12f, 22f),
                            service.GetPageLabel(i), buttonStyle))
                    {
                        service.SelectPageRow(i);
                    }
                    rowY += 26f;
                }
            }
            y += listHeight + 8f;

            DrawHeader(4f, y, w, "SELECTED PERSON");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 100f), boxStyle);
            DrawLabel(8f, y + 7f, w - 16f, 86f, service.GetSelectedDetails(),
                LabelCategory.Label, true);
            y += 108f;

            DrawHeader(4f, y, w, "RELATIONSHIP");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 88f), boxStyle);
            string[] levels = { "0", "1", "2", "3", "4", "5" };
            DrawButtonRow(y + 6f, 6f, w - 12f, levels, buttonStyle,
                i => service.SetRelationship(i));
            float half = (w - 22f) / 3f;
            if (GUIFit.Button(new Rect(6f, y + 34f, half, 22f), "- 0.25", buttonStyle))
                service.ChangeRelationship(-0.25f);
            if (GUIFit.Button(new Rect(11f + half, y + 34f, half, 22f), "+ 0.25", buttonStyle))
                service.ChangeRelationship(0.25f);
            if (GUIFit.Button(new Rect(16f + half * 2f, y + 34f, half, 22f),
                    "Unlock Person", buttonStyle))
                service.UnlockSelected();
            if (GUIFit.Button(new Rect(6f, y + 60f, w - 12f, 22f),
                    "Unlock This Person's Connections", buttonStyle))
                service.UnlockConnections();
            y += 96f;

            if (service.SelectedIsCustomer)
            {
                DrawCustomerControls(ref y, w, buttonStyle, boxStyle, service);
            }

            DrawLabel(8f, y, w - 16f, 34f,
                "Relationship edits use the game's network-aware setters and are host-only in multiplayer.",
                LabelCategory.Status, true);
            y += 40f;
        }

        private static void DrawCustomerControls(ref float y, float w, GUIStyle buttonStyle,
            GUIStyle boxStyle, RelationshipService service)
        {
            DrawHeader(4f, y, w, "CLIENT ADDICTION");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 58f), boxStyle);
            DrawButtonRow(y + 6f, 6f, w - 12f,
                new[] { "0%", "25%", "50%", "75%", "100%" }, buttonStyle,
                i => service.SetAddiction(new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }[i]));
            float half = (w - 16f) * 0.5f;
            if (GUIFit.Button(new Rect(6f, y + 33f, half, 20f), "- 10%", buttonStyle))
                service.ChangeAddiction(-0.1f);
            if (GUIFit.Button(new Rect(10f + half, y + 33f, half, 20f), "+ 10%", buttonStyle))
                service.ChangeAddiction(0.1f);
            y += 66f;

            DrawHeader(4f, y, w, "PRODUCT AFFINITY");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 86f), boxStyle);
            string[] drugs = new string[6];
            for (int i = 0; i < drugs.Length; i++)
                drugs[i] = service.GetDrugLabel(i);
            DrawButtonRow(y + 6f, 6f, w - 12f, drugs, buttonStyle, service.SetDrugType);
            float fifth = (w - 28f) / 5f;
            string[] affinity = { "0%", "50%", "100%", "- 10%", "+ 10%" };
            for (int i = 0; i < affinity.Length; i++)
            {
                if (!GUIFit.Button(new Rect(6f + i * (fifth + 4f), y + 34f, fifth, 22f),
                        affinity[i], buttonStyle))
                    continue;
                if (i < 3) service.SetAffinity(new[] { 0f, 0.5f, 1f }[i]);
                else service.ChangeAffinity(i == 3 ? -0.1f : 0.1f);
            }
            DrawLabel(8f, y + 61f, w - 16f, 18f,
                "Affinity affects which product types this client prefers.", LabelCategory.Subtitle);
            y += 94f;

            DrawHeader(4f, y, w, "CLIENT ACTIONS");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 38f), boxStyle);
            float actionHalf = (w - 18f) * 0.5f;
            if (GUIFit.Button(new Rect(6f, y + 7f, actionHalf, 24f),
                    "Mark Recommended", buttonStyle))
                service.MarkRecommended();
            if (GUIFit.Button(new Rect(12f + actionHalf, y + 7f, actionHalf, 24f),
                    "Force Deal Offer", buttonStyle))
                service.ForceDealOffer();
            y += 46f;
        }

        private static void DrawButtonRow(float y, float x, float width, string[] labels,
            GUIStyle style, System.Action<int> clicked)
        {
            float buttonWidth = (width - (labels.Length - 1) * 4f) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                if (GUIFit.Button(new Rect(x + i * (buttonWidth + 4f), y, buttonWidth, 22f),
                        labels[i], style))
                    clicked(i);
            }
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
