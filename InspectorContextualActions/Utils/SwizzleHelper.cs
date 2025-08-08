
using System;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class SwizzleDriverHelper
{
  public static bool TryGetSwizzleDriverType(this TypeManager typeManager, IField source, IField target, out Type driverType) =>
    TryGetSwizzleDriverType(typeManager, source.ValueType, target.ValueType, out driverType);

  public static bool TryGetSwizzleDriverType(this TypeManager typeManager, Type fromType, Type toType, out Type driverType)
  {
    var driverTypeName = $"FrooxEngine.{fromType.GetNiceName().Capitalize()}To{toType.GetNiceName().Capitalize()}SwizzleDriver";
    driverType = typeManager.GetDataModelType(driverTypeName, allowAmbigious: true);
    return driverType != null;
  }

  public static void CreateSwizzleDriver(this Slot slot, Type driverType, IField source, IField target)
  {
    var driver = slot.AttachComponent(driverType);
    var traverse = new Traverse(driver);
    traverse.Field("Source").Property("Target").SetValue(source);
    traverse.Field("Target").Property("Target").SetValue(target);
  }
}