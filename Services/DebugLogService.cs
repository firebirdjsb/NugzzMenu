using System;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class DebugLogService
    {
        private static readonly DebugLogService _instance = new DebugLogService();
        public static DebugLogService Instance => _instance;

        public bool VerboseEnabled { get; private set; }

        private DebugLogService() { }

        public void SetVerbose(bool enabled)
        {
            VerboseEnabled = enabled;
            Debug.Log("[Nugzz] Verbose debug logging " + (enabled ? "enabled" : "disabled"));
        }

        public void Verbose(string message)
        {
            if (!VerboseEnabled)
                return;

            Debug.Log("[Nugzz:Debug] " + (message ?? ""));
        }

        public void VerboseWarning(string message)
        {
            if (!VerboseEnabled)
                return;

            Debug.LogWarning("[Nugzz:Debug] " + (message ?? ""));
        }

        public void VerboseException(string context, Exception ex)
        {
            if (!VerboseEnabled)
                return;

            Debug.LogWarning("[Nugzz:Debug] " + context + ": " + ex);
        }

    }
}
