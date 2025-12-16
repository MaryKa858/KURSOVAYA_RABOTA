using System;

namespace ClinicDesctop.Models
{
    // В модели DoctorSchedule должны быть такие свойства:
    public class DoctorSchedule
    {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public int DayOfWeek { get; set; } // 1-7, где 1 - понедельник
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsActive { get; set; } = true;

        // Вычисляемые свойства (ОБЯЗАТЕЛЬНО должны быть)
        public string DayName
        {
            get
            {
                return DayOfWeek switch
                {
                    1 => "Понедельник",
                    2 => "Вторник",
                    3 => "Среда",
                    4 => "Четверг",
                    5 => "Пятница",
                    6 => "Суббота",
                    7 => "Воскресенье",
                    _ => "Неизвестно"
                };
            }
        }

        public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string DisplayStatus => IsActive ? "Активно" : "Не активно";
    }
}