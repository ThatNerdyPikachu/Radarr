using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dapper;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Datastore
{
    public static class SqlBuilderExtensions
    {
        public static bool LogSql { get; set; }

        public static SqlBuilder SelectAll(this SqlBuilder builder)
        {
            return builder.Select("*");
        }

        public static SqlBuilder SelectCount(this SqlBuilder builder)
        {
            return builder.Select("COUNT(*)");
        }

        public static SqlBuilder Where<TModel>(this SqlBuilder builder, Expression<Func<TModel, bool>> filter)
        {
            var wb = new WhereBuilder(filter);

            return builder.Where(wb.ToString(), wb.Parameters);
        }

        public static SqlBuilder OrWhere<TModel>(this SqlBuilder builder, Expression<Func<TModel, bool>> filter)
        {
            var wb = new WhereBuilder(filter);

            return builder.OrWhere(wb.ToString(), wb.Parameters);
        }

        public static SqlBuilder.Template LogQuery(this SqlBuilder.Template template)
        {
            if (LogSql)
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("==== Begin Query Trace ====");
                sb.AppendLine();
                sb.AppendLine("QUERY TEXT:");
                sb.AppendLine(template.RawSql);
                sb.AppendLine();
                sb.AppendLine("PARAMETERS:");
                foreach (var p in ((DynamicParameters)template.Parameters).ToDictionary())
                {
                    object val = (p.Value is string) ? string.Format("\"{0}\"", p.Value) : p.Value;
                    sb.AppendFormat("{0} = [{1}]", p.Key, val.ToJson() ?? "NULL").AppendLine();
                }
                sb.AppendLine();
                sb.AppendLine("==== End Query Trace ====");
                sb.AppendLine();

                Trace.Write(sb.ToString());
            }

            return template;
        }

        private static Dictionary<string, object> ToDictionary(this DynamicParameters dynamicParams)
        {
            var argsDictionary = new Dictionary<string, object>();
            var iLookup = (SqlMapper.IParameterLookup) dynamicParams;

            foreach (var paramName in dynamicParams.ParameterNames)
            {
                var value = iLookup[paramName];
                argsDictionary.Add(paramName, value);
            }

            var templates = dynamicParams.GetType().GetField("templates", BindingFlags.NonPublic | BindingFlags.Instance);
            if (templates != null)
            {
                var list = templates.GetValue(dynamicParams) as List<Object>;
                if (list != null)
                {
                    foreach (var objProps in list.Select(obj => obj.GetPropertyValuePairs().ToList()))
                    {
                        objProps.ForEach(p => argsDictionary.Add(p.Key, p.Value));
                    }
                }
            }

            return argsDictionary;
        }

        private static Dictionary<string, object> GetPropertyValuePairs(this object obj, String[] hidden = null)
        {
            var type = obj.GetType();
            var pairs = hidden == null
                ? type.GetProperties()
                .DistinctBy(propertyInfo => propertyInfo.Name)
                .ToDictionary(
                    propertyInfo => propertyInfo.Name,
                    propertyInfo => propertyInfo.GetValue(obj, null))
                : type.GetProperties()
                .Where(it => !hidden.Contains(it.Name))
                .DistinctBy(propertyInfo => propertyInfo.Name)
                .ToDictionary(
                    propertyInfo => propertyInfo.Name,
                    propertyInfo => propertyInfo.GetValue(obj, null));
            return pairs;
        }
    }
}
