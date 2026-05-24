using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Laundry.Patterns.Singleton
{
    public sealed class AppConfigManager
    {
        private static readonly Lazy<AppConfigManager> _instance = new(() => new AppConfigManager());
        private IConfiguration? _configuration;
        private readonly Dictionary<string, object> _settings = new();

        private AppConfigManager()
        {
            // Initialize with default settings
            _settings["BusinessStartHour"] = 8;
            _settings["BusinessEndHour"] = 17;
            _settings["BusinessDays"] = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday";
            _settings["DeliveryBaseFee"] = 50.00m;
            _settings["FreeDeliveryThreshold"] = 200.00m;
            _settings["PointsPerRand"] = 10;
            _settings["DefaultSubscriptionDiscount"] = 0.05m;
        }

        public static AppConfigManager Instance => _instance.Value;

        public void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;

            // Load from appsettings.json if available
            var businessStartHour = _configuration["AppSettings:BusinessStartHour"];
            if (!string.IsNullOrEmpty(businessStartHour))
                _settings["BusinessStartHour"] = int.Parse(businessStartHour);

            var businessEndHour = _configuration["AppSettings:BusinessEndHour"];
            if (!string.IsNullOrEmpty(businessEndHour))
                _settings["BusinessEndHour"] = int.Parse(businessEndHour);

            var deliveryBaseFee = _configuration["AppSettings:DeliveryBaseFee"];
            if (!string.IsNullOrEmpty(deliveryBaseFee))
                _settings["DeliveryBaseFee"] = decimal.Parse(deliveryBaseFee);
        }

        public T GetSetting<T>(string key, T defaultValue = default!)
        {
            if (_settings.TryGetValue(key, out var value))
                return (T)value;

            return defaultValue;
        }

        public void SetSetting(string key, object value)
        {
            _settings[key] = value;
            OrderLogger.Instance.LogEvent($"Configuration updated: {key} = {value}");
        }

        public Dictionary<string, object> GetAllSettings()
        {
            return new Dictionary<string, object>(_settings);
        }

        public bool IsBusinessHour()
        {
            var now = DateTime.Now;
            var startHour = GetSetting<int>("BusinessStartHour", 8);
            var endHour = GetSetting<int>("BusinessEndHour", 17);
            var businessDays = GetSetting<string>("BusinessDays", "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday");

            var days = businessDays.Split(',');
            var currentDay = now.DayOfWeek.ToString();

            return days.Contains(currentDay) && now.Hour >= startHour && now.Hour < endHour;
        }

        public string GetBusinessHoursDescription()
        {
            var startHour = GetSetting<int>("BusinessStartHour", 8);
            var endHour = GetSetting<int>("BusinessEndHour", 17);
            var businessDays = GetSetting<string>("BusinessDays", "Monday-Saturday");

            return $"{businessDays}, {startHour}:00 - {endHour}:00";
        }
    }
}