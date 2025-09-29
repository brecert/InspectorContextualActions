
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using Elements.Core;
// using FrooxEngine;
// using FrooxEngine.UIX;
// using HarmonyLib;
// using InspectorContextualActions.Utils;

// [HarmonyPatch]
// [HarmonyPatchCategory("OpenComponentInComponentSelectorPatch")]
// class OpenComponentInComponentSelectorPatch
// {
//   [HarmonyPostfix]
//   [HarmonyPatch(typeof(DevTool), nameof(DevTool.GenerateMenuItems))]
//   static void GenerateMenuItems(DevTool __instance, InteractionHandler tool, ContextMenu menu)
//   {
//     if (GetGrabbedReferenceProxy(__instance) is var proxy and not null)
//     {
//       UniLog.Log(proxy);
//       var inspector = proxy.Slot.GetComponentInParents<SceneInspector>();
//       UniLog.Log(inspector);
//       if (proxy.Reference.Target is Component component && inspector != null)
//       {
//         UniLog.Log(component);
//         var paths = component.GetType().GetCustomAttributes<CategoryAttribute>(inherit: true, fromInterfaces: true).FirstOrDefault()?.Paths ?? ["Uncategorized"];
//         var path = paths.FirstOrDefault();

//         UniLog.Log(path);
//         if (path != null)
//         {
//           var item = menu.AddItem("Open Component Category", (Uri)null, RadiantUI_Constants.Hero.GREEN);
//           item.Button.LocalPressed += (_, eventData) => CreateComponentSelector(inspector, eventData);
//         }
//       }
//     }
//   }

//   static ComponentSelector CreateComponentSelector(SceneInspector inspector, ButtonEventData eventData)
//   {
//     Slot slot = inspector.LocalUserSpace.AddSlot("Component Selector");
//     DestroyProxy destroyProxy = inspector.DestroyWhenDestroyed(slot);
//     destroyProxy.Persistent = false;
//     slot.DestroyWhenDestroyed(destroyProxy);
//     ComponentSelector componentSelector = slot.AttachComponent<ComponentSelector>();
//     componentSelector.SetupDefault();
//     ref readonly float3 globalPoint = ref eventData.globalPoint;
//     float3 v = inspector.Slot.Forward;
//     float3 v2 = v * -0.05f;
//     float3 b = v2 * inspector.LocalUserRoot.GlobalScale;
//     slot.GlobalPosition = globalPoint + b;
//     slot.GlobalRotation = inspector.Slot.GlobalRotation;
//     v = slot.LocalScale;
//     slot.LocalScale = v * inspector.LocalUserRoot.GlobalScale;
//     componentSelector.ComponentSelected.Target = (ComponentSelectionHandler)inspector.GetSyncMethod("OnComponentSelected");
//     return componentSelector;
//   }

//   public static ReferenceProxy? GetGrabbedReferenceProxy(DevTool tool) =>
//     tool.GetComponentInGrabbed((ReferenceProxy p) => p.Reference.Target != null);
// }