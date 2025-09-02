
using System;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class ReferenceCastHelper
{
  public static bool CanCastDrive(ISyncRef source, ISyncRef target)
  {
    return source.TargetType != target.TargetType && (
      target.TargetType.IsAssignableFrom(source.TargetType)
      || source.TargetType.IsAssignableFrom(target.TargetType)
      || CanConvertTo(source, target)
    );
  }

  public static void CreateReferenceCast(ISyncRef source, ISyncRef target, bool writeBack = false, bool keepOriginalValue = false, bool searchForDuplicate = true)
  {
    var I = source.TargetType;
    var O = target.TargetType;
    typeof(ReferenceCopyExtensions).GetGenericMethod("CastDriveFrom", BindingFlags.Static | BindingFlags.Public, [I, O])
      .Invoke(null, [target, source, writeBack, keepOriginalValue, searchForDuplicate]);
  }

  static bool CanConvertTo(this ISyncRef source, ISyncRef target) =>
    (bool)typeof(ReferenceCastHelper).GetMethod(nameof(CanConvertTo)).MakeGenericMethod(source.TargetType, target.TargetType).Invoke(null, [source.Target]);

  static bool CanConvertTo<I, O>(I input) => input is O;
}