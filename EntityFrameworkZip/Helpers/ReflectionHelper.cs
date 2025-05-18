using System.Collections;
using System.Reflection;

namespace EntityFrameworkZip.Helpers
{
    public static class ReflectionHelper
    {

        private static readonly HashSet<Type> PrimitiveTypes =
        [
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(string),
            typeof(DateTime),
        ];

        // Controleert of de eigenschap een foreign key attribuut heeft
        public static bool HasForeignKeyAttribute(PropertyInfo prop)
        {
            return
                prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute>() != null ||
                prop.GetCustomAttribute<ForeignKeyAttribute>() != null;
        }
        public static bool HasNotMappedAttribute(PropertyInfo prop)
        {
            return
                prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null ||
                prop.GetCustomAttribute<NotMappedAttribute>() != null;
        }

        // Haalt de naam op van de foreign key zoals aangegeven in het [ForeignKey("...")] attribuut
        public static string? GetForeignKeyAttributeName(PropertyInfo prop)
        {
            var attr1 = prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute>();
            var attr2 = prop.GetCustomAttribute<ForeignKeyAttribute>();
            return attr1?.Name ?? attr2?.Name;
        }

        // Controleert of de eigenschap een ICollection<T> is (gebruikelijk voor navigatiecollecties)
        private static bool IsICollection(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(ICollection<>);
        }
        private static bool IsIEnumerable(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }
        private static bool IsLazy(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Lazy<>);
        }

        // Controleert of de property publiek toegankelijk is (ten minste met een getter)
        public static bool HasPublicGetter(PropertyInfo prop)
        {
            var getter = prop.GetGetMethod(false);
            return getter != null;
        }

        // Controleert of de property een publieke of private setter heeft
        public static bool HasPublicSetter(PropertyInfo prop)
        {
            return prop.GetSetMethod(false) != null;
        }


        public static Type GetDbSetType(PropertyInfo prop)
        {
            return prop.PropertyType.GenericTypeArguments[0];
        }
        public static Type GetIEnumerableType(PropertyInfo prop)
        {
            return prop.PropertyType.GenericTypeArguments[0];
        }
        public static Type GetLazyType(PropertyInfo prop)
        {
            return prop.PropertyType.GenericTypeArguments[0];
        }

        public static bool IsNulleble(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            if (type.IsValueType)
            {
                return Nullable.GetUnderlyingType(type) != null;
            }
            else
            {
                return true; // Referentietypen zijn altijd nullable
            }
        }

        public static bool IsVirtual(PropertyInfo prop)
        {
            var method = prop.GetGetMethod(true);
            if (method == null)
                return false;
            return method.IsVirtual && !method.IsFinal;
        }
        public static bool IsDbSet(PropertyInfo prop)
        {
            if (!prop.PropertyType.IsGenericType)
                return false;

            var typeDef = prop.PropertyType.GetGenericTypeDefinition();
            return typeDef == typeof(DbSet<>);
        }

        public static bool HasExtendedForeignProperties(Type type, HashSet<Type>? visitedTypes = null)
        {
            visitedTypes ??= [];
            if (visitedTypes.Contains(type))
                return true; // Avoid cycles

            visitedTypes.Add(type);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!ReflectionHelper.HasPublicGetter(prop)) continue;
                if (!ReflectionHelper.HasPublicSetter(prop)) continue;

                if (ReflectionHelper.IsExtendedForeignEntityProperty(prop)) return true;
                if (ReflectionHelper.IsExtendedForeignListProperty(prop)) return true;
            }

            return false;
        }

        public static bool IsExtendedForeignProperty(PropertyInfo prop)
        {
            return IsExtendedForeignListProperty(prop) || IsExtendedForeignEntityProperty(prop);
        }

        public static bool IsExtendedForeignEntityProperty(PropertyInfo prop)
        {
            return
                ReflectionHelper.IsVirtual(prop) &&
                (ReflectionHelper.IsLazy(prop) || ReflectionHelper.IsIEnumerable(prop) || ReflectionHelper.IsICollection(prop));
        }

        public static bool IsExtendedForeignListProperty(PropertyInfo prop)
        {
            return
                ReflectionHelper.IsVirtual(prop) &&
                (ReflectionHelper.IsIEnumerable(prop) || ReflectionHelper.IsICollection(prop));
        }

        public static bool IsValidChildEntity(Type type, HashSet<Type>? visitedTypes = null)
        {
            visitedTypes ??= [];
            if (visitedTypes.Contains(type))
                return true; // Avoid cycles
            visitedTypes.Add(type);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!ReflectionHelper.HasPublicGetter(prop)) continue;
                if (!ReflectionHelper.HasPublicSetter(prop)) continue;

                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (ReflectionHelper.HasNotMappedAttribute(prop)) continue;
                if (ReflectionHelper.IsExtendedForeignEntityProperty(prop)) continue;
                if (ReflectionHelper.IsExtendedForeignListProperty(prop)) continue;

                if (ReflectionHelper.IsPrimitiveTypeOrEnum(propType)) continue;

                if (ReflectionHelper.IsGenericType(propType)) return false;
                if (!ReflectionHelper.HasAnyProperties(propType)) return false;

                if (propType.IsClass || propType.IsValueType)
                {
                    if (!IsValidChildEntity(propType, visitedTypes))
                        return false;
                }
                else
                {
                    return false;
                }

                return true;
            }

            return true;
        }

        public static bool IsGenericType(Type propType)
        {
            return propType.IsGenericType;
        }

        public static bool HasAnyProperties(Type propType)
        {
            return propType.GetProperties().Length != 0;
        }

        public static bool HasIEnumerableInterface(Type propType)
        {
            if (propType == typeof(string)) return false;
            var andere = typeof(IEnumerable);

            return propType.GetInterfaces()
                .Any(i => i == andere);
        }

        public static bool IsPrimitiveTypeOrEnum(Type propertyType)
        {
            return (PrimitiveTypes.Contains(propertyType)) || propertyType.IsEnum;
        }

        public static bool HasIEntityInterface(Type type)
        {
            if (type == typeof(string)) return false;

            return type.GetInterfaces()
                .Any(i => i == typeof(IEntity));
        }
    }

}

