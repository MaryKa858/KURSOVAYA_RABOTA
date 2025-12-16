using ClinicDesctop.Models;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicDesctop.Services
{
    public interface IDatabaseService
    {
        // Проверка подключения
        Task<bool> TestConnectionAsync();
        Task<bool> DeletePatientAsync(Guid patientId);
        Task<bool> CheckPatientHasAppointmentsAsync(Guid patientId);
        string GetConnectionString();

        // Операции с пациентами
        Task<Patient> CreatePatientAsync(Patient patient);
        Task<List<Patient>> GetPatientsAsync();
        Task<Patient> GetPatientByPhoneAsync(string phone);
        Task<Patient> GetPatientByIdAsync(Guid patientId);
        Task<List<Patient>> SearchPatientsAsync(string searchTerm);
        // В интерфейс IDatabaseService добавьте:
        Task<bool> UpdatePatientAsync(Patient patient);
        Task<bool> UpdatePatientTelegramIdAsync(Guid patientId, long telegramId);

        // Операции с врачами
        Task<Doctor> CreateDoctorAsync(Doctor doctor);
        Task<List<Doctor>> GetDoctorsAsync();
        Task<List<Doctor>> GetDoctorsBySpecializationAsync(string specialization);
        Task<Doctor> GetDoctorByIdAsync(Guid doctorId);
        Task<bool> UpdateDoctorAsync(Doctor doctor);
        Task<bool> DeleteDoctorAsync(Guid doctorId);

        // Операции с услугами
        Task<Service> CreateServiceAsync(Service service);
        Task<List<Service>> GetServicesAsync();
        Task<Service> GetServiceByIdAsync(Guid serviceId);
        Task<bool> UpdateServiceAsync(Service service);
        Task<bool> DeleteServiceAsync(Guid serviceId);

        // Операции с записями
        Task<Appointment> CreateAppointmentAsync(Appointment appointment);
        Task<List<Appointment>> GetPatientAppointmentsAsync(Guid patientId);
        Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date);
        Task<List<Appointment>> GetDoctorAppointmentsAsync(Guid doctorId, DateTime date);
        Task<List<Appointment>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, string status);
        Task<bool> CancelAppointmentAsync(Guid appointmentId);
        Task<bool> DeleteAppointmentAsync(Guid appointmentId);

        // Операции с расписанием
        Task<DoctorSchedule> CreateDoctorScheduleAsync(DoctorSchedule schedule);
        Task<List<DoctorSchedule>> GetDoctorSchedulesAsync(Guid doctorId);
        Task<List<DoctorSchedule>> GetDoctorSchedulesByDayAsync(Guid doctorId, int dayOfWeek);
        Task<bool> UpdateDoctorScheduleAsync(DoctorSchedule schedule);
        Task<bool> DeleteDoctorScheduleAsync(Guid scheduleId);

        // Вспомогательные операции
        Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(Guid doctorId, DateTime date);
        Task<bool> IsTimeSlotAvailableAsync(Guid doctorId, DateTime date, TimeSpan time);
        Task<List<string>> GetSpecializationsAsync();

        // Пользователи и авторизация
        Task<User> AuthenticateUserAsync(string username, string password);
        Task<User> GetUserByIdAsync(Guid userId);
        Task<List<User>> GetUsersAsync();
        Task<bool> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid userId);
    }
}