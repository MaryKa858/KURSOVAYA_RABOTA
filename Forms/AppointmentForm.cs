using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    // Класс для элементов ComboBox
    public class ComboBoxItem
    {
        public string Text { get; set; } = string.Empty;
        public Guid Value { get; set; } = Guid.Empty;

        public override string ToString()
        {
            return Text;
        }
    }

    // Класс для элементов выбора времени
    public class TimeSlotItem
    {
        public string Text { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public override string ToString()
        {
            return Text;
        }

        // Свойство для проверки, является ли элемент валидным временным слотом
        public bool IsValidTimeSlot =>
            StartTime != TimeSpan.Zero &&
            EndTime != TimeSpan.Zero &&
            !Text.Contains("Нет доступного") &&
            !Text.Contains("Сначала выберите") &&
            !Text.Contains("Ошибка") &&
            !Text.Contains("Выберите время");
    }

    public class AppointmentForm : Form
    {
        private readonly IDatabaseService _dbService;
        private List<Patient> _patients;
        private List<Doctor> _doctors;
        private List<Service> _services;
        private DateTime _lastTimeLoadRequest = DateTime.MinValue;

        private ComboBox cmbPatient;
        private ComboBox cmbDoctor;
        private ComboBox cmbService;
        private DateTimePicker dtpDate;
        private ComboBox cmbTime;
        private Button btnCreate;
        private Button btnCancel;
        private Button btnFindPatient;

        public AppointmentForm(IDatabaseService dbService)
        {
            _dbService = dbService;
            InitializeComponents();
            ApplyStyles();

            // Загружаем данные после инициализации компонентов
            LoadDataAsync();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Новая запись на прием";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Панель с полями ввода
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Заголовок
            var lblTitle = new Label
            {
                Text = "Создание новой записи на прием",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 90, 160),
                Location = new Point(0, 0),
                Size = new Size(400, 30)
            };

            // Поля ввода
            int yPos = 40;
            int labelWidth = 150;
            int fieldWidth = 300;

            // Пациент
            var lblPatient = new Label
            {
                Text = "Пациент*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbPatient = new ComboBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth - 40, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbPatient"
            };

            btnFindPatient = new Button
            {
                Text = "Найти",
                Location = new Point(labelWidth + fieldWidth - 30, yPos),
                Size = new Size(30, 30),
                Font = new Font("Segoe UI", 9),
                Name = "btnFindPatient",
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnFindPatient.Click += BtnFindPatient_Click;

            yPos += 40;

            // Врач
            var lblDoctor = new Label
            {
                Text = "Врач*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbDoctor = new ComboBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbDoctor"
            };
            cmbDoctor.SelectedIndexChanged += async (s, e) =>
            {
                // Загружаем время только если выбран реальный врач (не элемент по умолчанию)
                if (cmbDoctor.SelectedItem != null &&
                    cmbDoctor.SelectedItem is ComboBoxItem item &&
                    item.Value != Guid.Empty)
                {
                    await LoadAvailableTimeSlots();
                }
                else
                {
                    // Сбрасываем поле времени
                    cmbTime.Items.Clear();
                    cmbTime.Items.Add(new TimeSlotItem
                    {
                        Text = "Сначала выберите врача",
                        StartTime = TimeSpan.Zero,
                        EndTime = TimeSpan.Zero
                    });
                    cmbTime.SelectedIndex = 0;
                    cmbTime.Enabled = true;
                }
            };

            yPos += 40;

            // Услуга
            var lblService = new Label
            {
                Text = "Услуга*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbService = new ComboBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbService"
            };

            yPos += 40;

            // Дата
            var lblDate = new Label
            {
                Text = "Дата приема*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            dtpDate = new DateTimePicker
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 30),
                Font = new Font("Segoe UI", 11),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Name = "dtpDate"
            };
            dtpDate.ValueChanged += async (s, e) =>
            {
                if (cmbDoctor.SelectedItem != null &&
                    cmbDoctor.SelectedItem is ComboBoxItem item &&
                    item.Value != Guid.Empty)
                {
                    await LoadAvailableTimeSlots();
                }
            };

            yPos += 40;

            // Время
            var lblTime = new Label
            {
                Text = "Время*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbTime = new ComboBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbTime"
            };

            yPos += 50;

            // Кнопки
            var buttonPanel = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(550, 40)
            };

            btnCreate = new Button
            {
                Text = "Создать запись",
                Location = new Point(150, 0),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnCreate"
            };
            btnCreate.Click += BtnCreate_Click;

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(300, 0),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnCancel"
            };

            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.AddRange(new Control[] { btnCreate, btnCancel });

            // Добавление элементов на панель
            mainPanel.Controls.AddRange(new Control[] {
                lblTitle,
                lblPatient, cmbPatient, btnFindPatient,
                lblDoctor, cmbDoctor,
                lblService, cmbService,
                lblDate, dtpDate,
                lblTime, cmbTime,
                buttonPanel
            });

            this.Controls.Add(mainPanel);

            // Настройка обработки клавиш
            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;

            this.ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            // Стилизация кнопок
            btnCreate.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
            btnCreate.Cursor = Cursors.Hand;
            btnCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 110, 180);

            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            btnCancel.Cursor = Cursors.Hand;

            btnFindPatient.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
            btnFindPatient.Cursor = Cursors.Hand;
            btnFindPatient.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 110, 180);

            // Стилизация комбобоксов
            foreach (var control in this.Controls.OfType<ComboBox>())
            {
                control.FlatStyle = FlatStyle.Flat;
                control.BackColor = Color.White;
                control.ForeColor = Color.FromArgb(51, 51, 51);
                control.DropDownHeight = 200;
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                // Блокируем интерфейс во время загрузки
                btnCreate.Enabled = false;
                btnCancel.Enabled = false;
                cmbPatient.Enabled = false;
                cmbDoctor.Enabled = false;
                cmbService.Enabled = false;
                cmbTime.Enabled = false;

                // Загрузка данных
                Console.WriteLine("Загрузка данных для формы записи...");

                // Пациенты
                _patients = await _dbService.GetPatientsAsync();
                if (_patients == null) _patients = new List<Patient>();
                Console.WriteLine($"Загружено пациентов: {_patients.Count}");

                // Врачи
                _doctors = await _dbService.GetDoctorsAsync();
                if (_doctors == null) _doctors = new List<Doctor>();
                Console.WriteLine($"Загружено врачей: {_doctors.Count}");

                // Услуги
                _services = await _dbService.GetServicesAsync();
                if (_services == null) _services = new List<Service>();
                Console.WriteLine($"Загружено услуг: {_services.Count}");

                // Заполнение ComboBox пациентов
                cmbPatient.Items.Clear();

                // Добавляем элемент по умолчанию
                cmbPatient.Items.Add(new ComboBoxItem { Text = "Выберите пациента...", Value = Guid.Empty });

                // Добавляем пациентов
                foreach (var patient in _patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName))
                {
                    // Обновляем вычисляемые свойства
                    patient.UpdateCalculatedProperties();

                    cmbPatient.Items.Add(new ComboBoxItem
                    {
                        Text = $"{patient.FullName} | Тел: {patient.Phone}",
                        Value = patient.Id
                    });
                }
                cmbPatient.SelectedIndex = 0;

                // Заполнение ComboBox врачей
                cmbDoctor.Items.Clear();

                // Добавляем элемент по умолчанию
                cmbDoctor.Items.Add(new ComboBoxItem { Text = "Выберите врача...", Value = Guid.Empty });

                // Добавляем только активных врачей
                var activeDoctors = _doctors.Where(d => d.IsActive)
                                           .OrderBy(d => d.LastName)
                                           .ThenBy(d => d.FirstName);

                foreach (var doctor in activeDoctors)
                {
                    cmbDoctor.Items.Add(new ComboBoxItem
                    {
                        Text = $"{doctor.FullName} | {doctor.Specialization}",
                        Value = doctor.Id
                    });
                }
                cmbDoctor.SelectedIndex = 0;

                // Заполнение ComboBox услуг
                cmbService.Items.Clear();

                // Добавляем элемент по умолчанию
                cmbService.Items.Add(new ComboBoxItem { Text = "Выберите услугу...", Value = Guid.Empty });

                // Добавляем только активные услуги
                var activeServices = _services.Where(s => s.IsActive)
                                             .OrderBy(s => s.Name);

                foreach (var service in activeServices)
                {
                    cmbService.Items.Add(new ComboBoxItem
                    {
                        Text = $"{service.Name} | {service.Price:C} ({service.DurationMinutes} мин.)",
                        Value = service.Id
                    });
                }
                cmbService.SelectedIndex = 0;

                // Инициализируем поле времени
                cmbTime.Items.Clear();
                cmbTime.Items.Add(new TimeSlotItem
                {
                    Text = "Сначала выберите врача",
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                });
                cmbTime.SelectedIndex = 0;
                cmbTime.Enabled = true; // Поле активно

                Console.WriteLine("Данные для формы записи загружены успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            }
            finally
            {
                // Разблокируем интерфейс
                btnCreate.Enabled = true;
                btnCancel.Enabled = true;
                cmbPatient.Enabled = true;
                cmbDoctor.Enabled = true;
                cmbService.Enabled = true;
                cmbTime.Enabled = true; // Убедитесь, что поле времени активно
            }
        }

        private async Task LoadAvailableTimeSlots()
        {
            // Защита от слишком частых запросов
            var now = DateTime.Now;
            if ((now - _lastTimeLoadRequest).TotalMilliseconds < 500)
            {
                return;
            }

            _lastTimeLoadRequest = now;

            if (cmbDoctor.SelectedItem == null ||
                ((ComboBoxItem)cmbDoctor.SelectedItem).Value == Guid.Empty)
            {
                cmbTime.Items.Clear();
                cmbTime.Items.Add(new TimeSlotItem
                {
                    Text = "Сначала выберите врача",
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                });
                cmbTime.SelectedIndex = 0;
                cmbTime.Enabled = true;
                return;
            }

            var selectedItem = cmbDoctor.SelectedItem as ComboBoxItem;
            if (selectedItem == null || selectedItem.Value == Guid.Empty)
            {
                cmbTime.Items.Clear();
                cmbTime.Items.Add(new TimeSlotItem
                {
                    Text = "Сначала выберите врача",
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                });
                cmbTime.SelectedIndex = 0;
                cmbTime.Enabled = true;
                return;
            }

            var doctorId = selectedItem.Value;
            var selectedDate = dtpDate.Value;

            try
            {
                cmbTime.Enabled = true;
                cmbTime.Items.Clear();

                Console.WriteLine($"\n=== Загрузка доступного времени ===");
                Console.WriteLine($"Врач ID: {doctorId}");
                Console.WriteLine($"Дата: {selectedDate:dd.MM.yyyy}");

                // Получаем доступные временные слоты
                var timeSlots = await _dbService.GetAvailableTimeSlotsAsync(doctorId, selectedDate);

                if (timeSlots == null || timeSlots.Count == 0)
                {
                    Console.WriteLine("Нет доступных временных слотов из БД");

                    // Для тестирования генерируем тестовые слоты
                    timeSlots = GenerateTestTimeSlots();

                    Console.WriteLine($"Используем тестовые слоты: {timeSlots.Count}");
                }

                Console.WriteLine($"Всего временных слотов для отображения: {timeSlots.Count}");

                // Очищаем и заполняем ComboBox
                cmbTime.Items.Clear();

                // Добавляем элемент по умолчанию (опционально)
                cmbTime.Items.Add(new TimeSlotItem
                {
                    Text = "Выберите время...",
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                });

                // Группируем слоты в диапазоны по 30 минут
                var appointmentDuration = TimeSpan.FromMinutes(30);

                foreach (var timeSlot in timeSlots.OrderBy(t => t))
                {
                    var endTime = timeSlot.Add(appointmentDuration);
                    var timeRange = $"{timeSlot:hh\\:mm} - {endTime:hh\\:mm}";

                    cmbTime.Items.Add(new TimeSlotItem
                    {
                        Text = timeRange,
                        StartTime = timeSlot,
                        EndTime = endTime
                    });

                    Console.WriteLine($"  Добавлен диапазон: {timeRange}");
                }

                // Устанавливаем выбранный элемент
                if (cmbTime.Items.Count > 1)
                {
                    // Пропускаем первый элемент "Выберите время..."
                    cmbTime.SelectedIndex = 1;
                }
                else if (cmbTime.Items.Count == 1)
                {
                    cmbTime.SelectedIndex = 0;
                }
                else
                {
                    // Если нет элементов, добавляем информационное сообщение
                    cmbTime.Items.Add(new TimeSlotItem
                    {
                        Text = "Нет доступного времени",
                        StartTime = TimeSpan.Zero,
                        EndTime = TimeSpan.Zero
                    });
                    cmbTime.SelectedIndex = 0;
                }

                Console.WriteLine($"Итоговое количество элементов в списке: {cmbTime.Items.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки времени: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // В случае ошибки используем тестовые данные
                cmbTime.Items.Clear();

                // Добавляем тестовые слоты
                var testSlots = GenerateTestTimeSlots();
                var appointmentDuration = TimeSpan.FromMinutes(30);

                cmbTime.Items.Add(new TimeSlotItem
                {
                    Text = "Выберите время...",
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.Zero
                });

                foreach (var timeSlot in testSlots.OrderBy(t => t))
                {
                    var endTime = timeSlot.Add(appointmentDuration);
                    var timeRange = $"{timeSlot:hh\\:mm} - {endTime:hh\\:mm}";

                    cmbTime.Items.Add(new TimeSlotItem
                    {
                        Text = timeRange,
                        StartTime = timeSlot,
                        EndTime = endTime
                    });
                }

                if (cmbTime.Items.Count > 1)
                {
                    cmbTime.SelectedIndex = 1;
                }
                else
                {
                    cmbTime.SelectedIndex = 0;
                }

                // Показываем информационное сообщение
                MessageBox.Show($"Используются тестовые временные слоты для отладки. Ошибка: {ex.Message}",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Метод для генерации тестовых временных слотов
        private List<TimeSpan> GenerateTestTimeSlots()
        {
            var testSlots = new List<TimeSpan>();

            // Генерируем слоты с 9:00 до 18:00 с интервалом 30 минут
            for (int hour = 9; hour < 18; hour++)
            {
                testSlots.Add(new TimeSpan(hour, 0, 0));   // ЧЧ:00
                testSlots.Add(new TimeSpan(hour, 30, 0));  // ЧЧ:30
            }

            Console.WriteLine($"Сгенерировано тестовых слотов: {testSlots.Count}");
            return testSlots;
        }

        private void BtnFindPatient_Click(object sender, EventArgs e)
        {
            var searchForm = new PatientSearchForm(_dbService);
            if (searchForm.ShowDialog() == DialogResult.OK && searchForm.SelectedPatient != null)
            {
                // Найти и выбрать пациента в комбобоксе
                for (int i = 0; i < cmbPatient.Items.Count; i++)
                {
                    if (cmbPatient.Items[i] is ComboBoxItem item && item.Value == searchForm.SelectedPatient.Id)
                    {
                        cmbPatient.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private async void BtnCreate_Click(object sender, EventArgs e)
        {
            // Валидация данных
            if (cmbPatient.SelectedItem == null || cmbDoctor.SelectedItem == null ||
                cmbService.SelectedItem == null || cmbTime.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var patientItem = cmbPatient.SelectedItem as ComboBoxItem;
            var doctorItem = cmbDoctor.SelectedItem as ComboBoxItem;
            var serviceItem = cmbService.SelectedItem as ComboBoxItem;

            if (patientItem == null || patientItem.Value == Guid.Empty ||
                doctorItem == null || doctorItem.Value == Guid.Empty ||
                serviceItem == null || serviceItem.Value == Guid.Empty)
            {
                MessageBox.Show("Пожалуйста, выберите все обязательные поля",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка выбора времени
            var timeSlotItem = cmbTime.SelectedItem as TimeSlotItem;
            if (timeSlotItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите время приема",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, не выбрано ли информационное сообщение
            if (timeSlotItem.StartTime == TimeSpan.Zero && timeSlotItem.EndTime == TimeSpan.Zero)
            {
                MessageBox.Show("Пожалуйста, выберите доступное время приема",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, не выбрано ли "Выберите время..."
            if (timeSlotItem.Text == "Выберите время..." || timeSlotItem.Text.Contains("Выберите"))
            {
                MessageBox.Show("Пожалуйста, выберите время приема из списка",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, не выбрано ли сообщение об отсутствии времени
            if (timeSlotItem.Text.Contains("Нет доступного времени") ||
                timeSlotItem.Text.Contains("Сначала выберите врача") ||
                timeSlotItem.Text.Contains("Ошибка загрузки"))
            {
                MessageBox.Show("Нет доступного времени для записи. Пожалуйста, выберите другую дату или врача.",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка, что дата не в прошлом
            if (dtpDate.Value < DateTime.Today)
            {
                MessageBox.Show("Нельзя записаться на прошедшую дату",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка, что выбранная дата не сегодня и время не прошло (если сегодня)
            if (dtpDate.Value.Date == DateTime.Today.Date)
            {
                var selectedTime = timeSlotItem.StartTime;
                var currentTime = DateTime.Now.TimeOfDay;

                if (selectedTime < currentTime)
                {
                    MessageBox.Show("Нельзя записаться на прошедшее время сегодня",
                        "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Блокируем интерфейс
            btnCreate.Enabled = false;
            btnCancel.Enabled = false;

            try
            {
                // Получение выбранных значений
                var patientId = patientItem.Value;
                var doctorId = doctorItem.Value;
                var serviceId = serviceItem.Value;
                var appointmentDate = dtpDate.Value.Date;

                // Получаем время начала приема
                TimeSpan appointmentTime = timeSlotItem.StartTime;

                Console.WriteLine($"Выбран диапазон: {timeSlotItem.Text}, начало: {appointmentTime:hh\\:mm}");

                // Проверка доступности времени (повторная проверка на случай конфликтов)
                var isAvailable = await _dbService.IsTimeSlotAvailableAsync(doctorId, appointmentDate, appointmentTime);
                if (!isAvailable)
                {
                    MessageBox.Show("Выбранное время уже занято. Пожалуйста, выберите другое время.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Создание записи
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ServiceId = serviceId,
                    AppointmentDate = appointmentDate,
                    AppointmentTime = appointmentTime,
                    Status = "scheduled"
                };

                var result = await _dbService.CreateAppointmentAsync(appointment);

                // Получаем имена для отображения
                var patient = _patients.FirstOrDefault(p => p.Id == patientId);
                var doctor = _doctors.FirstOrDefault(d => d.Id == doctorId);
                var service = _services.FirstOrDefault(s => s.Id == serviceId);

                var patientName = patient?.FullName ?? "Неизвестный пациент";
                var doctorName = doctor?.FullName ?? "Неизвестный врач";
                var serviceName = service?.Name ?? "Неизвестная услуга";

                // Отображаем диапазон времени в сообщении об успехе
                var timeRange = timeSlotItem.Text;

                MessageBox.Show($"Запись успешно создана!\n" +
                    $"Пациент: {patientName}\n" +
                    $"Врач: {doctorName}\n" +
                    $"Дата: {appointmentDate:dd.MM.yyyy}\n" +
                    $"Время: {timeRange}\n" +
                    $"Услуга: {serviceName}",
                    "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания записи: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Разблокируем интерфейс
                btnCreate.Enabled = true;
                btnCancel.Enabled = true;
            }
        }
    }
}