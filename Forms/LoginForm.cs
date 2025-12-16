using ClinicDesctop.Services;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace ClinicDesctop.Forms
{
    public partial class LoginForm : Form
    {
        private readonly IDatabaseService _dbService;
        private readonly UserService _userService;

        public LoginForm(IDatabaseService dbService)
        {
            InitializeComponents();
            _dbService = dbService;
            _userService = new UserService(dbService);

            // Установка иконки приложения
            this.Icon = SystemIcons.Application;

            // Настройка внешнего вида
            ApplyStyles();
        }

        private void InitializeComponents()
        {
            this.SuspendLayout();

            // Основные настройки формы
            this.Text = "Авторизация - Система управления клиникой";
            this.Size = new Size(500, 550); // Увеличим высоту для лучшего отображения
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Панель заголовка
            var headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 80), // Фиксированная позиция и размер
                BackColor = Color.FromArgb(44, 90, 160)
            };

            var titleLabel = new Label
            {
                Text = "Система управления клиникой",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(titleLabel);

            // Панель с полями ввода
            var inputPanel = new Panel
            {
                Location = new Point(0, 80), // Располагаем под headerPanel
                Size = new Size(500, 370), // Оставшееся пространство
                Padding = new Padding(40, 40, 40, 20) // Увеличим верхний отступ
            };

            // Метка имени пользователя
            var lblUsername = new Label
            {
                Text = "Имя пользователя:",
                Location = new Point(50, 40), // Сдвинем немного вниз
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11)
            };

            // Поле ввода имени пользователя
            var txtUsername = new TextBox
            {
                Location = new Point(200, 40),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 11),
                Name = "txtUsername"
            };

            // Метка пароля
            var lblPassword = new Label
            {
                Text = "Пароль:",
                Location = new Point(50, 90), // Увеличим вертикальный отступ
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 11)
            };

            // Поле ввода пароля
            var txtPassword = new TextBox
            {
                Location = new Point(200, 90),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                Name = "txtPassword"
            };

            // Кнопка входа
            var btnLogin = new Button
            {
                Text = "Войти",
                Location = new Point(150, 150), // Увеличим отступ от полей ввода
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(44, 90, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Name = "btnLogin"
            };
            btnLogin.Click += BtnLogin_Click;

            // Кнопка отмены
            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(150, 200), // Увеличим отступ
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Name = "btnCancel"
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Добавление элементов на панель
            inputPanel.Controls.AddRange(new Control[] { lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnCancel });

            // Статусная строка
            var statusStrip = new StatusStrip
            {
                Location = new Point(0, 450), // Фиксированная позиция
                Size = new Size(500, 25)
            };
            var statusLabel = new ToolStripStatusLabel
            {
                Text = "Введите учетные данные для входа в систему",
                Spring = true
            };
            statusStrip.Items.Add(statusLabel);

            // Добавление всех элементов на форму
            this.Controls.Add(headerPanel);
            this.Controls.Add(inputPanel);
            this.Controls.Add(statusStrip);

            // Настройка обработки клавиш
            this.AcceptButton = btnLogin;
            this.CancelButton = btnCancel;

            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) txtPassword.Focus();
            };

            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) btnLogin.PerformClick();
            };

            this.ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            // Стилизация кнопок
            foreach (Control control in this.Controls)
            {
                if (control is Button button)
                {
                    button.FlatAppearance.BorderColor = Color.FromArgb(225, 232, 237);
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                    button.Cursor = Cursors.Hand;
                }
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            // Поиск элементов управления - исправленный вариант
            TextBox txtUsername = null;
            TextBox txtPassword = null;
            Button btnLogin = null;
            StatusStrip statusStrip = null;

            foreach (Control control in this.Controls)
            {
                if (control is Panel panel)
                {
                    // Ищем элементы в панели ввода
                    var usernameBox = panel.Controls.Find("txtUsername", true).FirstOrDefault();
                    if (usernameBox is TextBox)
                        txtUsername = (TextBox)usernameBox;

                    var passwordBox = panel.Controls.Find("txtPassword", true).FirstOrDefault();
                    if (passwordBox is TextBox)
                        txtPassword = (TextBox)passwordBox;

                    var loginBtn = panel.Controls.Find("btnLogin", true).FirstOrDefault();
                    if (loginBtn is Button)
                        btnLogin = (Button)loginBtn;
                }
                else if (control is StatusStrip)
                {
                    statusStrip = (StatusStrip)control;
                }
            }

            if (txtUsername == null || txtPassword == null || btnLogin == null)
            {
                // Альтернативный поиск
                txtUsername = this.Controls.Find("txtUsername", true).FirstOrDefault() as TextBox;
                txtPassword = this.Controls.Find("txtPassword", true).FirstOrDefault() as TextBox;
                btnLogin = this.Controls.Find("btnLogin", true).FirstOrDefault() as Button;
                statusStrip = this.Controls.OfType<StatusStrip>().FirstOrDefault();
            }

            if (txtUsername == null || txtPassword == null || btnLogin == null)
            {
                MessageBox.Show("Ошибка инициализации элементов управления",
                    "Системная ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите имя пользователя и пароль",
                    "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Блокируем интерфейс во время авторизации
            btnLogin.Enabled = false;
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;

            try
            {
                // Обновляем статус
                if (statusStrip != null && statusStrip.Items.Count > 0)
                    statusStrip.Items[0].Text = "Проверка учетных данных...";

                // Выполняем авторизацию
                var success = await _userService.LoginAsync(username, password);

                if (success)
                {
                    if (statusStrip != null && statusStrip.Items.Count > 0)
                        statusStrip.Items[0].Text = $"Добро пожаловать, {_userService.GetUserName()}!";

                    MessageBox.Show($"Авторизация успешна!\nДобро пожаловать, {_userService.GetUserName()}!",
                        "Успешный вход", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверное имя пользователя или пароль",
                        "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    txtPassword.Clear();
                    txtPassword.Focus();

                    if (statusStrip != null && statusStrip.Items.Count > 0)
                        statusStrip.Items[0].Text = "Неверные учетные данные";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}",
                    "Ошибка системы", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (statusStrip != null && statusStrip.Items.Count > 0)
                    statusStrip.Items[0].Text = "Ошибка подключения к базе данных";
            }
            finally
            {
                // Разблокируем интерфейс
                btnLogin.Enabled = true;
                txtUsername.Enabled = true;
                txtPassword.Enabled = true;
            }
        }
    }
}