using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    public partial class PatientAppointmentsForm : Form
    {
        private readonly IDatabaseService _dbService;
        private readonly Patient _patient;
        private List<Appointment> _appointments;

        private DataGridView dgvAppointments;
        private Button btnNewAppointment;
        private Button btnCancelAppointment;
        private Button btnClose;

        public PatientAppointmentsForm(IDatabaseService dbService, Patient patient)
        {
            _dbService = dbService;
            _patient = patient;
            InitializeComponents();
            ApplyStyles();
            LoadAppointmentsAsync();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = $"Записи пациента: {_patient.FullName}";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // Заголовок
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblPatientInfo = new Label
            {
                Text = $"Пациент: {_patient.FullName} | Телефон: {_patient.Phone} | Дата рождения: {_patient.FormattedBirthDate}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 90, 160),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            headerPanel.Controls.Add(lblPatientInfo);

            // DataGridView для записей
            dgvAppointments = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvAppointments",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10),
                MultiSelect = false
            };
            dgvAppointments.SelectionChanged += DgvAppointments_SelectionChanged;

            // Панель кнопок
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnNewAppointment = new Button
            {
                Text = "Новая запись",
                Location = new Point(10, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnNewAppointment"
            };
            btnNewAppointment.Click += BtnNewAppointment_Click;

            btnCancelAppointment = new Button
            {
                Text = "Отменить запись",
                Location = new Point(140, 15),
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnCancelAppointment",
                Enabled = false
            };
            btnCancelAppointment.Click += BtnCancelAppointment_Click;

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(290, 15),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnClose"
            };
            btnClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { btnNewAppointment, btnCancelAppointment, btnClose });

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] { headerPanel, dgvAppointments, buttonPanel });

            this.ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            // Стилизация DataGridView
            dgvAppointments.BorderStyle = BorderStyle.FixedSingle;
            dgvAppointments.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvAppointments.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 90, 160);
            dgvAppointments.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAppointments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvAppointments.EnableHeadersVisualStyles = false;
            dgvAppointments.RowHeadersVisible = false;
            dgvAppointments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Стилизация кнопок
            var buttons = new[] { btnNewAppointment, btnCancelAppointment, btnClose };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                    button.Cursor = Cursors.Hand;
                }
            }
        }

        private async void LoadAppointmentsAsync()
        {
            try
            {
                _appointments = await _dbService.GetPatientAppointmentsAsync(_patient.Id);
                UpdateDataGridView();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей пациента: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDataGridView()
        {
            if (_appointments == null) return;

            dgvAppointments.DataSource = null;
            dgvAppointments.Columns.Clear();

            // Создание временного списка для отображения
            var displayList = new List<object>();

            foreach (var appointment in _appointments)
            {
                displayList.Add(new
                {
                    FormattedDateTime = appointment.FormattedDateTime,
                    DoctorFullName = appointment.DoctorFullName,
                    ServiceName = appointment.ServiceName,
                    DisplayStatus = appointment.DisplayStatus,
                    Status = appointment.Status,
                    Appointment = appointment  // Сохраняем объект для доступа
                });
            }

            // Устанавливаем источник данных
            dgvAppointments.DataSource = displayList;

            // Настраиваем столбцы
            dgvAppointments.Columns["FormattedDateTime"].HeaderText = "Дата и время";
            dgvAppointments.Columns["FormattedDateTime"].Width = 150;

            dgvAppointments.Columns["DoctorFullName"].HeaderText = "Врач";
            dgvAppointments.Columns["DoctorFullName"].Width = 250;

            dgvAppointments.Columns["ServiceName"].HeaderText = "Услуга";
            dgvAppointments.Columns["ServiceName"].Width = 150;

            dgvAppointments.Columns["DisplayStatus"].HeaderText = "Статус";
            dgvAppointments.Columns["DisplayStatus"].Width = 100;

            dgvAppointments.Columns["Status"].Visible = false;
            dgvAppointments.Columns["Appointment"].Visible = false;

            // Подсветка строк по статусу
            foreach (DataGridViewRow row in dgvAppointments.Rows)
            {
                var appointment = ((dynamic)row.DataBoundItem).Appointment as Appointment;
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

        private void UpdateButtonStates()
        {
            var hasSelection = dgvAppointments.SelectedRows.Count > 0;
            btnCancelAppointment.Enabled = hasSelection;
        }

        private void DgvAppointments_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void BtnNewAppointment_Click(object sender, EventArgs e)
        {
            var appointmentForm = new AppointmentForm(_dbService);
            if (appointmentForm.ShowDialog() == DialogResult.OK)
            {
                LoadAppointmentsAsync();
            }
        }

        private async void BtnCancelAppointment_Click(object sender, EventArgs e)
        {
            if (dgvAppointments.SelectedRows.Count == 0) return;

            var selectedRow = dgvAppointments.SelectedRows[0];
            var appointment = selectedRow.Cells["Appointment"].Value as Appointment;

            if (appointment == null) return;

            if (appointment.Status == "cancelled")
            {
                MessageBox.Show("Эта запись уже отменена",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите отменить запись на {appointment.FormattedDateTime}?",
                "Подтверждение отмены", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    var success = await _dbService.CancelAppointmentAsync(appointment.Id);
                    if (success)
                    {
                        MessageBox.Show($"Запись на {appointment.FormattedDateTime} успешно отменена",
                            "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadAppointmentsAsync();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка отмены записи",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отмены записи: {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}