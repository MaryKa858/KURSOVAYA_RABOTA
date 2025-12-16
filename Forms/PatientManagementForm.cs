using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ClinicDesctop.Forms
{
    public partial class PatientManagementForm : Form
    {
        private readonly IDatabaseService _dbService;
        private List<Patient> _patients;

        // Элементы управления
        private TableLayoutPanel mainLayout;
        private Panel searchPanel;
        private DataGridView dgvPatients;
        private Panel buttonPanel;

        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;
        private Button btnViewDetails;
        private Button btnDeletePatient;
        private Button btnClose;

        public PatientManagementForm(IDatabaseService dbService)
        {
            if (dbService == null)
                throw new ArgumentNullException(nameof(dbService));

            _dbService = dbService;
            InitializeComponents();
            ApplyStyles();

            // Загружаем данные при запуске формы
            LoadPatientsAsync();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Управление пациентами";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(900, 500);
            this.BackColor = Color.White;

            // Основной TableLayoutPanel для размещения элементов
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Панель поиска
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Панель кнопок

            // ========== Панель поиска ==========
            searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 15, 15, 15),
                BackColor = Color.FromArgb(245, 245, 245),
                MinimumSize = new Size(0, 70)
            };

            var lblSearch = new Label
            {
                Text = "Поиск:",
                Location = new Point(15, 20),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtSearch = new TextBox
            {
                Location = new Point(85, 20),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 11),
                Name = "txtSearch"
            };

            btnSearch = new Button
            {
                Text = "Найти",
                Location = new Point(450, 20),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSearch.Click += BtnSearch_Click;

            btnRefresh = new Button
            {
                Text = "Обновить",
                Location = new Point(560, 20),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.Click += async (s, e) => await LoadPatientsAsync();

            searchPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch, btnRefresh });

            // ========== DataGridView для пациентов ==========
            dgvPatients = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvPatients",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10),
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 240, 240)
            };
            dgvPatients.SelectionChanged += DgvPatients_SelectionChanged;
            dgvPatients.CellDoubleClick += DgvPatients_CellDoubleClick;

            // ========== Панель кнопок ==========
            buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 15, 15, 15),
                BackColor = Color.FromArgb(245, 245, 245),
                MinimumSize = new Size(0, 70)
            };

            btnViewDetails = new Button
            {
                Text = "Просмотреть детали",
                Location = new Point(15, 15),
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnViewDetails",
                Enabled = false
            };
            btnViewDetails.Click += BtnViewDetails_Click;

            btnDeletePatient = new Button
            {
                Text = "Удалить пациента",
                Location = new Point(205, 15),
                Size = new Size(160, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(231, 76, 60), // Красный цвет для кнопки удаления
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnDeletePatient",
                Enabled = false
            };
            btnDeletePatient.Click += BtnDeletePatient_Click;

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(375, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnClose"
            };
            btnClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { btnViewDetails, btnDeletePatient, btnClose });

            // ========== Добавляем элементы в mainLayout ==========
            mainLayout.Controls.Add(searchPanel, 0, 0);
            mainLayout.Controls.Add(dgvPatients, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            // ========== Добавляем mainLayout на форму ==========
            this.Controls.Add(mainLayout);

            this.ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            // Стилизация DataGridView
            dgvPatients.BorderStyle = BorderStyle.FixedSingle;
            dgvPatients.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvPatients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 90, 160);
            dgvPatients.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPatients.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvPatients.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvPatients.EnableHeadersVisualStyles = false;
            dgvPatients.RowHeadersVisible = false;
            dgvPatients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка цвета выделения
            dgvPatients.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 248, 255);
            dgvPatients.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 90, 160);
            dgvPatients.RowHeadersDefaultCellStyle.SelectionBackColor = Color.Transparent;

            // Четные и нечетные строки
            dgvPatients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);

            // Стилизация кнопок
            var buttons = new[] { btnSearch, btnRefresh, btnViewDetails, btnDeletePatient, btnClose };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);

                    // Специальные цвета для кнопок
                    if (button == btnDeletePatient)
                    {
                        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 96, 80);
                    }
                    else if (button.BackColor == Color.FromArgb(245, 245, 245))
                    {
                        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 235, 235);
                    }
                    else
                    {
                        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                            Math.Min(255, button.BackColor.R + 20),
                            Math.Min(255, button.BackColor.G + 20),
                            Math.Min(255, button.BackColor.B + 20)
                        );
                    }

                    button.Cursor = Cursors.Hand;
                    button.FlatAppearance.BorderSize = 1;
                }
            }

            // Стилизация текстового поля
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.BackColor = Color.White;
            txtSearch.ForeColor = Color.FromArgb(51, 51, 51);
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                txtSearch.Enabled = false;
                btnSearch.Enabled = false;
                btnRefresh.Enabled = false;
                btnViewDetails.Enabled = false;
                btnDeletePatient.Enabled = false;

                // Загрузка всех пациентов из базы данных
                _patients = await _dbService.GetPatientsAsync();

                if (_patients == null)
                {
                    _patients = new List<Patient>();
                }

                UpdateDataGridView();
                UpdateButtonStates();

                if (_patients.Count == 0)
                {
                    Console.WriteLine("В базе данных нет пациентов");
                }
                else
                {
                    Console.WriteLine($"Загружено пациентов: {_patients.Count}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пациентов: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                txtSearch.Enabled = true;
                btnSearch.Enabled = true;
                btnRefresh.Enabled = true;
                UpdateButtonStates();
            }
        }

        private void UpdateDataGridView()
        {
            if (_patients == null) return;

            dgvPatients.DataSource = null;
            dgvPatients.Columns.Clear();
            dgvPatients.Rows.Clear();

            // Создаем столбцы вручную
            var fullNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "FullName",
                HeaderText = "ФИО пациента",
                Width = 250,
                ReadOnly = true
            };

            var phoneColumn = new DataGridViewTextBoxColumn
            {
                Name = "Phone",
                HeaderText = "Телефон",
                Width = 150,
                ReadOnly = true
            };

            var emailColumn = new DataGridViewTextBoxColumn
            {
                Name = "Email",
                HeaderText = "Email",
                Width = 200,
                ReadOnly = true
            };

            var birthDateColumn = new DataGridViewTextBoxColumn
            {
                Name = "FormattedBirthDate",
                HeaderText = "Дата рождения",
                Width = 120,
                ReadOnly = true
            };

            var ageColumn = new DataGridViewTextBoxColumn
            {
                Name = "Age",
                HeaderText = "Возраст",
                Width = 80,
                ReadOnly = true
            };

            var passportColumn = new DataGridViewTextBoxColumn
            {
                Name = "Passport",
                HeaderText = "Номер паспорта",
                Width = 150,
                ReadOnly = true
            };

            var idColumn = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Visible = false
            };

            // Добавляем столбцы
            dgvPatients.Columns.AddRange(new DataGridViewColumn[] {
                fullNameColumn, phoneColumn, emailColumn,
                birthDateColumn, ageColumn, passportColumn, idColumn
            });

            if (_patients.Count == 0)
            {
                // Добавляем информационную строку
                dgvPatients.Rows.Add("Нет пациентов для отображения");
                dgvPatients.Rows[0].ReadOnly = true;
                return;
            }

            // Заполняем DataGridView данными
            foreach (var patient in _patients)
            {
                // Обновляем вычисляемые свойства пациента
                patient.UpdateCalculatedProperties();

                // Добавляем строку
                int rowIndex = dgvPatients.Rows.Add();

                // Заполняем ячейки
                dgvPatients.Rows[rowIndex].Cells["FullName"].Value = patient.FullName ?? "Не указано";
                dgvPatients.Rows[rowIndex].Cells["Phone"].Value = patient.Phone ?? "Не указан";
                dgvPatients.Rows[rowIndex].Cells["Email"].Value = patient.Email ?? "Не указан";
                dgvPatients.Rows[rowIndex].Cells["FormattedBirthDate"].Value = patient.FormattedBirthDate ?? "Не указана";
                dgvPatients.Rows[rowIndex].Cells["Age"].Value = patient.Age;
                dgvPatients.Rows[rowIndex].Cells["Passport"].Value = patient.Passport ?? "Не указан";
                dgvPatients.Rows[rowIndex].Cells["Id"].Value = patient.Id;

                // Сохраняем ссылку на пациента в Tag строки
                dgvPatients.Rows[rowIndex].Tag = patient;
            }

            // Автонастройка ширины столбцов после заполнения
            dgvPatients.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void UpdateButtonStates()
        {
            var hasSelection = dgvPatients.SelectedRows.Count > 0 && _patients != null && _patients.Count > 0;
            btnViewDetails.Enabled = hasSelection;
            btnDeletePatient.Enabled = hasSelection;

            // Визуальная обратная связь для кнопок
            UpdateButtonAppearance(btnViewDetails, hasSelection, Color.FromArgb(44, 90, 160));
            UpdateButtonAppearance(btnDeletePatient, hasSelection, Color.FromArgb(231, 76, 60));
        }

        private void UpdateButtonAppearance(Button button, bool enabled, Color enabledColor)
        {
            if (enabled)
            {
                button.BackColor = enabledColor;
                button.ForeColor = Color.White;
                if (button == btnDeletePatient)
                {
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 96, 80);
                }
                else
                {
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                        Math.Min(255, enabledColor.R + 20),
                        Math.Min(255, enabledColor.G + 20),
                        Math.Min(255, enabledColor.B + 20)
                    );
                }
            }
            else
            {
                button.BackColor = Color.FromArgb(220, 220, 220);
                button.ForeColor = Color.FromArgb(150, 150, 150);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 220, 220);
            }
        }

        private void DgvPatients_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            var searchTerm = txtSearch.Text.Trim();

            try
            {
                Cursor = Cursors.WaitCursor;
                txtSearch.Enabled = false;
                btnSearch.Enabled = false;
                btnRefresh.Enabled = false;
                btnViewDetails.Enabled = false;
                btnDeletePatient.Enabled = false;

                if (string.IsNullOrEmpty(searchTerm))
                {
                    await LoadPatientsAsync();
                    return;
                }

                _patients = await _dbService.SearchPatientsAsync(searchTerm);
                UpdateDataGridView();
                UpdateButtonStates();

                if (_patients == null || _patients.Count == 0)
                {
                    MessageBox.Show("Пациенты по заданным критериям не найдены",
                        "Результаты поиска", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Console.WriteLine($"Найдено пациентов: {_patients.Count}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска пациентов: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                txtSearch.Enabled = true;
                btnSearch.Enabled = true;
                btnRefresh.Enabled = true;
                UpdateButtonStates();
            }
        }

        private void DgvPatients_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                ShowPatientDetails();
            }
        }

        private void BtnViewDetails_Click(object sender, EventArgs e)
        {
            ShowPatientDetails();
        }

        private async void BtnDeletePatient_Click(object sender, EventArgs e)
        {
            if (dgvPatients.SelectedRows.Count == 0) return;

            try
            {
                var selectedRow = dgvPatients.SelectedRows[0];

                // Получаем пациента из Tag строки
                var patient = selectedRow.Tag as Patient;

                if (patient == null)
                {
                    // Альтернативный способ: получаем по ID
                    if (selectedRow.Cells["Id"].Value is Guid patientId)
                    {
                        patient = _patients?.FirstOrDefault(p => p.Id == patientId);
                    }
                }

                if (patient == null)
                {
                    MessageBox.Show("Не удалось получить информацию о пациенте", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверяем, есть ли у пациента активные записи
                try
                {
                    bool hasActiveAppointments = await _dbService.CheckPatientHasAppointmentsAsync(patient.Id);

                    if (hasActiveAppointments)
                    {
                        var result = MessageBox.Show(
                            $"У пациента '{patient.FullName}' есть активные записи на прием.\n\n" +
                            "Вы можете:\n" +
                            "1. Посмотреть и отменить активные записи\n" +
                            "2. Продолжить удаление (все записи будут удалены)\n\n" +
                            "Хотите посмотреть активные записи перед удалением?",
                            "Активные записи пациента",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.Yes)
                        {
                            // Показываем записи пациента
                            ShowPatientAppointments(patient);
                            return;
                        }
                        else if (result == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке записей: {ex.Message}");
                }

                // Запрашиваем подтверждение удаления
                var patientInfo =
                    $"ФИО: {patient.FullName}\n" +
                    $"Телефон: {patient.Phone}\n" +
                    $"Email: {patient.Email ?? "не указан"}\n" +
                    $"Дата рождения: {patient.FormattedBirthDate}\n" +
                    $"Возраст: {patient.Age} лет";

                var resultDelete = MessageBox.Show(
                    $"ВЫ УВЕРЕНЫ, ЧТО ХОТИТЕ УДАЛИТЬ ПАЦИЕНТА?\n\n" +
                    $"{patientInfo}\n\n" +
                    "⚠️  Это действие нельзя отменить!\n" +
                    "⚠️  Будут удалены все связанные данные (записи, отзывы, уведомления)",
                    "ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (resultDelete == DialogResult.Yes)
                {
                    Cursor = Cursors.WaitCursor;
                    btnDeletePatient.Enabled = false;
                    btnViewDetails.Enabled = false;
                    btnSearch.Enabled = false;
                    btnRefresh.Enabled = false;

                    try
                    {
                        // Выполняем удаление
                        bool success = await _dbService.DeletePatientAsync(patient.Id);

                        if (success)
                        {
                            MessageBox.Show(
                                $"Пациент '{patient.FullName}' успешно удален.\n" +
                                "Все связанные данные также были удалены.",
                                "Успешно",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            // Обновляем список пациентов
                            await LoadPatientsAsync();

                            // Очищаем поле поиска
                            txtSearch.Clear();
                        }
                        else
                        {
                            MessageBox.Show(
                                "Не удалось удалить пациента. Возможно, пациент уже был удален.",
                                "Ошибка",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при удалении пациента:\n{ex.Message}",
                            "Ошибка удаления",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor = Cursors.Default;
                        UpdateButtonStates();
                        btnSearch.Enabled = true;
                        btnRefresh.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Cursor = Cursors.Default;
                UpdateButtonStates();
            }
        }

        private void ShowPatientAppointments(Patient patient)
        {
            try
            {
                // Создаем форму для просмотра записей пациента
                var appointmentsForm = new Form
                {
                    Text = $"Записи пациента: {patient.FullName}",
                    Size = new Size(800, 500),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.White
                };

                var mainPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(0)
                };
                mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                // Заголовок
                var headerPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Height = 70,
                    Padding = new Padding(20),
                    BackColor = Color.FromArgb(44, 90, 160)
                };

                var titleLabel = new Label
                {
                    Text = $"Активные записи пациента: {patient.FullName}",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var infoLabel = new Label
                {
                    Text = "Отмените все записи перед удалением пациента",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(200, 220, 255),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(0, 25, 0, 0)
                };

                headerPanel.Controls.Add(titleLabel);
                headerPanel.Controls.Add(infoLabel);

                // DataGridView для записей
                var dgvAppointments = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White
                };

                // Заглушка для данных (в реальном приложении нужно загрузить записи)
                dgvAppointments.Columns.Add("Date", "Дата");
                dgvAppointments.Columns.Add("Time", "Время");
                dgvAppointments.Columns.Add("Doctor", "Врач");
                dgvAppointments.Columns.Add("Service", "Услуга");
                dgvAppointments.Columns.Add("Status", "Статус");

                dgvAppointments.Rows.Add(
                    "Загрузка записей...",
                    "Пожалуйста, используйте",
                    "форму просмотра записей",
                    "для управления записями",
                    "пациента");

                // Кнопки
                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Height = 60,
                    Padding = new Padding(20),
                    BackColor = Color.FromArgb(245, 245, 245)
                };

                var btnOpenAppointmentsForm = new Button
                {
                    Text = "Открыть записи пациента",
                    Size = new Size(200, 35),
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.FromArgb(44, 90, 160),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnOpenAppointmentsForm.Click += (s, e) =>
                {
                    appointmentsForm.Close();
                    // Здесь нужно вызвать форму просмотра записей пациента
                    MessageBox.Show("Откройте форму 'Просмотр записей' для управления записями пациента",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                var btnCloseForm = new Button
                {
                    Text = "Закрыть",
                    Size = new Size(120, 35),
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.FromArgb(245, 245, 245),
                    ForeColor = Color.FromArgb(51, 51, 51),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnCloseForm.Click += (s, e) => appointmentsForm.Close();

                buttonPanel.Controls.Add(btnOpenAppointmentsForm);
                buttonPanel.Controls.Add(btnCloseForm);
                btnOpenAppointmentsForm.Location = new Point(20, 10);
                btnCloseForm.Location = new Point(230, 10);

                // Стилизация кнопок
                btnOpenAppointmentsForm.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                btnOpenAppointmentsForm.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 110, 180);
                btnCloseForm.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                btnCloseForm.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 235, 235);

                // Добавляем элементы
                mainPanel.Controls.Add(headerPanel, 0, 0);
                mainPanel.Controls.Add(dgvAppointments, 0, 1);
                mainPanel.Controls.Add(buttonPanel, 0, 2);

                appointmentsForm.Controls.Add(mainPanel);
                appointmentsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении записей: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPatientDetails()
        {
            if (dgvPatients.SelectedRows.Count == 0) return;

            try
            {
                var selectedRow = dgvPatients.SelectedRows[0];

                // Получаем пациента из Tag строки
                var patient = selectedRow.Tag as Patient;

                if (patient == null)
                {
                    // Альтернативный способ: получаем по ID
                    if (selectedRow.Cells["Id"].Value is Guid patientId)
                    {
                        patient = _patients?.FirstOrDefault(p => p.Id == patientId);
                    }
                }

                if (patient == null)
                {
                    MessageBox.Show("Не удалось получить информацию о пациенте", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Показываем детальную информацию о пациенте
                ShowPatientDetailsForm(patient);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении деталей пациента: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPatientDetailsForm(Patient patient)
        {
            var detailsForm = new Form
            {
                Text = $"Детальная информация о пациенте",
                Size = new Size(550, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Основной контейнер
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Заголовок
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Информация
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Кнопка

            // ========== Заголовок ==========
            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(25, 15, 25, 15),
                BackColor = Color.FromArgb(44, 90, 160)
            };

            var titleLabel = new Label
            {
                Text = patient.FullName,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 30
            };

            var subTitleLabel = new Label
            {
                Text = "Детальная информация о пациенте",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 220, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 30, 0, 0),
                Height = 20
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subTitleLabel);

            // ========== Панель с информацией ==========
            var infoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15),
                AutoScroll = true,
                BackColor = Color.White
            };

            // Создаем таблицу для информации
            var infoTable = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                ColumnStyles =
                {
                    new ColumnStyle(SizeType.Absolute, 160F), // Лейблы
                    new ColumnStyle(SizeType.Percent, 100F)   // Значения
                },
                Padding = new Padding(0),
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // Список полей для отображения
            var fields = new List<(string Label, string Value)>
            {
                ("ФИО полностью:", patient.FullName ?? "не указано"),
                ("Фамилия:", patient.LastName ?? "не указана"),
                ("Имя:", patient.FirstName ?? "не указано"),
                ("Отчество:", patient.MiddleName ?? "не указано"),
                ("Телефон:", patient.Phone ?? "не указан"),
                ("Email:", !string.IsNullOrWhiteSpace(patient.Email) ? patient.Email : "не указан"),
                ("Дата рождения:", patient.FormattedBirthDate ?? "не указана"),
                ("Возраст:", $"{patient.Age} лет"),
                ("Номер паспорта:", !string.IsNullOrWhiteSpace(patient.Passport) ? patient.Passport : "не указан"),
                ("ID пациента:", patient.Id.ToString()),
                ("Дата регистрации:", patient.CreatedAt.ToString("dd.MM.yyyy HH:mm")),
                ("Дата обновления:", patient.UpdatedAt.ToString("dd.MM.yyyy HH:mm"))
            };

            // Добавляем Telegram ID если есть
            if (patient.TelegramId.HasValue)
            {
                fields.Add(("Telegram ID:", patient.TelegramId.Value.ToString()));
            }

            // Устанавливаем количество строк
            infoTable.RowCount = fields.Count;

            // Добавляем строки
            for (int i = 0; i < fields.Count; i++)
            {
                infoTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

                // Лейбл
                var lbl = new Label
                {
                    Text = fields[i].Label,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5, 0, 10, 0),
                    Margin = new Padding(0, 5, 0, 5),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    Dock = DockStyle.Fill,
                    AutoEllipsis = true
                };

                // Значение
                var val = new Label
                {
                    Text = fields[i].Value,
                    Font = new Font("Segoe UI", 10),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5, 0, 5, 0),
                    Margin = new Padding(0, 5, 0, 5),
                    ForeColor = Color.FromArgb(51, 51, 51),
                    Dock = DockStyle.Fill,
                    AutoEllipsis = true
                };

                // Контейнер для лейбла
                var labelContainer = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                // Контейнер для значения
                var valueContainer = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                labelContainer.Controls.Add(lbl);
                valueContainer.Controls.Add(val);

                infoTable.Controls.Add(labelContainer, 0, i);
                infoTable.Controls.Add(valueContainer, 1, i);
            }

            infoPanel.Controls.Add(infoTable);

            // Автоматически подгоняем размер таблицы
            infoTable.Location = new Point(0, 0);
            infoTable.Width = infoPanel.ClientSize.Width;

            // ========== Панель кнопок ==========
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(20, 15, 20, 15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var btnCloseDetails = new Button
            {
                Text = "Закрыть",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.None
            };
            btnCloseDetails.Click += (s, e) => detailsForm.Close();

            // Позиционируем кнопку по центру
            btnCloseDetails.Location = new Point(
                (buttonPanel.ClientSize.Width - btnCloseDetails.Width) / 2,
                (buttonPanel.ClientSize.Height - btnCloseDetails.Height) / 2
            );

            buttonPanel.Controls.Add(btnCloseDetails);

            // ========== Добавляем все в основной контейнер ==========
            mainContainer.Controls.Add(headerPanel, 0, 0);
            mainContainer.Controls.Add(infoPanel, 0, 1);
            mainContainer.Controls.Add(buttonPanel, 0, 2);

            // ========== Добавляем основной контейнер на форму ==========
            detailsForm.Controls.Add(mainContainer);

            // Событие для изменения размера таблицы при изменении размера панели
            infoPanel.Resize += (s, e) =>
            {
                infoTable.Width = infoPanel.ClientSize.Width;
            };

            // Применяем стили к кнопке
            btnCloseDetails.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
            btnCloseDetails.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 110, 180);

            // Рассчитываем оптимальную высоту формы
            int totalHeight = 70 + (fields.Count * 40) + 70;
            detailsForm.Height = Math.Min(650, Math.Max(400, totalHeight));

            detailsForm.AcceptButton = btnCloseDetails;
            detailsForm.ShowDialog();
        }
    }

}