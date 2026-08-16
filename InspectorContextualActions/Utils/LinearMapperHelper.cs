using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class LinearMapperHelper
{
  public static Dictionary<Type, IEnumerable<Type>> LinearMapperTypes = new() {
    { typeof(float), [typeof(LinearMapper1D)] },
    { typeof(float2), [typeof(LinearMapper2D)] },
    { typeof(float3), [typeof(LinearMapper3D)] },
    { typeof(float4), [typeof(LinearMapper4D)] },
    { typeof(floatQ), [typeof(LinearAngleMapper), typeof(LinearRotationMapper)] },
    { typeof(colorX), [typeof(LinearColorMapper)] },
  };

  public static bool TryGetLinearMapperTypes(IField target, [NotNullWhen(true)] out IEnumerable<Type>? types) =>
    LinearMapperTypes.TryGetValue(target.ValueType, out types);

  public static Component CreateLinearMapper(this Slot slot, Type type, IField source, IField target)
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
