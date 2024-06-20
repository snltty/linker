using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace common.libs
{
    public sealed class ReflectionHelper
    {
        public static IEnumerable<Type> GetInterfaceSchieves(Type type)
        {
            return GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), type);
        }
        public static IEnumerable<Type> GetInterfaceSchieves(Assembly[] assemblys, Type type)
        {
            return assemblys.SelectMany(c => c.GetTypes())
               .Where(c => !c.IsAbstract).Where(c => c.GetInterfaces().Contains(type));
        }
        public static IEnumerable<Type> GetSubClass(Assembly[] assemblys, Type type)
        {
            return assemblys.SelectMany(c => c.GetTypes())
               .Where(c => !c.IsAbstract).Where(c => c.IsSubclassOf(type));
        }


        public static IEnumerable<Type> GetEnums(Assembly[] assemblys)
        {
            return assemblys.SelectMany(c => c.GetTypes())
               .Where(c => c.IsEnum);
        }
    }
}
