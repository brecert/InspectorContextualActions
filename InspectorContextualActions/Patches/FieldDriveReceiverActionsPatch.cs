
using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;

class FieldDriveReceiverActionsPatch
{
  internal class MenuItem(LocaleString label, Uri? icon, colorX color, Action action)
  {
    public LocaleString Label => label;
    public Uri? Icon => icon;
    public colorX Color => color;
    public Action Action => action;
  };


  // Patched from ComponentSubtypePatches
  [HarmonyPostfix]
  public static void TryReceive_Postfix(Component __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, ref bool __result)
  {
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Field").GetValue();
    var field = (IField)fieldRef.Target;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => MenuItems(field, grabbedReference)).ToArray();

    if (menuItems.Length > 0)
    {
      __instance.StartTask(async () =>
      {
        var menu = await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);
        await new Updates(0); // I don't know why this is needed...

        foreach (var item in menuItems)
        {
          var contextMenuItem = menu.AddItem(item.Label, item.Icon, item.Color);
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

  static IEnumerable<MenuItem> MenuItems(IField field, IWorldElement element)
  {
    var slot = field.FindNearestParent<Slot>();
    var typeManager = element.World.Types;

    if (TypeUtils.MatchInterface(element.GetType(), typeof(IDynamicVariable<>), out var matchedType))
    {
      var dynVar = (IDynamicVariable)element;
      var varType = matchedType!.GenericTypeArguments[0];

      if (field.IsDrivable && varType.IsAssignableFrom(field.ValueType))
      {
        yield return new MenuItem(
          label: DriveLabel("DynamicValueVariableDriver"),
          icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () => slot.CreateValueDriver(varType, dynVar.VariableName, field)
        );
      }
    }

    // element: source
    // field: target
    if (element is IField source && field.IsDrivable)
    {
      var target = field;
      if (ConvertibleDriverHelper.TryGetConvertibleDriverType(source, target, out var driverType))
      {
        yield return new MenuItem(
          label: DriveLabel(driverType!),
          icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () => slot.CreateConvertibleDriver(driverType!, source, target)
        );
      }

      if (typeManager.TryGetSwizzleDriverType(source, target, out var swizzleDriverType))
      {
        yield return new MenuItem(
          label: DriveLabel(swizzleDriverType!),
          icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () => slot.CreateSwizzleDriver(swizzleDriverType!, source, target)
        );
      }
    }

    switch (element)
    {
    }
  }

  private static LocaleString DriveLabel(string name) => "Inspector.Fields.Drive".AsLocaleKey($"{{0}} <size=50%>({name})</size>");
  private static LocaleString DriveLabel(Type type) => DriveLabel(type.GetNiceName());
}