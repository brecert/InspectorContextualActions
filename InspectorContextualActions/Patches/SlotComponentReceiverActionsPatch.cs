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
  static void TryReceiveActions(SlotComponentReceiver __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, ref bool __result)
  {
    var slot = __instance.Target.Target;
    if (slot == null) return;

    var menuItems = GrabbableHelper.GetGrabbedReferences(items).SelectMany(grabbedReference => MenuItems(slot, grabbedReference)).ToArray();

    if (menuItems.Length > 0)
    {
      __instance.StartTask(async () =>
      {
        var menu = __instance.LocalUser.IsContextMenuOpen()
          ? __instance.LocalUser.GetUserContextMenu()
          : await __instance.LocalUser.OpenContextMenu(__instance, eventData.source.Slot);

        // await new Updates(0); // I don't know why this is needed...

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

    // create field if the ref wants a field
    {
      if (element is ISyncRef syncRef)
      {
        if (TypeUtils.MatchInterface(syncRef.TargetType, typeof(IField<>), out var matchedFieldType))
        {
          var valueType = matchedFieldType!.GenericTypeArguments[0];
          var componentType = typeof(ValueField<>).MakeGenericType(valueType);
          if (componentType.IsValidGenericType(validForInstantiation: true))
          {
            yield return new MenuItem(
              label: CreateLabel("ValueField"),
              color: RadiantUI_Constants.Hero.CYAN,
              action: () =>
              {
                var field = slot.AttachComponent(componentType);
                syncRef.Target = Traverse.Create(field).Field("Value").GetValue<IWorldElement>();
              }
            );
          }
        }
        else if (TypeUtils.MatchInterface(syncRef.TargetType, typeof(ISyncRef<>), out var matchedSyncRefType))
        {
          var refType = matchedFieldType!.GenericTypeArguments[0];
          var componentType = typeof(ReferenceField<>).MakeGenericType(refType);
          if (componentType.IsValidGenericType(validForInstantiation: true))
          {
            yield return new MenuItem(
              label: CreateLabel("ValueField"),
              color: RadiantUI_Constants.Hero.CYAN,
              action: () =>
              {
                var field = slot.AttachComponent(componentType);
                syncRef.Target = Traverse.Create(field).Field("Reference").GetValue<IWorldElement>();
              }
            );
          }
        }
      }
    }

    switch (element)
    {
      case ISyncRef syncRef:
        // todo: this is bug prone, there is asymmetry between the creation and the check. unify them.
        if (typeof(DynamicReferenceVariable<>).MakeGenericType(syncRef.TargetType).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Reference Variable"),
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () => CreateDynamicReferenceVariable(slot, syncRef)
          );
        }
        if (typeof(DynamicReference<>).MakeGenericType(syncRef.TargetType).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Reference"),
            color: RadiantUI_Constants.Hero.CYAN,
            action: () => CreateDynamicReference(slot, syncRef)
          );
        }
        if (typeof(DynamicReferenceVariable<>).MakeGenericType(typeof(ISyncRef<>).MakeGenericType(syncRef.TargetType)).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Reference of ISyncRef"),
            color: RadiantUI_Constants.Hero.CYAN,
            action: () => CreateDynamicReferenceVariableOfISyncRef(slot, syncRef)
          );
        }
        break;
      case SyncType syncType:
        yield return new MenuItem(
          label: CreateLabel("Dynamic Type Variable"),
          color: RadiantUI_Constants.Hero.PURPLE,
          action: () =>
          {
            var typeVar = slot.AttachComponent<DynamicTypeVariable>();
            typeVar.Value.Value = syncType.Value;
            typeVar.Persistent = true;
            typeVar.UpdateLinking();
          }
        );
        yield return new MenuItem(
          label: CreateLabel("Dynamic Type Field"),
          color: RadiantUI_Constants.Hero.CYAN,
          action: () =>
          {
            var typeVar = slot.AttachComponent<DynamicTypeField>();
            typeVar.TargetField.Target = syncType;
            typeVar.Persistent = true;
            typeVar.UpdateLinking();
          }
        );
        break;
      case IField field:
        // todo: handle type creation errors with a "TryMakeGenericType" helper.
        if (typeof(DynamicValueVariable<>).MakeGenericType(field.ValueType).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Value Variable"),
            color: RadiantUI_Constants.Hero.PURPLE,
            action: () => CreateDynamicValueVariable(slot, field)
          );
        }
        if (typeof(DynamicField<>).MakeGenericType(field.ValueType).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Field"),
            color: RadiantUI_Constants.Hero.CYAN,
            action: () => CreateDynamicField(slot, field)
          );
        }
        if (typeof(DynamicReferenceVariable<>).MakeGenericType(typeof(IField<>).MakeGenericType(field.ValueType)).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Reference of IField"),
            color: RadiantUI_Constants.Hero.CYAN,
            action: () => CreateDynamicReferenceVariableOfIField(slot, field)
          );
        }
        if (typeof(DynamicReferenceVariable<>).MakeGenericType(typeof(IValue<>).MakeGenericType(field.ValueType)).IsValidGenericType(validForInstantiation: true))
        {
          yield return new MenuItem(
            label: CreateLabel("Dynamic Reference of IValue"),
            color: RadiantUI_Constants.Hero.CYAN,
            action: () => CreateDynamicReferenceVariableOfIValue(slot, field)
          );
        }
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

  static void CreateDynamicValueVariable(Slot slot, IField field)
  {
    InspectorContextualActions.Utils.DynamicVariableHelper.CreateDynamicValueVariable(slot, field);
  }

  static void CreateDynamicReferenceVariable(Slot slot, ISyncRef syncRef)
  {
    InspectorContextualActions.Utils.DynamicVariableHelper.CreateDynamicReferenceVariable(slot, syncRef);
  }

  static void CreateDynamicReferenceVariableOfISyncRef(Slot slot, ISyncRef syncRef)
  {
    InspectorContextualActions.Utils.DynamicVariableHelper.CreateDynamicReferenceVariableOfISyncRef(slot, syncRef);
  }

  static void CreateDynamicReferenceVariableOfIField(Slot slot, IField field)
  {
    InspectorContextualActions.Utils.DynamicVariableHelper.CreateDynamicReferenceVariableOfIField(slot, field);
  }

  static void CreateDynamicReferenceVariableOfIValue(Slot slot, IField field)
  {
    InspectorContextualActions.Utils.DynamicVariableHelper.CreateDynamicReferenceVariableOfIValue(slot, field);
  }
}