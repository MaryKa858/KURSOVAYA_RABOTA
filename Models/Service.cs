using System;

namespace ClinicDesctop.Models
{
    public class Service
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;

        // Вычисляемые свойства
        public string DisplayPrice => $"{Price:C}";
        public string DurationFormatted => $"{DurationMinutes} мин.";
        public string FullInfo => $"{Name} - {DisplayPrice} ({DurationFormatted})";
        public string DisplayStatus => IsActive ? "Активна" : "Не активна";
    }
}