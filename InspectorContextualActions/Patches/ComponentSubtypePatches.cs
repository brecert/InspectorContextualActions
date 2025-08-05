using System;
using System.Collections.Generic;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using InspectorContextualActions;

[HarmonyPatchCategory("ComponentSubtypePatches")]
[HarmonyPatch(typeof(Component), "InitializeSyncMembers")]
public static class ComponentSubtypePatches
{
  internal static readonly HashSet<Type> SeenTypes = [];

  static readonly MethodInfo ReferenceDriveReceiverActionsPatch_TryReceive_Postfix =
    AccessTools.DeclaredMethod(typeof(ReferenceDriveReceiverActionsPatch), nameof(ReferenceDriveReceiverActionsPatch.TryReceive_Postfix));

  static readonly MethodInfo FieldDriveReceiverActionsPatch_TryReceive_Postfix =
    AccessTools.DeclaredMethod(typeof(FieldDriveReceiverActionsPatch), nameof(FieldDriveReceiverActionsPatch.TryReceive_Postfix));

  public static void Postfix(Component __instance)
  {
    var type = __instance.GetType();
    if (SeenTypes.Contains(type)) return;
    if (type.IsGenericType)
    {
      var genericType = type.GetGenericTypeDefinition();
      if (genericType == typeof(ReferenceDriveReceiver<>))
      {
        var refType = type.GenericTypeArguments[0];
        InspectorContextualActionsMod.harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(ReferenceDriveReceiver<>).MakeGenericType(refType), nameof(ReferenceDriveReceiver<Slot>.TryReceive)),
          postfix: ReferenceDriveReceiverActionsPatch_TryReceive_Postfix
        );
      }
      else if (genericType == typeof(FieldDriveReceiver<>))
      {
        var valueType = type.GenericTypeArguments[0];
        InspectorContextualActionsMod.harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(FieldDriveReceiver<>).MakeGenericType(valueType), nameof(FieldDriveReceiver<dummy>.TryReceive)),
          postfix: FieldDriveReceiverActionsPatch_TryReceive_Postfix
        );
      }
    }
    SeenTypes.Add(type);
  }
}