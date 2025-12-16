using ClinicDesctop.Models;

namespace ClinicDesctop.Services
{
    public static class SessionManager
    {
        private static User _currentUser;

        public static User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnUserChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler OnUserChanged;

        public static bool IsLoggedIn => CurrentUser != null && CurrentUser.IsActive;

        public static string UserName => CurrentUser?.FullName ?? "Гость";
        public static string UserRole => CurrentUser?.Role ?? "guest";

        public static bool HasPermission(string requiredRole)
        {
            if (!IsLoggedIn) return false;

            // Проверка ролей по иерархии
            return CurrentUser.Role switch
            {
                "admin" => true, // Админ имеет все права
                "doctor" => requiredRole == "doctor" || requiredRole == "receptionist",
                "receptionist" => requiredRole == "receptionist",
                _ => false
            };
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}