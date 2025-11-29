using System;
using System.Collections.Generic;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class ConvertibleDriverHelper
{
  public static Dictionary<Type, Type> ConvertibleDriversTypeMap = new() {
    {typeof(bool), typeof(ConvertibleBoolDriver<>)},
    {typeof(byte), typeof(ConvertibleByteDriver<>)},
    {typeof(decimal), typeof(ConvertibleDecimalDriver<>)},
    {typeof(double), typeof(ConvertibleDoubleDriver<>)},
    {typeof(float), typeof(ConvertibleFloatDriver<>)},
    {typeof(int), typeof(ConvertibleIntDriver<>)},
    {typeof(long), typeof(ConvertibleLongDriver<>)},
    {typeof(sbyte), typeof(ConvertibleSbyteDriver<>)},
    {typeof(short), typeof(ConvertibleShortDriver<>)},
    {typeof(uint), typeof(ConvertibleUintDriver<>)},
    {typeof(ulong), typeof(ConvertibleUlongDriver<>)},
    {typeof(ushort), typeof(ConvertibleUshortDriver<>)},
  };

  public static bool TryGetConvertibleDriverType(IField source, IField target, out Type? type)
  {
    if (ConvertibleDriversTypeMap.TryGetValue(target.ValueType, out var driverType))
    {
      var convertibleType = driverType.MakeGenericType(source.ValueType);
      if (convertibleType.IsValidGenericType(validForInstantiation: true))
      {
        type = convertibleType;
        return true;
      }
    }
    type = null;
    return false;
  }

  public static Component CreateConvertibleDriver(this Slot slot, Type type, IField source, IField target)
  {
    var driver = slot.AttachComponent(type, beforeAttach: c =>
    {
      var traverse = Traverse.Create(c);
      traverse.Field("Source").Property("Target").SetValue(source);
      traverse.Field("Target").Property("Target").SetValue(target);
    });
    return driver;
  }
}