using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClinicDesctop.Models
{
    public class Appointment : INotifyPropertyChanged
    {
        private DateTime _appointmentDate;
        private TimeSpan _appointmentTime;

        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid ServiceId { get; set; }

        public DateTime AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                if (_appointmentDate != value)
                {
                    _appointmentDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedDateTime));
                    OnPropertyChanged(nameof(DateTime));
                }
            }
        }

        public TimeSpan AppointmentTime
        {
            get => _appointmentTime;
            set
            {
                if (_appointmentTime != value)
                {
                    _appointmentTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedTime));
                    OnPropertyChanged(nameof(FormattedDateTime));
                    OnPropertyChanged(nameof(DateTime));
                }
            }
        }

        public string Status { get; set; } = "scheduled";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Навигационные свойства
        public Patient Patient { get; set; } = new Patient();
        public Doctor Doctor { get; set; } = new Doctor();
        public Service Service { get; set; } = new Service();

        // Вычисляемые свойства
        public DateTime DateTime => AppointmentDate.Add(AppointmentTime);
        public string FormattedTime => AppointmentTime.ToString(@"hh\:mm");
        public string PatientFullName => Patient?.FullName ?? "Не указан";
        public string ServiceName => Service?.Name ?? "Не указана";
        public string DoctorFullName => Doctor?.FullName ?? "Не указан";
        public string FormattedDateTime => $"{AppointmentDate:dd.MM.yyyy} {FormattedTime}";
        public string DisplayStatus
        {
            get
            {
                return Status switch
                {
                    "scheduled" => "Запланирован",
                    "completed" => "Завершен",
                    "cancelled" => "Отменен",
                    "no_show" => "Не явился",
                    _ => Status
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}