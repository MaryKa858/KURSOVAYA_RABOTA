using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ClinicDesctop.Utilitis
{
    public static class Extensions
    {
        /// <summary>
        /// Безопасно выполняет действие в UI потоке
        /// </summary>
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Проверяет, является ли строка допустимым номером телефона
        /// </summary>
        public static bool IsValidPhoneNumber(this string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Удаляем все нецифровые символы, кроме +
            var cleaned = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d+]", "");
            return cleaned.Length >= 10;
        }

        /// <summary>
        /// Форматирует номер телефона в российском формате
        /// </summary>
        public static string FormatPhoneNumber(this string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");

            if (cleaned.Length == 11 && cleaned.StartsWith("7") || cleaned.StartsWith("8"))
            {
                return $"+7 ({cleaned.Substring(1, 3)}) {cleaned.Substring(4, 3)}-{cleaned.Substring(7, 2)}-{cleaned.Substring(9, 2)}";
            }
            else if (cleaned.Length == 10)
            {
                return $"+7 ({cleaned.Substring(0, 3)}) {cleaned.Substring(3, 3)}-{cleaned.Substring(6, 2)}-{cleaned.Substring(8, 2)}";
            }

            return phone;
        }

        /// <summary>
        /// Проверяет, является ли строка допустимым email адресом
        /// </summary>
        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Преобразует TimeSpan в читаемое время
        /// </summary>
        public static string ToReadableTime(this TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"hh\:mm");
        }

        /// <summary>
        /// Получает день недели на русском языке
        /// </summary>
        public static string GetRussianDayOfWeek(this DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => "Неизвестно"
            };
        }

        /// <summary>
        /// Безопасно получает значение из словаря или возвращает значение по умолчанию
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Преобразует строку в TimeSpan
        /// </summary>
        public static TimeSpan? ToTimeSpan(this string timeString)
        {
            if (TimeSpan.TryParse(timeString, out TimeSpan result))
                return result;

            return null;
        }

        /// <summary>
        /// Проверяет, является ли строка допустимой датой
        /// </summary>
        public static bool IsValidDate(this string dateString)
        {
            return DateTime.TryParse(dateString, out _);
        }

        /// <summary>
        /// Ограничивает длину строки
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Создает хэш пароля
        /// </summary>
        public static string HashPassword(this string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}