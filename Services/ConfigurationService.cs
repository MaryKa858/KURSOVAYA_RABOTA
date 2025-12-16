using ClinicDesctop.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ClinicDesctop.Services
{
    public class ConfigurationService
    {
        private readonly string _configFilePath;

        public ConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ClinicDesctop");

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            _configFilePath = Path.Combine(appFolder, "appsettings.json");
        }

        public AppConfig LoadConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                // Конфигурация по умолчанию
                var defaultConfig = new AppConfig
                {
                    ConnectionString = "Host=localhost;Database=clinic_db;Username=postgres;Password=123;Port=5432",
                    ApplicationSettings = new ApplicationSettings
                    {
                        AppName = "Система управления клиникой",
                        Version = "1.0.0",
                        DefaultTimezone = "Europe/Moscow"
                    },
                    SecuritySettings = new SecuritySettings
                    {
                        EncryptionKey = "your-secure-key-here-change-in-production",
                        TokenExpirationHours = 8
                    },
                    TelegramSettings = new TelegramSettings
                    {
                        BotToken = "",
                        WebhookUrl = ""
                    }
                };

                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка загрузки конфигурации: {ex.Message}", ex);
            }
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения конфигурации: {ex.Message}", ex);
            }
        }
    }
}