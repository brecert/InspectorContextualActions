
using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;

class ReferenceDriveReceiverActionsPatch
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
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Reference").GetValue();
    var syncRef = (ISyncRef)fieldRef.Target;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => MenuItems(syncRef, grabbedReference)).ToArray();

    if (menuItems.Length > 0)
    {
      __instance.StartTask(async () =>
      {
        var menu = __instance.LocalUser.IsContextMenuOpen()
          ? __instance.LocalUser.GetUserContextMenu()
          : await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);

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

  static IEnumerable<MenuItem> MenuItems(ISyncRef syncRef, IWorldElement element)
  {
    var slot = syncRef.FindNearestParent<Slot>();

    if (TypeUtils.MatchInterface(element.GetType(), typeof(IDynamicVariable<>), out var matchedType))
    {
      var dynVar = (IDynamicVariable)element;
      var varType = matchedType!.GenericTypeArguments[0];

      if (syncRef.IsDrivable && varType.IsAssignableFrom(syncRef.TargetType))
      {
        yield return new MenuItem(
          label: "Inspector.Fields.Drive".AsLocaleKey(format: $"{{0}} <size=50%>(DynamicReferenceVariableDriver)</size>"),
          icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () => slot.CreateDynamicReferenceDriver(varType, dynVar.VariableName, syncRef)
        );
      }
    }

    if (element is ISyncRef source && syncRef.IsDrivable)
    {
      var target = syncRef;

      if (ReferenceCastHelper.CanCastDrive(source, target))
      {
        yield return new MenuItem(
          label: DriveLabel("ReferenceCast"),
          icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () => ReferenceCastHelper.CreateReferenceCast(source, target)
        );
      }
    }
  }

  private static LocaleString DriveLabel(string name) => "Inspector.Fields.Drive".AsLocaleKey($"{{0}} <size=50%>({name})</size>");
  private static LocaleString DriveLabel(Type type) => DriveLabel(type.GetNiceName());
}