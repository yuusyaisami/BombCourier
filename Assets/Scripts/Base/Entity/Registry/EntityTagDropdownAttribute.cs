using System;
using UnityEngine;

namespace BC.Base
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class EntityTagDropdownAttribute : PropertyAttribute
    {
        public EntityTagDropdownAttribute()
            : this(null, true)
        {
        }

        public EntityTagDropdownAttribute(string pathPrefix, bool allowNone = true)
        {
            PathPrefix = pathPrefix;
            AllowNone = allowNone;
        }

        public string PathPrefix { get; }
        public bool AllowNone { get; }
    }
}
