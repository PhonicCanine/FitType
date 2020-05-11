using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypeMagic
{
    public class FitType
    {
        static bool IsTypeBaseType(Type t)
        {
            return t == typeof(string) || t == typeof(char) || t == typeof(sbyte) ||
                   t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
                   t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(double) || t == typeof(float) || t == typeof(decimal) ||
                   t == typeof(System.String);
        }

        static List<KeyValuePair<string, object>> SearchForColumn(string search, ICollection<KeyValuePair<string, object>> baseObject)
        {
            int wildcardloc = search.IndexOf("*");
            string s = wildcardloc >= 0 ? search.Substring(0, wildcardloc)?.ToLower() : search.ToLower();

            return baseObject.Where((x) => x.Key.ToLower().StartsWith(s)).ToList();
        }

        static List<string> UniqueNumbers(int search, ICollection<KeyValuePair<string, object>> baseObject)
        {
            return baseObject.Select(x => x.Key.Substring(0, search) + String.Join("", x.Key.Substring(search).TakeWhile(z => z <= '9' && z >= '0'))).Distinct().ToList();
        }

        static Type getMemberType(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case FieldInfo fi:
                    return fi.FieldType;
                case PropertyInfo pi:
                    return pi.PropertyType;
            }
            return null;
        }

        public static bool TryFitType<T>(ICollection<KeyValuePair<string, object>> baseObject, ref T result)
        {
            try
            {
                result = CoerceFitType<T>(baseObject);
                return true;
            }
            catch (TypeFittingException ex)
            {
                return false;
            }
        }

        public static T CoerceFitType<T>(ICollection<KeyValuePair<string, object>> baseObject)
        {
            var t = typeof(T);
            
            if (baseObject.Count == 1)

                if (baseObject.First().Value.GetType() == typeof(string))
                {
                    var parseMethodQuery = t.GetRuntimeMethods().Where(m => m.Name == "Parse" && m.IsStatic).ToList();
                    if (parseMethodQuery.Any())
                        return (T)parseMethodQuery.First().Invoke(null, new[] { baseObject.First().Value });
                    if (t == typeof(string))
                        return (T)baseObject.First().Value;
                }
                else if (IsTypeBaseType(t))
                    return (T)baseObject.First().Value;

            T returnObject = Activator.CreateInstance<T>();
            MethodInfo thisMethod = MethodInfo.GetCurrentMethod().DeclaringType.GetMethod(MethodInfo.GetCurrentMethod().Name);

            PropertyInfo[] properties = t.GetTypeInfo().GetProperties().Where((p) => p.CanWrite && p.CanRead).ToArray();
            FieldInfo[] fields = t.GetTypeInfo().GetFields().Where((f) => (f.IsPublic || f.CustomAttributes.Count() > 0) && !f.IsStatic).ToArray();
            List<MemberInfo> memberInfos = fields.Cast<MemberInfo>().ToList();
            memberInfos.AddRange(properties);
            foreach (var member in memberInfos)
            {
                Type memberType = getMemberType(member);
                object value = null;
                if (member.GetCustomAttribute<PrefixAttribute>() != null)
                {
                    string prefix = member.GetCustomAttribute<PrefixAttribute>().PrefixString;
                    var pairs = SearchForColumn(prefix, baseObject);
                    if (pairs.Count == 1)
                    {
                        value = pairs.First().Value;
                        pairs.Remove(pairs.First());
                    }
                    else if (memberType.GetInterface(nameof(ICollection)) == null)
                    {
                        var gmethod = thisMethod.MakeGenericMethod(memberType);
                        try
                        {
                            value = gmethod.Invoke(null, new[] { pairs.Select((x) => new KeyValuePair<string, object>(x.Key.Substring(prefix.Length), x.Value)).ToList() });
                        }
                        catch (TargetInvocationException e)
                        {
                            throw new TypeFittingException();
                        }
                    }
                    else if (memberType.GetInterface(nameof(ICollection)) != null)
                    {
                        var itmList = Activator.CreateInstance(memberType);
                        var items = UniqueNumbers(prefix.Length - 1, pairs);
                        foreach (var itm in items)
                        {
                            var specificPairs = SearchForColumn(itm, pairs);
                            var gmethod = thisMethod.MakeGenericMethod(memberType.GetInterfaces().Where(x => x.IsGenericType).First().GetGenericArguments());
                            object v = gmethod.Invoke(null, new[] { specificPairs.Select((x) => new KeyValuePair<string, object>(x.Key.Substring(itm.Length), x.Value)).ToList() });
                            memberType.GetMethod("Add").Invoke(itmList, new[] { v });
                        }
                        value = itmList;
                    }
                }
                else
                {
                    string name = member.Name.ToLower();
                    var pairs = SearchForColumn(name, baseObject);
                    if (pairs.Count == 0)
                        throw new TypeFittingException();
                    value = pairs.First().Value;
                    pairs.Remove(pairs.First());
                }

                if (value != null && value.GetType() == typeof(string))
                {
                    if (memberType.GetRuntimeMethods().Where(m => m.Name == "Parse" && m.IsStatic).Any())
                    {
                        try
                        {
                            value = memberType.GetRuntimeMethods().Where(m => m.Name == "Parse" && m.IsStatic).First().Invoke(null, new[] { (string)value });
                        }
                        catch (TargetInvocationException e)
                        {
                            throw new TypeFittingException();
                        }
                    }
                }

                switch (member)
                {
                    case FieldInfo fi:
                        fi.SetValue(returnObject, value);
                        break;
                    case PropertyInfo pi:
                        pi.SetValue(returnObject, value);
                        break;
                }
            }

            return returnObject;
        }

    }

    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class PrefixAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string prefix;

        // This is a positional argument
        public PrefixAttribute(string positionalString)
        {
            this.prefix = positionalString;
        }

        public string PrefixString
        {
            get { return prefix; }
        }
    }

    public class TypeFittingException : Exception
    {
        public TypeFittingException() : base() {

        }
        public TypeFittingException(string desc) : base(desc)
        {

        }
    }
}
