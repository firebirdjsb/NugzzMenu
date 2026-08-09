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
        public bool ConfirmMixtureDelete { get; set; }
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
        private static GUIStyle _mixtureItemButton;
        private static GUIStyle _selectedMixtureItemButton;

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
                GUIFit.Panel(new Rect(0f, y, w, 24f), boxStyle);
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
                GUIFit.Panel(new Rect(0f, y, w, 24f), boxStyle);
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

                TMPHybridService.Instance.Label(4f, y, w, 18f, "CLOTHING COLOR",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUIFit.Panel(new Rect(0f, y, w, 24f), boxStyle);
                rowY = y + 3f;
                float colorSideWidth = Mathf.Max(58f, w * 0.2f);
                float colorLabelWidth = w - colorSideWidth * 2f - 16f;
                if (GUIFit.Button(new Rect(4f, rowY, colorSideWidth, 18f), "Previous", _smallButton))
                    service.CycleClothingColor(-1);
                GUIFit.Button(
                    new Rect(8f + colorSideWidth, rowY, colorLabelWidth, 18f),
                    service.GetClothingColorLabel(),
                    _selectedButton);
                if (GUIFit.Button(
                        new Rect(12f + colorSideWidth + colorLabelWidth, rowY, colorSideWidth, 18f),
                        "Next",
                        _smallButton))
                {
                    service.CycleClothingColor(1);
                }
                y += 34f;

                TMPHybridService.Instance.Label(4f, y, w, 18f, "CATEGORY",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;

                int categoryCount = ItemService.CategoryCount;
                const int categoriesPerRow = 4;
                int categoryRows = (categoryCount + categoriesPerRow - 1) / categoriesPerRow;
                float categoryBoxHeight = categoryRows * 30f + 12f;

                GUIFit.Panel(new Rect(0f, y, w, categoryBoxHeight), boxStyle);
                rowY = y + 4f;
                float categoryButtonWidth = (w - (categoriesPerRow + 1) * 4f) / categoriesPerRow;

                for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
                {
                    int row = categoryIndex / categoriesPerRow;
                    int column = categoryIndex % categoriesPerRow;
                    string label = ItemService.GetCategoryLabel(categoryIndex);
                    bool selectedCategory = categoryIndex == state.FilterIndex;
                    if (selectedCategory)
                        label = "> " + label + " <";

                    if (GUIFit.Button(
                            new Rect(4f + column * (categoryButtonWidth + 4f), rowY + row * 30f, categoryButtonWidth, 26f),
                            label,
                            selectedCategory ? _selectedButton : _categoryButton))
                    {
                        state.FilterIndex = categoryIndex;
                        state.ConfirmMixtureDelete = false;
                        updateFilter?.Invoke(categoryIndex);
                    }
                }
                y += categoryBoxHeight + 6f;

                if (service.IsMixtureFilterSelected)
                {
                    TMPHybridService.Instance.Label(4f, y, w, 18f, "CREATED MIXTURE TYPE",
                        GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                        GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                        GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                        GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                    y += 20f;
                    GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
                    float mixtureWidth = (w - 24f) / ItemService.MixtureTypeCount;
                    for (int i = 0; i < ItemService.MixtureTypeCount; i++)
                    {
                        bool selected = service.GetMixtureTypeFilter() == i;
                        string label = ItemService.GetMixtureTypeLabel(i);
                        if (GUIFit.Button(
                                new Rect(4f + i * (mixtureWidth + 4f), y + 4f, mixtureWidth, 24f),
                                selected ? "> " + label + " <" : label,
                                selected ? _selectedButton : _categoryButton))
                        {
                            service.SetMixtureTypeFilter(i);
                            state.PageIndex = 0;
                            state.ConfirmMixtureDelete = false;
                        }
                    }
                    y += 38f;
                }

                TMPHybridService.Instance.Label(4f, y, w, 18f, "ITEM SPAWNER",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
                y += 20f;
                GUIFit.Panel(new Rect(0f, y, w, 84f), boxStyle);

                string previousSearch = string.Empty;
                string newSearch = string.Empty;
                try
                {
                    previousSearch = service.GetSearchText() ?? string.Empty;
                    newSearch = GUIFit.TextField(new Rect(68f, y + 4f, w - 260f, 22f),
                        previousSearch, 50, "items.search");
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

                if (GUIFit.Button(new Rect(6f, y + 32f, w - 12f, 22f), "Unlock Vendor Access / All Items", _smallButton))
                {
                    UnlockService.Instance.UnlockAllProductsAndItems();
                }

                y += 88f;

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

                bool mixtureView = service.IsMixtureFilterSelected;
                int itemColumns = mixtureView ? 2 : 3;
                float itemRowHeight = mixtureView ? 28f : 24f;
                int itemRows = (pageItemCount + itemColumns - 1) / itemColumns;
                GUIFit.Panel(new Rect(0f, y, w, itemRows * itemRowHeight + 8f), boxStyle);
                float itemY = y + 3f;
                float buttonW = (w - (itemColumns + 1) * 4f) / itemColumns;
                for (int i = 0; i < pageItemCount; i += itemColumns)
                {
                    for (int j = 0; j < itemColumns && i + j < pageItemCount; j++)
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

                            GUIStyle itemStyle = mixtureView && service.IsMixtureSelected(id)
                                ? _selectedMixtureItemButton
                                : mixtureView ? _mixtureItemButton : _itemButton;
                            string buttonLabel = FitButtonText(name, _itemButton, buttonW - 8f);

                            if (GUIFit.Button(new Rect(4f + j * (buttonW + 4f), itemY,
                                    buttonW, itemRowHeight - 2f), buttonLabel, itemStyle))
                            {
                                if (mixtureView)
                                {
                                    service.SelectMixture(id);
                                    state.ConfirmMixtureDelete = false;
                                }
                                else
                                {
                                    try { service.SpawnItem(id, state.SpawnQuantity, state.QualityIndex); }
                                    catch (Exception ex) { UnityEngine.Debug.LogError("[Nugzz] SpawnItem failed: " + ex.Message); }
                                }
                            }
                        }
                    }
                    itemY += itemRowHeight;
                }
                y += itemRows * itemRowHeight + 12f;

                if (mixtureView)
                    DrawSelectedMixture(ref y, w, boxStyle, service, state);

                DrawShapeSpawner(ref y, w, buttonStyle, boxStyle,
                    ShapePrefabService.Instance);
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

            _mixtureItemButton = new GUIStyle(_itemButton)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            _selectedMixtureItemButton = new GUIStyle(_mixtureItemButton);
            _selectedMixtureItemButton.normal.textColor = Color.yellow;
            _selectedMixtureItemButton.hover.textColor = Color.yellow;
            _selectedMixtureItemButton.active.textColor = Color.yellow;
            _selectedMixtureItemButton.fontStyle = FontStyle.Bold;
        }

        private static void DrawShapeSpawner(ref float y, float w, GUIStyle buttonStyle,
            GUIStyle boxStyle, ShapePrefabService service)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "3D SHAPE PREFABS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 174f), boxStyle);

            DrawShapeSelector(y + 6f, w, "Shape", service.SelectedTypeLabel,
                service.CycleType, buttonStyle);
            DrawShapeSelector(y + 34f, w, "Size", service.SelectedScaleLabel,
                service.CycleScale, buttonStyle);
            DrawShapeSelector(y + 62f, w, "Color", service.SelectedColorLabel,
                service.CycleColor, buttonStyle);

            if (GUIFit.Button(new Rect(6f, y + 90f, w - 12f, 22f),
                    "Physics For New Shapes: " + service.PhysicsModeLabel, buttonStyle))
                service.TogglePhysicsForNewShapes();

            float half = (w - 18f) * 0.5f;
            if (GUIFit.Button(new Rect(6f, y + 118f, half, 22f),
                    "Spawn Selected Shape", buttonStyle))
                service.SpawnSelected();
            if (GUIFit.Button(new Rect(12f + half, y + 118f, half, 22f),
                    "Clear Spawned Shapes", buttonStyle))
                service.ClearAll();

            string help = service.IsCarrying
                ? "Carrying a shape: left click to place, right click to cancel."
                : "Spawned: " + service.SpawnedCount + " | Aim at a shape and use the pickup prompt.";
            TMPHybridService.Instance.Label(8f, y + 146f, w - 16f, 20f, help,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Status), 11f,
                TextAnchor.MiddleCenter, FontStyle.Italic);
            y += 182f;
        }

        private static void DrawShapeSelector(float y, float w, string name, string value,
            Action<int> cycle, GUIStyle style)
        {
            TMPHybridService.Instance.Label(8f, y + 2f, 74f, 20f, name,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label), 11f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            if (GUIFit.Button(new Rect(84f, y, 54f, 22f), "Previous", style)) cycle(-1);
            GUIFit.Button(new Rect(144f, y, w - 288f, 22f), value, style);
            if (GUIFit.Button(new Rect(w - 138f, y, 132f, 22f), "Next", style)) cycle(1);
        }

        private static void DrawSelectedMixture(ref float y, float w, GUIStyle boxStyle,
            ItemService service, ItemsState state)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "SELECTED MIXTURE",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            if (!service.HasSelectedMixture)
            {
                GUIFit.Panel(new Rect(0f, y, w, 34f), boxStyle);
                TMPHybridService.Instance.Label(8f, y + 8f, w - 16f, 18f,
                    "Select a mixture above to view its type and effects.",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Catalog), 11,
                    TextAnchor.MiddleLeft, FontStyle.Normal);
                y += 40f;
                return;
            }

            int effectCount = service.GetSelectedMixtureEffectCount();
            int visibleEffectRows = Mathf.Max(1, effectCount);
            float panelHeight = 72f + visibleEffectRows * 20f +
                (state.ConfirmMixtureDelete ? 54f : 30f);
            GUIFit.Panel(new Rect(0f, y, w, panelHeight), boxStyle);

            float contentY = y + 6f;
            TMPHybridService.Instance.Label(8f, contentY, w - 16f, 20f,
                service.GetSelectedMixtureName(),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header), 13,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            contentY += 22f;
            TMPHybridService.Instance.Label(8f, contentY, w - 16f, 18f,
                "Drug type: " + service.GetSelectedMixtureType(),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Catalog), 11,
                TextAnchor.MiddleLeft, FontStyle.Normal);
            contentY += 22f;
            TMPHybridService.Instance.Label(8f, contentY, w - 16f, 18f,
                "EFFECTS (" + effectCount + ")",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header), 11,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            contentY += 20f;

            if (effectCount == 0)
            {
                TMPHybridService.Instance.Label(14f, contentY, w - 22f, 18f, "None",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Catalog), 11,
                    TextAnchor.MiddleLeft, FontStyle.Italic);
                contentY += 20f;
            }
            else
            {
                for (int i = 0; i < effectCount; i++)
                {
                    TMPHybridService.Instance.Label(14f, contentY, w - 22f, 18f,
                        (i + 1) + ". " + service.GetSelectedMixtureEffectAt(i),
                        GUISystemService.Instance.GetColorForCategory(LabelCategory.Catalog), 11,
                        TextAnchor.MiddleLeft, FontStyle.Normal);
                    contentY += 20f;
                }
            }

            if (!state.ConfirmMixtureDelete)
            {
                float half = (w - 12f) / 2f;
                if (GUIFit.Button(new Rect(4f, contentY + 2f, half, 24f),
                        "Spawn Selected", _selectedButton))
                {
                    service.SpawnItem(service.SelectedMixtureId,
                        state.SpawnQuantity, state.QualityIndex);
                }
                if (GUIFit.Button(new Rect(8f + half, contentY + 2f, half, 24f),
                        "Delete Selected", _smallButton))
                {
                    state.ConfirmMixtureDelete = true;
                }
            }
            else
            {
                TMPHybridService.Instance.Label(8f, contentY, w - 16f, 20f,
                    "Delete this saved recipe? Existing stacks may no longer load.",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Error), 10,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                contentY += 22f;
                float half = (w - 12f) / 2f;
                if (GUIFit.Button(new Rect(4f, contentY, half, 24f),
                        "Confirm Delete", _selectedButton))
                {
                    service.DeleteSelectedMixture(out string message);
                    NotificationService.Instance.Status(message);
                    state.ConfirmMixtureDelete = false;
                }
                if (GUIFit.Button(new Rect(8f + half, contentY, half, 24f),
                        "Cancel", _smallButton))
                {
                    state.ConfirmMixtureDelete = false;
                }
            }

            y += panelHeight + 8f;
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
