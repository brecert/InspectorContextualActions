
using System;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class ReferenceCastHelper
{
  public static bool TryGetReferenceCastType(ISyncRef source, ISyncRef target, out Type? type)
  {
    // TODO: make leniency configurable
    if (source.TargetType != target.TargetType && target.TargetType.IsAssignableFrom(source.TargetType) || source.TargetType.IsAssignableFrom(target.TargetType))
    {
      type = typeof(ReferenceCast<,>).MakeGenericType(source.TargetType, target.TargetType);
      return true;
    }
    type = null;
    return false;
  }

  public static void CreateReferenceCast(ISyncRef source, ISyncRef target, bool writeBack = false, bool keepOriginalValue = false, bool searchForDuplicate = true)
  {
    var I = source.TargetType;
    var O = target.TargetType;
    typeof(ReferenceCopyExtensions).GetGenericMethod("CastDriveFrom", BindingFlags.Static | BindingFlags.Public, [I, O])
      .Invoke(null, [target, source, writeBack, keepOriginalValue, searchForDuplicate]);
  }
}