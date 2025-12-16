using System;

namespace ClinicDesctop.Models
{
    public class Doctor
    {
        public Guid Id { get; set; }

        // УБЕРИТЕ сеттеры с проверкой на null
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // ИСПРАВЛЕННЫЕ вычисляемые свойства
        public string FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LastName) &&
                    string.IsNullOrWhiteSpace(FirstName) &&
                    string.IsNullOrWhiteSpace(MiddleName))
                    return "Не указано";

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(LastName)) parts.Add(LastName.Trim());
                if (!string.IsNullOrWhiteSpace(FirstName)) parts.Add(FirstName.Trim());
                if (!string.IsNullOrWhiteSpace(MiddleName)) parts.Add(MiddleName.Trim());

                return string.Join(" ", parts);
            }
        }

        public string ShortName =>
            $"{LastName} {(!string.IsNullOrEmpty(FirstName) ? FirstName[0] + "." : "")}" +
            $"{(!string.IsNullOrEmpty(MiddleName) ? MiddleName[0] + "." : "")}";

        public string FullInfo => $"{FullName} - {Specialization}";
        public string DisplayStatus => IsActive ? "Активен" : "Не активен";
    }
}