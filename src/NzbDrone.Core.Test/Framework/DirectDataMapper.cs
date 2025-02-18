﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Test.Framework
{
    public interface IDirectDataMapper
    {
        List<Dictionary<string, object>> Query(string sql);
        List<T> Query<T>(string sql) where T : new();
    }

    public class DirectDataMapper : IDirectDataMapper
    {
        private readonly IDatabase _database;

        public DirectDataMapper(IDatabase database)
        {
            _database = database;
        }
        
        public DataTable GetDataTable(string sql)
        {
            using (var connection = _database.OpenConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    var dataTable = new DataTable();
                    cmd.CommandText = sql;
                    dataTable.Load(cmd.ExecuteReader());
                    return dataTable;
                }
            }
        }

        public List<Dictionary<string, object>> Query(string sql)
        {
            var dataTable = GetDataTable(sql);

            return dataTable.Rows.Cast<DataRow>().Select(MapToDictionary).ToList();
        }

        public List<T> Query<T>(string sql) where T : new()
        {
            var dataTable = GetDataTable(sql);

            return dataTable.Rows.Cast<DataRow>().Select(MapToObject<T>).ToList();
        }

        protected Dictionary<string, object> MapToDictionary(DataRow dataRow)
        {
            var item = new Dictionary<string, object>();

            for (var i = 0; i < dataRow.Table.Columns.Count; i++)
            {
                var columnName = dataRow.Table.Columns[i].ColumnName;

                object value;
                if (dataRow.ItemArray[i] == DBNull.Value)
                {
                    value = null;
                }
                else
                {
                    value = dataRow.ItemArray[i];
                }

                item[columnName] = dataRow.ItemArray[i];
            }

            return item;
        }

        protected T MapToObject<T>(DataRow dataRow) where T : new()
        {
            var item = new T();

            for (var i = 0; i < dataRow.Table.Columns.Count; i++)
            {
                var columnName = dataRow.Table.Columns[i].ColumnName;
                var propertyInfo = typeof(T).GetProperty(columnName);

                if (propertyInfo == null)
                {
                    throw new Exception(string.Format("Column {0} doesn't exist on type {1}.", columnName, typeof(T)));
                }

                var propertyType = propertyInfo.PropertyType;

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                object value;
                if (dataRow.ItemArray[i] == DBNull.Value)
                {
                    value = null;
                }
                else if (dataRow.Table.Columns[i].DataType == typeof(string) && propertyType != typeof(string))
                {
                    value = Json.Deserialize((string)dataRow.ItemArray[i], propertyType);
                }
                else
                {
                    value = Convert.ChangeType(dataRow.ItemArray[i], propertyType);
                }

                propertyInfo.SetValue(item, value, null);
            }

            return item;
        }
    }
}
