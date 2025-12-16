using ClinicDesctop.Models;
using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
namespace ClinicDesctop.Services
{
    public class PostgreSQLService : IDatabaseService
    {
        private readonly string _connectionString;

        public string GetConnectionString() => _connectionString;

        public PostgreSQLService(string connectionString)
        {
            _connectionString = connectionString;

            // Конфигурируем обработчики типов Dapper
            DapperTypeHandlers.Configure();
        }

        /// <summary>
        /// Преобразует TimeOnly в TimeSpan
        /// </summary>
        private TimeSpan TimeOnlyToTimeSpan(object timeOnlyValue)
        {
            if (timeOnlyValue == null || timeOnlyValue == DBNull.Value)
                return TimeSpan.Zero;

            // Если уже TimeSpan
            if (timeOnlyValue is TimeSpan timeSpan)
                return timeSpan;

            // Если TimeOnly (PostgreSQL тип)
            var typeName = timeOnlyValue.GetType().Name;
            if (typeName == "TimeOnly")
            {
                try
                {
                    // Используем рефлексию для получения значений
                    var type = timeOnlyValue.GetType();
                    var hourProperty = type.GetProperty("Hour");
                    var minuteProperty = type.GetProperty("Minute");
                    var secondProperty = type.GetProperty("Second");

                    // Получаем значения с проверкой на null
                    int hour = 0, minute = 0, second = 0;

                    if (hourProperty != null)
                    {
                        var hourValue = hourProperty.GetValue(timeOnlyValue);
                        if (hourValue != null) hour = Convert.ToInt32(hourValue);
                    }

                    if (minuteProperty != null)
                    {
                        var minuteValue = minuteProperty.GetValue(timeOnlyValue);
                        if (minuteValue != null) minute = Convert.ToInt32(minuteValue);
                    }

                    if (secondProperty != null)
                    {
                        var secondValue = secondProperty.GetValue(timeOnlyValue);
                        if (secondValue != null) second = Convert.ToInt32(secondValue);
                    }

                    return new TimeSpan(hour, minute, second);
                }
                catch
                {
                    // Пробуем альтернативный способ через строку
                    return TryParseTimeString(timeOnlyValue.ToString());
                }
            }

            // Если DateTime
            if (timeOnlyValue is DateTime dateTime)
                return dateTime.TimeOfDay;

            // Если строка
            if (timeOnlyValue is string timeString)
                return TryParseTimeString(timeString);

            // Пробуем преобразовать в строку и распарсить
            return TryParseTimeString(timeOnlyValue.ToString());
        }

        public class PatientDbRecord
        {
            public Guid Id { get; set; }
            public long? TelegramId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string MiddleName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime BirthDate { get; set; } // Измените на не-nullable
            public string Passport { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        /// <summary>
        /// Безопасное преобразование любого объекта времени в TimeSpan
        /// </summary>
        private TimeSpan SafeConvertToTimeSpan(object timeValue)
        {
            return TimeOnlyToTimeSpan(timeValue);
        }

        /// <summary>
        /// Попытка парсинга строки времени
        /// </summary>
        private TimeSpan TryParseTimeString(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return TimeSpan.Zero;

            // Пробуем разные форматы
            if (TimeSpan.TryParse(timeString, out var timeSpan))
                return timeSpan;

            // Формат "HH:mm"
            if (System.Text.RegularExpressions.Regex.IsMatch(timeString, @"^\d{1,2}:\d{2}$"))
            {
                var parts = timeString.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
                {
                    return new TimeSpan(hours, minutes, 0);
                }
            }

            // Формат "HH:mm:ss"
            if (System.Text.RegularExpressions.Regex.IsMatch(timeString, @"^\d{1,2}:\d{2}:\d{2}$"))
            {
                var parts = timeString.Split(':');
                if (parts.Length == 3 &&
                    int.TryParse(parts[0], out var hours) &&
                    int.TryParse(parts[1], out var minutes) &&
                    int.TryParse(parts[2], out var seconds))
                {
                    return new TimeSpan(hours, minutes, seconds);
                }
            }

            return TimeSpan.Zero;
        }

        // ==================== ОСНОВНЫЕ МЕТОДЫ ====================

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        // Пациенты
        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            object emailValue = string.IsNullOrWhiteSpace(patient.Email) ?
                DBNull.Value : (object)patient.Email.Trim();
            object telegramIdValue = patient.TelegramId.HasValue ?
                (object)patient.TelegramId.Value : DBNull.Value;

            var query = @"
                INSERT INTO patients 
                (telegram_id, first_name, last_name, middle_name, phone, email, birth_date, passport)
                VALUES (@TelegramId, @FirstName, @LastName, @MiddleName, @Phone, @Email, @BirthDate, @Passport)
                RETURNING id, created_at, updated_at";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new
            {
                TelegramId = telegramIdValue,
                patient.FirstName,
                patient.LastName,
                patient.MiddleName,
                patient.Phone,
                Email = emailValue,
                BirthDate = patient.BirthDate.Date,
                patient.Passport
            });

            if (result != null)
            {
                patient.Id = result.id;
                patient.CreatedAt = result.created_at;
                patient.UpdatedAt = result.updated_at;
            }

            return patient;
        }

        public async Task<List<Patient>> GetPatientsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);

            try
            {
                await connection.OpenAsync();
                Console.WriteLine("=== PostgreSQLService.GetPatientsAsync() START ===");

                var query = "SELECT * FROM patients ORDER BY last_name, first_name";

                Console.WriteLine("Выполняем запрос...");

                using var command = new NpgsqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                var patients = new List<Patient>();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var patient = new Patient
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("id")),
                            FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("first_name")).Trim(),
                            LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("last_name")).Trim(),
                            MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("middle_name")).Trim(),
                            Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("phone")).Trim(),
                            Email = reader.IsDBNull(reader.GetOrdinal("email")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("email")).Trim(),
                            Passport = reader.IsDBNull(reader.GetOrdinal("passport")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("passport")).Trim()
                        };

                        // Обработка даты рождения через вспомогательный метод
                        int birthDateOrdinal = reader.GetOrdinal("birth_date");
                        if (!reader.IsDBNull(birthDateOrdinal))
                        {
                            var birthDateValue = reader.GetValue(birthDateOrdinal);
                            patient.BirthDate = SafeConvertToDateTime(birthDateValue, new DateTime(1980, 1, 1));
                        }
                        else
                        {
                            patient.BirthDate = new DateTime(1980, 1, 1);
                        }

                        // Telegram ID
                        int telegramIdOrdinal = reader.GetOrdinal("telegram_id");
                        if (!reader.IsDBNull(telegramIdOrdinal))
                        {
                            patient.TelegramId = reader.GetInt64(telegramIdOrdinal);
                        }

                        // Даты
                        patient.CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
                        patient.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));

                        patient.UpdateCalculatedProperties();
                        patients.Add(patient);

                        Console.WriteLine($"Добавлен пациент: {patient.FullName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при чтении пациента: {ex.Message}");
                    }
                }

                Console.WriteLine($"=== Итого создано пациентов: {patients.Count} ===");
                return patients;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА в GetPatientsAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Patient> GetPatientByPhoneAsync(string phone)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM patients WHERE phone = @Phone";
            var patient = await connection.QueryFirstOrDefaultAsync<Patient>(query, new { Phone = phone });

            if (patient == null)
                throw new KeyNotFoundException($"Пациент с телефоном {phone} не найден");

            return patient;
        }
        public async Task<bool> DeletePatientAsync(Guid patientId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                // Начинаем транзакцию
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Проверяем, есть ли у пациента активные записи
                    var checkAppointmentsQuery = @"
                SELECT COUNT(*) 
                FROM appointments 
                WHERE patient_id = @PatientId 
                AND status IN ('scheduled', 'confirmed')";

                    var activeAppointmentsCount = await connection.ExecuteScalarAsync<int>(
                        checkAppointmentsQuery,
                        new { PatientId = patientId },
                        transaction: transaction);

                    if (activeAppointmentsCount > 0)
                    {
                        throw new InvalidOperationException(
                            $"Невозможно удалить пациента. У пациента есть {activeAppointmentsCount} активных записей на прием. " +
                            "Перед удалением отмените все активные записи.");
                    }

                    // 2. Проверяем, есть ли отзывы
                    var checkReviewsQuery = @"
                SELECT COUNT(*) 
                FROM reviews r
                INNER JOIN appointments a ON r.appointment_id = a.id
                WHERE a.patient_id = @PatientId";

                    var reviewsCount = await connection.ExecuteScalarAsync<int>(
                        checkReviewsQuery,
                        new { PatientId = patientId },
                        transaction: transaction);

                    // 3. Проверяем, есть ли уведомления
                    var checkNotificationsQuery = @"
                SELECT COUNT(*) 
                FROM notifications 
                WHERE patient_id = @PatientId";

                    var notificationsCount = await connection.ExecuteScalarAsync<int>(
                        checkNotificationsQuery,
                        new { PatientId = patientId },
                        transaction: transaction);

                    // 4. Если есть связанные данные, предлагаем варианты
                    bool hasRelatedData = reviewsCount > 0 || notificationsCount > 0;

                    // 5. Сначала удаляем зависимые записи (каскадно или вручную)
                    if (notificationsCount > 0)
                    {
                        var deleteNotificationsQuery = "DELETE FROM notifications WHERE patient_id = @PatientId";
                        await connection.ExecuteAsync(deleteNotificationsQuery,
                            new { PatientId = patientId },
                            transaction: transaction);
                        Console.WriteLine($"Удалено уведомлений: {notificationsCount}");
                    }

                    if (reviewsCount > 0)
                    {
                        // Удаляем отзывы через каскадное удаление
                        // Или используем подзапрос:
                        var deleteReviewsQuery = @"
                    DELETE FROM reviews 
                    WHERE appointment_id IN (
                        SELECT id FROM appointments WHERE patient_id = @PatientId
                    )";
                        await connection.ExecuteAsync(deleteReviewsQuery,
                            new { PatientId = patientId },
                            transaction: transaction);
                        Console.WriteLine($"Удалено отзывов: {reviewsCount}");
                    }

                    // 6. Удаляем записи на прием
                    var deleteAppointmentsQuery = "DELETE FROM appointments WHERE patient_id = @PatientId";
                    var deletedAppointments = await connection.ExecuteAsync(deleteAppointmentsQuery,
                        new { PatientId = patientId },
                        transaction: transaction);
                    Console.WriteLine($"Удалено записей на прием: {deletedAppointments}");

                    // 7. Удаляем самого пациента
                    var deletePatientQuery = "DELETE FROM patients WHERE id = @PatientId";
                    var affectedRows = await connection.ExecuteAsync(deletePatientQuery,
                        new { PatientId = patientId },
                        transaction: transaction);

                    // Подтверждаем транзакцию
                    await transaction.CommitAsync();

                    bool success = affectedRows > 0;

                    if (success)
                    {
                        Console.WriteLine($"Пациент с ID {patientId} успешно удален");
                        Console.WriteLine($"Удалено связанных записей: " +
                                         $"записей на прием: {deletedAppointments}, " +
                                         $"отзывов: {reviewsCount}, " +
                                         $"уведомлений: {notificationsCount}");
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    // Откатываем транзакцию при ошибке
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Ошибка при удалении пациента: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка транзакции: {ex.Message}");
                throw new Exception($"Ошибка удаления пациента: {ex.Message}", ex);
            }
        }

        public async Task<bool> CheckPatientHasAppointmentsAsync(Guid patientId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT EXISTS(
            SELECT 1 
            FROM appointments 
            WHERE patient_id = @PatientId 
            AND status IN ('scheduled', 'confirmed')
        )";

            var hasAppointments = await connection.ExecuteScalarAsync<bool>(query, new { PatientId = patientId });
            return hasAppointments;
        }
        public async Task<Patient> GetPatientByIdAsync(Guid patientId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM patients WHERE id = @PatientId";
            var patient = await connection.QueryFirstOrDefaultAsync<Patient>(query, new { PatientId = patientId });

            if (patient == null)
                throw new KeyNotFoundException($"Пациент с ID {patientId} не найден");

            return patient;
        }

        public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                Console.WriteLine($"=== Поиск пациентов по запросу: '{searchTerm}' ===");

                // ИСПРАВЛЕННЫЙ ЗАПРОС: ищем только по фамилии
                var query = @"
            SELECT 
                id,
                first_name,
                last_name, 
                middle_name,
                phone,
                email,
                birth_date,
                passport,
                telegram_id,
                created_at,
                updated_at
            FROM patients 
            WHERE LOWER(TRIM(last_name)) ILIKE @SearchTerm 
            ORDER BY last_name, first_name";

                // Добавляем % для поиска по части строки
                var searchPattern = $"%{searchTerm.ToLower().Trim()}%";

                Console.WriteLine($"Используем поисковый паттерн: '{searchPattern}'");

                // Используем DataReader для ручного парсинга
                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchTerm", searchPattern);

                using var reader = await command.ExecuteReaderAsync();

                var patients = new List<Patient>();
                int count = 0;

                while (await reader.ReadAsync())
                {
                    try
                    {
                        Console.WriteLine($"\nНайден пациент #{++count}:");

                        var patient = new Patient
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("id")),
                            FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("first_name")).Trim(),
                            LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("last_name")).Trim(),
                            MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("middle_name")).Trim(),
                            Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("phone")).Trim(),
                            Email = reader.IsDBNull(reader.GetOrdinal("email")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("email")).Trim(),
                            Passport = reader.IsDBNull(reader.GetOrdinal("passport")) ?
                                string.Empty : reader.GetString(reader.GetOrdinal("passport")).Trim()
                        };

                        // Обработка даты рождения - безопасное преобразование
                        int birthDateOrdinal = reader.GetOrdinal("birth_date");
                        if (!reader.IsDBNull(birthDateOrdinal))
                        {
                            try
                            {
                                // Пробуем разные способы получения даты
                                var birthDateValue = reader.GetValue(birthDateOrdinal);

                                if (birthDateValue is DateTime dateTime)
                                {
                                    patient.BirthDate = dateTime;
                                }
                                else if (birthDateValue is DateOnly dateOnly)
                                {
                                    patient.BirthDate = new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);
                                }
                                else if (birthDateValue is string dateString)
                                {
                                    if (DateTime.TryParse(dateString, out var parsedDate))
                                    {
                                        patient.BirthDate = parsedDate;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"  ВНИМАНИЕ: не удалось распарсить дату рождения: '{dateString}'");
                                        patient.BirthDate = new DateTime(1980, 1, 1); // Значение по умолчанию
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"  ВНИМАНИЕ: неизвестный тип даты: {birthDateValue?.GetType().Name}");
                                    patient.BirthDate = new DateTime(1980, 1, 1);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  ОШИБКА парсинга даты рождения: {ex.Message}");
                                patient.BirthDate = new DateTime(1980, 1, 1); // Значение по умолчанию
                            }
                        }
                        else
                        {
                            patient.BirthDate = new DateTime(1980, 1, 1);
                        }

                        // Telegram ID
                        int telegramIdOrdinal = reader.GetOrdinal("telegram_id");
                        if (!reader.IsDBNull(telegramIdOrdinal))
                        {
                            patient.TelegramId = reader.GetInt64(telegramIdOrdinal);
                        }

                        // Даты создания и обновления
                        patient.CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
                        patient.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));

                        patient.UpdateCalculatedProperties();

                        Console.WriteLine($"  Фамилия: '{patient.LastName}'");
                        Console.WriteLine($"  Имя: '{patient.FirstName}'");
                        Console.WriteLine($"  ФИО: {patient.FullName}");
                        Console.WriteLine($"  Телефон: '{patient.Phone}'");
                        Console.WriteLine($"  Дата рождения: {patient.FormattedBirthDate}");
                        Console.WriteLine($"  Возраст: {patient.Age}");

                        patients.Add(patient);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Ошибка создания пациента: {ex.Message}");
                        Console.WriteLine($"  StackTrace: {ex.StackTrace}");
                    }
                }

                Console.WriteLine($"Всего найдено пациентов: {patients.Count}");
                return patients;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска пациентов: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка поиска пациентов: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// Безопасное преобразование любого объекта в DateTime
        /// </summary>
        private DateTime SafeConvertToDateTime(object dateValue, DateTime defaultValue)
        {
            if (dateValue == null || dateValue == DBNull.Value)
                return defaultValue;

            try
            {
                if (dateValue is DateTime dateTime)
                    return dateTime;

                if (dateValue is DateOnly dateOnly)
                    return new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);

                if (dateValue is string dateString)
                {
                    if (DateTime.TryParse(dateString, out var parsedDate))
                        return parsedDate;
                }

                // Пробуем преобразовать через строку
                var stringValue = dateValue.ToString();
                if (DateTime.TryParse(stringValue, out var parsedFromString))
                    return parsedFromString;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            object emailValue = string.IsNullOrWhiteSpace(patient.Email) ?
                DBNull.Value : (object)patient.Email.Trim();
            object telegramIdValue = patient.TelegramId.HasValue ?
                (object)patient.TelegramId.Value : DBNull.Value;

            var query = @"
                UPDATE patients 
                SET telegram_id = @TelegramId,
                    first_name = @FirstName,
                    last_name = @LastName,
                    middle_name = @MiddleName,
                    phone = @Phone,
                    email = @Email,
                    birth_date = @BirthDate,
                    passport = @Passport,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @Id";

            var affected = await connection.ExecuteAsync(query, new
            {
                TelegramId = telegramIdValue,
                patient.FirstName,
                patient.LastName,
                patient.MiddleName,
                patient.Phone,
                Email = emailValue,
                BirthDate = patient.BirthDate.Date,
                patient.Passport,
                patient.Id
            });

            return affected > 0;
        }

        // Врачи
        public async Task<Doctor> CreateDoctorAsync(Doctor doctor)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = @"
                INSERT INTO doctors 
                (first_name, last_name, middle_name, specialization, license_number, phone, email, is_active)
                VALUES (@FirstName, @LastName, @MiddleName, @Specialization, @LicenseNumber, @Phone, @Email, @IsActive)
                RETURNING id, created_at";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new
            {
                doctor.FirstName,
                doctor.LastName,
                doctor.MiddleName,
                doctor.Specialization,
                doctor.LicenseNumber,
                doctor.Phone,
                doctor.Email,
                doctor.IsActive
            });

            if (result != null)
            {
                doctor.Id = result.id;
                doctor.CreatedAt = result.created_at;
            }

            return doctor;
        }
        public async Task<DoctorSchedule> CreateDoctorScheduleAsync(DoctorSchedule schedule)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                // Сначала проверяем, существует ли врач
                var doctorExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM doctors WHERE id = @DoctorId",
                    new { schedule.DoctorId });

                if (doctorExists == 0)
                {
                    throw new Exception($"Врач с ID {schedule.DoctorId} не найден");
                }

                var query = @"
            INSERT INTO doctor_schedules 
            (doctor_id, day_of_week, start_time, end_time, is_active)
            VALUES (@DoctorId, @DayOfWeek, @StartTime, @EndTime, @IsActive)
            RETURNING id";

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new
                {
                    schedule.DoctorId,
                    schedule.DayOfWeek,
                    schedule.StartTime,
                    schedule.EndTime,
                    schedule.IsActive
                });

                if (result != null)
                {
                    schedule.Id = result.id;
                }

                return schedule;
            }
            catch (NpgsqlException ex) when (ex.SqlState == "23505")
            {
                // Ошибка уникальности - расписание на этот день уже существует
                throw new Exception($"У врача уже есть расписание на выбранный день недели", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения расписания: {ex.Message}", ex);
            }
        }
        public async Task<List<Doctor>> GetDoctorsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = @"
        SELECT 
            id, 
            first_name as FirstName, 
            last_name as LastName, 
            middle_name as MiddleName, 
            specialization, 
            license_number as LicenseNumber, 
            phone, 
            email, 
            is_active as IsActive, 
            created_at as CreatedAt
        FROM doctors 
        ORDER BY last_name, first_name";

            var doctors = await connection.QueryAsync<Doctor>(query);
            return doctors.ToList();
        }

        public async Task<List<Doctor>> GetDoctorsBySpecializationAsync(string specialization)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = "SELECT * FROM doctors WHERE specialization = @Specialization AND is_active = true ORDER BY last_name, first_name";
            var doctors = await connection.QueryAsync<Doctor>(query, new { Specialization = specialization });
            return doctors.ToList();
        }

        public async Task<Doctor> GetDoctorByIdAsync(Guid doctorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM doctors WHERE id = @DoctorId";
            var doctor = await connection.QueryFirstOrDefaultAsync<Doctor>(query, new { DoctorId = doctorId });

            if (doctor == null)
                throw new KeyNotFoundException($"Врач с ID {doctorId} не найден");

            return doctor;
        }

        public async Task<bool> UpdateDoctorAsync(Doctor doctor)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = @"
                UPDATE doctors 
                SET first_name = @FirstName,
                    last_name = @LastName,
                    middle_name = @MiddleName,
                    specialization = @Specialization,
                    license_number = @LicenseNumber,
                    phone = @Phone,
                    email = @Email,
                    is_active = @IsActive
                WHERE id = @Id";

            var affected = await connection.ExecuteAsync(query, new
            {
                doctor.FirstName,
                doctor.LastName,
                doctor.MiddleName,
                doctor.Specialization,
                doctor.LicenseNumber,
                doctor.Phone,
                doctor.Email,
                doctor.IsActive,
                doctor.Id
            });

            return affected > 0;
        }

        // Услуги
        public async Task<List<Service>> GetServicesAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = "SELECT * FROM services WHERE is_active = true ORDER BY name";
            var services = await connection.QueryAsync<Service>(query);
            return services.ToList();
        }

        // Записи на прием
        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        INSERT INTO appointments 
        (patient_id, doctor_id, service_id, appointment_date, appointment_time, status)
        VALUES (@PatientId, @DoctorId, @ServiceId, @AppointmentDate, @AppointmentTime, @Status)
        RETURNING id, created_at, updated_at";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new
            {
                appointment.PatientId,
                appointment.DoctorId,
                appointment.ServiceId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                appointment.Status
            });

            if (result != null)
            {
                appointment.Id = result.id;
                appointment.CreatedAt = result.created_at;
                appointment.UpdatedAt = result.updated_at;
            }

            return appointment;
        }

        public async Task<List<Appointment>> GetPatientAppointmentsAsync(Guid patientId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT a.*, 
               p.first_name, p.last_name, p.middle_name, p.phone,
               d.first_name as doctor_first_name, d.last_name as doctor_last_name, 
               d.middle_name as doctor_middle_name, d.specialization,
               s.name as service_name, s.duration_minutes, s.price
        FROM appointments a
        INNER JOIN patients p ON a.patient_id = p.id
        INNER JOIN doctors d ON a.doctor_id = d.id
        INNER JOIN services s ON a.service_id = s.id
        WHERE a.patient_id = @PatientId
        ORDER BY a.appointment_date DESC, a.appointment_time DESC";

            var results = await connection.QueryAsync<dynamic>(query, new { PatientId = patientId });

            var appointments = new List<Appointment>();
            foreach (var result in results)
            {
                // Преобразуем DateOnly в DateTime
                var appointmentDate = DateTime.Parse(result.appointment_date.ToString());

                appointments.Add(new Appointment
                {
                    Id = result.id,
                    PatientId = result.patient_id,
                    DoctorId = result.doctor_id,
                    ServiceId = result.service_id,
                    AppointmentDate = appointmentDate,
                    AppointmentTime = SafeConvertToTimeSpan(result.appointment_time),
                    Status = result.status,
                    CreatedAt = result.created_at,
                    UpdatedAt = result.updated_at,
                    Patient = new Patient
                    {
                        FirstName = result.first_name ?? string.Empty,
                        LastName = result.last_name ?? string.Empty,
                        MiddleName = result.middle_name ?? string.Empty,
                        Phone = result.phone ?? string.Empty
                    },
                    Doctor = new Doctor
                    {
                        FirstName = result.doctor_first_name ?? string.Empty,
                        LastName = result.doctor_last_name ?? string.Empty,
                        MiddleName = result.doctor_middle_name ?? string.Empty,
                        Specialization = result.specialization ?? string.Empty
                    },
                    Service = new Service
                    {
                        Name = result.service_name ?? string.Empty,
                        DurationMinutes = result.duration_minutes,
                        Price = result.price
                    }
                });
            }

            return appointments;
        }

        public async Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT a.*, 
               p.first_name, p.last_name, p.middle_name, p.phone,
               d.first_name as doctor_first_name, d.last_name as doctor_last_name, 
               d.middle_name as doctor_middle_name, d.specialization,
               s.name as service_name
        FROM appointments a
        INNER JOIN patients p ON a.patient_id = p.id
        INNER JOIN doctors d ON a.doctor_id = d.id
        INNER JOIN services s ON a.service_id = s.id
        WHERE a.appointment_date = @Date
        ORDER BY a.appointment_time";

            var results = await connection.QueryAsync<dynamic>(query, new { Date = date.Date });

            var appointments = new List<Appointment>();
            foreach (var result in results)
            {
                try
                {
                    // Преобразуем DateOnly в DateTime
                    DateTime appointmentDate;
                    if (result.appointment_date is DateOnly dateOnly)
                    {
                        appointmentDate = new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);
                    }
                    else
                    {
                        appointmentDate = DateTime.Parse(result.appointment_date.ToString());
                    }

                    appointments.Add(new Appointment
                    {
                        Id = result.id,
                        PatientId = result.patient_id,
                        DoctorId = result.doctor_id,
                        ServiceId = result.service_id,
                        AppointmentDate = appointmentDate,
                        AppointmentTime = SafeConvertToTimeSpan(result.appointment_time),
                        Status = result.status,
                        CreatedAt = result.created_at,
                        UpdatedAt = result.updated_at,
                        Patient = new Patient
                        {
                            FirstName = result.first_name ?? string.Empty,
                            LastName = result.last_name ?? string.Empty,
                            MiddleName = result.middle_name ?? string.Empty,
                            Phone = result.phone ?? string.Empty
                        },
                        Doctor = new Doctor
                        {
                            FirstName = result.doctor_first_name ?? string.Empty,
                            LastName = result.doctor_last_name ?? string.Empty,
                            MiddleName = result.doctor_middle_name ?? string.Empty,
                            Specialization = result.specialization ?? string.Empty
                        },
                        Service = new Service
                        {
                            Name = result.service_name ?? string.Empty
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга записи: {ex.Message}");
                }
            }

            return appointments;
        }

        public async Task<List<Appointment>> GetDoctorAppointmentsAsync(Guid doctorId, DateTime date)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT a.*, 
               p.first_name, p.last_name, p.middle_name, p.phone,
               d.first_name as doctor_first_name, d.last_name as doctor_last_name, 
               d.middle_name as doctor_middle_name, d.specialization,
               s.name as service_name
        FROM appointments a
        INNER JOIN patients p ON a.patient_id = p.id
        INNER JOIN doctors d ON a.doctor_id = d.id
        INNER JOIN services s ON a.service_id = s.id
        WHERE a.doctor_id = @DoctorId AND a.appointment_date = @Date
        ORDER BY a.appointment_time";

            var results = await connection.QueryAsync<dynamic>(query, new
            {
                DoctorId = doctorId,
                Date = date.Date
            });

            var appointments = new List<Appointment>();
            foreach (var result in results)
            {
                try
                {
                    // Преобразуем DateOnly в DateTime
                    DateTime appointmentDate;
                    if (result.appointment_date is DateOnly dateOnly)
                    {
                        appointmentDate = new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);
                    }
                    else
                    {
                        appointmentDate = DateTime.Parse(result.appointment_date.ToString());
                    }

                    appointments.Add(new Appointment
                    {
                        Id = result.id,
                        PatientId = result.patient_id,
                        DoctorId = result.doctor_id,
                        ServiceId = result.service_id,
                        AppointmentDate = appointmentDate,
                        AppointmentTime = SafeConvertToTimeSpan(result.appointment_time),
                        Status = result.status,
                        CreatedAt = result.created_at,
                        UpdatedAt = result.updated_at,
                        Patient = new Patient
                        {
                            FirstName = result.first_name ?? string.Empty,
                            LastName = result.last_name ?? string.Empty,
                            MiddleName = result.middle_name ?? string.Empty,
                            Phone = result.phone ?? string.Empty
                        },
                        Doctor = new Doctor
                        {
                            FirstName = result.doctor_first_name ?? string.Empty,
                            LastName = result.doctor_last_name ?? string.Empty,
                            MiddleName = result.doctor_middle_name ?? string.Empty,
                            Specialization = result.specialization ?? string.Empty
                        },
                        Service = new Service
                        {
                            Name = result.service_name ?? string.Empty
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга записи врача: {ex.Message}");
                }
            }

            return appointments;
        }

        public async Task<bool> CancelAppointmentAsync(Guid appointmentId)
        {
            return await UpdateAppointmentStatusAsync(appointmentId, "cancelled");
        }

        public async Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE appointments SET status = @Status WHERE id = @AppointmentId";
            var affected = await connection.ExecuteAsync(query, new
            {
                Status = status,
                AppointmentId = appointmentId
            });

            return affected > 0;
        }

        // Расписание врачей
        public async Task<List<DoctorSchedule>> GetDoctorSchedulesAsync(Guid doctorId)
        {
            Console.WriteLine($"\n=== PostgreSQLService.GetDoctorSchedulesAsync({doctorId}) ===");

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = @"
            SELECT 
                id,
                doctor_id,
                day_of_week,
                start_time,
                end_time,
                is_active
            FROM doctor_schedules 
            WHERE doctor_id = @DoctorId 
            ORDER BY day_of_week, start_time";

                Console.WriteLine($"Выполняем запрос для doctor_id = {doctorId}");

                var results = await connection.QueryAsync<dynamic>(query, new { DoctorId = doctorId });

                Console.WriteLine($"Результатов получено: {results?.Count() ?? 0}");

                var schedules = new List<DoctorSchedule>();
                foreach (var row in results)
                {
                    try
                    {
                        var schedule = new DoctorSchedule
                        {
                            Id = row.id,
                            DoctorId = row.doctor_id,
                            DayOfWeek = row.day_of_week,
                            IsActive = row.is_active,
                            StartTime = SafeConvertToTimeSpan(row.start_time),
                            EndTime = SafeConvertToTimeSpan(row.end_time)
                        };

                        schedules.Add(schedule);
                        Console.WriteLine($"  Добавлено расписание: Day={schedule.DayOfWeek}, Time={schedule.StartTime:hh\\:mm}-{schedule.EndTime:hh\\:mm}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Ошибка парсинга расписания: {ex.Message}");
                    }
                }

                Console.WriteLine($"Всего создано расписаний: {schedules.Count}");
                return schedules;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки расписания: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Ошибка загрузки расписания: {ex.Message}", ex);
            }
        }

        // Доступные временные слоты
        public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(Guid doctorId, DateTime date)
        {
            Console.WriteLine($"=== GetAvailableTimeSlotsAsync START ===");
            Console.WriteLine($"Врач ID: {doctorId}");
            Console.WriteLine($"Дата: {date:dd.MM.yyyy}");

            var timeSlots = new List<TimeSpan>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Получаем стандартную длительность приема (30 минут)
                // В реальном приложении это может зависеть от выбранной услуги
                var appointmentDuration = TimeSpan.FromMinutes(30);

                // 2. Получаем расписание врача на этот день недели
                int dayOfWeek = (int)date.DayOfWeek;
                // PostgreSQL: Sunday = 0, Monday = 1, etc.
                // Наша система: Monday = 1, Sunday = 7
                if (dayOfWeek == 0) dayOfWeek = 7; // Воскресенье

                Console.WriteLine($"День недели (PostgreSQL формат): {dayOfWeek}");

                var scheduleQuery = @"
            SELECT start_time, end_time 
            FROM doctor_schedules 
            WHERE doctor_id = @DoctorId 
              AND day_of_week = @DayOfWeek 
              AND is_active = true
            ORDER BY start_time";

                var schedules = await connection.QueryAsync<(TimeSpan StartTime, TimeSpan EndTime)>(
                    scheduleQuery,
                    new { DoctorId = doctorId, DayOfWeek = dayOfWeek });

                Console.WriteLine($"Найдено расписаний на этот день: {schedules.Count()}");

                if (!schedules.Any())
                {
                    Console.WriteLine("У врача нет расписания на этот день");
                    return timeSlots;
                }

                // 3. Получаем существующие записи на эту дату
                var existingAppointmentsQuery = @"
            SELECT appointment_time 
            FROM appointments 
            WHERE doctor_id = @DoctorId 
              AND appointment_date = @Date 
              AND status IN ('scheduled', 'confirmed')
            ORDER BY appointment_time";

                var existingTimes = await connection.QueryAsync<TimeSpan>(
                    existingAppointmentsQuery,
                    new { DoctorId = doctorId, Date = date.Date });

                Console.WriteLine($"Существующих записей на этот день: {existingTimes.Count()}");

                // 4. Генерируем доступные временные слоты для каждого расписания
                foreach (var schedule in schedules)
                {
                    Console.WriteLine($"Рабочий день: {schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}");

                    var currentTime = schedule.StartTime;

                    while (currentTime + appointmentDuration <= schedule.EndTime)
                    {
                        // Проверяем, не занято ли это время
                        bool isTimeSlotTaken = existingTimes.Any(t =>
                            Math.Abs((t - currentTime).TotalMinutes) < 1); // Сравнение с точностью до минуты

                        if (!isTimeSlotTaken)
                        {
                            timeSlots.Add(currentTime);
                            Console.WriteLine($"  Доступно: {currentTime:hh\\:mm}");
                        }
                        else
                        {
                            Console.WriteLine($"  Занято: {currentTime:hh\\:mm}");
                        }

                        // Следующий слот через 30 минут
                        currentTime = currentTime.Add(appointmentDuration);
                    }
                }

                Console.WriteLine($"Всего доступных слотов: {timeSlots.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения временных слотов: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            Console.WriteLine($"=== GetAvailableTimeSlotsAsync END ===");
            return timeSlots;
        }

        public async Task<bool> IsTimeSlotAvailableAsync(Guid doctorId, DateTime date, TimeSpan time)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT COUNT(*) 
        FROM appointments 
        WHERE doctor_id = @DoctorId 
          AND appointment_date = @Date 
          AND appointment_time = @Time 
          AND status IN ('scheduled', 'confirmed')";

            var count = await connection.ExecuteScalarAsync<int>(query, new
            {
                DoctorId = doctorId,
                Date = date.Date,
                Time = time
            });

            return count == 0;
        }

        // Специализации
        public async Task<List<string>> GetSpecializationsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = "SELECT DISTINCT specialization FROM doctors WHERE is_active = true ORDER BY specialization";
            var specializations = await connection.QueryAsync<string>(query);
            return specializations.ToList();
        }

        public async Task<bool> DeleteDoctorAsync(Guid doctorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM doctors WHERE id = @DoctorId";
            var affected = await connection.ExecuteAsync(query, new { DoctorId = doctorId });
            return affected > 0;
        }

        public async Task<Service> GetServiceByIdAsync(Guid serviceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM services WHERE id = @ServiceId";
            var service = await connection.QueryFirstOrDefaultAsync<Service>(query, new { ServiceId = serviceId });

            if (service == null)
                throw new KeyNotFoundException($"Услуга с ID {serviceId} не найден");

            return service;
        }

        public async Task<bool> UpdateServiceAsync(Service service)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = @"
                UPDATE services 
                SET name = @Name,
                    description = @Description,
                    duration_minutes = @DurationMinutes,
                    price = @Price,
                    is_active = @IsActive
                WHERE id = @Id";

            var affected = await connection.ExecuteAsync(query, new
            {
                service.Name,
                service.Description,
                service.DurationMinutes,
                service.Price,
                service.IsActive,
                service.Id
            });

            return affected > 0;
        }

        public async Task<bool> DeleteServiceAsync(Guid serviceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM services WHERE id = @ServiceId";
            var affected = await connection.ExecuteAsync(query, new { ServiceId = serviceId });
            return affected > 0;
        }

        public async Task<Service> CreateServiceAsync(Service service)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("SET client_encoding TO 'UTF8';");

            var query = @"
                INSERT INTO services 
                (name, description, duration_minutes, price, is_active)
                VALUES (@Name, @Description, @DurationMinutes, @Price, @IsActive)
                RETURNING id";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new
            {
                service.Name,
                service.Description,
                service.DurationMinutes,
                service.Price,
                service.IsActive
            });

            if (result != null)
            {
                service.Id = result.id;
            }

            return service;
        }

        public async Task<bool> DeleteAppointmentAsync(Guid appointmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM appointments WHERE id = @AppointmentId";
            var affected = await connection.ExecuteAsync(query, new { AppointmentId = appointmentId });
            return affected > 0;
        }



        // В класс PostgreSQLService добавим метод:
        public async Task<List<DoctorSchedule>> GetDoctorSchedulesByDayAsync(Guid doctorId, int dayOfWeek)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var query = @"
            SELECT 
                id,
                doctor_id as DoctorId,
                day_of_week as DayOfWeek,
                start_time as StartTime,
                end_time as EndTime,
                is_active as IsActive
            FROM doctor_schedules 
            WHERE doctor_id = @DoctorId AND day_of_week = @DayOfWeek 
            ORDER BY start_time";

                var results = await connection.QueryAsync<dynamic>(query, new
                {
                    DoctorId = doctorId,
                    DayOfWeek = dayOfWeek
                });

                var schedules = new List<DoctorSchedule>();
                foreach (var row in results)
                {
                    try
                    {
                        var schedule = new DoctorSchedule
                        {
                            Id = row.id,
                            DoctorId = row.doctor_id,
                            DayOfWeek = row.day_of_week,
                            IsActive = row.is_active,
                            StartTime = SafeConvertToTimeSpan(row.start_time),
                            EndTime = SafeConvertToTimeSpan(row.end_time)
                        };

                        schedules.Add(schedule);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка парсинга расписания: {ex.Message}");
                    }
                }

                return schedules;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки расписания по дню: {ex.Message}");
                return new List<DoctorSchedule>();
            }
        }
        public async Task<bool> FixScheduleDuplicatesAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                // Удаляем дубликаты, оставляя только первую запись для каждой комбинации doctor_id + day_of_week
                var query = @"
            DELETE FROM doctor_schedules 
            WHERE id NOT IN (
                SELECT MIN(id) 
                FROM doctor_schedules 
                GROUP BY doctor_id, day_of_week
            )";

                var affected = await connection.ExecuteAsync(query);
                Console.WriteLine($"Удалено дублирующихся записей: {affected}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка исправления дубликатов: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateDoctorScheduleAsync(DoctorSchedule schedule)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE doctor_schedules 
                SET doctor_id = @DoctorId,
                    day_of_week = @DayOfWeek,
                    start_time = @StartTime,
                    end_time = @EndTime,
                    is_active = @IsActive
                WHERE id = @Id";

            var affected = await connection.ExecuteAsync(query, new
            {
                schedule.DoctorId,
                schedule.DayOfWeek,
                schedule.StartTime,
                schedule.EndTime,
                schedule.IsActive,
                schedule.Id
            });

            return affected > 0;
        }

        public async Task<bool> DeleteDoctorScheduleAsync(Guid scheduleId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM doctor_schedules WHERE id = @ScheduleId";
            var affected = await connection.ExecuteAsync(query, new { ScheduleId = scheduleId });
            return affected > 0;
        }

        public async Task<List<Appointment>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT a.*, 
               p.first_name, p.last_name, p.middle_name, p.phone,
               d.first_name as doctor_first_name, d.last_name as doctor_last_name, 
               d.middle_name as doctor_middle_name, d.specialization,
               s.name as service_name
        FROM appointments a
        INNER JOIN patients p ON a.patient_id = p.id
        INNER JOIN doctors d ON a.doctor_id = d.id
        INNER JOIN services s ON a.service_id = s.id
        WHERE a.appointment_date BETWEEN @StartDate AND @EndDate
        ORDER BY a.appointment_date, a.appointment_time";

            var results = await connection.QueryAsync<dynamic>(query, new
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date
            });

            var appointments = new List<Appointment>();
            foreach (var result in results)
            {
                try
                {
                    // Преобразуем DateOnly в DateTime
                    DateTime appointmentDate;
                    if (result.appointment_date is DateOnly dateOnly)
                    {
                        appointmentDate = new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);
                    }
                    else
                    {
                        appointmentDate = DateTime.Parse(result.appointment_date.ToString());
                    }

                    appointments.Add(new Appointment
                    {
                        Id = result.id,
                        PatientId = result.patient_id,
                        DoctorId = result.doctor_id,
                        ServiceId = result.service_id,
                        AppointmentDate = appointmentDate,
                        AppointmentTime = SafeConvertToTimeSpan(result.appointment_time),
                        Status = result.status,
                        CreatedAt = result.created_at,
                        UpdatedAt = result.updated_at,
                        Patient = new Patient
                        {
                            FirstName = result.first_name ?? string.Empty,
                            LastName = result.last_name ?? string.Empty,
                            MiddleName = result.middle_name ?? string.Empty,
                            Phone = result.phone ?? string.Empty
                        },
                        Doctor = new Doctor
                        {
                            FirstName = result.doctor_first_name ?? string.Empty,
                            LastName = result.doctor_last_name ?? string.Empty,
                            MiddleName = result.doctor_middle_name ?? string.Empty,
                            Specialization = result.specialization ?? string.Empty
                        },
                        Service = new Service
                        {
                            Name = result.service_name ?? string.Empty
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга записи по диапазону: {ex.Message}");
                }
            }

            return appointments;
        }

        public async Task<bool> UpdatePatientTelegramIdAsync(Guid patientId, long telegramId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE patients SET telegram_id = @TelegramId WHERE id = @PatientId";
            var affected = await connection.ExecuteAsync(query, new
            {
                TelegramId = telegramId,
                PatientId = patientId
            });

            return affected > 0;
        }

        public async Task<User> AuthenticateUserAsync(string username, string password)
        {
            // ВРЕМЕННО: Пропускаем проверку пароля для тестирования
            var users = await GetUsersAsync();
            var user = users.FirstOrDefault(u => u.Username == username);

            if (user != null)
            {
                // Для тестирования принимаем любой пароль
                return user;
            }

            return null;
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM users WHERE id = @UserId";
            var user = await connection.QueryFirstOrDefaultAsync<User>(query, new { UserId = userId });
            return user;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM users ORDER BY username";
            var users = await connection.QueryAsync<User>(query);
            return users.ToList();
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO users 
                (username, password_hash, role, full_name, email, is_active)
                VALUES (@Username, @PasswordHash, @Role, @FullName, @Email, @IsActive)
                RETURNING id, created_at";

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(query, new
            {
                user.Username,
                PasswordHash = HashPassword(user.Password),
                user.Role,
                user.FullName,
                user.Email,
                user.IsActive
            });

            return result != null;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE users 
                SET username = @Username,
                    role = @Role,
                    full_name = @FullName,
                    email = @Email,
                    is_active = @IsActive
                WHERE id = @Id";

            if (!string.IsNullOrEmpty(user.Password))
            {
                query = @"
                    UPDATE users 
                    SET username = @Username,
                        password_hash = @PasswordHash,
                        role = @Role,
                        full_name = @FullName,
                        email = @Email,
                        is_active = @IsActive
                    WHERE id = @Id";
            }

            var parameters = new
            {
                user.Username,
                PasswordHash = !string.IsNullOrEmpty(user.Password) ? HashPassword(user.Password) : null,
                user.Role,
                user.FullName,
                user.Email,
                user.IsActive,
                user.Id
            };

            var affected = await connection.ExecuteAsync(query, parameters);
            return affected > 0;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM users WHERE id = @UserId";
            var affected = await connection.ExecuteAsync(query, new { UserId = userId });
            return affected > 0;
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    // Модель пользователя для авторизации
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Вычисляемые свойства
        public string DisplayRole => Role switch
        {
            "admin" => "Администратор",
            "doctor" => "Врач",
            "receptionist" => "Регистратор",
            _ => Role
        };

        public string DisplayStatus => IsActive ? "Активен" : "Не активен";
    }
}
