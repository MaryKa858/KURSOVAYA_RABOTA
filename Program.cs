using System;
using System.Windows.Forms;
using ClinicDesctop.Forms;
using ClinicDesctop.Services;
// Дополнительно добавьте этот using, если его нет:
using System.Linq; // для использования LINQ методов
namespace ClinicDesctop
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Настройка обработки необработанных исключений
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Конфигурируем Dapper перед созданием сервиса
                DapperTypeHandlers.Configure();

                // Загрузка конфигурации
                var configService = new ConfigurationService();
                var config = configService.LoadConfig();

                // Создание сервиса базы данных
                var dbService = new PostgreSQLService(config.ConnectionString);

                // Проверка подключения к базе данных
                if (!dbService.TestConnectionAsync().GetAwaiter().GetResult())
                {
                    MessageBox.Show("Ошибка подключения к базе данных. Проверьте настройки подключения в appsettings.json",
                        "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                try
                {
                    var testPatients = dbService.GetPatientsAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"Всего пациентов в базе: {testPatients.Count}");

                    if (testPatients.Count > 0)
                    {
                        Console.WriteLine("Первые 3 пациента:");
                        for (int i = 0; i < Math.Min(3, testPatients.Count); i++)
                        {
                            var p = testPatients[i];
                            Console.WriteLine($"{i + 1}. {p.FullName}, Телефон: {p.Phone}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при тестировании загрузки пациентов: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");

                    // Показываем сообщение пользователю
                    MessageBox.Show($"Ошибка загрузки данных из БД: {ex.Message}\n\nПриложение будет работать, но данные могут не отображаться.",
                        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Главный цикл приложения
                bool restartApp = true;

                while (restartApp)
                {
                    restartApp = false;

                    // Запуск формы авторизации
                    var loginForm = new LoginForm(dbService);
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        if (SessionManager.IsLoggedIn)
                        {
                            // Запуск главной формы
                            var mainForm = new MainForm(dbService);
                            Application.Run(mainForm);

                            // Проверяем, нужно ли перезапустить приложение
                            if (mainForm.Tag?.ToString() == "restart")
                            {
                                restartApp = true;
                                SessionManager.Logout();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Ошибка авторизации. Пожалуйста, попробуйте еще раз.",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        // Пользователь нажал Отмена
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка при запуске приложения: {ex.Message}",
                    "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Необработанная ошибка в потоке UI: {e.Exception.Message}\n{e.Exception.StackTrace}",
                "Ошибка приложения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Необработанная ошибка домена приложения: {(e.ExceptionObject as Exception)?.Message}",
                "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }

}