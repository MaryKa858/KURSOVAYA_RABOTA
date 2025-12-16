namespace ClinicDesctop.Models
{
    public class AppConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public ApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();
        public SecuritySettings SecuritySettings { get; set; } = new SecuritySettings();
        public TelegramSettings TelegramSettings { get; set; } = new TelegramSettings();
    }

    public class ApplicationSettings
    {
        public string AppName { get; set; } = "Система управления клиникой";
        public string Version { get; set; } = "1.0.0";
        public string DefaultTimezone { get; set; } = "Europe/Moscow";
    }

    public class SecuritySettings
    {
        public string EncryptionKey { get; set; } = string.Empty;
        public int TokenExpirationHours { get; set; } = 8;
    }

    public class TelegramSettings
    {
        public string BotToken { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
    }
}