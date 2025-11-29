
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;

[HarmonyPatch]
[HarmonyPatchCategory("ComponentSelectorTypeReceiverPatch")]
class ComponentSelectorTypeReceiverPatch
{
  internal class TextFieldData(ComponentSelector componentSelector, Type? typeArgument)
  {
    public readonly ComponentSelector ComponentSelector = componentSelector;
    public readonly Type? TypeArgument = typeArgument;
  };

  static readonly ConditionalWeakTable<TextField, TextFieldData> ComponentSelectorFields = new();

  [HarmonyPostfix]
  [HarmonyPatch(typeof(ComponentSelector), nameof(ComponentSelector.BuildUI))]
  static void BuildUI(ComponentSelector __instance, string path, bool genericType, string group, bool doNotGenerateBack, SyncRefList<TextField> ____customGenericArguments, SyncType ____genericType)
  {
    if (____genericType.Value == null || !____genericType.Value.IsGenericType) return;

    var typeArguments = ____genericType.Value.GetGenericArguments();

    foreach (var (field, type) in ____customGenericArguments.Zip(typeArguments, (a, b) => (a, b)))
    {
      ComponentSelectorFields.Add(field, new(__instance, type));
    }
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(TextField), nameof(TextField.TryReceive))]
  static void TryReceiveType(TextField __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 point, ref bool __result)
  {
    if (__result == true) return;

    if (ComponentSelectorFields.TryGetValue(__instance, out var textFieldData) && textFieldData.TypeArgument is Type type)
    {
      foreach (var reference in GrabbableHelper.GetGrabbedReferences(items))
      {
        // todo: heuristically determine if we want the inner or outer type of a field
        __instance.TargetString = reference.GetType().GetNiceName();
        __result = true;
      }
    }
  }
}