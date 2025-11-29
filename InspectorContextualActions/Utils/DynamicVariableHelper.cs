using System;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace InspectorContextualActions.Utils;

public static class DynamicVariableHelper
{
  public static bool CreateDynamicValueDriver(this Slot slot, Type type, string name, IField field) =>
    (bool)typeof(DynamicVariableHelper)
      .GetGenericMethod(nameof(CreateDynamicValueDriver), BindingFlags.Static | BindingFlags.Public, type)
      .Invoke(null, [slot, name, field, true, true])!;

  public static bool CreateDynamicReferenceDriver(this Slot slot, Type type, string name, ISyncRef syncRef) =>
    (bool)typeof(DynamicVariableHelper)
      .GetGenericMethod(nameof(CreateDynamicReferenceDriver), BindingFlags.Static | BindingFlags.Public, type)
      .Invoke(null, [slot, name, syncRef, true, true])!;

  public static bool CreateDynamicValueDriver<T>(this Slot slot, string name, IField<T> field, bool keepOrginalValue = true, bool persistent = true) =>
    CreateDynamicValueDriver(slot, name, field, defaultValue: keepOrginalValue ? field.Value : default!, persistent);

  public static bool CreateDynamicValueDriver<T>(this Slot slot, string name, IField<T> field, T defaultValue = default!, bool persistent = true)
  {
    if (DynamicValueVariable<T>.IsValidGenericType)
    {
      var driver = slot.AttachComponent<DynamicValueVariableDriver<T>>();
      driver.VariableName.Value = name;
      driver.Target.Target = field;
      driver.Persistent = persistent;
      driver.DefaultValue.Value = defaultValue;
      driver.UpdateLinking();
      return true;
    }

    return false;
  }

  public static bool CreateDynamicReferenceDriver<T>(this Slot slot, string name, SyncRef<T> syncRef, bool keepOrginalTarget = true, bool persistent = true) where T : class, IWorldElement
    => CreateDynamicReferenceDriver(slot, name, syncRef, defaultTarget: keepOrginalTarget ? syncRef.Target : null, persistent);

  public static bool CreateDynamicReferenceDriver<T>(this Slot slot, string name, SyncRef<T> syncRef, T? defaultTarget = default, bool persistent = true) where T : class, IWorldElement
  {
    if (typeof(IWorldElement).IsAssignableFrom(typeof(T)))
    {
      var driver = slot.AttachComponent<DynamicReferenceVariableDriver<T>>();
      driver.VariableName.Value = name;
      driver.Target.Target = syncRef;
      driver.Persistent = persistent;
      if (defaultTarget != null) driver.DefaultTarget.Target = defaultTarget;
      driver.UpdateLinking();
      return true;
    }

    return false;
  }

  public static bool CreateDynamicValueVariable(Slot slot, IField field) =>
    (bool)typeof(FrooxEngine.DynamicVariableHelper)
      .GetGenericMethod(nameof(FrooxEngine.DynamicVariableHelper.CreateVariable), BindingFlags.Static | BindingFlags.Public, field.ValueType)
      .Invoke(null, [slot, null, field.BoxedValue, true])!;

  public static bool CreateDynamicReferenceVariable(Slot slot, ISyncRef syncRef) =>
    (bool)typeof(FrooxEngine.DynamicVariableHelper)
      .GetGenericMethod(nameof(FrooxEngine.DynamicVariableHelper.CreateVariable), BindingFlags.Static | BindingFlags.Public, syncRef.TargetType)
      .Invoke(null, [slot, null, syncRef.Target, true])!;

  public static bool CreateDynamicReferenceVariableOfISyncRef(Slot slot, ISyncRef syncRef) =>
    (bool)typeof(FrooxEngine.DynamicVariableHelper)
      .GetGenericMethod(nameof(FrooxEngine.DynamicVariableHelper.CreateVariable), BindingFlags.Static | BindingFlags.Public, typeof(ISyncRef<>).MakeGenericType(syncRef.TargetType))
      .Invoke(null, [slot, null, syncRef, true])!;


  public static bool CreateDynamicReferenceVariableOfIField(Slot slot, IField field) =>
    (bool)typeof(FrooxEngine.DynamicVariableHelper)
      .GetGenericMethod(nameof(FrooxEngine.DynamicVariableHelper.CreateVariable), BindingFlags.Static | BindingFlags.Public, typeof(IField<>).MakeGenericType(field.ValueType))
      .Invoke(null, [slot, null, field, true])!;

  public static bool CreateDynamicReferenceVariableOfIValue(Slot slot, IField field) =>
    (bool)typeof(FrooxEngine.DynamicVariableHelper)
      .GetGenericMethod(nameof(FrooxEngine.DynamicVariableHelper.CreateVariable), BindingFlags.Static | BindingFlags.Public, typeof(IValue<>).MakeGenericType(field.ValueType))
      .Invoke(null, [slot, null, field, true])!;

}