using System;
using System.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Centralized notification system for popup messages and status updates.
    /// N() messages are popup notifications, S() messages are status bar updates.
    /// </summary>
    public sealed class NotificationService
    {
        private static readonly NotificationService _instance = new NotificationService();
        public static NotificationService Instance => _instance;

        private string _notificationMessage = "";
        private string _statusMessage = "";
        private float _notificationTimer = 0f;
        private float _statusTimer = 0f;
        private const float DISPLAY_DURATION = 5f;

        private NotificationService() { }

        public string NotificationMessage => _notificationMessage;
        public string StatusMessage => _statusMessage;
        public float NotificationTimer => _notificationTimer;
        public float StatusTimer => _statusTimer;
        public bool HasNotification => _notificationTimer > 0f;
        public bool HasStatus => _statusTimer > 0f;

        public void Notify(string message)
        {
            _notificationMessage = message ?? "";
            _notificationTimer = DISPLAY_DURATION;
        }

        public void Status(string message)
        {
            _statusMessage = message ?? "";
            _statusTimer = DISPLAY_DURATION;
        }

        public void Update()
        {
            if (_notificationTimer > 0f)
                _notificationTimer -= Time.deltaTime;
            if (_statusTimer > 0f)
                _statusTimer -= Time.deltaTime;
        }

        public void Error(string message)
        {
            Status($"ERR: {message}");
        }

        public void Success(string message)
        {
            Notify(message);
        }

        public void Warning(string message)
        {
            Status($"WARN: {message}");
        }

        public void Clear()
        {
            _notificationMessage = "";
            _statusMessage = "";
            _notificationTimer = 0f;
            _statusTimer = 0f;
        }
    }
}