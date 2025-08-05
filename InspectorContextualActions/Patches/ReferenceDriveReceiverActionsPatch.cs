
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions;
using InspectorContextualActions.Utils;

[HarmonyPatchCategory("ReferenceDriveReceiverActions")]
[HarmonyPatch(typeof(Component), "InitializeSyncMembers")]
class ReferenceDriveReceiverActionsPatch
{
  internal static readonly HashSet<Type> SeenTypes = [];

  public static void Postfix(Component __instance)
  {
    var type = __instance.GetType();
    if (SeenTypes.Contains(type)) return;
    if (type.IsGenericType)
    {
      var genericType = type.GetGenericTypeDefinition();
      if (genericType == typeof(ReferenceDriveReceiver<>))
      {
        UniLog.Log($"PATCHING {type}");
        var refType = type.GenericTypeArguments[0];
        InspectorContextualActionsMod.harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(ReferenceDriveReceiver<>).MakeGenericType(refType), nameof(ReferenceDriveReceiver<Slot>.TryReceive)),
          postfix: AccessTools.DeclaredMethod(typeof(ReferenceDriveReceiverActionsPatch), nameof(TryReceive_Postfix))
        );
      }
    }
    SeenTypes.Add(type);
  }

  // [HarmonyTargetMethods]
  // public static IEnumerable<MethodBase> TargetMethods() =>
  //   GetReferenceTypes()
  //     .Select(t => AccessTools.DeclaredMethod(typeof(ReferenceDriveReceiver<>).MakeGenericType(t), nameof(ReferenceDriveReceiver<Slot>.TryReceive)))
  //     .Where(m => m != null);

  public static void TryReceive_Postfix(object __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, bool __result)
  {
    var component = (Component)__instance;
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Reference").GetValue();
    var field = (ISyncRef)fieldRef.Target;

    foreach (var reference in GrabbableHelper.GetGrabbedReferences(items))
    {
      UniLog.Log(reference);
      if (TypeUtils.MatchInterface(reference.GetType(), typeof(IDynamicVariable<>), out var matchedType))
      {
        var dynVar = (IDynamicVariable)reference;
        var varType = matchedType!.GenericTypeArguments[0];

        if (varType.IsAssignableFrom(field.TargetType))
        {
          component.StartTask(async () =>
          {
            var menu = await component.LocalUser.OpenContextMenu(component, eventData.source.Slot);
            await new Updates(0); // I don't know why this is needed...

            var item = menu.AddItem(
              label: "Inspector.Fields.Drive".AsLocaleKey(format: $"{{0}} <size=50%>(DynamicReferenceVariableDriver)</size>"),
              icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
              color: RadiantUI_Constants.Hero.PURPLE
            );
            item.Button.LocalPressed += (_, _) =>
            {
              DriveFromDynamicVariable(field, dynVar, keepOriginalValue: true);
              component.LocalUser.CloseContextMenu(component);
            };
          });

          __result = true;
        }

      }
    }
  }

  static void DriveFromDynamicVariable(ISyncRef field, IDynamicVariable source, bool keepOriginalValue = false)
  {
    var slot = field.FindNearestParent<Slot>();
    var driver = (IDynamicVariable)slot.AttachComponent(GetDynamicVariableDriverType(field.TargetType));
    SetDynamicVariableName(driver, source.VariableName);
    Traverse.Create(driver).Field("Target").Property("Target").SetValue(field);

    if (keepOriginalValue)
    {
      if (field.TargetType.IsEnginePrimitive())
      {
        Traverse.Create(driver).Field("DefaultValue").Property("Value").SetValue(field.Target);
      }
      else
      {
        Traverse.Create(driver).Field("DefaultTarget").Property("Target").SetValue(field.Target);
      }
    }
  }

  static void SetDynamicVariableName(IDynamicVariable dynVar, string name) =>
    Traverse.Create(dynVar).Field<Sync<string>>("VariableName").Value.Value = name;

  static Type GetDynamicVariableDriverType(Type type) =>
    type.IsEnginePrimitive()
      ? typeof(DynamicValueVariableDriver<>).MakeGenericType(type)
      : typeof(DynamicReferenceVariableDriver<>).MakeGenericType(type);

  static IEnumerable<Type> GetSystemTypes() =>
    Enumerable.Range(0, GlobalTypeRegistry.SystemTypeCount)
      .Select(GlobalTypeRegistry.GetSystemType);

  static IEnumerable<Type> GetAllTypes() =>
    GetSystemTypes().Concat(GlobalTypeRegistry.DataModelAssemblies.SelectMany(a => a.Types));

  static IEnumerable<Type> GetFieldTypes() =>
    GetAllTypes().Where(t => !t.IsAbstract && !t.ContainsGenericParameters && !t.IsVoid() && t.IsValueType);

  static IEnumerable<Type> GetReferenceTypes() =>
    GetAllTypes().Where(t => !t.IsAbstract && !t.ContainsGenericParameters && !t.IsVoid() && t.IsClass && typeof(IWorldElement).IsAssignableFrom(t));
}