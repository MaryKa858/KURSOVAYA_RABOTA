using System;
using System.Collections.Generic;

namespace ClinicDesctop.Models
{
    public class Patient
    {
        public Guid Id { get; set; }
        public long? TelegramId { get; set; }

        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _middleName = string.Empty;
        private DateTime _birthDate = new DateTime(1980, 1, 1);
        private string _phone = string.Empty;
        private string _email = string.Empty;
        private string _passport = string.Empty;

        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value ?? string.Empty;
                UpdateCalculatedProperties();
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value ?? string.Empty;
                UpdateCalculatedProperties();
            }
        }

        public string MiddleName
        {
            get => _middleName;
            set
            {
                _middleName = value ?? string.Empty;
                UpdateCalculatedProperties();
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value ?? string.Empty;
                UpdateCalculatedProperties();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value ?? string.Empty;
                UpdateCalculatedProperties();
            }
        }

        public DateTime BirthDate
        {
            get => _birthDate;
            set
            {
                _birthDate = value;
                UpdateCalculatedProperties();
            }
        }

        public string Passport
        {
            get => _passport;
            set
            {
                _passport = value ?? string.Empty;
                UpdateCalculatedProperties();
            }
        }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Вычисляемые свойства
        public string FullName { get; private set; } = string.Empty;
        public string ShortName { get; private set; } = string.Empty;
        public string DisplayEmail => string.IsNullOrWhiteSpace(Email) ? "не указан" : Email;
        public string FormattedBirthDate { get; private set; } = string.Empty;
        public string FullInfo => $"{FullName} | {Phone} | {FormattedBirthDate} ({Age} лет)";

        // Вычисляемое свойство для возраста
        public int Age
        {
            get
            {
                if (BirthDate.Year <= 1900 || BirthDate > DateTime.Today)
                    return 0;

                var today = DateTime.Today;
                var age = today.Year - BirthDate.Year;

                // Проверяем, был ли уже день рождения в этом году
                if (BirthDate.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        // Метод для обновления всех вычисляемых свойств
        public void UpdateCalculatedProperties()
        {
            try
            {
                // Обновление полного ФИО
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(LastName)) parts.Add(LastName.Trim());
                if (!string.IsNullOrWhiteSpace(FirstName)) parts.Add(FirstName.Trim());
                if (!string.IsNullOrWhiteSpace(MiddleName)) parts.Add(MiddleName.Trim());
                FullName = parts.Count > 0 ? string.Join(" ", parts) : "Не указано";

                // Обновление короткого имени
                var shortParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(LastName)) shortParts.Add(LastName.Trim());
                if (!string.IsNullOrWhiteSpace(FirstName) && FirstName.Length > 0)
                    shortParts.Add($"{FirstName.Trim()[0]}.");
                if (!string.IsNullOrWhiteSpace(MiddleName) && MiddleName.Length > 0)
                    shortParts.Add($"{MiddleName.Trim()[0]}.");
                ShortName = shortParts.Count > 0 ? string.Join(" ", shortParts) : "Не указано";

                // Обновление отформатированной даты рождения
                if (BirthDate.Year > 1900 && BirthDate <= DateTime.Today)
                {
                    FormattedBirthDate = BirthDate.ToString("dd.MM.yyyy");
                }
                else
                {
                    FormattedBirthDate = "Не указана";
                }

                // Проверка и исправление проблемных дат
                if (BirthDate == DateTime.MinValue || BirthDate.Year <= 1900)
                {
                    FormattedBirthDate = "Не указана";
                }
            }
            catch (Exception)
            {
                // В случае ошибки устанавливаем значения по умолчанию
                FullName = "Ошибка данных";
                FormattedBirthDate = "Ошибка даты";
                ShortName = "Ошибка";
            }
        }

        // Вспомогательный метод для получения строки даты в формате SQL
        public string GetBirthDateForSql()
        {
            if (BirthDate.Year <= 1900 || BirthDate == DateTime.MinValue)
                return "NULL";

            return BirthDate.ToString("yyyy-MM-dd");
        }

        // Метод для проверки валидности данных
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   Phone.Length >= 10 &&
                   (string.IsNullOrWhiteSpace(Email) ||
                    (Email.Contains("@") && Email.Contains(".")));
        }

        // Конструктор для корректной инициализации
        public Patient()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdateCalculatedProperties();
        }

        // Конструктор с параметрами
        public Patient(string firstName, string lastName, string phone, DateTime birthDate)
        {
            Id = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            Phone = phone;
            BirthDate = birthDate;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdateCalculatedProperties();
        }

        // Переопределение ToString для удобства отладки
        public override string ToString()
        {
            return $"Patient: {FullName} | Phone: {Phone} | BirthDate: {FormattedBirthDate} | Age: {Age}";
        }

        // Метод для копирования данных из другого пациента
        public void CopyFrom(Patient other)
        {
            if (other == null) return;

            FirstName = other.FirstName;
            LastName = other.LastName;
            MiddleName = other.MiddleName;
            Phone = other.Phone;
            Email = other.Email;
            BirthDate = other.BirthDate;
            Passport = other.Passport;
            TelegramId = other.TelegramId;
            UpdatedAt = DateTime.UtcNow;
        }

        // Статический метод для создания пациента с минимальными данными
        public static Patient CreateMinimal(string firstName, string lastName, string phone)
        {
            return new Patient
            {
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                BirthDate = new DateTime(1980, 1, 1), // Значение по умолчанию
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Метод для форматирования телефона
        public string GetFormattedPhone()
        {
            if (string.IsNullOrWhiteSpace(Phone))
                return "Не указан";

            // Простое форматирование российских номеров
            var digits = System.Text.RegularExpressions.Regex.Replace(Phone, @"[^\d]", "");

            if (digits.Length == 11 && (digits.StartsWith("7") || digits.StartsWith("8")))
            {
                return $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
            }
            else if (digits.Length == 10)
            {
                return $"+7 ({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8, 2)}";
            }

            return Phone;
        }

        // Метод для получения информации для отладки
        public string GetDebugInfo()
        {
            return $"ID: {Id}\n" +
                   $"ФИО: {FullName}\n" +
                   $"Имя: {FirstName}\n" +
                   $"Фамилия: {LastName}\n" +
                   $"Отчество: {MiddleName}\n" +
                   $"Телефон: {Phone}\n" +
                   $"Email: {Email}\n" +
                   $"Дата рождения (сырая): {BirthDate}\n" +
                   $"Дата рождения (формат): {FormattedBirthDate}\n" +
                   $"Возраст: {Age}\n" +
                   $"Паспорт: {Passport}\n" +
                   $"Создан: {CreatedAt:dd.MM.yyyy HH:mm}\n" +
                   $"Обновлен: {UpdatedAt:dd.MM.yyyy HH:mm}";
        }
    }
}