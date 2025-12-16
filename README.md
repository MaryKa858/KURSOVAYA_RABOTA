Цель проекта
Разработка комплексной системы автоматизации для сетевых частных клиник, обеспечивающей:
Централизованное управление пациентами, врачами и расписанием
Автоматизацию записи на приём с сокращением времени на 85%
Повышение прозрачности всех медицинских процессов
Интеграцию с Telegram для удобного взаимодействия с пациентами
Полное соответствие требованиям 152-ФЗ о защите персональных данных


Установка и запуск
Предварительные требования
Windows 10/11 или Windows Server 2019+
.NET 8.0 Runtime или SDK
PostgreSQL 18+ с правами на создание базы данных
4+ ГБ ОЗУ, 2+ ГБ свободного места на диске

Шаг 1: Клонирование репозитория
bash
git clone https://github.com/yourusername/clinic-management-system.git
cd clinic-management-system

Настройка базы данных PostgreSQL
-- 1. Создание базы данных
CREATE DATABASE clinic_db 
ENCODING 'UTF8' 
LC_COLLATE 'Russian_Russia.1251' 
LC_CTYPE 'Russian_Russia.1251';

-- 2. Создание пользователя (опционально)
CREATE USER clinic_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE clinic_db TO clinic_user;

-- 3. Запуск скрипта инициализации
\i database/schema.sql
\i database/seed_data.sql

Шаг 4: Сборка и запуск
Вариант A: Через Visual Studio 2022
# 1. Откройте решение
ClinicDesctop.sln
# 2. Восстановите NuGet пакеты
Tools → NuGet Package Manager → Manage NuGet Packages for Solution
# 3. Нажмите F5 для запуска

Вариант B: Через командную строку
# 1. Восстановление зависимостей
dotnet restore
# 2. Сборка проекта
dotnet build --configuration Release
# 3. Запуск приложения
cd bin/Release/net8.0-windows
ClinicDesctop.exe

Шаг 5: Первый вход в систему
Используйте тестовые учетные данные:

Логин: admin
Пароль: admin123

Рекомендуется изменить пароль после первого входа!
