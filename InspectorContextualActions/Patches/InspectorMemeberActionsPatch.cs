
using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

[HarmonyPatch]
[HarmonyPatchCategory("InspectorMemberActions")]
class InspectorMemberActionsPatch
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(InspectorMemberActions), nameof(InspectorMemberActions.Pressed))]
  static void PressedActions(InspectorMemberActions __instance, IButton button, ButtonEventData eventData)
  {
    var target = __instance.Member.Target;
    var menu = __instance.LocalUser.GetUserContextMenu();

    switch (target)
    {
      case IField field:
        {
          break;
        }
    }
  }
}