using System;

namespace ClinicDesctop.Models
{
    public class DoctorScheduleDisplay
    {
        public Guid Id { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DoctorSchedule Schedule { get; set; } = new DoctorSchedule();

        // Конструктор для удобства
        public DoctorScheduleDisplay() { }

        public DoctorScheduleDisplay(DoctorSchedule schedule)
        {
            Id = schedule.Id;
            DayName = schedule.DayName;
            TimeRange = $"{schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}";
            Status = schedule.IsActive ? "Активно" : "Не активно";
            Schedule = schedule;
        }
    }
}