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
    var slot = __instance.Target.Target;
    if (slot == null) return;
    foreach (var reference in GrabbableHelper.GetGrabbedReferences(items))
    {
      if (reference is ISyncRef<IAssetProvider<ITexture2D>> itex2d)
      {
        __instance.StartTask(async () =>
        {
          var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
          await new Updates(0); // I don't know why this is needed...

          Type[] types = [
            typeof(GridTexture),
            typeof(NoiseTexture),
            typeof(SimplexTexture),
            typeof(SolidColorTexture),
            typeof(UVTexture),
          ];

          foreach (var type in types)
          {
            var item = menu.AddItem($"Create {type.GetNiceName()}", (Uri)null, RadiantUI_Constants.Hero.CYAN);
            item.Button.LocalPressed += (_, _) =>
            {
              var tex = (ITexture2DProvider)slot.AttachComponent(type);
              itex2d.Target = tex;
              __instance.LocalUser.CloseContextMenu(__instance);
            };
          }
        });
      }

      if (reference is ISyncRef<IAssetProvider<RenderTexture>> rtpRef)
      {
        __instance.StartTask(async () =>
        {
          var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
          await new Updates(0); // I don't know why this is needed...

          var item = menu.AddItem("Create RenderTextureProvider", (Uri)null, RadiantUI_Constants.Hero.CYAN);
          item.Button.LocalPressed += (_, _) =>
          {
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
            CreateDynamicReference(slot, syncRef);
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
            CreateDynamicField(slot, field);
            __instance.LocalUser.CloseContextMenu(__instance);
          };
        });

        __result = true;
      }
    }
  }

  static void CreateDynamicReference(Slot slot, ISyncRef syncRef)
  {
    var driver = (IDynamicVariable)slot.AttachComponent(typeof(DynamicReference<>).MakeGenericType(syncRef.TargetType));
    Traverse.Create(driver).Field("TargetReference").Property("Target").SetValue(syncRef);
  }

  static void CreateDynamicField(Slot slot, IField field)
  {
    var driver = (IDynamicVariable)slot.AttachComponent(typeof(DynamicField<>).MakeGenericType(field.ValueType));
    Traverse.Create(driver).Field("TargetField").Property("Target").SetValue(field);
  }
}