
using System.Reflection;
using Elements.Core;
using FrooxEngine;

namespace InspectorContextualActions.Utils;

public static class BooleanValueDriverHelper
{
  public static bool IsValidBooleanValueDriverTarget(this IField target) =>
    typeof(BooleanValueDriver<>).MakeGenericType(target.ValueType).IsValidGenericType(validForInstantiation: true);

  public static Component AttachBooleanValueDriver(this Slot slot, IField target, bool keepOriginalValue = true) =>
    typeof(BooleanValueDriverHelper)
      .GetGenericMethod(nameof(AttachBooleanValueDriver), BindingFlags.Static | BindingFlags.Public, target.ValueType)
      .Invoke(null, [slot, target, keepOriginalValue]) as Component;

  public static BooleanValueDriver<T> AttachBooleanValueDriver<T>(this Slot slot, IField<T> valueTarget, bool keepOriginalValue = true)
  {
    var originalValue = valueTarget.Value;
    var driver = slot.AttachComponent<BooleanValueDriver<T>>();
    driver.TargetField.Target = valueTarget;
    if (keepOriginalValue)
    {
      driver.FalseValue.Value = originalValue;
      driver.TrueValue.Value = originalValue;
    }
    return driver;
  }
}
