using Dapper;
using System;
using System.Data;
using Npgsql;

namespace ClinicDesctop.Services
{
    public static class DapperTypeHandlers
    {
        public static void Configure()
        {
            // Регистрируем обработчики для PostgreSQL типов
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

            // Для обратной совместимости
            SqlMapper.RemoveTypeMap(typeof(DateOnly));
            SqlMapper.RemoveTypeMap(typeof(TimeOnly));
        }

        public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
        {
            public override DateOnly Parse(object value)
            {
                if (value == null || value == DBNull.Value)
                    return DateOnly.MinValue;

                if (value is DateOnly dateOnly)
                    return dateOnly;

                if (value is DateTime dateTime)
                    return DateOnly.FromDateTime(dateTime);

                if (value is string dateString)
                {
                    if (DateOnly.TryParse(dateString, out var parsedDate))
                        return parsedDate;
                }

                return DateOnly.MinValue;
            }

            public override void SetValue(IDbDataParameter parameter, DateOnly value)
            {
                parameter.Value = value.ToDateTime(TimeOnly.MinValue);
                parameter.DbType = DbType.Date;
            }
        }
    }
}