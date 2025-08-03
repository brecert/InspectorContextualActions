using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

[HarmonyPatch]
[HarmonyPatchCategory("TryReceiveIDynamicVariable")]
class TryReceiveIDynamicVariablePatch
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(SlotComponentReceiver), nameof(SlotComponentReceiver.TryReceive))]
  static void TryReceiveIDynamicVariable(SlotComponentReceiver __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint)
  {
    foreach (var grabbable in items)
    {
      foreach (var refProxy in grabbable.Slot.GetComponentsInChildren<ReferenceProxy>())
      {
        var refType = refProxy.Reference.Target.GetType();
        UniLog.Log(refType);
        if (MatchInterface(refType, typeof(IDynamicVariable<>), out var matchedType))
        {
          var dynVar = (IDynamicVariable)refProxy.Reference.Target;
          var varType = matchedType!.GenericTypeArguments[0];
          var menu = __instance.LocalUser.GetUserContextMenu();

          __instance.StartTask(async () =>
          {
            if (!__instance.LocalUser.IsContextMenuOpen()) await menu.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
          });

          {
            var item = menu.AddItem("Create Dynamic Driver", (Uri)null, RadiantUI_Constants.Hero.CYAN);
            item.Button.LocalPressed += (_, _) =>
            {
              var x = __instance.Target.Target.AttachComponent(typeof(DynamicValueVariableDriver<>).MakeGenericType(varType));
              Traverse.Create(x).Field<Sync<string>>("VariableName").Value.Value = dynVar.VariableName;
              __instance.LocalUser.CloseContextMenu(__instance);
            };
          }
        }
      }
    }
  }

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