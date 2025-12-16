using ClinicDesctop.Models;
using System;

namespace ClinicDesctop.Services
{
    public class UserService
    {
        private readonly IDatabaseService _dbService;
        private User _currentUser;

        public UserService(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        // Изменяем свойство на чтение/запись
        public User CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _dbService.AuthenticateUserAsync(username, password);
                if (user != null && user.IsActive)
                {
                    _currentUser = user;
                    // Также устанавливаем пользователя в SessionManager
                    SessionManager.CurrentUser = user;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Logout()
        {
            _currentUser = null;
            SessionManager.Logout();
        }

        public bool HasPermission(string requiredRole)
        {
            // Теперь используем SessionManager для проверки прав
            return SessionManager.HasPermission(requiredRole);
        }

        public string GetUserRole()
        {
            return SessionManager.UserRole;
        }

        public string GetUserName()
        {
            return SessionManager.UserName;
        }
    }
}