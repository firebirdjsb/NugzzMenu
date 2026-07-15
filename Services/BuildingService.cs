using System;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Vanilla building is intentionally left alone. This service only handles
    /// grow UI safety patches that prevent broken plant widgets from log-spamming.
    /// </summary>
    public sealed class BuildingService
    {
        private static readonly BuildingService _instance = new BuildingService();
        public static BuildingService Instance => _instance;

        private float _nextGrowComponentLogTime;

        private BuildingService() { }

        public void HandleBrokenGrowComponent(Component component, Exception exception)
        {
            try
            {
                if (component is Behaviour behaviour)
                    behaviour.enabled = false;

                if (Time.unscaledTime >= _nextGrowComponentLogTime)
                {
                    _nextGrowComponentLogTime = Time.unscaledTime + 3f;
                    DebugLogService.Instance.VerboseWarning(
                        "Disabled broken grow UI component: " +
                        (component != null ? component.GetType().Name : "unknown") +
                        " - " + (exception != null ? exception.Message : "unknown"));
                }
            }
            catch { }
        }
    }
}
