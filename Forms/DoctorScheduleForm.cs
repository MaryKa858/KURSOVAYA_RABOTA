using ClinicDesctop.Models;
using ClinicDesctop.Services;
using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{

    public partial class DoctorScheduleForm : Form
    {
        private readonly IDatabaseService _dbService;
        private readonly Guid _doctorId;
        private readonly string _doctorName;
        private List<DoctorSchedule> _schedules;
        private List<Doctor> _doctors;

        private ComboBox cmbDoctor;
        private DataGridView dgvSchedules;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnClose;

        public DoctorScheduleForm(IDatabaseService dbService, Guid doctorId, string doctorName)
        {
            _dbService = dbService;
            _doctorId = doctorId;
            _doctorName = doctorName;
            InitializeComponents();
            ApplyStyles();
            LoadDoctorsAsync();

            if (doctorId != Guid.Empty)
            {
                LoadSchedulesAsync(doctorId);
            }
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Расписание врачей";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);

            // Основной layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Заголовок
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Выбор врача
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Кнопки

            // ========== Заголовок ==========
            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(44, 90, 160)
            };

            var titleLabel = new Label
            {
                Text = "Расписание врачей",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 30
            };

            var subTitleLabel = new Label
            {
                Text = "Управление рабочим расписанием врачей",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 220, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 30, 0, 0),
                Height = 20
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subTitleLabel);

            // ========== Панель выбора врача ==========
            var doctorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(20, 10, 20, 10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblDoctor = new Label
            {
                Text = "Врач:",
                Location = new Point(20, 20),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbDoctor = new ComboBox
            {
                Location = new Point(90, 20),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbDoctor"
            };
            cmbDoctor.SelectedIndexChanged += CmbDoctor_SelectedIndexChanged;

            var btnRefresh = new Button
            {
                Text = "Обновить",
                Location = new Point(450, 20),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.Click += async (s, e) =>
            {
                if (cmbDoctor.SelectedItem is DoctorComboBoxItem selectedItem && selectedItem.Value != Guid.Empty)
                {
                    await LoadSchedulesAsync(selectedItem.Value);
                }
            };

            doctorPanel.Controls.AddRange(new Control[] { lblDoctor, cmbDoctor, btnRefresh });

            // ========== DataGridView для расписания ==========
            dgvSchedules = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvSchedules",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10),
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            dgvSchedules.SelectionChanged += DgvSchedules_SelectionChanged;

            // ========== Панель кнопок (БЕЗ кнопки Тест) ==========
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(20, 15, 20, 15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnAdd = new Button
            {
                Text = "Добавить",
                Location = new Point(20, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnAdd",
                Enabled = false
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "Редактировать",
                Location = new Point(150, 15),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnEdit",
                Enabled = false
            };
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "Удалить",
                Location = new Point(300, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnDelete",
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(430, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnClose"
            };
            btnClose.Click += (s, e) => this.Close();

            // УБИРАЕМ КНОПКУ "ТЕСТ" - больше не добавляем ее в панель
            buttonPanel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnClose });

            // ========== Добавляем элементы в mainLayout ==========
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(doctorPanel, 0, 1);
            mainLayout.Controls.Add(dgvSchedules, 0, 2);
            mainLayout.Controls.Add(buttonPanel, 0, 3);

            // ========== Добавляем mainLayout на форму ==========
            this.Controls.Add(mainLayout);

            this.ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            // Стилизация DataGridView
            dgvSchedules.BorderStyle = BorderStyle.FixedSingle;
            dgvSchedules.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvSchedules.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 90, 160);
            dgvSchedules.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSchedules.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvSchedules.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvSchedules.EnableHeadersVisualStyles = false;
            dgvSchedules.RowHeadersVisible = false;
            dgvSchedules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSchedules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSchedules.AllowUserToAddRows = false;
            dgvSchedules.AllowUserToDeleteRows = false;
            dgvSchedules.ReadOnly = true;

            // Настройка цвета выделения
            dgvSchedules.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 248, 255);
            dgvSchedules.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 90, 160);
            dgvSchedules.RowHeadersDefaultCellStyle.SelectionBackColor = Color.Transparent;

            // Четные и нечетные строки
            dgvSchedules.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);

            // Стилизация кнопок (ТОЛЬКО существующие кнопки)
            var buttons = new[] { btnAdd, btnEdit, btnDelete, btnClose };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                    button.Cursor = Cursors.Hand;
                }
            }

            // Стилизация ComboBox
            cmbDoctor.FlatStyle = FlatStyle.Flat;
            cmbDoctor.BackColor = Color.White;
        }

        private async Task LoadDoctorsAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                cmbDoctor.Enabled = false;

                _doctors = await _dbService.GetDoctorsAsync();
                if (_doctors == null) _doctors = new List<Doctor>();

                cmbDoctor.Items.Clear();

                // Добавляем элемент по умолчанию
                cmbDoctor.Items.Add(new DoctorComboBoxItem
                {
                    Text = "Выберите врача...",
                    Value = Guid.Empty
                });

                // Добавляем только активных врачей
                foreach (var doctor in _doctors.Where(d => d.IsActive).OrderBy(d => d.LastName))
                {
                    cmbDoctor.Items.Add(new DoctorComboBoxItem
                    {
                        Text = $"{doctor.FullName} - {doctor.Specialization}",
                        Value = doctor.Id
                    });
                }

                cmbDoctor.SelectedIndex = 0;

                // Если был передан врач при открытии формы, выбираем его
                if (_doctorId != Guid.Empty)
                {
                    for (int i = 0; i < cmbDoctor.Items.Count; i++)
                    {
                        if (cmbDoctor.Items[i] is DoctorComboBoxItem item && item.Value == _doctorId)
                        {
                            cmbDoctor.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка врачей: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                cmbDoctor.Enabled = true;
            }
        }

        // В методе LoadSchedulesAsync добавим более подробную диагностику и правильное обновление
        private async Task LoadSchedulesAsync(Guid doctorId)
        {
            try
            {
                Console.WriteLine($"\n=== ЗАГРУЗКА РАСПИСАНИЯ ДЛЯ ВРАЧА {doctorId} ===");

                Cursor = Cursors.WaitCursor;
                dgvSchedules.Enabled = false;
                btnAdd.Enabled = false;
                btnEdit.Enabled = false;
                btnDelete.Enabled = false;

                // Получаем имя врача для отладки
                var doctor = _doctors?.FirstOrDefault(d => d.Id == doctorId);
                if (doctor != null)
                {
                    Console.WriteLine($"Врач: {doctor.FullName}");
                }

                // Загружаем расписание
                Console.WriteLine("Вызываем GetDoctorSchedulesAsync...");
                _schedules = await _dbService.GetDoctorSchedulesAsync(doctorId);

                Console.WriteLine($"=== РЕЗУЛЬТАТЫ ЗАГРУЗКИ ===");
                Console.WriteLine($"Всего записей получено: {_schedules?.Count ?? 0}");

                if (_schedules != null && _schedules.Count > 0)
                {
                    Console.WriteLine("Детали расписания:");
                    foreach (var schedule in _schedules)
                    {
                        Console.WriteLine($"  ID: {schedule.Id}");
                        Console.WriteLine($"  День: {schedule.DayOfWeek} ({schedule.DayName})");
                        Console.WriteLine($"  Время: {schedule.StartTime:hh\\:mm}-{schedule.EndTime:hh\\:mm}");
                        Console.WriteLine($"  Активно: {schedule.IsActive}");
                        Console.WriteLine($"  ---");
                    }

                    // УБРАЛИ MessageBox с диагностикой - только консольный вывод
                    // Выводим краткое уведомление в статус или заголовок формы
                    this.Text = $"Расписание врачей - {doctor?.FullName ?? "неизвестного"} (найдено: {_schedules.Count})";
                }
                else
                {
                    Console.WriteLine("Расписаний не найдено");
                    // Меняем заголовок формы вместо MessageBox
                    this.Text = $"Расписание врачей - {doctor?.FullName ?? "неизвестного"} (расписание не установлено)";
                }

                Console.WriteLine("Обновляем DataGridView...");

                // Проверяем контекст UI
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateDataGridView()));
                }
                else
                {
                    UpdateDataGridView();
                }

                Console.WriteLine($"=== ЗАВЕРШЕНИЕ ЗАГРУЗКИ ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ОШИБКА ===");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // Показываем ошибку только если это не первая загрузка
                if (_schedules == null || _schedules.Count == 0)
                {
                    MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Даже при ошибке показываем пустую таблицу
                _schedules = new List<DoctorSchedule>();

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateDataGridView()));
                }
                else
                {
                    UpdateDataGridView();
                }

                // Обновляем заголовок
                this.Text = $"Расписание врачей - ошибка загрузки";
            }
            finally
            {
                Cursor = Cursors.Default;
                dgvSchedules.Enabled = true;
                UpdateButtonStates();
            }
        }
        private void UpdateDataGridView()
        {
            Console.WriteLine($"\n=== ОБНОВЛЕНИЕ DATAGRIDVIEW ===");
            Console.WriteLine($"Количество расписаний: {_schedules?.Count ?? 0}");

            try
            {
                // Полностью очищаем DataGridView
                dgvSchedules.DataSource = null;
                dgvSchedules.Columns.Clear();
                dgvSchedules.Rows.Clear();

                if (_schedules == null || _schedules.Count == 0)
                {
                    Console.WriteLine("Нет данных для отображения - создаем пустую таблицу");
                    CreateEmptyGridView();
                    return;
                }

                Console.WriteLine("Создаем столбцы...");

                // Создаем столбцы вручную
                var dayColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Day",
                    HeaderText = "День недели",
                    Width = 150,
                    ReadOnly = true
                };

                var timeColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Time",
                    HeaderText = "Время работы",
                    Width = 200,
                    ReadOnly = true
                };

                var statusColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    HeaderText = "Статус",
                    Width = 100,
                    ReadOnly = true
                };

                var idColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    HeaderText = "ID",
                    Visible = false
                };

                dgvSchedules.Columns.AddRange(new DataGridViewColumn[]
                {
            dayColumn,
            timeColumn,
            statusColumn,
            idColumn
                });

                Console.WriteLine($"Добавляем {_schedules.Count} строк...");

                // Сортируем по дню недели и времени
                var sortedSchedules = _schedules
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .ToList();

                // Добавляем строки вручную
                foreach (var schedule in sortedSchedules)
                {
                    Console.WriteLine($"  Добавляем: День {schedule.DayOfWeek} ({GetDayName(schedule.DayOfWeek)}), " +
                                    $"{schedule.StartTime:hh\\:mm}-{schedule.EndTime:hh\\:mm}");

                    int rowIndex = dgvSchedules.Rows.Add();

                    // Заполняем ячейки - используем метод GetDayName для гарантии
                    dgvSchedules.Rows[rowIndex].Cells["Day"].Value = GetDayName(schedule.DayOfWeek);
                    dgvSchedules.Rows[rowIndex].Cells["Time"].Value = $"{schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}";
                    dgvSchedules.Rows[rowIndex].Cells["Status"].Value = schedule.IsActive ? "Активно" : "Не активно";
                    dgvSchedules.Rows[rowIndex].Cells["Id"].Value = schedule.Id;

                    // Сохраняем объект в Tag
                    dgvSchedules.Rows[rowIndex].Tag = schedule;

                    // Подсветка неактивных записей
                    if (!schedule.IsActive)
                    {
                        dgvSchedules.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                        dgvSchedules.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(245, 124, 0);
                    }
                    else
                    {
                        // Четные строки другого цвета для лучшей читаемости
                        if (rowIndex % 2 == 0)
                        {
                            dgvSchedules.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
                        }
                    }
                }

                Console.WriteLine($"Всего строк в DataGridView: {dgvSchedules.Rows.Count}");

                // Автонастройка ширины столбцов
                dgvSchedules.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                Console.WriteLine("DataGridView успешно обновлен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА в UpdateDataGridView: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // Показываем ошибку в DataGridView
                dgvSchedules.Columns.Clear();
                dgvSchedules.Columns.Add("Error", "Ошибка");
                dgvSchedules.Columns["Error"].Width = 400;
                dgvSchedules.Rows.Add($"Ошибка отображения: {ex.Message}");

                dgvSchedules.Refresh();
                dgvSchedules.Update();

                Console.WriteLine($"После обновления: строк в DataGridView: {dgvSchedules.Rows.Count}");
            }
        }

        // Используем асинхронное обновление DataGridView
        private async Task UpdateDataGridViewAsync()
        {
            Console.WriteLine($"\n=== ОБНОВЛЕНИЕ DATAGRIDVIEW (АСИНХРОННО) ===");
            Console.WriteLine($"Количество расписаний: {_schedules?.Count ?? 0}");

            try
            {
                // Используем Invoke для потокобезопасности
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(async () => await UpdateDataGridViewAsync()));
                    return;
                }

                // Полностью очищаем DataGridView
                dgvSchedules.DataSource = null;
                dgvSchedules.Columns.Clear();
                dgvSchedules.Rows.Clear();

                if (_schedules == null || _schedules.Count == 0)
                {
                    Console.WriteLine("Нет данных для отображения");
                    CreateEmptyGridView();
                    return;
                }

                Console.WriteLine("Создаем столбцы...");

                // Создаем столбцы вручную с правильными именами
                var columns = new DataGridViewColumn[]
                {
            new DataGridViewTextBoxColumn
            {
                Name = "Day",
                HeaderText = "День недели",
                Width = 150,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Time",
                HeaderText = "Время работы",
                Width = 200,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Статус",
                Width = 100,
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Visible = false
            }
                };

                dgvSchedules.Columns.AddRange(columns);

                Console.WriteLine($"Добавляем {_schedules.Count} строк...");

                // Сортируем расписание по дню недели и времени
                var sortedSchedules = _schedules
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .ToList();

                // Добавляем строки вручную
                foreach (var schedule in sortedSchedules)
                {
                    Console.WriteLine($"  Добавляем: {schedule.DayName}, {schedule.StartTime:hh\\:mm}-{schedule.EndTime:hh\\:mm}");

                    // ВАЖНО: DayName - это свойство только для чтения, не пытаемся его устанавливать
                    // Вместо этого используем вспомогательный метод, если DayName пустой
                    string dayNameToDisplay = schedule.DayName;
                    if (string.IsNullOrEmpty(dayNameToDisplay))
                    {
                        dayNameToDisplay = GetDayName(schedule.DayOfWeek);
                    }

                    int rowIndex = dgvSchedules.Rows.Add();

                    // Заполняем ячейки
                    dgvSchedules.Rows[rowIndex].Cells["Day"].Value = dayNameToDisplay; // ИЗМЕНЕНО: используем dayNameToDisplay
                    dgvSchedules.Rows[rowIndex].Cells["Time"].Value = $"{schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}";
                    dgvSchedules.Rows[rowIndex].Cells["Status"].Value = schedule.IsActive ? "Активно" : "Не активно";
                    dgvSchedules.Rows[rowIndex].Cells["Id"].Value = schedule.Id;

                    // Сохраняем объект в Tag
                    dgvSchedules.Rows[rowIndex].Tag = schedule;

                    // Подсветка неактивных записей
                    if (!schedule.IsActive)
                    {
                        dgvSchedules.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                        dgvSchedules.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(245, 124, 0);
                    }
                    else
                    {
                        // Подсветка четных/нечетных строк для лучшей читаемости
                        if (rowIndex % 2 == 0)
                        {
                            dgvSchedules.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
                        }
                    }
                }

                Console.WriteLine($"Всего строк в DataGridView: {dgvSchedules.Rows.Count}");

                // Автонастройка ширины столбцов
                dgvSchedules.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                // Устанавливаем минимальную ширину для лучшего отображения
                dgvSchedules.Columns["Day"].MinimumWidth = 120;
                dgvSchedules.Columns["Time"].MinimumWidth = 180;
                dgvSchedules.Columns["Status"].MinimumWidth = 80;

                Console.WriteLine("DataGridView успешно обновлен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА в UpdateDataGridViewAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // Показываем ошибку в DataGridView
                dgvSchedules.Columns.Clear();
                dgvSchedules.Columns.Add("Error", "Ошибка");
                dgvSchedules.Columns["Error"].Width = 400;
                dgvSchedules.Rows.Add($"Ошибка отображения: {ex.Message}");
                dgvSchedules.Rows.Add("Пожалуйста, попробуйте перезагрузить форму или обратитесь к администратору.");
            }
        }

        // Вспомогательный метод для получения названия дня недели
        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                6 => "Суббота",
                7 => "Воскресенье",
                _ => $"День {dayOfWeek}"
            };
        }

        // Обновленный метод CreateEmptyGridView для лучшего UX
        private void CreateEmptyGridView()
        {
            try
            {
                Console.WriteLine("Создаем таблицу для пустого расписания...");

                // Создаем столбцы для пустой таблицы
                var dayColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Day",
                    HeaderText = "День недели",
                    Width = 150,
                    ReadOnly = true
                };

                var timeColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Time",
                    HeaderText = "Время работы",
                    Width = 200,
                    ReadOnly = true
                };

                var statusColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    HeaderText = "Статус",
                    Width = 100,
                    ReadOnly = true
                };

                dgvSchedules.Columns.AddRange(new DataGridViewColumn[] { dayColumn, timeColumn, statusColumn });

                // Добавляем информационную строку
                int rowIndex = dgvSchedules.Rows.Add();
                dgvSchedules.Rows[rowIndex].Cells["Day"].Value = "Расписание не установлено";
                dgvSchedules.Rows[rowIndex].Cells["Time"].Value = "—";
                dgvSchedules.Rows[rowIndex].Cells["Status"].Value = "—";

                Console.WriteLine("Пустая таблица создана");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании пустой таблицы: {ex.Message}");
            }
        }

        private void UpdateButtonStates()
        {
            var hasDoctorSelected = cmbDoctor.SelectedItem != null &&
                            cmbDoctor.SelectedItem is DoctorComboBoxItem item &&
                            item.Value != Guid.Empty;

            var hasScheduleSelected = dgvSchedules.SelectedRows.Count > 0 &&
                                     _schedules != null &&
                                     _schedules.Count > 0;

            btnAdd.Enabled = hasDoctorSelected;
            btnEdit.Enabled = hasScheduleSelected;
            btnDelete.Enabled = hasScheduleSelected;
        }

        private void DgvSchedules_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void CmbDoctor_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Устанавливаем заголовок по умолчанию
            this.Text = "Расписание врачей";

            if (cmbDoctor.SelectedItem is DoctorComboBoxItem selectedItem && selectedItem.Value != Guid.Empty)
            {
                // Обновляем заголовок с именем выбранного врача
                var doctor = _doctors?.FirstOrDefault(d => d.Id == selectedItem.Value);
                if (doctor != null)
                {
                    this.Text = $"Расписание врачей - загрузка расписания {doctor.FullName}...";
                }

                LoadSchedulesAsync(selectedItem.Value);
            }
            else
            {
                _schedules = new List<DoctorSchedule>();

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateDataGridView()));
                }
                else
                {
                    UpdateDataGridView();
                }
                UpdateButtonStates();
            }
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            if (cmbDoctor.SelectedItem is not DoctorComboBoxItem selectedItem || selectedItem.Value == Guid.Empty)
                return;

            var schedule = new DoctorSchedule
            {
                DoctorId = selectedItem.Value,
                DayOfWeek = 1, // Понедельник по умолчанию
                StartTime = new TimeSpan(9, 0, 0), // 9:00
                EndTime = new TimeSpan(18, 0, 0),  // 18:00
                IsActive = true
            };

            var editForm = new DoctorScheduleEditForm(_dbService, schedule, true);
            var result = editForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                // Блокируем интерфейс во время операции
                Cursor = Cursors.WaitCursor;
                btnAdd.Enabled = false;
                dgvSchedules.Enabled = false;

                try
                {
                    // Обновляем расписание
                    await LoadSchedulesAsync(selectedItem.Value);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    dgvSchedules.Enabled = true;
                    UpdateButtonStates();
                }
            }
        }

        private async void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvSchedules.SelectedRows.Count == 0) return;

            var selectedRow = dgvSchedules.SelectedRows[0];

            // Получаем расписание из Tag
            var schedule = selectedRow.Tag as DoctorSchedule;

            if (schedule == null)
            {
                Console.WriteLine("Ошибка: не удалось получить расписание из выбранной строки");
                return;
            }

            var editForm = new DoctorScheduleEditForm(_dbService, schedule, false);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                if (cmbDoctor.SelectedItem is DoctorComboBoxItem selectedItem && selectedItem.Value != Guid.Empty)
                {
                    // Добавляем визуальную обратную связь
                    Cursor = Cursors.WaitCursor;
                    btnEdit.Enabled = false;

                    try
                    {
                        // Обновляем расписание
                        await LoadSchedulesAsync(selectedItem.Value);
                    }
                    finally
                    {
                        Cursor = Cursors.Default;
                        UpdateButtonStates();
                    }
                }
            }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvSchedules.SelectedRows.Count == 0) return;

            var selectedRow = dgvSchedules.SelectedRows[0];

            // Получаем расписание из Tag
            var schedule = selectedRow.Tag as DoctorSchedule;

            if (schedule == null)
            {
                Console.WriteLine("Ошибка: не удалось получить расписание из выбранной строки");
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить расписание на {GetDayName(schedule.DayOfWeek)} ({schedule.StartTime:hh\\:mm}-{schedule.EndTime:hh\\:mm})?",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    // Блокируем интерфейс во время операции
                    Cursor = Cursors.WaitCursor;
                    btnDelete.Enabled = false;
                    btnEdit.Enabled = false;
                    dgvSchedules.Enabled = false;

                    var success = await _dbService.DeleteDoctorScheduleAsync(schedule.Id);
                    if (success)
                    {
                        // Вместо MessageBox обновляем заголовок формы
                        var doctor = _doctors?.FirstOrDefault(d => d.Id == schedule.DoctorId);
                        if (doctor != null)
                        {
                            this.Text = $"Расписание врачей - {doctor.FullName} (расписание удалено)";
                        }

                        // Сразу обновляем таблицу
                        if (cmbDoctor.SelectedItem is DoctorComboBoxItem selectedItem && selectedItem.Value != Guid.Empty)
                        {
                            await LoadSchedulesAsync(selectedItem.Value);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка удаления расписания",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления расписания: {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    dgvSchedules.Enabled = true;
                    UpdateButtonStates();
                }
            }
        }

        // Вспомогательная форма для редактирования расписания
        public class DoctorScheduleEditForm : Form
        {
            private readonly IDatabaseService _dbService;
            private readonly DoctorSchedule _schedule;
            private readonly bool _isNew;

            private ComboBox cmbDayOfWeek;
            private DateTimePicker dtpStartTime;
            private DateTimePicker dtpEndTime;
            private CheckBox chkIsActive;
            private Button btnSave;
            private Button btnCancel;
            private List<DoctorSchedule> _existingSchedules;

            public DoctorScheduleEditForm(IDatabaseService dbService, DoctorSchedule schedule, bool isNew)
            {
                _dbService = dbService;
                _schedule = schedule;
                _isNew = isNew;
                InitializeComponent();
                ApplyStyles();
                LoadScheduleData();
                LoadExistingSchedulesAsync();
            }

            private async void LoadExistingSchedulesAsync()
            {
                try
                {
                    _existingSchedules = await _dbService.GetDoctorSchedulesAsync(_schedule.DoctorId);

                    // Обновляем список дней недели, показывая занятые дни
                    for (int i = 0; i < cmbDayOfWeek.Items.Count; i++)
                    {
                        int dayOfWeek = i + 1;
                        var existing = _existingSchedules?.FirstOrDefault(s => s.DayOfWeek == dayOfWeek &&
                                                                            (!_isNew || s.Id != _schedule.Id));

                        if (existing != null)
                        {
                            cmbDayOfWeek.Items[i] = $"{GetDayName(dayOfWeek)} - Уже есть расписание";
                        }
                        else
                        {
                            cmbDayOfWeek.Items[i] = GetDayName(dayOfWeek);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки расписаний: {ex.Message}");
                }
            }

            private string GetDayName(int dayOfWeek)
            {
                return dayOfWeek switch
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

            private void InitializeComponent()
            {
                this.SuspendLayout();

                // Основные настройки формы
                this.Text = _isNew ? "Добавление расписания" : "Редактирование расписания";
                this.Size = new Size(450, 300);
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
                    Text = _isNew ? "Добавление нового расписания" : "Редактирование расписания",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    ForeColor = Color.FromArgb(44, 90, 160),
                    Location = new Point(0, 0),
                    Size = new Size(400, 30)
                };

                // Поля ввода
                int yPos = 40;
                int labelWidth = 150;
                int fieldWidth = 200;

                // День недели
                var lblDayOfWeek = new Label
                {
                    Text = "День недели*:",
                    Location = new Point(0, yPos),
                    Size = new Size(labelWidth, 25),
                    Font = new Font("Segoe UI", 11),
                    TextAlign = ContentAlignment.MiddleRight
                };

                cmbDayOfWeek = new ComboBox
                {
                    Location = new Point(labelWidth + 10, yPos),
                    Size = new Size(fieldWidth, 30),
                    Font = new Font("Segoe UI", 11),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Name = "cmbDayOfWeek"
                };

                // Инициализируем дни недели
                for (int i = 1; i <= 7; i++)
                {
                    cmbDayOfWeek.Items.Add(GetDayName(i));
                }

                yPos += 40;

                // Время начала
                var lblStartTime = new Label
                {
                    Text = "Время начала*:",
                    Location = new Point(0, yPos),
                    Size = new Size(labelWidth, 25),
                    Font = new Font("Segoe UI", 11),
                    TextAlign = ContentAlignment.MiddleRight
                };

                dtpStartTime = new DateTimePicker
                {
                    Location = new Point(labelWidth + 10, yPos),
                    Size = new Size(fieldWidth, 30),
                    Font = new Font("Segoe UI", 11),
                    Format = DateTimePickerFormat.Time,
                    ShowUpDown = true,
                    Name = "dtpStartTime",
                    Value = DateTime.Today.AddHours(9)
                };

                yPos += 40;

                // Время окончания
                var lblEndTime = new Label
                {
                    Text = "Время окончания*:",
                    Location = new Point(0, yPos),
                    Size = new Size(labelWidth, 25),
                    Font = new Font("Segoe UI", 11),
                    TextAlign = ContentAlignment.MiddleRight
                };

                dtpEndTime = new DateTimePicker
                {
                    Location = new Point(labelWidth + 10, yPos),
                    Size = new Size(fieldWidth, 30),
                    Font = new Font("Segoe UI", 11),
                    Format = DateTimePickerFormat.Time,
                    ShowUpDown = true,
                    Name = "dtpEndTime",
                    Value = DateTime.Today.AddHours(18)
                };

                yPos += 40;

                // Активен
                var lblIsActive = new Label
                {
                    Text = "Активно:",
                    Location = new Point(0, yPos),
                    Size = new Size(labelWidth, 25),
                    Font = new Font("Segoe UI", 11),
                    TextAlign = ContentAlignment.MiddleRight
                };

                chkIsActive = new CheckBox
                {
                    Location = new Point(labelWidth + 10, yPos),
                    Size = new Size(25, 25),
                    Checked = true,
                    Name = "chkIsActive"
                };

                yPos += 45;

                // Кнопки
                var buttonPanel = new Panel
                {
                    Location = new Point(0, yPos),
                    Size = new Size(400, 40)
                };

                btnSave = new Button
                {
                    Text = "Сохранить",
                    Location = new Point(100, 0),
                    Size = new Size(120, 35),
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    BackColor = Color.FromArgb(44, 90, 160),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Name = "btnSave"
                };
                btnSave.Click += BtnSave_Click;

                btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(230, 0),
                    Size = new Size(120, 35),
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.FromArgb(245, 245, 245),
                    ForeColor = Color.FromArgb(51, 51, 51),
                    FlatStyle = FlatStyle.Flat,
                    Name = "btnCancel"
                };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

                // Добавление элементов на панель
                mainPanel.Controls.AddRange(new Control[] {
            lblTitle,
            lblDayOfWeek, cmbDayOfWeek,
            lblStartTime, dtpStartTime,
            lblEndTime, dtpEndTime,
            lblIsActive, chkIsActive,
            buttonPanel
        });

                this.Controls.Add(mainPanel);

                // Настройка обработки клавиш
                this.AcceptButton = btnSave;
                this.CancelButton = btnCancel;

                this.ResumeLayout(false);
            }

            private void ApplyStyles()
            {
                // Стилизация кнопок
                btnSave.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                btnSave.Cursor = Cursors.Hand;

                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                btnCancel.Cursor = Cursors.Hand;
            }

            private void LoadScheduleData()
            {
                if (!_isNew)
                {
                    // Устанавливаем день недели
                    cmbDayOfWeek.SelectedIndex = Math.Max(0, Math.Min(_schedule.DayOfWeek - 1, 6));

                    // Устанавливаем время
                    var startDateTime = DateTime.Today.Add(_schedule.StartTime);
                    var endDateTime = DateTime.Today.Add(_schedule.EndTime);
                    dtpStartTime.Value = startDateTime;
                    dtpEndTime.Value = endDateTime;

                    // Устанавливаем активность
                    chkIsActive.Checked = _schedule.IsActive;
                }
                else
                {
                    // Значения по умолчанию для нового расписания
                    cmbDayOfWeek.SelectedIndex = 0; // Понедельник
                    dtpStartTime.Value = DateTime.Today.AddHours(9); // 9:00
                    dtpEndTime.Value = DateTime.Today.AddHours(18);  // 18:00
                    chkIsActive.Checked = true;
                }
            }

            private async void BtnSave_Click(object sender, EventArgs e)
            {
                // Валидация
                if (cmbDayOfWeek.SelectedIndex == -1)
                {
                    MessageBox.Show("Пожалуйста, выберите день недели",
                        "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbDayOfWeek.Focus();
                    return;
                }

                if (dtpEndTime.Value.TimeOfDay <= dtpStartTime.Value.TimeOfDay)
                {
                    MessageBox.Show("Время окончания должно быть позже времени начала",
                        "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtpEndTime.Focus();
                    return;
                }

                // Получаем выбранный день недели
                int selectedDayOfWeek = cmbDayOfWeek.SelectedIndex + 1;

                // Проверяем, занят ли этот день
                var existingSchedule = _existingSchedules?.FirstOrDefault(s =>
                    s.DayOfWeek == selectedDayOfWeek &&
                    (!_isNew || s.Id != _schedule.Id));

                if (existingSchedule != null)
                {
                    var result = MessageBox.Show(
                        $"На {GetDayName(selectedDayOfWeek)} уже есть расписание:\n" +
                        $"{existingSchedule.StartTime:hh\\:mm} - {existingSchedule.EndTime:hh\\:mm}\n\n" +
                        "Хотите заменить существующее расписание?",
                        "Расписание уже существует",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Cancel)
                        return;

                    if (result == DialogResult.No)
                    {
                        cmbDayOfWeek.Focus();
                        return;
                    }

                    // Если выбрали "Да", удаляем старое расписание
                    try
                    {
                        await _dbService.DeleteDoctorScheduleAsync(existingSchedule.Id);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления старого расписания: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Блокируем интерфейс
                btnSave.Enabled = false;
                btnCancel.Enabled = false;

                try
                {
                    // Обновляем данные расписания
                    _schedule.DayOfWeek = selectedDayOfWeek;
                    _schedule.StartTime = dtpStartTime.Value.TimeOfDay;
                    _schedule.EndTime = dtpEndTime.Value.TimeOfDay;
                    _schedule.IsActive = chkIsActive.Checked;

                    bool success;
                    if (_isNew)
                    {
                        var result = await _dbService.CreateDoctorScheduleAsync(_schedule);
                        success = result != null && result.Id != Guid.Empty;
                    }
                    else
                    {
                        success = await _dbService.UpdateDoctorScheduleAsync(_schedule);
                    }

                    if (success)
                    {
                        MessageBox.Show(_isNew ? "Расписание успешно добавлено!" : "Расписание успешно обновлено!",
                            "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка сохранения расписания",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    // Более подробное сообщение об ошибке
                    string errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\n\nВнутренняя ошибка: {ex.InnerException.Message}";
                    }

                    if (errorMessage.Contains("23505") || errorMessage.Contains("уникальности"))
                    {
                        errorMessage = "Расписание на этот день недели уже существует для этого врача.\n\n" +
                                     "Пожалуйста, выберите другой день или отредактируйте существующее расписание.";
                    }

                    MessageBox.Show($"Ошибка сохранения расписания: {errorMessage}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // Разблокируем интерфейс
                    btnSave.Enabled = true;
                    btnCancel.Enabled = true;
                }
            }
        }

        // Вспомогательная форма для разрешения конфликтов
        public class ConflictResolutionForm : Form
        {
            public enum ConflictAction
            {
                UpdateExisting,
                ChooseAnotherDay,
                Cancel
            }

            public ConflictAction Action { get; private set; } = ConflictAction.Cancel;

            private readonly DoctorSchedule _existingSchedule;
            private readonly int _selectedDayOfWeek;

            public ConflictResolutionForm(DoctorSchedule existingSchedule, int selectedDayOfWeek)
            {
                _existingSchedule = existingSchedule;
                _selectedDayOfWeek = selectedDayOfWeek;
                InitializeComponent();
            }

            private void InitializeComponent()
            {
                this.SuspendLayout();

                this.Text = "Конфликт расписания";
                this.Size = new Size(500, 250);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;

                var mainPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20)
                };

                var lblTitle = new Label
                {
                    Text = "Конфликт расписания",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    ForeColor = Color.FromArgb(231, 76, 60),
                    Location = new Point(0, 0),
                    Size = new Size(400, 30)
                };

                var dayNames = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье" };
                var dayName = _selectedDayOfWeek >= 1 && _selectedDayOfWeek <= 7 ? dayNames[_selectedDayOfWeek - 1] : "Неизвестный день";

                var lblMessage = new Label
                {
                    Text = $"У врача уже есть расписание на {dayName}:\n" +
                           $"Время: {_existingSchedule.StartTime:hh\\:mm} - {_existingSchedule.EndTime:hh\\:mm}\n" +
                           $"Статус: {(_existingSchedule.IsActive ? "Активно" : "Не активно")}\n\n" +
                           "Что вы хотите сделать?",
                    Font = new Font("Segoe UI", 11),
                    Location = new Point(0, 40),
                    Size = new Size(450, 80)
                };

                var btnUpdateExisting = new Button
                {
                    Text = "Обновить существующее расписание",
                    Location = new Point(20, 130),
                    Size = new Size(250, 35),
                    Font = new Font("Segoe UI", 10),
                    BackColor = Color.FromArgb(44, 90, 160),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                btnUpdateExisting.Click += (s, e) =>
                {
                    Action = ConflictAction.UpdateExisting;
                    this.Close();
                };

                var btnChooseAnotherDay = new Button
                {
                    Text = "Выбрать другой день недели",
                    Location = new Point(20, 175),
                    Size = new Size(250, 35),
                    Font = new Font("Segoe UI", 10),
                    BackColor = Color.FromArgb(243, 156, 18),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                btnChooseAnotherDay.Click += (s, e) =>
                {
                    Action = ConflictAction.ChooseAnotherDay;
                    this.Close();
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(280, 175),
                    Size = new Size(100, 35),
                    Font = new Font("Segoe UI", 10),
                    BackColor = Color.FromArgb(245, 245, 245),
                    ForeColor = Color.FromArgb(51, 51, 51),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                btnCancel.Click += (s, e) =>
                {
                    Action = ConflictAction.Cancel;
                    this.Close();
                };

                mainPanel.Controls.AddRange(new Control[]
                {
            lblTitle,
            lblMessage,
            btnUpdateExisting,
            btnChooseAnotherDay,
            btnCancel
                });

                this.Controls.Add(mainPanel);
                this.ResumeLayout(false);
            }
        }


        // Класс для элементов ComboBox в форме расписания врачей
        // Переименован чтобы избежать конфликта с классом из AppointmentForm.cs
        public class DoctorComboBoxItem
        {
            public string Text { get; set; } = string.Empty;
            public Guid Value { get; set; } = Guid.Empty;

            public override string ToString()
            {
                return Text;
            }
        }

    }
}