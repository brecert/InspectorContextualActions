
using System;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class UserOverrideHelper
{
  public static Component CreateValueUserOverride(this Slot slot, IField field, bool keepOriginalValue = true) =>
    (Component)typeof(UserOverrideHelper)
      .GetGenericMethod(nameof(CreateValueUserOverride), BindingFlags.Static | BindingFlags.Public, [field.ValueType])
      .Invoke(null, [slot, field, keepOriginalValue])!;

  public static ValueUserOverride<T> CreateValueUserOverride<T>(this Slot slot, IField<T> field, bool keepOriginalValue)
  {
    return slot.AttachComponent<ValueUserOverride<T>>(beforeAttach: c =>
    {
      if (keepOriginalValue)
      {
        c.Default.Value = field.Value;
      }
      c.Target.Target = field;
    });
  }

  public static Component CreateReferenceUserOverride(this Slot slot, SyncRef syncRef, bool keepOriginalValue = true) =>
    (Component)typeof(UserOverrideHelper)
      .GetGenericMethod(nameof(CreateReferenceUserOverride), BindingFlags.Static | BindingFlags.Public, [syncRef.TargetType])
      .Invoke(null, [slot, syncRef, keepOriginalValue])!;

  public static ReferenceUserOverride<T> CreateReferenceUserOverride<T>(this Slot slot, SyncRef<T> syncRef, bool keepOriginalValue) where T : class, IWorldElement
  {
    return slot.AttachComponent<ReferenceUserOverride<T>>(beforeAttach: c =>
    {
      if (keepOriginalValue)
      {
        c.Default.Target = syncRef.Target;
      }
      c.Target.Target = syncRef;
    });
  }

  public static bool IsValidNumericValueUserOverrideGatherTarget(this IField target) =>
    typeof(NumericUserOverrideGather<>).MakeGenericType(target.ValueType).IsValidGenericType(validForInstantiation: true);

  public static Component CreateNumericValueUserOverrideGather(this Slot slot, IField field, bool keepOriginalValue = true) =>
    (Component)typeof(UserOverrideHelper)
      .GetGenericMethod(nameof(CreateNumericValueUserOverrideGather), BindingFlags.Static | BindingFlags.Public, [field.ValueType])
      .Invoke(null, [slot, field, keepOriginalValue])!;

  public static NumericUserOverrideGather<T> CreateNumericValueUserOverrideGather<T>(this Slot slot, IField<T> field, bool keepOriginalValue)
  {
    return slot.AttachComponent<NumericUserOverrideGather<T>>(beforeAttach: c =>
    {
      if (keepOriginalValue)
      {
        c.Default.Value = field.Value;
      }
      c.Target.Target = field;
    });
  }
}
