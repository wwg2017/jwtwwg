using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace JwtAndRefreshTokenAuth.Cache
{
    public static class Extend
    {
        public static HashEntry[] TohashEntries(this object obj)
        {
            PropertyInfo[] propertyInfos = obj.GetType().GetProperties();
            return propertyInfos.Select(p => new HashEntry(p.Name, p.GetValue(obj).ToString())).ToArray();
        }
        public static T ConvertFromRedis<T>(this HashEntry[] hashEntries)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            var obj = Activator.CreateInstance(typeof(T));
            foreach (var item in properties)
            {
                HashEntry entry = hashEntries.FirstOrDefault(g => g.Name.ToString().Equals(item.Name));
                if (entry.Equals(new HashEntry())) continue;
                item.SetValue(obj, Convert.ChangeType(entry.Value.ToString(), item.PropertyType));
            }
            return (T)obj;
        }
    }
    public class Person
    {
        public string Names { get; set; }
        public string Age { get; set; }
    }
}
