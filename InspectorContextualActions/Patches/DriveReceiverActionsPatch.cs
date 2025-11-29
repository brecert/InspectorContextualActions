using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;
using static FrooxEngine.UIX.Canvas;

namespace InspectorContextualActions.Patches;


[HarmonyPatchCategory("DriveReceiverActionsPatch")]
class FieldDriveReceiverActionsPatch
{
  internal class MenuItem(LocaleString label, Uri? icon, colorX color, Action action)
  {
    public LocaleString Label => label;
    public Uri? Icon => icon;
    public colorX Color => color;
    public Action Action => action;
  };

  public enum IterationState
  {
    Continue,
    Stop
  }

  [HarmonyPrefix]
  [HarmonyPatch(typeof(Canvas), nameof(Canvas.Release))]
  public static bool Release_Prefix(IEnumerable<IGrabbable> items, Component grabber, in float3 point, Dictionary<Component, InteractionData> ____currentInteractions)
  {
    if (____currentInteractions.TryGetValue(grabber, out var data) && Traverse.Create(data).Property<IUIInteractable>("CurrentInteractable").Value is { } currentInteractable)
    {
      List<IUIGrabReceiver> receivers = Pool.BorrowList<IUIGrabReceiver>();
      try
      {
        currentInteractable.Slot.GetComponentsInParents(receivers);

        foreach (var receiver in receivers)
        {
          UniLog.Log($"FOUND {receiver}");
          if (receiver.GetType() is { IsGenericType: true } type)
          {
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(FieldDriveReceiver<>))
            {
              receiver.TryReceive(items, grabber, data, in point);
              if (FieldDriveReceiver_TryReceive((Component)receiver, items, grabber, data, in point) == IterationState.Stop)
              {
                UniLog.Log($"BREAK {receiver}");
                break;
              }
            }
            else if (genericType == typeof(ReferenceDriveReceiver<>))
            {
              receiver.TryReceive(items, grabber, data, in point);
              if (ReferenceDriveReceiver_TryReceive((Component)receiver, items, grabber, data, in point) == IterationState.Stop)
              {
                UniLog.Log($"BREAK {receiver}");
                break;
              }
            }
            else
            {
              if (receiver.TryReceive(items, grabber, data, in point))
              {
                break;
              }
            }
          }
        }
      }
      finally
      {
        Pool.Return(ref receivers);
      }
    }
    return false;
  }

  // Patched from ComponentSubtypePatches
  // [HarmonyPostfix]
  public static IterationState FieldDriveReceiver_TryReceive(Component __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint)
  {
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Field").GetValue();
    var field = (IField)fieldRef.Target;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => FieldMenuItems(field, grabbedReference)).ToArray();

    return DisplayMenuItems(__instance, eventData, menuItems);
  }

  static IEnumerable<MenuItem> FieldMenuItems(IField field, IWorldElement grabbedReference)
  {
    var slot = field.FindNearestParent<Slot>();
    var typeManager = grabbedReference.World.Types;

    {
      if (TypeUtils.MatchInterface(grabbedReference.GetType(), typeof(IDynamicVariable<>), out var matchedType))
      {
        var dynVar = (IDynamicVariable)grabbedReference;
        var varType = matchedType!.GenericTypeArguments[0];

        if (field.IsDrivable && varType.IsAssignableFrom(field.ValueType))
        {
          yield return new MenuItem(
            label: DriveLabel("DynamicValueVariableDriver"),
            icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () => slot.CreateDynamicValueDriver(varType, dynVar.VariableName, field)
          );
        }
      }
    }

    {
      if (grabbedReference is IValue source && field.IsDrivable)
      {
        var target = field;
        if (ValueDriverHelper.TryGetValueDriverType(source, out var driverType))
        {
          yield return new MenuItem(
            label: DriveLabel("Driver"),
            icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () => slot.CreateValueDriver(driverType!, source, target)
          );
        }
      }
    }

    {
      // element: source
      // field: target
      if (grabbedReference is IField source && field.IsDrivable)
      {
        var target = field;
        if (ConvertibleDriverHelper.TryGetConvertibleDriverType(source, target, out var driverType) && source.ValueType != field.ValueType)
        {
          yield return new MenuItem(
            label: DriveLabel(driverType!),
            icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () => slot.CreateConvertibleDriver(driverType!, source, target)
          );
        }

        if (typeManager.TryGetSwizzleDriverType(source.ValueType, field.ValueType, out var swizzleDriverType))
        {
          yield return new MenuItem(
            label: DriveLabel(swizzleDriverType!),
            icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () => slot.CreateSwizzleDriver(swizzleDriverType!, source, target)
          );
        }
      }
    }

    {
      if (grabbedReference is IField<double> source && field is IField<float> target && target.IsDrivable)
      {
        yield return new MenuItem(
            label: DriveLabel(nameof(DoubleToFloatCopy)),
            icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () =>
            {
              slot.AttachComponent<DoubleToFloatCopy>(beforeAttach: c =>
              {
                c.Source.Target = source;
                c.Target.Target = target;
              });
            }
        );
      }
    }
  }

  public static IterationState ReferenceDriveReceiver_TryReceive(Component __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint)
  {
    var component = __instance;
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Reference").GetValue();
    var syncRef = (ISyncRef)fieldRef.Target;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => SyncRefMenuItems(syncRef, grabbedReference)).ToArray();

    return DisplayMenuItems(__instance, eventData, menuItems);
  }

  static IEnumerable<MenuItem> SyncRefMenuItems(ISyncRef syncRef, IWorldElement droppedElement)
  {
    var slot = syncRef.FindNearestParent<Slot>();

    if (TypeUtils.MatchInterface(droppedElement.GetType(), typeof(IDynamicVariable<>), out var matchedType))
    {
      var dynVar = (IDynamicVariable)droppedElement;
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

    if (droppedElement is ISyncRef source && syncRef.IsDrivable)
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

  private static IterationState DisplayMenuItems(Component __instance, InteractionData eventData, MenuItem[] menuItems)
  {
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
      return IterationState.Stop;
    }
    return IterationState.Continue;
  }

  private static LocaleString DriveLabel(string name) => "Inspector.Fields.Drive".AsLocaleKey($"{{0}} <size=50%>({name})</size>");
  private static LocaleString DriveLabel(Type type) => DriveLabel(type.GetNiceName());
}