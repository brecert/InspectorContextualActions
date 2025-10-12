using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using InspectorContextualActions;

/// <summary>
/// Used to patch `Component` subtypes that are otherwise difficult to patch due to generics.
/// </summary>
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
    /// if the component type has been seen before we can skip it
    if (SeenTypes.Contains(type)) return;
    __instance.StartTask(() =>
    {
      if (type.IsGenericType)
      {
        var genericType = type.GetGenericTypeDefinition();
        if (genericType == typeof(ReferenceDriveReceiver<>))
        {
          var refType = type.GenericTypeArguments[0];
          InspectorContextualActionsMod.harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(ReferenceDriveReceiver<>).MakeGenericType(refType), nameof(ReferenceDriveReceiver<>.TryReceive)),
            postfix: ReferenceDriveReceiverActionsPatch_TryReceive_Postfix
          );
        }
        else if (genericType == typeof(FieldDriveReceiver<>))
        {
          var valueType = type.GenericTypeArguments[0];
          InspectorContextualActionsMod.harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(FieldDriveReceiver<>).MakeGenericType(valueType), nameof(FieldDriveReceiver<>.TryReceive)),
            postfix: FieldDriveReceiverActionsPatch_TryReceive_Postfix
          );
        }
      }
      return Task.CompletedTask;
    });
    SeenTypes.Add(type);
  }
}