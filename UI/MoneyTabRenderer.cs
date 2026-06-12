using System;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    /// <summary>
    /// Renders the Money tab (D1). Handles cash and XP manipulation.
    /// </summary>
    public static class MoneyTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle buttonStyle, GUIStyle boxStyle, string[] amountLabels, string[] xpLabels,
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
            float num = y + 3f;

            TMPHybridService.Instance.Label(6f, num, 50f, 18f, "Amt:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (GUIFit.Button(new Rect(52f, num, 24f, 18f), "<", buttonStyle))
            {
                setAmountIndex(Math.Max(0, selectedAmountIndex - 1));
            }

            TMPHybridService.Instance.Label(80f, num, 60f, 18f, amountLabels[Mathf.Clamp(selectedAmountIndex, 0, amountLabels.Length - 1)],
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));

            if (GUIFit.Button(new Rect(144f, num, 24f, 18f), ">", buttonStyle))
            {
                setAmountIndex(Math.Min(amountLabels.Length - 1, selectedAmountIndex + 1));
            }

            float num2 = (w - 8f) / 2f;
            if (GUIFit.Button(new Rect(4f, num + 22f, num2, 18f), "+ Cash", buttonStyle)) onAddCash?.Invoke();
            if (GUIFit.Button(new Rect(8f + num2, num + 22f, num2, 18f), "+ Online", buttonStyle)) onAddOnlineBalance?.Invoke();

            y += 50f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "XP",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
            num = y + 3f;

            TMPHybridService.Instance.Label(6f, num, 24f, 18f, "XP:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (GUIFit.Button(new Rect(32f, num, 24f, 18f), "<", buttonStyle))
            {
                setXpIndex(Math.Max(0, selectedXpIndex - 1));
            }

            TMPHybridService.Instance.Label(60f, num, 45f, 18f, xpLabels[Mathf.Clamp(selectedXpIndex, 0, xpLabels.Length - 1)],
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));

            if (GUIFit.Button(new Rect(108f, num, 24f, 18f), ">", buttonStyle))
            {
                setXpIndex(Math.Min(xpLabels.Length - 1, selectedXpIndex + 1));
            }

            if (GUIFit.Button(new Rect(136f, num, 70f, 18f), "+ Add XP", buttonStyle)) onAddXp?.Invoke();

            y += 28f;
        }
    }
}