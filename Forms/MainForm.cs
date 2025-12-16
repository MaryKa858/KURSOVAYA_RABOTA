using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    public partial class MainForm : Form
    {
        private readonly IDatabaseService _dbService;
        private List<Doctor> _doctors;
        private List<Patient> _patients;
        private List<Service> _services;

        public MainForm(IDatabaseService dbService)
        {
            _dbService = dbService;

            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            InitializeComponents();
            ApplyStyles();
            ConfigureMenuBasedOnRole();

            LoadDataAsync();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            this.Text = "Система управления клиникой";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);

            // Главное меню
            var menuStrip = new MenuStrip();
            menuStrip.BackColor = Color.FromArgb(44, 62, 80);
            menuStrip.ForeColor = Color.White;
            menuStrip.Font = new Font("Segoe UI", 11);

            // Меню "Файл"
            var fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Выход", null, (s, e) => this.Close());
            menuStrip.Items.Add(fileMenu);

            // Меню "Пациенты"
            var patientsMenu = new ToolStripMenuItem("Пациенты");
            patientsMenu.DropDownItems.Add("Регистрация пациента", null, ShowPatientRegistrationForm);
            patientsMenu.DropDownItems.Add("Управление пациентами", null, ShowPatientManagementForm);
            patientsMenu.DropDownItems.Add("Поиск пациента", null, ShowPatientSearchForm);
            menuStrip.Items.Add(patientsMenu);

            // Меню "Врачи"
            var doctorsMenu = new ToolStripMenuItem("Врачи");
            doctorsMenu.DropDownItems.Add("Управление врачами", null, ShowDoctorsManagementForm);
            doctorsMenu.DropDownItems.Add("Расписание врачей", null, ShowDoctorScheduleForm);
            menuStrip.Items.Add(doctorsMenu);

            // Меню "Записи"
            var appointmentsMenu = new ToolStripMenuItem("Записи");
            appointmentsMenu.DropDownItems.Add("Новая запись", null, ShowAppointmentForm);
            appointmentsMenu.DropDownItems.Add("Просмотр записей", null, ShowAppointmentsViewForm);
            appointmentsMenu.DropDownItems.Add("Расписание на сегодня", null, ShowTodayAppointments);
            menuStrip.Items.Add(appointmentsMenu);

            // Меню "Отчеты"
            var reportsMenu = new ToolStripMenuItem("Отчеты");
            reportsMenu.DropDownItems.Add("Статистика за месяц", null, ShowMonthlyReport);
            reportsMenu.DropDownItems.Add("Загруженность врачей", null, ShowDoctorsWorkload);
            menuStrip.Items.Add(reportsMenu);

            // Меню "Справка"
            var helpMenu = new ToolStripMenuItem("Справка");
            helpMenu.DropDownItems.Add("О программе", null, ShowAboutForm);
            helpMenu.DropDownItems.Add("Руководство пользователя", null, ShowUserManual);
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;

            // Панель инструментов
            var toolStrip = new ToolStrip();
            toolStrip.BackColor = Color.FromArgb(245, 245, 245);
            toolStrip.Font = new Font("Segoe UI", 10);

            var btnNewPatient = new ToolStripButton("Новый пациент");
            try
            {
                btnNewPatient.Image = SystemIcons.Information.ToBitmap();
            }
            catch
            {
                btnNewPatient.Image = null;
            }
            btnNewPatient.Click += (s, e) => ShowPatientRegistrationForm(s, e);
            toolStrip.Items.Add(btnNewPatient);

            var btnFindPatient = new ToolStripButton("Найти пациента");
            try
            {
                btnFindPatient.Image = SystemIcons.Information.ToBitmap();
            }
            catch
            {
                btnFindPatient.Image = null;
            }
            btnFindPatient.Click += (s, e) => ShowPatientSearchForm(s, e);
            toolStrip.Items.Add(btnFindPatient);

            var btnNewAppointment = new ToolStripButton("Новая запись");
            btnNewAppointment.Image = SystemIcons.Shield.ToBitmap();
            btnNewAppointment.Click += (s, e) => ShowAppointmentForm(s, e);
            toolStrip.Items.Add(btnNewAppointment);

            toolStrip.Items.Add(new ToolStripSeparator());

            var btnTodaySchedule = new ToolStripButton("Сегодня");
            btnTodaySchedule.Image = SystemIcons.Information.ToBitmap();
            btnTodaySchedule.Click += (s, e) => ShowTodayAppointments(s, e);
            toolStrip.Items.Add(btnTodaySchedule);

            var btnDoctorSchedule = new ToolStripButton("Расписание");
            try
            {
                btnDoctorSchedule.Image = SystemIcons.Application.ToBitmap();
            }
            catch
            {
                btnDoctorSchedule.Image = null;
            }
            btnDoctorSchedule.Click += (s, e) => ShowDoctorScheduleForm(s, e);
            toolStrip.Items.Add(btnDoctorSchedule);

            // Панель статуса
            var statusStrip = new StatusStrip();
            var lblStatus = new ToolStripStatusLabel("Готов");
            var lblUser = new ToolStripStatusLabel($"Пользователь: {SessionManager.UserName} ({SessionManager.UserRole})");
            lblUser.Alignment = ToolStripItemAlignment.Right;
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblUser });

            // Контейнер с вкладками
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal,
                Font = new Font("Segoe UI", 11)
            };

            // Вкладка "Панель управления"
            var tabDashboard = new TabPage("Панель управления");
            tabDashboard.BackColor = Color.White;

            var dashboardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            var welcomeLabel = new Label
            {
                Text = $"Добро пожаловать, {SessionManager.UserName}!",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 90, 160),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var statsPanel = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(400, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            var statsTitle = new Label
            {
                Text = "Статистика за сегодня:",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            var lblPatientsToday = new Label
            {
                Text = "Пациентов сегодня: 0",
                Location = new Point(10, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Name = "lblPatientsToday"
            };

            var lblAppointmentsToday = new Label
            {
                Text = "Записей на сегодня: 0",
                Location = new Point(10, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Name = "lblAppointmentsToday"
            };

            var lblDoctorsActive = new Label
            {
                Text = "Активных врачей: 0",
                Location = new Point(10, 100),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Name = "lblDoctorsActive"
            };

            statsPanel.Controls.AddRange(new Control[] { statsTitle, lblPatientsToday, lblAppointmentsToday, lblDoctorsActive });

            // Быстрые действия
            var quickActionsPanel = new Panel
            {
                Location = new Point(440, 70),
                Size = new Size(300, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            var quickActionsTitle = new Label
            {
                Text = "Быстрые действия:",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            var btnQuickNewPatient = new Button
            {
                Text = "Новый пациент",
                Location = new Point(10, 40),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnQuickNewPatient.Click += (s, e) => ShowPatientRegistrationForm(s, e);

            var btnQuickFindPatient = new Button
            {
                Text = "Найти пациента",
                Location = new Point(10, 85),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnQuickFindPatient.Click += (s, e) => ShowPatientSearchForm(s, e);

            var btnQuickNewAppointment = new Button
            {
                Text = "Новая запись",
                Location = new Point(10, 130),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnQuickNewAppointment.Click += (s, e) => ShowAppointmentForm(s, e);

            var btnQuickTodaySchedule = new Button
            {
                Text = "Сегодня",
                Location = new Point(10, 175),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnQuickTodaySchedule.Click += (s, e) => ShowTodayAppointments(s, e);

            quickActionsPanel.Controls.AddRange(new Control[] {
                quickActionsTitle,
                btnQuickNewPatient,
                btnQuickFindPatient,
                btnQuickNewAppointment,
                btnQuickTodaySchedule
            });

            dashboardPanel.Controls.AddRange(new Control[] { welcomeLabel, statsPanel, quickActionsPanel });
            tabDashboard.Controls.Add(dashboardPanel);

            // Вкладка "Пациенты"
            var tabPatients = new TabPage("Пациенты");
            tabPatients.BackColor = Color.White;

            var patientsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var patientsDataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvPatients",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10)
            };

            patientsPanel.Controls.Add(patientsDataGrid);
            tabPatients.Controls.Add(patientsPanel);

            // Вкладка "Врачи"
            var tabDoctors = new TabPage("Врачи");
            tabDoctors.BackColor = Color.White;

            var doctorsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var doctorsDataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvDoctors",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10)
            };

            doctorsPanel.Controls.Add(doctorsDataGrid);
            tabDoctors.Controls.Add(doctorsPanel);

            // Вкладка "Записи"
            var tabAppointments = new TabPage("Записи");
            tabAppointments.BackColor = Color.White;

            var appointmentsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var appointmentsDataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvAppointments",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10)
            };

            appointmentsPanel.Controls.Add(appointmentsDataGrid);
            tabAppointments.Controls.Add(appointmentsPanel);

            tabControl.TabPages.AddRange(new TabPage[] { tabDashboard, tabPatients, tabDoctors, tabAppointments });

            this.Controls.AddRange(new Control[] { menuStrip, toolStrip, tabControl, statusStrip });

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ApplyStyles()
        {
            this.BackColor = Color.White;
            this.ForeColor = Color.FromArgb(51, 51, 51);

            var dataGridViews = this.Controls.OfType<DataGridView>();
            foreach (var dgv in dataGridViews)
            {
                dgv.BorderStyle = BorderStyle.FixedSingle;
                dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 90, 160);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                dgv.EnableHeadersVisualStyles = false;
                dgv.RowHeadersVisible = false;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.AllowUserToAddRows = false;
                dgv.AllowUserToDeleteRows = false;
            }

            var buttons = this.Controls.OfType<Button>();
            foreach (var button in buttons)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                button.Cursor = Cursors.Hand;
                button.Font = new Font("Segoe UI", 10);
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                _doctors = await _dbService.GetDoctorsAsync();
                _patients = await _dbService.GetPatientsAsync();
                _services = await _dbService.GetServicesAsync();

                UpdatePatientsGrid();
                UpdateDoctorsGrid();
                UpdateAppointmentsGrid();
                UpdateDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdatePatientsGrid()
        {
            var dgvPatients = this.Controls.Find("dgvPatients", true).FirstOrDefault() as DataGridView;
            if (dgvPatients == null || _patients == null) return;

            dgvPatients.DataSource = null;
            dgvPatients.AutoGenerateColumns = false;
            dgvPatients.Columns.Clear();

            dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FullName",
                DataPropertyName = "FullName",
                HeaderText = "ФИО",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Phone",
                DataPropertyName = "Phone",
                HeaderText = "Телефон",
                Width = 150
            });

            dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Email",
                DataPropertyName = "Email",
                HeaderText = "Email",
                Width = 200
            });

            dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FormattedBirthDate",
                DataPropertyName = "FormattedBirthDate",
                HeaderText = "Дата рождения",
                Width = 120
            });

            dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Age",
                DataPropertyName = "Age",
                HeaderText = "Возраст",
                Width = 80
            });

            dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            });

            dgvPatients.DataSource = _patients;
        }

        private void UpdateDoctorsGrid()
        {
            var dgvDoctors = this.Controls.Find("dgvDoctors", true).FirstOrDefault() as DataGridView;
            if (dgvDoctors == null || _doctors == null) return;

            dgvDoctors.DataSource = null;
            dgvDoctors.AutoGenerateColumns = false;
            dgvDoctors.Columns.Clear();

            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FullName",
                DataPropertyName = "FullName",
                HeaderText = "ФИО",
                Width = 250,
                ReadOnly = true
            });

            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Specialization",
                DataPropertyName = "Specialization",
                HeaderText = "Специализация",
                Width = 200,
                ReadOnly = true
            });

            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LicenseNumber",
                DataPropertyName = "LicenseNumber",
                HeaderText = "Лицензия",
                Width = 150,
                ReadOnly = true
            });

            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Phone",
                DataPropertyName = "Phone",
                HeaderText = "Телефон",
                Width = 150,
                ReadOnly = true
            });

            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Email",
                DataPropertyName = "Email",
                HeaderText = "Email",
                Width = 200,
                ReadOnly = true
            });

            var statusColumn = new DataGridViewTextBoxColumn
            {
                Name = "StatusColumn",
                HeaderText = "Статус",
                Width = 100,
                ReadOnly = true
            };
            dgvDoctors.Columns.Add(statusColumn);

            dgvDoctors.DataSource = _doctors;

            foreach (DataGridViewRow row in dgvDoctors.Rows)
            {
                var doctor = _doctors[row.Index];
                row.Cells["StatusColumn"].Value = doctor.IsActive ? "Активен" : "Не активен";
            }
        }

        private async void UpdateAppointmentsGrid()
        {
            var dgvAppointments = this.Controls.Find("dgvAppointments", true).FirstOrDefault() as DataGridView;
            if (dgvAppointments == null) return;

            try
            {
                var appointments = await _dbService.GetAppointmentsByDateAsync(DateTime.Today);

                dgvAppointments.DataSource = null;
                dgvAppointments.AutoGenerateColumns = false;
                dgvAppointments.Columns.Clear();

                if (appointments == null || appointments.Count == 0)
                {
                    dgvAppointments.Columns.Add("Message", "Записи на сегодня");
                    dgvAppointments.Columns["Message"].Width = 400;
                    dgvAppointments.Rows.Add($"На {DateTime.Today:dd.MM.yyyy} нет записей");
                    return;
                }

                dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "FormattedDateTime",
                    DataPropertyName = "FormattedDateTime",
                    HeaderText = "Дата и время",
                    Width = 150
                });

                dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "PatientFullName",
                    DataPropertyName = "PatientFullName",
                    HeaderText = "Пациент",
                    Width = 200
                });

                dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "DoctorFullName",
                    DataPropertyName = "DoctorFullName",
                    HeaderText = "Врач",
                    Width = 200
                });

                dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "ServiceName",
                    DataPropertyName = "ServiceName",
                    HeaderText = "Услуга",
                    Width = 150
                });

                dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "DisplayStatus",
                    DataPropertyName = "DisplayStatus",
                    HeaderText = "Статус",
                    Width = 100
                });

                dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    DataPropertyName = "Id",
                    Visible = false
                });

                dgvAppointments.DataSource = appointments;

                foreach (DataGridViewRow row in dgvAppointments.Rows)
                {
                    var appointment = row.DataBoundItem as Appointment;
                    if (appointment != null)
                    {
                        switch (appointment.Status)
                        {
                            case "scheduled":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(227, 242, 253);
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(21, 101, 192);
                                break;
                            case "completed":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 232);
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(46, 125, 50);
                                break;
                            case "cancelled":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(245, 124, 0);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDashboardStats()
        {
            var lblPatientsToday = this.Controls.Find("lblPatientsToday", true).FirstOrDefault() as Label;
            var lblAppointmentsToday = this.Controls.Find("lblAppointmentsToday", true).FirstOrDefault() as Label;
            var lblDoctorsActive = this.Controls.Find("lblDoctorsActive", true).FirstOrDefault() as Label;

            if (lblPatientsToday != null)
                lblPatientsToday.Text = $"Всего пациентов: {_patients?.Count ?? 0}";

            if (lblDoctorsActive != null)
                lblDoctorsActive.Text = $"Активных врачей: {_doctors?.Count(d => d.IsActive) ?? 0}";
        }

        private void ShowPatientRegistrationForm(object sender, EventArgs e)
        {
            if (!SessionManager.HasPermission("receptionist"))
            {
                MessageBox.Show("У вас недостаточно прав для выполнения этой операции. Только регистратор может регистрировать пациентов.",
                    "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new PatientRegistrationForm(_dbService);
            form.ShowDialog();
            LoadDataAsync();
        }

        private void ShowPatientManagementForm(object sender, EventArgs e)
        {
            if (!SessionManager.HasPermission("receptionist"))
            {
                MessageBox.Show("У вас недостаточно прав для выполнения этой операции. Только регистратор может управлять пациентами.",
                    "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new PatientManagementForm(_dbService);
            form.ShowDialog();
            LoadDataAsync();
        }

        private void ShowPatientSearchForm(object sender, EventArgs e)
        {
            var form = new PatientSearchForm(_dbService);

            if (form.ShowDialog() == DialogResult.OK)
            {
                var selectedPatient = form.SelectedPatient;
                if (selectedPatient != null)
                {
                    MessageBox.Show($"Выбран пациент: {selectedPatient.FullName}\n" +
                                  $"Телефон: {selectedPatient.Phone}\n" +
                                  $"Возраст: {selectedPatient.Age} лет",
                                  "Выбран пациент",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);

                    var result = MessageBox.Show($"Создать запись для пациента {selectedPatient.FullName}?",
                        "Создание записи",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        var appointmentForm = new AppointmentForm(_dbService);
                        appointmentForm.ShowDialog();
                    }
                }
            }

            LoadDataAsync();
        }

        private void ShowDoctorsManagementForm(object sender, EventArgs e)
        {
            if (!SessionManager.HasPermission("admin"))
            {
                MessageBox.Show("Только администраторы могут управлять врачами.",
                    "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new DoctorsManagementForm(_dbService);
            form.ShowDialog();
            LoadDataAsync();
        }

        private void ShowDoctorScheduleForm(object sender, EventArgs e)
        {
            var form = new DoctorScheduleForm(_dbService, Guid.Empty, "Выбор врача");
            form.ShowDialog();
        }

        private void ShowAppointmentForm(object sender, EventArgs e)
        {
            if (!SessionManager.HasPermission("receptionist"))
            {
                MessageBox.Show("У вас недостаточно прав для выполнения этой операции. Только регистратор может создавать записи.",
                    "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new AppointmentForm(_dbService);
            form.ShowDialog();
            LoadDataAsync();
        }

        private void ShowAppointmentsViewForm(object sender, EventArgs e)
        {
            var form = new AppointmentsViewForm(_dbService);
            form.ShowDialog();
        }

        private async void ShowTodayAppointments(object sender, EventArgs e)
        {
            try
            {
                var form = new Form
                {
                    Text = $"Расписание на {DateTime.Today:dd.MM.yyyy}",
                    Size = new Size(1000, 600),
                    StartPosition = FormStartPosition.CenterParent,
                    MinimumSize = new Size(800, 500)
                };

                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };

                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                var headerPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Height = 80,
                    Padding = new Padding(20),
                    BackColor = Color.FromArgb(44, 90, 160)
                };

                var titleLabel = new Label
                {
                    Text = $"Расписание на сегодня ({DateTime.Today:dd.MM.yyyy})",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Height = 30
                };

                var subTitleLabel = new Label
                {
                    Text = "Записи на прием и их статусы",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(200, 220, 255),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(0, 30, 0, 0),
                    Height = 20
                };

                var statsLabel = new Label
                {
                    Name = "lblStats",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(200, 220, 255),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Padding = new Padding(0, 30, 0, 0),
                    Height = 20
                };

                headerPanel.Controls.Add(titleLabel);
                headerPanel.Controls.Add(subTitleLabel);
                headerPanel.Controls.Add(statsLabel);

                var dgvAppointments = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    Name = "dgvTodayAppointments",
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

                dgvAppointments.BorderStyle = BorderStyle.FixedSingle;
                dgvAppointments.DefaultCellStyle.Font = new Font("Segoe UI", 10);
                dgvAppointments.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 90, 160);
                dgvAppointments.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvAppointments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                dgvAppointments.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvAppointments.EnableHeadersVisualStyles = false;
                dgvAppointments.RowHeadersVisible = false;
                dgvAppointments.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);

                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 60,
                    Padding = new Padding(20, 10, 20, 10),
                    BackColor = Color.FromArgb(245, 245, 245)
                };

                var btnRefresh = new Button
                {
                    Text = "Обновить",
                    Location = new Point(20, 10),
                    Size = new Size(120, 35),
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.FromArgb(44, 90, 160),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var btnPrint = new Button
                {
                    Text = "Печать",
                    Location = new Point(150, 10),
                    Size = new Size(120, 35),
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.FromArgb(39, 174, 96),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var btnClose = new Button
                {
                    Text = "Закрыть",
                    Location = new Point(280, 10),
                    Size = new Size(120, 35),
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.FromArgb(245, 245, 245),
                    ForeColor = Color.FromArgb(51, 51, 51),
                    FlatStyle = FlatStyle.Flat
                };
                btnClose.Click += (s, e) => form.Close();

                var buttons = new[] { btnRefresh, btnPrint, btnClose };
                foreach (var button in buttons)
                {
                    button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                    button.Cursor = Cursors.Hand;
                }

                buttonPanel.Controls.AddRange(new Control[] { btnRefresh, btnPrint, btnClose });

                mainLayout.Controls.Add(headerPanel, 0, 0);
                mainLayout.Controls.Add(dgvAppointments, 0, 1);

                form.Controls.Add(mainLayout);
                form.Controls.Add(buttonPanel);

                async Task LoadTodayAppointments()
                {
                    try
                    {
                        dgvAppointments.Cursor = Cursors.WaitCursor;
                        btnRefresh.Enabled = false;

                        var appointments = await _dbService.GetAppointmentsByDateAsync(DateTime.Today);

                        var totalAppointments = appointments?.Count ?? 0;
                        var scheduledCount = appointments?.Count(a => a.Status == "scheduled") ?? 0;
                        var completedCount = appointments?.Count(a => a.Status == "completed") ?? 0;
                        var cancelledCount = appointments?.Count(a => a.Status == "cancelled") ?? 0;

                        statsLabel.Text = $"Всего: {totalAppointments} | Запланировано: {scheduledCount} | Завершено: {completedCount} | Отменено: {cancelledCount}";

                        dgvAppointments.DataSource = null;
                        dgvAppointments.Columns.Clear();

                        if (appointments == null || appointments.Count == 0)
                        {
                            dgvAppointments.Columns.Add("Message", "Информация");
                            dgvAppointments.Columns["Message"].Width = 400;
                            dgvAppointments.Rows.Add($"На {DateTime.Today:dd.MM.yyyy} нет записей на прием");
                            return;
                        }

                        var columns = new DataGridViewColumn[]
                        {
                            new DataGridViewTextBoxColumn
                            {
                                Name = "Time",
                                HeaderText = "Время",
                                Width = 100,
                                ReadOnly = true
                            },
                            new DataGridViewTextBoxColumn
                            {
                                Name = "Patient",
                                HeaderText = "Пациент",
                                Width = 200,
                                ReadOnly = true
                            },
                            new DataGridViewTextBoxColumn
                            {
                                Name = "Doctor",
                                HeaderText = "Врач",
                                Width = 200,
                                ReadOnly = true
                            },
                            new DataGridViewTextBoxColumn
                            {
                                Name = "Service",
                                HeaderText = "Услуга",
                                Width = 150,
                                ReadOnly = true
                            },
                            new DataGridViewTextBoxColumn
                            {
                                Name = "Status",
                                HeaderText = "Статус",
                                Width = 120,
                                ReadOnly = true
                            },
                            new DataGridViewTextBoxColumn
                            {
                                Name = "Id",
                                HeaderText = "ID",
                                Visible = false
                            }
                        };

                        dgvAppointments.Columns.AddRange(columns);

                        var sortedAppointments = appointments
                            .OrderBy(a => a.AppointmentTime)
                            .ThenBy(a => a.PatientFullName)
                            .ToList();

                        foreach (var appointment in sortedAppointments)
                        {
                            int rowIndex = dgvAppointments.Rows.Add();

                            dgvAppointments.Rows[rowIndex].Cells["Time"].Value = appointment.FormattedTime;
                            dgvAppointments.Rows[rowIndex].Cells["Patient"].Value = appointment.PatientFullName;
                            dgvAppointments.Rows[rowIndex].Cells["Doctor"].Value = appointment.DoctorFullName;
                            dgvAppointments.Rows[rowIndex].Cells["Service"].Value = appointment.ServiceName;
                            dgvAppointments.Rows[rowIndex].Cells["Status"].Value = appointment.DisplayStatus;
                            dgvAppointments.Rows[rowIndex].Cells["Id"].Value = appointment.Id;

                            switch (appointment.Status)
                            {
                                case "scheduled":
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(227, 242, 253);
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(21, 101, 192);
                                    break;
                                case "completed":
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 232);
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(46, 125, 50);
                                    break;
                                case "cancelled":
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(245, 124, 0);
                                    break;
                                case "no_show":
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                                    dgvAppointments.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(211, 47, 47);
                                    break;
                            }

                            dgvAppointments.Rows[rowIndex].Tag = appointment;
                        }

                        dgvAppointments.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки записей: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        dgvAppointments.Columns.Clear();
                        dgvAppointments.Columns.Add("Error", "Ошибка");
                        dgvAppointments.Columns["Error"].Width = 400;
                        dgvAppointments.Rows.Add($"Ошибка загрузки: {ex.Message}");
                    }
                    finally
                    {
                        dgvAppointments.Cursor = Cursors.Default;
                        btnRefresh.Enabled = true;
                    }
                }

                btnRefresh.Click += async (s, e) => await LoadTodayAppointments();
                btnPrint.Click += (s, e) =>
                {
                    MessageBox.Show("Функция печати находится в разработке",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                await LoadTodayAppointments();

                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании формы расписания: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowMonthlyReport(object sender, EventArgs e)
        {
            if (!SessionManager.HasPermission("admin"))
            {
                MessageBox.Show("Только администраторы могут просматривать отчеты.",
                    "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Функция отчетов находится в разработке.",
                "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowDoctorsWorkload(object sender, EventArgs e)
        {
            if (!SessionManager.HasPermission("admin"))
            {
                MessageBox.Show("Только администраторы могут просматривать отчеты.",
                    "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Функция отчетов находится в разработке.",
                "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAboutForm(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Система управления клиникой\n" +
                "Версия 1.0.0\n" +
                "Разработано для автоматизации работы частных клиник\n" +
                "© 2025 Все права защищены",
                "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowUserManual(object sender, EventArgs e)
        {
            MessageBox.Show("Руководство пользователя находится в файле Руководство_пользователя.docx",
                "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void ConfigureMenuBasedOnRole()
        {
            var menuStrip = this.MainMenuStrip;
            if (menuStrip == null) return;

            string role = SessionManager.UserRole;

            foreach (ToolStripMenuItem menuItem in menuStrip.Items)
            {
                switch (menuItem.Text)
                {
                    case "Пациенты":
                        ConfigurePatientsMenu(menuItem, role);
                        break;
                    case "Врачи":
                        ConfigureDoctorsMenu(menuItem, role);
                        break;
                    case "Записи":
                        ConfigureAppointmentsMenu(menuItem, role);
                        break;
                    case "Отчеты":
                        ConfigureReportsMenu(menuItem, role);
                        break;
                }
            }
        }

        private void ConfigurePatientsMenu(ToolStripMenuItem menuItem, string role)
        {
            foreach (ToolStripItem item in menuItem.DropDownItems)
            {
                if (item.Text.Contains("Поиск"))
                {
                    item.Enabled = true;
                }
                else if (item.Text.Contains("Регистрация") || item.Text.Contains("Управление"))
                {
                    item.Enabled = (role == "admin" || role == "receptionist");
                }
                else
                {
                    item.Enabled = true;
                }
            }
        }

        private void ConfigureDoctorsMenu(ToolStripMenuItem menuItem, string role)
        {
            if (role == "admin")
            {
                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    item.Enabled = true;
                }
            }
            else if (role == "doctor" || role == "receptionist")
            {
                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    if (item.Text.Contains("Расписание"))
                    {
                        item.Enabled = true;
                    }
                    else
                    {
                        item.Enabled = false;
                    }
                }
            }
        }

        private void ConfigureAppointmentsMenu(ToolStripMenuItem menuItem, string role)
        {
            if (role == "admin" || role == "receptionist")
            {
                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    item.Enabled = true;
                }
            }
            else if (role == "doctor")
            {
                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    if (item.Text.Contains("Новая запись"))
                    {
                        item.Enabled = false;
                    }
                    else
                    {
                        item.Enabled = true;
                    }
                }
            }
        }

        private void ConfigureReportsMenu(ToolStripMenuItem menuItem, string role)
        {
            if (role == "admin")
            {
                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    item.Enabled = true;
                }
            }
            else
            {
                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    item.Enabled = false;
                }
            }
        }
    }
}