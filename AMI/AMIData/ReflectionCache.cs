using System;
using System.Collections.Generic;
using System.Reflection;

namespace AMI.AMIData
{
    class ReflectionCache<T> : ReflectionCache where T : class
    {

        Dictionary<string, MethodInfo> instanceMethods;
        Dictionary<string, FieldInfo> instanceField;
        Dictionary<string, PropertyInfo> instanceProperties;

        public ReflectionCache() : base(typeof(T))
        {
            instanceMethods = new Dictionary<string, MethodInfo>();
            instanceField = new Dictionary<string, FieldInfo>();
            instanceProperties = new Dictionary<string, PropertyInfo>();
        }

        internal R Run<R>(string name, T instance, params object[] parameters) =>
            (R)GetFunction(name, instanceMethods)?.Invoke(instance, parameters);

        internal void Run(string name, T instance, params object[] parameters) =>
                GetFunction(name, instanceMethods)?.Invoke(instance, parameters);

        internal R GetValue<R>(string name, T instance)
            => (R)GetField(name, instanceField)?.GetValue(instance);

        internal R SetValue<R>(string name, R value, T instance)
        {
            FieldInfo field = GetField(name, instanceField);
            if (field != null)
            {
                field.SetValue(instance, value);
                return (R)field.GetValue(instance);
            }

            return default;
        }

        internal R GetProperty<R>(string name, T instance)
            => (R)GetProperty(name, instanceProperties)?.GetValue(instance);
    }

    class ReflectionCache
    {
        Type type;

        Dictionary<string, MethodInfo> staticMethods;
        Dictionary<string, FieldInfo> staticField;

        public ReflectionCache(Type t)
        {
            staticMethods = new Dictionary<string, MethodInfo>();
            staticField = new Dictionary<string, FieldInfo>();

            this.type = t;
        }


        //Methods
        internal MethodInfo GetFunction(string name, Dictionary<string, MethodInfo> dict)
        {
            if (dict.TryGetValue(name, out MethodInfo method)) return method;

            method = type.GetMethod(name);
            if (method != null) dict.Add(name, method);

            return method;
        }

        internal R Run<R>(string name, params object[] parameters) =>
            (R)GetFunction(name, staticMethods)?.Invoke(null, parameters);

        internal void Run(string name, params object[] parameters) =>
            GetFunction(name, staticMethods)?.Invoke(null, parameters);


        //Fields
        internal FieldInfo GetField(string name, Dictionary<string, FieldInfo> dict)
        {
            if (dict.TryGetValue(name, out FieldInfo field)) return field;

            field = type.GetField(name);
            if (field != null) dict.Add(name, field);

            return field;
        }

        internal R GetValue<R>(string name)
            => (R)GetField(name, staticField).GetValue(null);

        internal R SetValue<R>(string name, R value)
        {
            FieldInfo field = GetField(name, staticField);
            field.SetValue(null, value);
            return (R)field.GetValue(null);
        }


        internal PropertyInfo GetProperty(string name, Dictionary<string, PropertyInfo> dict)
        {
            if (dict.TryGetValue(name, out PropertyInfo field)) return field;

            field = type.GetProperty(name);
            if (field != null) dict.Add(name, field);

            return field;
        }
    }
}
