using ClinicDesctop.Models;
using ClinicDesctop.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClinicDesctop.Forms
{
    public partial class PatientRegistrationForm : Form
    {
        private readonly IDatabaseService _dbService;

        private TextBox txtFirstName;
        private TextBox txtLastName;
        private TextBox txtMiddleName;
        private TextBox txtPhone;
        private TextBox txtEmail;
        private DateTimePicker dtpBirthDate;
        private TextBox txtPassport;
        private Button btnSave;
        private Button btnCancel;

        public PatientRegistrationForm(IDatabaseService dbService)
        {
            _dbService = dbService;
            InitializeComponents();
            ApplyStyles();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Регистрация нового пациента";
            this.Size = new Size(500, 400);
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
                Text = "Регистрация нового пациента",
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
                Name = "txtPhone",
                MaxLength = 20
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

            // Дата рождения
            var lblBirthDate = new Label
            {
                Text = "Дата рождения:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            dtpBirthDate = new DateTimePicker
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Format = DateTimePickerFormat.Short,
                Name = "dtpBirthDate"
            };

            yPos += 35;

            // Паспорт
            var lblPassport = new Label
            {
                Text = "Паспорт:",
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtPassport = new TextBox
            {
                Location = new Point(labelWidth + 10, yPos),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 11),
                Name = "txtPassport",
                MaxLength = 10
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
                Text = "Зарегистрировать",
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
                lblPhone, txtPhone,
                lblEmail, txtEmail,
                lblBirthDate, dtpBirthDate,
                lblPassport, txtPassport,
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

            // Стилизация текстовых полей
            foreach (var control in this.Controls.OfType<TextBox>())
            {
                control.BorderStyle = BorderStyle.FixedSingle;
                control.BackColor = Color.White;
                control.ForeColor = Color.FromArgb(51, 51, 51);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля (отмеченные *)",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Валидация телефона
            if (!IsValidPhone(txtPhone.Text))
            {
                MessageBox.Show("Пожалуйста, введите корректный номер телефона",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus();
                return;
            }

            // Валидация email (если указан)
            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Пожалуйста, введите корректный email адрес",
                    "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            // Блокируем интерфейс
            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            try
            {
                // Создание объекта пациента
                var patient = new Patient
                {
                    FirstName = txtFirstName.Text.Trim(),
                    LastName = txtLastName.Text.Trim(),
                    MiddleName = txtMiddleName.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    BirthDate = dtpBirthDate.Value,
                    Passport = txtPassport.Text.Trim()
                };

                // Сохранение в базу данных
                var result = await _dbService.CreatePatientAsync(patient);

                MessageBox.Show($"Пациент успешно зарегистрирован!\nID: {result.Id}",
                    "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации пациента: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Разблокируем интерфейс
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Простая проверка телефона (можно расширить)
            return !string.IsNullOrWhiteSpace(phone) && phone.Length >= 10;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}