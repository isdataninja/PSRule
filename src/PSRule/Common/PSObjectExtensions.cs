﻿using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Management.Automation;

namespace PSRule
{
    internal static class PSObjectExtensions
    {
        public static T PropertyValue<T>(this PSObject o, string propertyName)
        {
            if (typeof(T).IsValueType)
            {
                return (T)Convert.ChangeType(o.Properties[propertyName].Value, typeof(T));
            }

            return (T)o.Properties[propertyName].Value;
        }

        public static string ValueAsString(this PSObject o, string propertyName, bool caseSensitive)
        {
            var p = o.Properties[propertyName];

            if (p == null)
            {
                return null;
            }

            if (caseSensitive && !StringComparer.Ordinal.Equals(p.Name, propertyName))
            {
                return null;
            }

            return p.Value.ToString();
        }

        public static bool PropertyValue(this PSObject o, string propertyName, bool caseSensitive, out object value)
        {
            value = null;
            var p = o.Properties[propertyName];

            if (p == null)
            {
                return false;
            }

            if (caseSensitive && !StringComparer.Ordinal.Equals(p.Name, propertyName))
            {
                return false;
            }

            value = p.Value;
            return true;
        }

        public static string ToJson(this PSObject o)
        {
            var settings = new JsonSerializerSettings { Formatting = Formatting.None, TypeNameHandling = TypeNameHandling.None, MaxDepth = 1024, Culture = CultureInfo.InvariantCulture };
            settings.Converters.Insert(0, new PSObjectJsonConverter());
            return JsonConvert.SerializeObject(o, settings);
        }
    }
}