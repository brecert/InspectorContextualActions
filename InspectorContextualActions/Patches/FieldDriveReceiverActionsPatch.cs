
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
  public static void TryReceive_Postfix(object __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, bool __result)
  {
    var component = (Component)__instance;
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Field").GetValue();
    var field = (IField)fieldRef.Target;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => MenuItems(field, grabbedReference)).ToArray();

    if (menuItems.Length > 0)
    {
      component.StartTask(async () =>
      {
        var menu = await component.LocalUser.OpenContextMenu(component, eventData.source.Slot);
        await new Updates(0); // I don't know why this is needed...

        foreach (var item in menuItems)
        {
          var contextMenuItem = menu.AddItem(item.Label, item.Icon, item.Color);
          contextMenuItem.Button.LocalPressed += (_, _) =>
          {
            item.Action();
            component.LocalUser.CloseContextMenu(component);
          };
        }
      });
      __result = true;
    }
  }

  static IEnumerable<MenuItem> MenuItems(IField field, IWorldElement element)
  {
    var slot = field.FindNearestParent<Slot>();

    if (TypeUtils.MatchInterface(element.GetType(), typeof(IDynamicVariable<>), out var matchedType))
    {
      var dynVar = (IDynamicVariable)element;
      var varType = matchedType!.GenericTypeArguments[0];

      if (field.IsDrivable && varType.IsAssignableFrom(field.ValueType))
      {
        yield return new MenuItem(
          label: "Inspector.Fields.Drive".AsLocaleKey(format: $"{{0}} <size=50%>(DynamicValueVariableDriver)</size>"),
          icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () => slot.CreateValueDriver(varType, dynVar.VariableName, field)
        );
      }
    }

    switch (element)
    {
    }
  }
}