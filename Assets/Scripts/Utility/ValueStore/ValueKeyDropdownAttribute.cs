using System;
using UnityEngine;

namespace BC.Base
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ValueKeyDropdownAttribute : PropertyAttribute
    {
        public ValueKeyDropdownAttribute()
            : this(null, null)
        {
        }

        public ValueKeyDropdownAttribute(Type valueType)
            : this(valueType, null)
        {
        }

        public ValueKeyDropdownAttribute(string pathPrefix, bool allowNone = true)
            : this(null, pathPrefix, allowNone)
        {
        }

        public ValueKeyDropdownAttribute(Type valueType, string pathPrefix, bool allowNone = true)
        {
            ValueType = valueType;
            PathPrefix = pathPrefix;
            AllowNone = allowNone;
        }

        public Type ValueType { get; }
        public string PathPrefix { get; }
        public bool AllowNone { get; }
    }
}