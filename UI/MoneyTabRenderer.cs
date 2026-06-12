using System;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    public static class MoneyTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            string[] amountLabels, string[] xpLabels,
            int selectedAmountIndex, int selectedXpIndex, Action<int> setAmountIndex, Action<int> setXpIndex,
            Action onAddCash, Action onAddOnlineBalance, Action onAddXp)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "MONEY",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 46f), "", boxStyle);
            float rowY = y + 3f;

            TMPHybridService.Instance.Label(6f, rowY, 50f, 18f, "Amt:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (GUIFit.Button(new Rect(52f, rowY, 24f, 18f), "<", buttonStyle))
            {
                setAmountIndex(Math.Max(0, selectedAmountIndex - 1));
            }

            TMPHybridService.Instance.Label(80f, rowY, 60f, 18f, amountLabels[Mathf.Clamp(selectedAmountIndex, 0, amountLabels.Length - 1)],
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));

            if (GUIFit.Button(new Rect(144f, rowY, 24f, 18f), ">", buttonStyle))
            {
                setAmountIndex(Math.Min(amountLabels.Length - 1, selectedAmountIndex + 1));
            }

            float actionButtonWidth = (w - 8f) / 2f;
            if (GUIFit.Button(new Rect(4f, rowY + 22f, actionButtonWidth, 18f), "+ Cash", buttonStyle)) onAddCash?.Invoke();
            if (GUIFit.Button(new Rect(8f + actionButtonWidth, rowY + 22f, actionButtonWidth, 18f), "+ Online", buttonStyle)) onAddOnlineBalance?.Invoke();

            y += 50f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "XP",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
            rowY = y + 3f;

            TMPHybridService.Instance.Label(6f, rowY, 24f, 18f, "XP:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (GUIFit.Button(new Rect(32f, rowY, 24f, 18f), "<", buttonStyle))
            {
                setXpIndex(Math.Max(0, selectedXpIndex - 1));
            }

            TMPHybridService.Instance.Label(60f, rowY, 45f, 18f, xpLabels[Mathf.Clamp(selectedXpIndex, 0, xpLabels.Length - 1)],
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));

            if (GUIFit.Button(new Rect(108f, rowY, 24f, 18f), ">", buttonStyle))
            {
                setXpIndex(Math.Min(xpLabels.Length - 1, selectedXpIndex + 1));
            }

            if (GUIFit.Button(new Rect(136f, rowY, 70f, 18f), "+ Add XP", buttonStyle)) onAddXp?.Invoke();

            y += 28f;
        }
    }
}
