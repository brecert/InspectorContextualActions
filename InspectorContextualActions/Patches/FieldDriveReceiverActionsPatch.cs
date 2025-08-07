
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using InspectorContextualActions.Utils;

class FieldDriveReceiverActionsPatch
{
  // Patched from ComponentSubtypePatches
  [HarmonyPostfix]
  public static void TryReceive_Postfix(object __instance, IEnumerable<IGrabbable> items, Component grabber, Canvas.InteractionData eventData, in float3 globalPoint, bool __result)
  {
    var component = (Component)__instance;
    var fieldRef = (ISyncRef)Traverse.Create(__instance).Field("Field").GetValue();
    var field = (IField)fieldRef.Target;
    var slot = field.FindNearestParent<Slot>();

    foreach (var reference in GrabbableHelper.GetGrabbedReferences(items))
    {
      if (TypeUtils.MatchInterface(reference.GetType(), typeof(IDynamicVariable<>), out var matchedType))
      {
        var dynVar = (IDynamicVariable)reference;
        var varType = matchedType!.GenericTypeArguments[0];

        if (varType.IsAssignableFrom(field.ValueType))
        {
          component.StartTask(async () =>
          {
            var menu = await component.LocalUser.OpenContextMenu(component, eventData.source.Slot);
            await new Updates(0); // I don't know why this is needed...

            var item = menu.AddItem(
              label: "Inspector.Fields.Drive".AsLocaleKey(format: $"{{0}} <size=50%>(DynamicValueVariableDriver)</size>"),
              icon: OfficialAssets.Graphics.Icons.ProtoFlux.Drive,
              color: RadiantUI_Constants.Hero.PURPLE
            );
            item.Button.LocalPressed += (_, _) =>
            {
              slot.CreateValueDriver(varType, null, field);
              component.LocalUser.CloseContextMenu(component);
            };
          });

          __result = true;
        }
      }
    }
  }
}