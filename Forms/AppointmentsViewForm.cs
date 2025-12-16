using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    public partial class AppointmentsViewForm : Form
    {
        private readonly IDatabaseService _dbService;
        private List<Appointment> _appointments;

        private DateTimePicker dtpStartDate;
        private DateTimePicker dtpEndDate;
        private Button btnFilter;
        private Button btnToday;
        private Button btnWeek;
        private Button btnMonth;
        private DataGridView dgvAppointments;
        private Button btnViewDetails;
        private Button btnCancelAppointment;
        private Button btnClose;

        // Панели для правильного расположения
        private TableLayoutPanel mainLayout;
        private Panel filterPanel;
        private Panel gridPanel;
        private Panel buttonPanel;

        public AppointmentsViewForm(IDatabaseService dbService)
        {
            _dbService = dbService;
            InitializeComponents();
            ApplyStyles();
            LoadAppointmentsAsync(DateTime.Today, DateTime.Today.AddDays(7));
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Просмотр записей на прием";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);

            // Основной layout для правильного расположения элементов
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Панель фильтров
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Панель кнопок

            // ========== Панель фильтров ==========
            filterPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblStartDate = new Label
            {
                Text = "С:",
                Location = new Point(15, 20),
                Size = new Size(20, 25),
                Font = new Font("Segoe UI", 11)
            };

            dtpStartDate = new DateTimePicker
            {
                Location = new Point(40, 20),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10),
                Format = DateTimePickerFormat.Short,
                Name = "dtpStartDate"
            };

            var lblEndDate = new Label
            {
                Text = "По:",
                Location = new Point(170, 20),
                Size = new Size(25, 25),
                Font = new Font("Segoe UI", 11)
            };

            dtpEndDate = new DateTimePicker
            {
                Location = new Point(200, 20),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10),
                Format = DateTimePickerFormat.Short,
                Name = "dtpEndDate"
            };

            btnFilter = new Button
            {
                Text = "Применить",
                Location = new Point(330, 20),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnFilter"
            };
            btnFilter.Click += BtnFilter_Click;

            btnToday = new Button
            {
                Text = "Сегодня",
                Location = new Point(440, 20),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnToday"
            };
            btnToday.Click += BtnToday_Click;

            btnWeek = new Button
            {
                Text = "Неделя",
                Location = new Point(530, 20),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnWeek"
            };
            btnWeek.Click += BtnWeek_Click;

            btnMonth = new Button
            {
                Text = "Месяц",
                Location = new Point(620, 20),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnMonth"
            };
            btnMonth.Click += BtnMonth_Click;

            filterPanel.Controls.AddRange(new Control[] {
                lblStartDate, dtpStartDate,
                lblEndDate, dtpEndDate,
                btnFilter, btnToday, btnWeek, btnMonth
            });

            // ========== Панель для DataGridView ==========
            gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // DataGridView для записей - помещаем в Panel
            dgvAppointments = new DataGridView
            {
                Dock = DockStyle.Fill, // Заполняем всю панель
                Name = "dgvAppointments",
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
            dgvAppointments.SelectionChanged += DgvAppointments_SelectionChanged;

            // Добавляем DataGridView в панель
            gridPanel.Controls.Add(dgvAppointments);

            // ========== Панель кнопок ==========
            buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnViewDetails = new Button
            {
                Text = "Детали",
                Location = new Point(15, 15),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnViewDetails",
                Enabled = false
            };
            btnViewDetails.Click += BtnViewDetails_Click;

            btnCancelAppointment = new Button
            {
                Text = "Отменить",
                Location = new Point(125, 15),
                Size = new Size(100, 35),
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
                Location = new Point(235, 15),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnClose"
            };
            btnClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { btnViewDetails, btnCancelAppointment, btnClose });

            // ========== Добавление элементов в основной layout ==========
            mainLayout.Controls.Add(filterPanel, 0, 0);
            mainLayout.Controls.Add(gridPanel, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            // ========== Добавляем основной layout на форму ==========
            this.Controls.Add(mainLayout);

            // Установка начальных дат
            dtpStartDate.Value = DateTime.Today;
            dtpEndDate.Value = DateTime.Today.AddDays(7);

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
            dgvAppointments.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvAppointments.GridColor = Color.FromArgb(240, 240, 240);

            // Четные и нечетные строки разного цвета для лучшей читаемости
            dgvAppointments.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);

            // Стилизация кнопок
            var buttons = new[] { btnFilter, btnToday, btnWeek, btnMonth, btnViewDetails, btnCancelAppointment, btnClose };
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

        private async void LoadAppointmentsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _appointments = await _dbService.GetAppointmentsByDateRangeAsync(startDate, endDate);
                UpdateDataGridView();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDataGridView()
        {
            if (_appointments == null) return;

            dgvAppointments.DataSource = null;
            dgvAppointments.AutoGenerateColumns = false;
            dgvAppointments.Columns.Clear();

            // Создаем столбцы с привязкой
            DataGridViewColumn[] columns = new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "FormattedDateTime",
                    DataPropertyName = "FormattedDateTime",
                    HeaderText = "Дата и время",
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "PatientFullName",
                    DataPropertyName = "PatientFullName",
                    HeaderText = "Пациент",
                    Width = 250,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "DoctorFullName",
                    DataPropertyName = "DoctorFullName",
                    HeaderText = "Врач",
                    Width = 250,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "ServiceName",
                    DataPropertyName = "ServiceName",
                    HeaderText = "Услуга",
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "DisplayStatus",
                    DataPropertyName = "DisplayStatus",
                    HeaderText = "Статус",
                    Width = 100,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    DataPropertyName = "Id",
                    Visible = false
                }
            };

            dgvAppointments.Columns.AddRange(columns);
            dgvAppointments.DataSource = _appointments;

            // Подсветка строк по статусу
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
                        case "no_show":
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(211, 47, 47);
                            break;
                    }
                }
            }

            // Автонастройка ширины столбцов после заполнения
            dgvAppointments.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void UpdateButtonStates()
        {
            var hasSelection = dgvAppointments.SelectedRows.Count > 0;
            btnViewDetails.Enabled = hasSelection;
            btnCancelAppointment.Enabled = hasSelection;
        }

        private void DgvAppointments_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            var startDate = dtpStartDate.Value.Date;
            var endDate = dtpEndDate.Value.Date;

            if (startDate > endDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoadAppointmentsAsync(startDate, endDate);
        }

        private void BtnToday_Click(object sender, EventArgs e)
        {
            dtpStartDate.Value = DateTime.Today;
            dtpEndDate.Value = DateTime.Today;
            LoadAppointmentsAsync(DateTime.Today, DateTime.Today);
        }

        private void BtnWeek_Click(object sender, EventArgs e)
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // Понедельник
            var endOfWeek = startOfWeek.AddDays(6); // Воскресенье

            dtpStartDate.Value = startOfWeek;
            dtpEndDate.Value = endOfWeek;
            LoadAppointmentsAsync(startOfWeek, endOfWeek);
        }

        private void BtnMonth_Click(object sender, EventArgs e)
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            dtpStartDate.Value = startOfMonth;
            dtpEndDate.Value = endOfMonth;
            LoadAppointmentsAsync(startOfMonth, endOfMonth);
        }

        private void BtnViewDetails_Click(object sender, EventArgs e)
        {
            if (dgvAppointments.SelectedRows.Count == 0) return;

            var selectedRow = dgvAppointments.SelectedRows[0];
            var appointmentId = (Guid)selectedRow.Cells["Id"].Value;
            var appointment = _appointments.FirstOrDefault(a => a.Id == appointmentId);

            if (appointment == null) return;

            // Показ детальной информации о записи
            var detailsForm = new Form
            {
                Text = $"Детали записи: {appointment.FormattedDateTime}",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Text = $"Дата и время: {appointment.FormattedDateTime}\n" +
                       $"Пациент: {appointment.PatientFullName}\n" +
                       $"Врач: {appointment.DoctorFullName}\n" +
                       $"Услуга: {appointment.ServiceName}\n" +
                       $"Статус: {appointment.DisplayStatus}\n" +
                       $"Создано: {appointment.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                       $"Обновлено: {appointment.UpdatedAt:dd.MM.yyyy HH:mm}"
            };

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            scrollPanel.Controls.Add(textBox);

            detailsForm.Controls.Add(scrollPanel);
            detailsForm.ShowDialog();
        }

        private async void BtnCancelAppointment_Click(object sender, EventArgs e)
        {
            if (dgvAppointments.SelectedRows.Count == 0) return;

            var selectedRow = dgvAppointments.SelectedRows[0];
            var appointmentId = (Guid)selectedRow.Cells["Id"].Value;
            var appointment = _appointments.FirstOrDefault(a => a.Id == appointmentId);

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

                        // Обновляем список записей
                        var startDate = dtpStartDate.Value.Date;
                        var endDate = dtpEndDate.Value.Date;
                        LoadAppointmentsAsync(startDate, endDate);
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