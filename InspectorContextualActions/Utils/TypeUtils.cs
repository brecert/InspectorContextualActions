using System;
using System.Linq;

namespace InspectorContextualActions.Utils;

public class TypeUtils
{
  public static bool MatchInterface(Type type, Type interfaceType, /* [NotNullWhen(true)] */ out Type? matchedType)
  {
    if (type == interfaceType)
    {
      matchedType = type;
      return true;
    }

    if (interfaceType.IsGenericTypeDefinition)
    {
      if (type.IsGenericType && interfaceType == type.GetGenericTypeDefinition())
      {
        matchedType = type;
      }
      else
      {
        matchedType = type.FindInterfaces((t, _) => t.IsGenericType && interfaceType == t.GetGenericTypeDefinition(), null).FirstOrDefault();
      }
    }
    else
    {
      matchedType = type.FindInterfaces((t, _) => interfaceType == t, null).FirstOrDefault();
    }

    return matchedType != null;
  }
}
