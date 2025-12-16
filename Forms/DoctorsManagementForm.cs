using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    public partial class DoctorsManagementForm : Form
    {
        private readonly IDatabaseService _dbService;
        private List<Doctor> _doctors;
        private DataGridView dgvDoctors;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnSchedule;
        private Button btnClose;

        public DoctorsManagementForm(IDatabaseService dbService)
        {
            _dbService = dbService;
            InitializeComponents();
            ApplyStyles();
            LoadDoctorsAsync();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Управление врачами";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // DataGridView для врачей
            dgvDoctors = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvDoctors",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10),
                MultiSelect = false
            };
            dgvDoctors.SelectionChanged += DgvDoctors_SelectionChanged;

            // Панель кнопок
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnAdd = new Button
            {
                Text = "Добавить",
                Location = new Point(10, 15),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnAdd"
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "Редактировать",
                Location = new Point(120, 15),
                Size = new Size(120, 35),
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
                Location = new Point(250, 15),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnDelete",
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnSchedule = new Button
            {
                Text = "Расписание",
                Location = new Point(360, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnSchedule",
                Enabled = false
            };
            btnSchedule.Click += BtnSchedule_Click;

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(490, 15),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnClose"
            };
            btnClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnSchedule, btnClose });

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] { dgvDoctors, buttonPanel });

            this.ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            // Стилизация DataGridView
            dgvDoctors.BorderStyle = BorderStyle.FixedSingle;
            dgvDoctors.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvDoctors.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 90, 160);
            dgvDoctors.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvDoctors.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvDoctors.EnableHeadersVisualStyles = false;
            dgvDoctors.RowHeadersVisible = false;
            dgvDoctors.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Стилизация кнопок
            var buttons = new[] { btnAdd, btnEdit, btnDelete, btnSchedule, btnClose };
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

        private async void LoadDoctorsAsync()
        {
            try
            {
                _doctors = await _dbService.GetDoctorsAsync();
                UpdateDataGridView();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки врачей: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDataGridView()
        {
            if (_doctors == null) return;

            dgvDoctors.DataSource = null;
            dgvDoctors.Columns.Clear();

            // Создаем временный список для отображения
            var displayList = new List<object>();

            foreach (var doctor in _doctors)
            {
                displayList.Add(new
                {
                    Id = doctor.Id,
                    FullName = doctor.FullName,
                    Specialization = doctor.Specialization,
                    Phone = doctor.Phone,
                    Email = doctor.Email,
                    IsActive = doctor.IsActive,
                    DisplayStatus = doctor.IsActive ? "Активен" : "Не активен"
                });
            }

            // Устанавливаем источник данных
            dgvDoctors.DataSource = displayList;

            // Настраиваем столбцы
            dgvDoctors.Columns["Id"].Visible = false;
            dgvDoctors.Columns["FullName"].HeaderText = "ФИО";
            dgvDoctors.Columns["Specialization"].HeaderText = "Специализация";
            dgvDoctors.Columns["Phone"].HeaderText = "Телефон";
            dgvDoctors.Columns["Email"].HeaderText = "Email";
            dgvDoctors.Columns["IsActive"].Visible = false; // Скрываем булевое значение
            dgvDoctors.Columns["DisplayStatus"].HeaderText = "Статус";

            // Настройка ширины
            dgvDoctors.Columns["FullName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void UpdateButtonStates()
        {
            var hasSelection = dgvDoctors.SelectedRows.Count > 0;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
            btnSchedule.Enabled = hasSelection;
        }

        private void DgvDoctors_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var doctor = new Doctor();
            var editForm = new DoctorEditForm(_dbService, doctor, true);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadDoctorsAsync();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvDoctors.SelectedRows.Count == 0) return;

            var selectedRow = dgvDoctors.SelectedRows[0];
            var doctorId = (Guid)selectedRow.Cells["Id"].Value;

            var doctor = _doctors.FirstOrDefault(d => d.Id == doctorId);
            if (doctor == null) return;

            var editForm = new DoctorEditForm(_dbService, doctor, false);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadDoctorsAsync();
            }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvDoctors.SelectedRows.Count == 0) return;

            var selectedRow = dgvDoctors.SelectedRows[0];
            var doctorId = (Guid)selectedRow.Cells["Id"].Value;
            var doctorName = selectedRow.Cells["FullName"].Value.ToString();

            if (MessageBox.Show($"Вы уверены, что хотите удалить врача '{doctorName}'?",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    var success = await _dbService.DeleteDoctorAsync(doctorId);
                    if (success)
                    {
                        MessageBox.Show($"Врач '{doctorName}' успешно удален",
                            "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDoctorsAsync();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка удаления врача",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления врача: {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            if (dgvDoctors.SelectedRows.Count == 0) return;

            var selectedRow = dgvDoctors.SelectedRows[0];
            var doctorId = (Guid)selectedRow.Cells["Id"].Value;
            var doctorName = selectedRow.Cells["FullName"].Value.ToString();

            var scheduleForm = new DoctorScheduleForm(_dbService, doctorId, doctorName);
            scheduleForm.ShowDialog();
        }
    }

    // Вспомогательная форма для редактирования врача
    public class DoctorEditForm : Form
    {
        private readonly IDatabaseService _dbService;
        private readonly Doctor _doctor;
        private readonly bool _isNew;

        private TextBox txtFirstName;
        private TextBox txtLastName;
        private TextBox txtMiddleName;
        private TextBox txtSpecialization;
        private TextBox txtLicenseNumber;
        private TextBox txtPhone;
        private TextBox txtEmail;
        private CheckBox chkIsActive;
        private Button btnSave;
        private Button btnCancel;

        public DoctorEditForm(IDatabaseService dbService, Doctor doctor, bool isNew)
        {
            _dbService = dbService;
            _doctor = doctor;
            _isNew = isNew;
            InitializeComponent();
            ApplyStyles();
            LoadDoctorData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = _isNew ? "Добавление нового врача" : "Редактирование врача";
            this.Size = new Size(500, 450);
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
                Text = _isNew ? "Добавление нового врача" : "Редактирование врача",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 90, 160),
                Location = new Point(0, 0),
                Size = new Size(400, 30)
            };

            // Поля ввода
            int yPos = 40;
            int labelWidth = 150;
            int fieldWidth = 250;

            // Фамилия
            var lblLastName = new Label
            {
                Text = "Фамилия*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtLastName = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtLastName"
            };

            yPos += 35;

            // Имя
            var lblFirstName = new Label
            {
                Text = "Имя*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtFirstName = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtFirstName"
            };

            yPos += 35;

            // Отчество
            var lblMiddleName = new Label
            {
                Text = "Отчество:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtMiddleName = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtMiddleName"
            };

            yPos += 35;

            // Специализация
            var lblSpecialization = new Label
            {
                Text = "Специализация*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtSpecialization = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtSpecialization"
            };

            yPos += 35;

            // Лицензия
            var lblLicenseNumber = new Label
            {
                Text = "Номер лицензии*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtLicenseNumber = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtLicenseNumber"
            };

            yPos += 35;

            // Телефон
            var lblPhone = new Label
            {
                Text = "Телефон*:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtPhone = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtPhone"
            };

            yPos += 35;

            // Email
            var lblEmail = new Label
            {
                Text = "Email:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtEmail = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtEmail"
            };

            yPos += 35;

            // Активен
            var lblIsActive = new Label
            {
                Text = "Активен:",
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
                Size = new Size(450, 40)
            };

            btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(150, 0),
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
                Location = new Point(280, 0),
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
                lblLastName, txtLastName,
                lblFirstName, txtFirstName,
                lblMiddleName, txtMiddleName,
                lblSpecialization, txtSpecialization,
                lblLicenseNumber, txtLicenseNumber,
                lblPhone, txtPhone,
                lblEmail, txtEmail,
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

        private void LoadDoctorData()
        {
            if (!_isNew)
            {
                txtLastName.Text = _doctor.LastName;
                txtFirstName.Text = _doctor.FirstName;
                txtMiddleName.Text = _doctor.MiddleName;
                txtSpecialization.Text = _doctor.Specialization;
                txtLicenseNumber.Text = _doctor.LicenseNumber;
                txtPhone.Text = _doctor.Phone;
                txtEmail.Text = _doctor.Email;
                chkIsActive.Checked = _doctor.IsActive;
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtSpecialization.Text) ||
                string.IsNullOrWhiteSpace(txtLicenseNumber.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля (отмеченные *)",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Блокируем интерфейс
            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            try
            {
                // Обновление данных врача
                _doctor.LastName = txtLastName.Text.Trim();
                _doctor.FirstName = txtFirstName.Text.Trim();
                _doctor.MiddleName = txtMiddleName.Text.Trim();
                _doctor.Specialization = txtSpecialization.Text.Trim();
                _doctor.LicenseNumber = txtLicenseNumber.Text.Trim();
                _doctor.Phone = txtPhone.Text.Trim();
                _doctor.Email = txtEmail.Text.Trim();
                _doctor.IsActive = chkIsActive.Checked;

                bool success;
                if (_isNew)
                {
                    var result = await _dbService.CreateDoctorAsync(_doctor);
                    success = result != null && result.Id != Guid.Empty;
                }
                else
                {
                    success = await _dbService.UpdateDoctorAsync(_doctor);
                }

                if (success)
                {
                    MessageBox.Show(_isNew ? "Врач успешно добавлен!" : "Данные врача успешно обновлены!",
                        "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения данных врача",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения врача: {ex.Message}",
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
}