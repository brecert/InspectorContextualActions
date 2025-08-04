using System;
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;

[HarmonyPatch]
[HarmonyPatchCategory("SlotComponentReceiverActions")]
class SlotComponentReceiverActionsPatch
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(SlotComponentReceiver), nameof(SlotComponentReceiver.TryReceive))]
  static void TryReceiveActions(SlotComponentReceiver __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, bool __result)
  {
    foreach (var reference in GrabbableHelper.GetGrabbedReferences(items))
    {
      if (reference is ISyncRef<IAssetProvider<RenderTexture>> rtpRef)
      {
        __instance.StartTask(async () =>
        {
          var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
          await new Updates(0); // I don't know why this is needed...

          var item = menu.AddItem("Create RenderTextureProvider", (Uri)null, RadiantUI_Constants.Hero.CYAN);
          item.Button.LocalPressed += (_, _) =>
          {
            var slot = reference.FindNearestParent<Slot>();
            var rtp = slot.AttachComponent<RenderTextureProvider>();
            rtpRef.Target = rtp;
            __instance.LocalUser.CloseContextMenu(__instance);
          };
        });
      }

      if (reference is ISyncRef syncRef)
      {
        __instance.StartTask(async () =>
        {
          var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
          await new Updates(0); // I don't know why this is needed...

          var item = menu.AddItem("Create Dynamic Reference", (Uri)null, RadiantUI_Constants.Hero.CYAN);
          item.Button.LocalPressed += (_, _) =>
          {
            CreateDynamicReference(syncRef);
            __instance.LocalUser.CloseContextMenu(__instance);
          };
        });

      }
      else if (reference is IField field)
      {

        __instance.StartTask(async () =>
        {
          var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
          await new Updates(0); // I don't know why this is needed...

          var item = menu.AddItem("Create Dynamic Field", (Uri)null, RadiantUI_Constants.Hero.CYAN);
          item.Button.LocalPressed += (_, _) =>
          {
            CreateDynamicField(field);
            __instance.LocalUser.CloseContextMenu(__instance);
          };
        });

        __result = true;
      }
    }
  }

  static void CreateDynamicReference(ISyncRef syncRef)
  {
    var slot = syncRef.FindNearestParent<Slot>();
    var driver = (IDynamicVariable)slot.AttachComponent(typeof(DynamicReference<>).MakeGenericType(syncRef.TargetType));
    Traverse.Create(driver).Field("TargetReference").Property("Target").SetValue(syncRef);
  }

  static void CreateDynamicField(IField field)
  {
    var slot = field.FindNearestParent<Slot>();
    var driver = (IDynamicVariable)slot.AttachComponent(typeof(DynamicField<>).MakeGenericType(field.ValueType));
    Traverse.Create(driver).Field("TargetField").Property("Target").SetValue(field);
  }
}