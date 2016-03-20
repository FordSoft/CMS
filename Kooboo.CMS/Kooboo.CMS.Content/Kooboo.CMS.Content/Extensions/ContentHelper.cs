using Kooboo.CMS.Common;
using Kooboo.CMS.Common.Runtime;
using Kooboo.CMS.Content.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Kooboo.Extensions.Extensions;
using System.Reflection;
using Kooboo.CMS.Content.Persistence.Default;

namespace Kooboo.CMS.Content.Extensions
{
    public static class ContentHelper
    {
        public static ITimeZoneHelper TimeZoneHelper = EngineContext.Current.Resolve<ITimeZoneHelper>();

        public static DateTime FixUTCDateTime(DateTime value)
        {
            var dt = new DateTime(((DateTime)value).Ticks, DateTimeKind.Utc);
            return TimeZoneHelper.ConvertToLocalTime(dt, TimeZoneInfo.Utc);            
        }

        public static TextContent ToContent(Schema schema, JToken jToken, TextContent content)
        {
            foreach (var property in jToken)
            {
                if (property.Type != JTokenType.Property)
                    continue;
                if (property.Type != JTokenType.Property)
                    continue;

                var prop = (JProperty)property;
                var name = prop.Name;

                var column = schema.GetActualSchema().AllColumns.FirstOrDefault(c => c.Name == name);
                if (column == null)
                    continue;

                var value = DataTypeHelper.ParseValue(column.DataType, jToken.GetValue<string>(name), false);

                if (value is DateTime)
                {
                    value = FixUTCDateTime((DateTime)value);
                }

                content[column.Name] = value;
            }
            return content;
        }

        public static Dictionary<Type, PropertyInfo[]> _properties = new Dictionary<Type, PropertyInfo[]>();

        public static T ToContent<T>(object remoteContent, T content) where T : ContentBase
        {
            PropertyInfo[] properties;
            if (!_properties.TryGetValue(remoteContent.GetType(), out properties))
            {
                _properties.Add(remoteContent.GetType(), remoteContent.GetType().GetProperties());
                return ToContent<T>(remoteContent, content);
            }
            
            foreach (var property in properties)
            {
                var fieldName = property.Name;
                var value = property.GetValue(remoteContent);
                if (value is DateTime)
                {
                    value = FixUTCDateTime((DateTime)value);
                }

                content[fieldName] = value;
            }

            return content;
        }

        public static TextContent[] ParseTextContent(Schema schema, string rawData)
        {
            IList<TextContent> contents = new List<TextContent>();

            var jObject = JObject.Parse(rawData);

            //array
            //
            if (jObject["value"] != null && jObject["value"].Type == JTokenType.Array)
            {
                var items = jObject["value"];

                //items
                //
                foreach (JToken item in items)
                {
                    contents.Add(ContentHelper.ToContent(schema, item, new TextContent()));
                }
            }
            //object
            //
            else if (jObject["value"] != null && jObject["value"].Type == JTokenType.Object)
            {
                contents.Add(ContentHelper.ToContent(schema, jObject["value"], new TextContent()));
            }

            return contents.ToArray();
        }
    }
}
