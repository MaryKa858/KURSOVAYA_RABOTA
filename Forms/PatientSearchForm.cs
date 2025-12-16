using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    public partial class PatientSearchForm : Form
    {
        private readonly IDatabaseService _dbService;
        private List<Patient> _patients;
        public Patient SelectedPatient { get; private set; }

        private TableLayoutPanel mainLayout;
        private Panel searchPanel;
        private Panel gridPanel;
        private Panel buttonPanel;

        private TextBox txtSearch;
        private Button btnSearch;
        private DataGridView dgvPatients;
        private Button btnSelect;
        private Button btnCancel;

        public PatientSearchForm(IDatabaseService dbService)
        {
            _dbService = dbService;
            InitializeComponents();
            ApplyStyles();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Поиск пациента";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 400);

            // Основной TableLayoutPanel
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Панель поиска
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // DataGridView
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Панель кнопок

            // ========== Панель поиска ==========
            searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblSearch = new Label
            {
                Text = "Поиск:",
                Location = new Point(15, 20),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtSearch = new TextBox
            {
                Location = new Point(85, 20),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 11),
                Name = "txtSearch"
            };

            btnSearch = new Button
            {
                Text = "Найти",
                Location = new Point(395, 20),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnSearch"
            };
            btnSearch.Click += BtnSearch_Click;

            searchPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch });

            // ========== Панель для DataGridView ==========
            gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = Color.White
            };

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
                BorderStyle = BorderStyle.None
            };
            dgvPatients.CellDoubleClick += DgvPatients_CellDoubleClick;
            dgvPatients.SelectionChanged += (s, e) => UpdateButtonStates();

            gridPanel.Controls.Add(dgvPatients);

            // ========== Панель кнопок ==========
            buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 70,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            btnSelect = new Button
            {
                Text = "Выбрать",
                Location = new Point(15, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnSelect",
                Enabled = false
            };
            btnSelect.Click += BtnSelect_Click;

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(145, 15),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnCancel"
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.AddRange(new Control[] { btnSelect, btnCancel });

            // ========== Добавляем элементы в mainLayout ==========
            mainLayout.Controls.Add(searchPanel, 0, 0);
            mainLayout.Controls.Add(gridPanel, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            // ========== Добавляем mainLayout на форму ==========
            this.Controls.Add(mainLayout);

            this.AcceptButton = btnSearch;
            this.CancelButton = btnCancel;

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
            dgvPatients.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvPatients.GridColor = Color.FromArgb(240, 240, 240);

            // Четные и нечетные строки
            dgvPatients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);

            // Стилизация кнопок
            var buttons = new[] { btnSearch, btnSelect, btnCancel };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                    button.Cursor = Cursors.Hand;
                }
            }

            // Стилизация текстового поля
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.BackColor = Color.White;
            txtSearch.ForeColor = Color.FromArgb(51, 51, 51);
        }

        private void UpdateButtonStates()
        {
            btnSelect.Enabled = dgvPatients.SelectedRows.Count > 0;
        }

        // Остальные методы остаются без изменений
        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            var searchTerm = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Введите критерии поиска", "Поиск",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                btnSearch.Enabled = false;
                txtSearch.Enabled = false;
                btnSelect.Enabled = false;
                btnCancel.Enabled = false;

                // Выполняем поиск в базе данных
                _patients = await _dbService.SearchPatientsAsync(searchTerm);

                // Очищаем DataGridView
                dgvPatients.DataSource = null;
                dgvPatients.Columns.Clear();
                dgvPatients.Rows.Clear();

                if (_patients == null || _patients.Count == 0)
                {
                    MessageBox.Show($"Пациенты по запросу '{searchTerm}' не найдены",
                        "Результаты поиска", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Добавляем информационную строку
                    dgvPatients.Columns.Add("Info", "Информация");
                    dgvPatients.Columns["Info"].Width = 400;
                    dgvPatients.Rows.Add("Пациенты не найдены");

                    UpdateButtonStates();
                    return;
                }

                // Создаем список для привязки
                var displayPatients = new List<object>();
                foreach (var patient in _patients)
                {
                    displayPatients.Add(new
                    {
                        FullName = patient.FullName ?? "Не указано",
                        Phone = patient.Phone ?? "Не указан",
                        Email = patient.Email ?? "Не указан",
                        FormattedBirthDate = patient.FormattedBirthDate ?? "Не указана",
                        Age = patient.Age,
                        Id = patient.Id
                    });
                }

                // Устанавливаем DataSource
                dgvPatients.AutoGenerateColumns = false;

                // Создаем столбцы
                dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "FullName",
                    HeaderText = "ФИО",
                    DataPropertyName = "FullName",
                    Width = 250
                });

                dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Phone",
                    HeaderText = "Телефон",
                    DataPropertyName = "Phone",
                    Width = 150
                });

                dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Email",
                    HeaderText = "Email",
                    DataPropertyName = "Email",
                    Width = 200
                });

                dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "FormattedBirthDate",
                    HeaderText = "Дата рождения",
                    DataPropertyName = "FormattedBirthDate",
                    Width = 120
                });

                dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Age",
                    HeaderText = "Возраст",
                    DataPropertyName = "Age",
                    Width = 80
                });

                dgvPatients.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    DataPropertyName = "Id",
                    Visible = false
                });

                // Привязываем данные
                dgvPatients.DataSource = displayPatients;

                // Сохраняем пациентов в Tag
                int rowIndex = 0;
                foreach (DataGridViewRow row in dgvPatients.Rows)
                {
                    if (rowIndex < _patients.Count)
                    {
                        row.Tag = _patients[rowIndex];
                    }
                    rowIndex++;
                }

                UpdateButtonStates();

                MessageBox.Show($"Найдено пациентов: {_patients.Count}",
                    "Результат поиска", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnSearch.Enabled = true;
                txtSearch.Enabled = true;
                btnCancel.Enabled = true;
                UpdateButtonStates();
            }
        }

        private void DgvPatients_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                SelectPatient();
            }
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            SelectPatient();
        }

        private void SelectPatient()
        {
            if (dgvPatients.SelectedRows.Count == 0) return;

            var selectedRow = dgvPatients.SelectedRows[0];

            // Получаем пациента из Tag строки
            SelectedPatient = selectedRow.Tag as Patient;

            if (SelectedPatient != null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}