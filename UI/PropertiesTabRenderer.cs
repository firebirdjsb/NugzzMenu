using System;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Property;
using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public class PropertiesState
    {
        public int SelectedPropertyIndex { get; set; }
        public int AvailablePageIndex { get; set; }
    }

    public static class PropertiesTabRenderer
    {
        private static readonly EEmployeeType[] WorkerTypes =
        {
            EEmployeeType.Botanist,
            EEmployeeType.Handler,
            EEmployeeType.Chemist,
            EEmployeeType.Cleaner
        };

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            PropertiesState state, PropertyWorkerService service)
        {
            DrawHeader(ref y, w, "OWNED PROPERTIES");

            int propertyCount = service.GetOwnedPropertyCount();
            if (propertyCount <= 0)
            {
                GUIFit.Panel(new Rect(0f, y, w, 38f), boxStyle);
                Label(8f, y + 8f, w - 16f, 20f, "No bought properties found in the current save.");
                y += 46f;
                return;
            }

            state.SelectedPropertyIndex = Mathf.Clamp(state.SelectedPropertyIndex, 0, propertyCount - 1);
            DrawPropertyPicker(ref y, w, buttonStyle, boxStyle, state, service, propertyCount);

            Property selectedProperty = service.GetOwnedPropertyAt(state.SelectedPropertyIndex);
            DrawPropertySummary(ref y, w, boxStyle, selectedProperty, service);
            DrawHirePanel(ref y, w, buttonStyle, boxStyle, selectedProperty, service);
            DrawAssignedWorkers(ref y, w, buttonStyle, boxStyle, selectedProperty, service);
            DrawAvailableWorkers(ref y, w, buttonStyle, boxStyle, selectedProperty, state, service);
        }

        private static void DrawPropertyPicker(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            PropertiesState state, PropertyWorkerService service, int propertyCount)
        {
            int rows = (propertyCount + 2) / 3;
            float height = rows * 28f + 8f;
            GUIFit.Panel(new Rect(0f, y, w, height), boxStyle);

            float colW = (w - 16f) / 3f;
            for (int i = 0; i < propertyCount; i++)
            {
                int row = i / 3;
                int col = i % 3;
                Property property = service.GetOwnedPropertyAt(i);
                string label = i == state.SelectedPropertyIndex
                    ? "> " + service.GetPropertyLabel(property) + " <"
                    : service.GetPropertyLabel(property);

                if (GUIFit.Button(new Rect(4f + col * (colW + 4f), y + 4f + row * 28f, colW, 22f), label, buttonStyle))
                {
                    state.SelectedPropertyIndex = i;
                    state.AvailablePageIndex = 0;
                }
            }

            y += height + 8f;
        }

        private static void DrawPropertySummary(ref float y, float w, GUIStyle boxStyle,
            Property property, PropertyWorkerService service)
        {
            DrawHeader(ref y, w, "PROPERTY INFO");
            GUIFit.Panel(new Rect(0f, y, w, 42f), boxStyle);

            int assigned = property?.Employees?.Count ?? 0;
            int capacity = property != null ? property.EmployeeCapacity : 0;
            string line = service.GetPropertyLabel(property) + "  |  Workers: " + assigned + " / " + capacity;
            Label(8f, y + 6f, w - 16f, 18f, line, LabelCategory.Header);
            Label(8f, y + 24f, w - 16f, 16f, "Code: " + service.GetPropertyCode(property));

            y += 50f;
        }

        private static void DrawHirePanel(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            Property property, PropertyWorkerService service)
        {
            DrawHeader(ref y, w, "HIRE NEW WORKER");
            GUIFit.Panel(new Rect(0f, y, w, 36f), boxStyle);

            float buttonW = (w - 20f) / WorkerTypes.Length;
            for (int i = 0; i < WorkerTypes.Length; i++)
            {
                EEmployeeType type = WorkerTypes[i];
                if (GUIFit.Button(new Rect(4f + i * (buttonW + 4f), y + 6f, buttonW, 22f),
                        "+ " + service.GetWorkerTypeLabel(type), buttonStyle))
                {
                    string error = service.HireWorker(property, type);
                    if (!string.IsNullOrEmpty(error))
                        NotificationService.Instance.Warning(error);
                }
            }

            y += 44f;
        }

        private static void DrawAssignedWorkers(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            Property property, PropertyWorkerService service)
        {
            DrawHeader(ref y, w, "WORKERS ON THIS PROPERTY");

            int count = property?.Employees?.Count ?? 0;
            float height = Mathf.Max(36f, count * 26f + 8f);
            GUIFit.Panel(new Rect(0f, y, w, height), boxStyle);

            if (count <= 0)
            {
                Label(8f, y + 8f, w - 16f, 20f, "No workers assigned here.");
                y += height + 8f;
                return;
            }

            float rowY = y + 4f;
            for (int i = 0; i < count; i++)
            {
                Employee employee = null;
                try { employee = property.Employees[i]; } catch { }
                Label(8f, rowY + 2f, w - 116f, 18f, service.GetEmployeeLabel(employee));

                if (GUIFit.Button(new Rect(w - 104f, rowY, 96f, 20f), "Remove/Fire", buttonStyle))
                {
                    string error = service.FireEmployee(employee);
                    if (!string.IsNullOrEmpty(error))
                        NotificationService.Instance.Warning(error);
                }

                rowY += 26f;
            }

            y += height + 8f;
        }

        private static void DrawAvailableWorkers(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            Property property, PropertiesState state, PropertyWorkerService service)
        {
            DrawHeader(ref y, w, "MOVE EXISTING WORKER HERE");

            int totalEmployees = service.GetEmployeeCount();
            int availableCount = CountAvailableEmployees(property, service, totalEmployees);
            if (availableCount <= 0)
            {
                GUIFit.Panel(new Rect(0f, y, w, 36f), boxStyle);
                Label(8f, y + 8f, w - 16f, 20f, "No other workers found. Use hire buttons above.");
                y += 44f;
                return;
            }

            const int perPage = 6;
            int pageCount = Mathf.Max(1, (availableCount + perPage - 1) / perPage);
            state.AvailablePageIndex = Mathf.Clamp(state.AvailablePageIndex, 0, pageCount - 1);

            float height = Mathf.Min(perPage, availableCount) * 26f + 34f;
            GUIFit.Panel(new Rect(0f, y, w, height), boxStyle);

            int start = state.AvailablePageIndex * perPage;
            int drawn = 0;
            int seen = 0;
            float rowY = y + 4f;

            for (int i = 0; i < totalEmployees && drawn < perPage; i++)
            {
                Employee employee = service.GetEmployeeAt(i);
                if (!service.IsEmployeeAvailableFor(property, employee))
                    continue;

                if (seen++ < start)
                    continue;

                string label = service.GetEmployeeLabel(employee) + "  |  " + service.GetEmployeePropertyLabel(employee);
                Label(8f, rowY + 2f, w - 116f, 18f, label);

                if (GUIFit.Button(new Rect(w - 104f, rowY, 96f, 20f), "Move Here", buttonStyle))
                {
                    string error = service.TransferEmployeeToProperty(employee, property);
                    if (!string.IsNullOrEmpty(error))
                        NotificationService.Instance.Warning(error);
                }

                rowY += 26f;
                drawn++;
            }

            float pagerY = y + height - 26f;
            Label(8f, pagerY + 3f, 120f, 18f, "Page " + (state.AvailablePageIndex + 1) + " / " + pageCount);
            if (GUIFit.Button(new Rect(w - 106f, pagerY, 48f, 20f), "Prev", buttonStyle))
                state.AvailablePageIndex = Mathf.Max(0, state.AvailablePageIndex - 1);
            if (GUIFit.Button(new Rect(w - 54f, pagerY, 48f, 20f), "Next", buttonStyle))
                state.AvailablePageIndex = Mathf.Min(pageCount - 1, state.AvailablePageIndex + 1);

            y += height + 8f;
        }

        private static int CountAvailableEmployees(Property property, PropertyWorkerService service, int totalEmployees)
        {
            int count = 0;
            for (int i = 0; i < totalEmployees; i++)
            {
                if (service.IsEmployeeAvailableFor(property, service.GetEmployeeAt(i)))
                    count++;
            }

            return count;
        }

        private static void DrawHeader(ref float y, float w, string title)
        {
            Label(4f, y, w, 18f, title, LabelCategory.Header);
            y += 20f;
        }

        private static void Label(float x, float y, float w, float h, string text,
            LabelCategory category = LabelCategory.Label)
        {
            TMPHybridService.Instance.Label(x, y, w, h, text,
                GUISystemService.Instance.GetColorForCategory(category),
                GUISystemService.Instance.GetFontSizeForCategory(category),
                GUISystemService.Instance.GetAlignmentForCategory(category),
                GUISystemService.Instance.GetStyleForCategory(category));
        }
    }
}
