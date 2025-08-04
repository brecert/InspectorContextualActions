
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
using InspectorContextualActions.Utils;

[HarmonyPatch]
[HarmonyPatchCategory("FieldDriveReceiverActions")]
class FieldDriveReceiverActionsPatch
{
  [HarmonyTargetMethods]
  public static IEnumerable<MethodBase> TargetMethods() =>
    GetSystemTypes()
      .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && !t.IsVoid())
      .Select(t => AccessTools.DeclaredMethod(typeof(FieldDriveReceiver<>).MakeGenericType(t), nameof(FieldDriveReceiver<dummy>.TryReceive)))
      .Where(m => m != null);

  public static void Postfix(object __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, bool __result)
  {
    var component = (Component)__instance;
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Field").GetValue();
    var field = (IField)fieldRef.Target;

    foreach (var reference in GrabbableHelper.GetGrabbedReferences(items))
    {
      if (TypeUtils.MatchInterface(reference.GetType(), typeof(IDynamicVariable<>), out var matchedType))
      {
        var dynVar = (IDynamicVariable)reference;
        var varType = matchedType!.GenericTypeArguments[0];

        if (field.ValueType == varType)
        {
          component.StartTask(async () =>
          {
            var menu = await component.LocalUser.OpenContextMenu(component, eventData.source.Slot);
            await new Updates(0); // I don't know why this is needed...

            var item = menu.AddItem("Drive with Dynamic Driver", (Uri)null, RadiantUI_Constants.Hero.PURPLE);
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

  static void DriveFromDynamicVariable(IField field, IDynamicVariable source, bool keepOriginalValue = false)
  {
    var slot = field.FindNearestParent<Slot>();
    var driver = (IDynamicVariable)slot.AttachComponent(GetDynamicVariableDriverType(field.ValueType));
    SetDynamicVariableName(driver, source.VariableName);
    Traverse.Create(driver).Field("Target").Property("Target").SetValue(field);

    if (keepOriginalValue)
    {
      if (field.ValueType.IsEnginePrimitive())
      {
        Traverse.Create(driver).Field("DefaultValue").Property("Value").SetValue(field.BoxedValue);
      }
      else
      {
        Traverse.Create(driver).Field("DefaultTarget").Property("Target").SetValue(field.BoxedValue);
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
}