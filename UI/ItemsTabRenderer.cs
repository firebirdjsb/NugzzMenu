using System;
using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public class ItemsState
    {
        public int SpawnQuantity { get; set; } = 1;
        public int QualityIndex { get; set; } = 2;
        public int FilterIndex { get; set; } = 0;
        public int PageIndex { get; set; } = 0;
        public string SearchText { get; set; } = "";
    }

    public static class ItemsTabRenderer
    {
        private static readonly string[] QualityLabels = { "Trash", "Poor", "Std", "Prem", "Heaven" };
        private static readonly int[] SpawnQuantities = { 1, 5, 10, 25, 50, 100 };
        private static GUIStyle _styleSource;
        private static GUIStyle _smallButton;
        private static GUIStyle _selectedButton;
        private static GUIStyle _categoryButton;
        private static GUIStyle _itemButton;

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            ItemService service, ItemsState state,
            Action<int> updateQuantity, Action<int> updateQuality, Action<int> updateFilter)
        {
            try
            {
                EnsureStyles(buttonStyle);

                TMPHybridService.Instance.Label(4f, y, w, 18f, "SPAWN COUNT",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
                float rowY = y + 3f;
                float quantityButtonWidth = (w - 28f) / SpawnQuantities.Length;

                for (int i = 0; i < SpawnQuantities.Length; i++)
                {
                    string countLabel = state.SpawnQuantity == SpawnQuantities[i] ? "> " + SpawnQuantities[i] + " <" : SpawnQuantities[i].ToString();
                    if (GUIFit.Button(new Rect(4f + i * (quantityButtonWidth + 4f), rowY, quantityButtonWidth, 18f),
                            countLabel,
                            state.SpawnQuantity == SpawnQuantities[i] ? _selectedButton : _smallButton))
                    {
                        state.SpawnQuantity = SpawnQuantities[i];
                        updateQuantity?.Invoke(SpawnQuantities[i]);
                    }
                }
                y += 28f;

                TMPHybridService.Instance.Label(4f, y, w, 18f, "QUALITY LEVEL",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
                rowY = y + 3f;
                float qualityButtonWidth = (w - 24f) / QualityLabels.Length;

                for (int qualityIndex = 0; qualityIndex < QualityLabels.Length; qualityIndex++)
                {
                    string qualityLabel = qualityIndex == state.QualityIndex
                        ? "> " + QualityLabels[qualityIndex] + " <"
                        : QualityLabels[qualityIndex];
                    if (GUIFit.Button(
                            new Rect(4f + qualityIndex * (qualityButtonWidth + 4f), rowY, qualityButtonWidth, 18f),
                            qualityLabel,
                            qualityIndex == state.QualityIndex ? _selectedButton : buttonStyle))
                    {
                        state.QualityIndex = qualityIndex;
                        updateQuality?.Invoke(qualityIndex);
                    }
                }
                y += 36f;

                TMPHybridService.Instance.Label(4f, y, w, 18f, "CATEGORY",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;

                int categoryCount = ItemService.CategoryCount;
                const int categoriesPerRow = 6;
                int categoryRows = (categoryCount + categoriesPerRow - 1) / categoriesPerRow;
                float categoryBoxHeight = categoryRows * 30f + 12f;

                GUI.Box(new Rect(0f, y, w, categoryBoxHeight), "", boxStyle);
                rowY = y + 4f;
                float categoryButtonWidth = (w - (categoriesPerRow + 1) * 4f) / categoriesPerRow;

                for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
                {
                    int row = categoryIndex / categoriesPerRow;
                    int column = categoryIndex % categoriesPerRow;
                    string label = ItemService.GetCategoryLabel(categoryIndex);
                    if (GUIFit.Button(
                            new Rect(4f + column * (categoryButtonWidth + 4f), rowY + row * 30f, categoryButtonWidth, 26f),
                            label,
                            _categoryButton))
                    {
                        state.FilterIndex = categoryIndex;
                        updateFilter?.Invoke(categoryIndex);
                    }
                }
                y += categoryBoxHeight + 6f;

                TMPHybridService.Instance.Label(4f, y, w, 18f, "ITEM SPAWNER",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUI.Box(new Rect(0f, y, w, 56f), "", boxStyle);

                string previousSearch = string.Empty;
                string newSearch = string.Empty;
                try
                {
                    previousSearch = service.GetSearchText() ?? string.Empty;
                    newSearch = GUIFit.TextField(new Rect(68f, y + 4f, w - 260f, 22f), previousSearch, 50);
                }
                catch (Exception) { }

                if (newSearch != null && newSearch != previousSearch)
                {
                    service.SetSearchText(newSearch);
                    state.SearchText = newSearch;
                }

                if (GUIFit.Button(new Rect(w - 184f, y + 4f, 86f, 22f), "Clear Inv", _smallButton))
                {
                    service.ClearInventoryItemsOnly();
                }

                if (GUIFit.Button(new Rect(w - 94f, y + 4f, 88f, 22f), "Clear Search", _smallButton))
                {
                    service.SetSearchText("");
                    state.SearchText = "";
                }
                y += 60f;

                int pageItemCount = 0;
                int pageCount = 1;

                try
                {
                    pageItemCount = service.GetCurrentPageItemCount();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("[Nugzz] GetCurrentPageItemCount failed: " + ex);
                }

                try
                {
                    pageCount = service.GetPageCount();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("[Nugzz] GetPageCount failed: " + ex);
                }

                if (!service.IsCached)
                {
                    try
                    {
                        service.InitializeCache();
                        try { pageItemCount = service.GetCurrentPageItemCount(); }
                        catch (Exception ex) { UnityEngine.Debug.LogError("[Nugzz] GetCurrentPageItemCount after init failed: " + ex); }
                        try { pageCount = service.GetPageCount(); }
                        catch (Exception ex) { UnityEngine.Debug.LogError("[Nugzz] GetPageCount after init failed: " + ex); }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError("[Nugzz] Cache init failed in Draw: " + ex);
                        try
                        {
                            TMPHybridService.Instance.Label(4f, y, w, 18f, "Failed to load items - check console",
                                GUISystemService.Instance.GetColorForCategory(LabelCategory.Error),
                                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Error),
                                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Error),
                                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Error));
                            y += 20f;
                        }
                        catch (Exception) { }
                        return;
                    }
                }

                int filteredCount = 0;
                try { filteredCount = service.GetFilteredCount(); }
                catch (Exception) { }

                TMPHybridService.Instance.Label(4f, y, w, 18f, filteredCount + " items (page " + (service.GetPageIndex() + 1) + "/" + pageCount + ")",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Catalog),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Catalog),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Catalog),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Catalog));
                y += 20f;

                if (service.GetPageIndex() != state.PageIndex)
                    state.PageIndex = service.GetPageIndex();

                float paginationW = w - 12f;
                float prevW = paginationW / 2f;
                if (GUIFit.Button(new Rect(4f, y, prevW, 18f), "Prev", _smallButton))
                {
                    try { service.PreviousPage(); }
                    catch (Exception) { }
                    state.PageIndex = service.GetPageIndex();
                    try { pageItemCount = service.GetCurrentPageItemCount(); }
                    catch (Exception) { pageItemCount = 0; }
                }
                if (GUIFit.Button(new Rect(8f + prevW, y, paginationW - prevW - 4f, 18f), "Next", _smallButton))
                {
                    try { service.NextPage(); }
                    catch (Exception) { }
                    state.PageIndex = service.GetPageIndex();
                    try { pageItemCount = service.GetCurrentPageItemCount(); }
                    catch (Exception) { pageItemCount = 0; }
                }
                y += 24f;

                int itemRows = (pageItemCount + 2) / 3;
                GUI.Box(new Rect(0f, y, w, (float)(itemRows * 24 + 8)), "", boxStyle);
                float itemY = y + 3f;
                float buttonW = (w - 12f) / 3f;
                for (int i = 0; i < pageItemCount; i += 3)
                {
                    for (int j = 0; j < 3 && i + j < pageItemCount; j++)
                    {
                        string id = null;
                        string name = null;
                        try { id = service.GetCurrentPageItemIdAt(i + j); }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError("[Nugzz] GetCurrentPageItemIdAt(" + (i + j) + ") failed: " + ex.Message);
                        }

                        if (id != null)
                        {
                            try { name = service.GetCurrentPageItemNameAt(i + j) ?? id; }
                            catch (Exception) { name = id; }

                            if (GUIFit.Button(new Rect(4f + j * (buttonW + 4f), itemY, buttonW, 22f),
                                    FitButtonText(name, _itemButton, buttonW - 8f), _itemButton))
                            {
                                try { service.SpawnItem(id, state.SpawnQuantity, state.QualityIndex); }
                                catch (Exception ex) { UnityEngine.Debug.LogError("[Nugzz] SpawnItem failed: " + ex.Message); }
                            }
                        }
                    }
                    itemY += 24f;
                }
                y += (float)(itemRows * 24 + 12);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] ItemsTab error: " + ex);
            }
        }

        private static void EnsureStyles(GUIStyle buttonStyle)
        {
            if (_styleSource == buttonStyle && _smallButton != null)
                return;

            _styleSource = buttonStyle;
            _smallButton = new GUIStyle(buttonStyle) { fontSize = 10 };

            _selectedButton = new GUIStyle(_smallButton);
            _selectedButton.normal.textColor = Color.yellow;
            _selectedButton.hover.textColor = Color.yellow;
            _selectedButton.active.textColor = Color.yellow;
            _selectedButton.fontStyle = FontStyle.Bold;

            _categoryButton = new GUIStyle(_smallButton)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            _itemButton = new GUIStyle(_smallButton)
            {
                fontSize = 10,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 1) + ".";
        }

        private static string FitButtonText(string text, GUIStyle style, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;
            try
            {
                if (style.CalcSize(new GUIContent(text)).x <= maxWidth)
                    return text;

                string compact = text.Replace(" ", "").Replace("_", "");
                if (compact.Length > 0 && style.CalcSize(new GUIContent(compact)).x <= maxWidth)
                    return compact;

                for (int len = compact.Length - 1; len > 3; len--)
                {
                    string candidate = compact.Substring(0, len) + "..";
                    if (style.CalcSize(new GUIContent(candidate)).x <= maxWidth)
                        return candidate;
                }
            }
            catch { }

            return TruncateText(text, 10);
        }
    }
}
