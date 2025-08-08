using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;

[HarmonyPatch]
[HarmonyPatchCategory("SlotComponentReceiverActions")]
class SlotComponentReceiverActionsPatch
{
  internal class MenuItem(LocaleString label, colorX color, Action action)
  {
    public LocaleString Label => label;
    public colorX Color => color;
    public Action Action => action;
  };

  [HarmonyPostfix]
  [HarmonyPatch(typeof(SlotComponentReceiver), nameof(SlotComponentReceiver.TryReceive))]
  static void TryReceiveActions(SlotComponentReceiver __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, bool __result)
  {
    var slot = __instance.Target.Target;
    if (slot == null) return;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => MenuItems(slot, grabbedReference)).ToArray();

    if (menuItems.Length > 0)
    {
      __instance.StartTask(async () =>
      {
        var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
        await new Updates(0); // I don't know why this is needed...

        foreach (var item in menuItems)
        {
          var contextMenuItem = menu.AddItem(item.Label, (Uri)null, item.Color);
          contextMenuItem.Button.LocalPressed += (_, _) =>
          {
            item.Action();
            menu.Close();
          };
        }
      });
      __result = true;
    }
  }

  static readonly IEnumerable<Type> CommonProceduralTextureTypes = [
    typeof(GridTexture),
    typeof(NoiseTexture),
    typeof(SimplexTexture),
    typeof(SolidColorTexture),
    typeof(UVTexture),
  ];

  static IEnumerable<MenuItem> MenuItems(Slot slot, IWorldElement? element)
  {

    switch (element)
    {
      case ISyncRef<IAssetProvider<ITexture2D>> itex2d:
        foreach (var type in CommonProceduralTextureTypes)
        {
          yield return new MenuItem(
            label: CreateLabel(type),
            color: RadiantUI_Constants.Hero.CYAN,
            action: () => itex2d.Target = (ITexture2DProvider)slot.AttachComponent(type)
          );
        }
        break;
      case ISyncRef<IAssetProvider<RenderTexture>> rtpRef:
        yield return new MenuItem(
          label: CreateLabel(typeof(RenderTextureProvider)),
          color: RadiantUI_Constants.Hero.CYAN,
          action: () => rtpRef.Target = slot.AttachComponent<RenderTextureProvider>()
        );
        break;
    }

    switch (element)
    {
      case ISyncRef syncRef:
        yield return new MenuItem(
          label: CreateLabel("Dynamic Reference"),
          color: RadiantUI_Constants.Hero.CYAN,
          action: () => CreateDynamicReference(slot, syncRef)
        );
        break;
      case IField field:
        yield return new MenuItem(
          label: CreateLabel("Dynamic Field"),
          color: RadiantUI_Constants.Hero.CYAN,
          action: () => CreateDynamicField(slot, field)
        );
        break;
    }
  }

  private static LocaleString CreateLabel(string name) => "General.Create".AsLocaleKey($"{{0}} {name}");
  private static LocaleString CreateLabel(Type type) => CreateLabel(type.GetNiceName());

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