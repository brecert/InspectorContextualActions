
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class ValueDriverHelper
{
  public static bool TryGetValueDriverType(IValue source, [NotNullWhen(true)] out Type? driverType)
  {
    var type = typeof(ValueDriver<>).MakeGenericType(source.ValueType);
    if (Traverse.Create(type).Property<bool>("IsValidGenericType").Value)
    {
      driverType = type;
      return true;
    }
    driverType = null;
    return false;
  }

  public static void CreateValueDriver(this Slot slot, Type driverType, IValue source, IField target)
  {
    var driver = slot.AttachComponent(driverType);
    var traverse = new Traverse(driver);
    traverse.Field(nameof(ValueDriver<>.ValueSource)).Property("Target").SetValue(source);
    traverse.Field(nameof(ValueDriver<>.DriveTarget)).Property("Target").SetValue(target);
  }
}