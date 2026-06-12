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
        // Local copy to avoid IL2CPP static field access issues
        private static readonly string[] QualityLabels = new[] { "Trash", "Poor", "Std", "Prem", "Heaven" };
        private static readonly int[] SpawnQuantities = new[] { 1, 5, 10, 25, 50, 100 };
        private static GUIStyle _styleSource;
        private static GUIStyle _smallButton;
        private static GUIStyle _selectedButton;
        private static GUIStyle _categoryButton;
        private static GUIStyle _itemButton;

        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle buttonStyle, GUIStyle boxStyle, ItemService service, ItemsState state,
            Action<int> updateQuantity, Action<int> updateQuality, Action<int> updateFilter)
        {
            try
            {
                EnsureStyles(buttonStyle);

                // Spawn count selector
                TMPHybridService.Instance.Label(4f, y, w, 18f, "SPAWN COUNT",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
                float num = y + 3f;
                float num2 = (w - 28f) / 6f;

                for (int i = 0; i < 6; i++)
                {
                    string countLabel = state.SpawnQuantity == SpawnQuantities[i] ? "> " + SpawnQuantities[i] + " <" : SpawnQuantities[i].ToString();
                    if (GUIFit.Button(new Rect(4f + (float)i * (num2 + 4f), num, num2, 18f),
                            countLabel,
                            state.SpawnQuantity == SpawnQuantities[i] ? _selectedButton : _smallButton))
                    {
                        state.SpawnQuantity = SpawnQuantities[i];
                        updateQuantity?.Invoke(SpawnQuantities[i]);
                    }
                }
                y += 28f;

                // Quality selector
                TMPHybridService.Instance.Label(4f, y, w, 18f, "QUALITY LEVEL",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
                num = y + 3f;
                float num3 = (w - 24f) / 5f;

                for (int j = 0; j < 5; j++)
                {
                    string qLabel = j == state.QualityIndex ? "> " + QualityLabels[j] + " <" : QualityLabels[j];
                    if (GUIFit.Button(new Rect(4f + (float)j * (num3 + 4f), num, num3, 18f),
                            qLabel,
                            j == state.QualityIndex ? _selectedButton : buttonStyle))
                    {
                        state.QualityIndex = j;
                        try { service.SetQualityIndex(j); } catch { }
                        updateQuality?.Invoke(j);
                    }
                }
                y += 36f;

                // Category filter - compact six-category layout
                TMPHybridService.Instance.Label(4f, y, w, 18f, "CATEGORY",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;

                int catCount = ItemService.CategoryCount;
                int catsPerRow = 6;
                int catRows = (catCount + catsPerRow - 1) / catsPerRow;
                float catBoxH = (float)(catRows * 30 + 12);

                GUI.Box(new Rect(0f, y, w, catBoxH), "", boxStyle);
                num = y + 4f;
                float catW = (w - (float)(catsPerRow + 1) * 4f) / catsPerRow;

                for (int c = 0; c < catCount; c++)
                {
                    float row = c / catsPerRow;
                    float col = c % catsPerRow;
                    string label = ItemService.GetCategoryLabel(c);
                    if (GUIFit.Button(new Rect(4f + col * (catW + 4f), num + row * 30f, catW, 26f),
                            label, _categoryButton))
                    {
                        state.FilterIndex = c;
                        updateFilter?.Invoke(c);
                    }
                }
                y += catBoxH + 6f;

                // Search
                TMPHybridService.Instance.Label(4f, y, w, 18f, "ITEM SPAWNER",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUI.Box(new Rect(0f, y, w, 56f), "", boxStyle);

                // Defensive handling for text field
                string prevSearch = "";
                string newSearch = "";
                try
                {
                    prevSearch = service.GetSearchText();
                    if (prevSearch == null) prevSearch = "";
                    newSearch = GUIFit.TextField(new Rect(68f, y + 4f, w - 260f, 22f), prevSearch, 50);
                }
                catch (Exception)
                {
                    // Ignore text field errors
                }

                if (newSearch != null && newSearch != prevSearch)
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

                // Item grid with pagination - with aggressive error handling
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

                // If cache not initialized, try to initialize it
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
                        // Show error in GUI instead of crashing - with defensive label
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
                        return; // Exit early - can't draw items without cache
                    }
                }

                // Get filtered count for display (cached value may be stale)
                int filteredCount2 = 0;
                try { filteredCount2 = service.GetFilteredCount(); }
                catch (Exception) { }

                TMPHybridService.Instance.Label(4f, y, w, 18f, filteredCount2 + " items (page " + (service.GetPageIndex() + 1) + "/" + pageCount + ")",
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
                // Log the full exception details to console
                UnityEngine.Debug.LogError("[Nugzz] ItemsTab error: " + ex);
                // Don't re-throw - just exit silently to prevent GUI crash
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
