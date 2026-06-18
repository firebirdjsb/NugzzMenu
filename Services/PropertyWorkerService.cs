using System;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Property;

namespace NugzzMenu.Services
{
    public sealed class PropertyWorkerService
    {
        private static readonly PropertyWorkerService _instance = new PropertyWorkerService();
        public static PropertyWorkerService Instance => _instance;

        private PropertyWorkerService() { }

        public int GetOwnedPropertyCount()
        {
            try
            {
                return Property.OwnedProperties?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public Property GetOwnedPropertyAt(int index)
        {
            try
            {
                var properties = Property.OwnedProperties;
                if (properties == null || index < 0 || index >= properties.Count)
                    return null;

                return properties[index];
            }
            catch
            {
                return null;
            }
        }

        public EmployeeManager GetEmployeeManager()
        {
            return ManagerCacheService.Instance.EmployeeManager;
        }

        public int GetEmployeeCount()
        {
            try
            {
                return GetEmployeeManager()?.AllEmployees?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public Employee GetEmployeeAt(int index)
        {
            try
            {
                var employees = GetEmployeeManager()?.AllEmployees;
                if (employees == null || index < 0 || index >= employees.Count)
                    return null;

                return employees[index];
            }
            catch
            {
                return null;
            }
        }

        public string GetPropertyLabel(Property property)
        {
            if (property == null)
                return "No property";

            string name = Safe(() => property.PropertyName, null);
            if (string.IsNullOrEmpty(name))
                name = Safe(() => property.propertyName, null);
            if (string.IsNullOrEmpty(name))
                name = Safe(() => property.PropertyCode, null);

            return string.IsNullOrEmpty(name) ? "Unnamed Property" : name;
        }

        public string GetPropertyCode(Property property)
        {
            if (property == null)
                return string.Empty;

            string code = Safe(() => property.PropertyCode, null);
            if (string.IsNullOrEmpty(code))
                code = Safe(() => property.propertyCode, null);

            return code ?? string.Empty;
        }

        public string GetEmployeeLabel(Employee employee)
        {
            if (employee == null)
                return "No worker";

            string first = Safe(() => employee.FirstName, string.Empty) ?? string.Empty;
            string last = Safe(() => employee.LastName, string.Empty) ?? string.Empty;
            string fullName = (first + " " + last).Trim();
            if (string.IsNullOrEmpty(fullName))
                fullName = Safe(() => employee.name, string.Empty);

            string type = Safe(() => employee.EmployeeType.ToString(), "Worker");
            return string.IsNullOrEmpty(fullName) ? type : fullName + " - " + type;
        }

        public string GetEmployeePropertyLabel(Employee employee)
        {
            Property property = Safe(() => employee.AssignedProperty, null);
            return property == null ? "Unassigned" : GetPropertyLabel(property);
        }

        public bool IsEmployeeAssignedTo(Employee employee, Property property)
        {
            if (employee == null || property == null)
                return false;

            Property assigned = Safe(() => employee.AssignedProperty, null);
            if (assigned == null)
                return false;

            string assignedCode = GetPropertyCode(assigned);
            string propertyCode = GetPropertyCode(property);
            return !string.IsNullOrEmpty(assignedCode) &&
                string.Equals(assignedCode, propertyCode, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEmployeeAvailableFor(Property property, Employee employee)
        {
            if (property == null || employee == null)
                return false;
            if (DoesNotSupportWorkers(property))
                return false;

            if (Safe(() => employee.Fired, false))
                return false;

            return !IsEmployeeAssignedTo(employee, property);
        }

        public string TransferEmployeeToProperty(Employee employee, Property property)
        {
            if (employee == null || property == null)
                return "No worker or property selected";
            if (DoesNotSupportWorkers(property))
                return "Workers cannot be moved to " + GetPropertyLabel(property);

            string code = GetPropertyCode(property);
            if (string.IsNullOrEmpty(code))
                return "Selected property has no code";

            try
            {
                employee.SendTransfer(code);
                NotificationService.Instance.Notify("Moved " + GetEmployeeLabel(employee) + " to " + GetPropertyLabel(property));
                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to transfer worker: " + ex);
                return "Failed to move worker";
            }
        }

        public string FireEmployee(Employee employee)
        {
            if (employee == null)
                return "No worker selected";

            try
            {
                employee.SendFire();
                NotificationService.Instance.Notify("Removed " + GetEmployeeLabel(employee));
                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to fire worker: " + ex);
                return "Failed to remove worker";
            }
        }

        public string HireWorker(Property property, EEmployeeType type)
        {
            if (property == null)
                return "No property selected";
            if (DoesNotSupportWorkers(property))
                return "Workers cannot be hired for " + GetPropertyLabel(property);

            var manager = GetEmployeeManager();
            if (manager == null)
                return "EmployeeManager not found";

            try
            {
                manager.CreateNewEmployee(property, type);
                NotificationService.Instance.Notify("Hired " + GetWorkerTypeLabel(type) + " for " + GetPropertyLabel(property));
                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to hire worker: " + ex);
                return "Failed to hire worker";
            }
        }

        public string GetWorkerTypeLabel(EEmployeeType type)
        {
            switch (type)
            {
                case EEmployeeType.Botanist:
                    return "Botanist";
                case EEmployeeType.Handler:
                    return "Handler";
                case EEmployeeType.Chemist:
                    return "Chemist";
                case EEmployeeType.Cleaner:
                    return "Cleaner";
                default:
                    return type.ToString();
            }
        }

        public bool IsRVProperty(Property property)
        {
            if (property == null)
                return false;

            try
            {
                if (property.TryCast<Il2CppScheduleOne.Property.RV>() != null)
                    return true;
            }
            catch { }

            string code = GetPropertyCode(property);
            if (LooksLikeRV(code))
                return true;

            try
            {
                string typeName = property.GetIl2CppType()?.Name;
                if (string.Equals(typeName, "RV", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            return LooksLikeRV(GetPropertyLabel(property));
        }

        public bool DoesNotSupportWorkers(Property property)
        {
            if (property == null)
                return false;

            try
            {
                if (property.EmployeeCapacity <= 0)
                    return true;
            }
            catch { }

            return IsRVProperty(property);
        }

        public string GetWorkerUnsupportedMessage(Property property)
        {
            return "Workers are not supported at " + GetPropertyLabel(property) + ".";
        }

        private static bool LooksLikeRV(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string lower = value.Trim().ToLowerInvariant();
            return lower == "rv" ||
                lower == "the rv" ||
                lower.Contains("story rv");
        }

        private static T Safe<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter();
            }
            catch
            {
                return fallback;
            }
        }
    }
}
