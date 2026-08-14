
using System.Reflection;
using Elements.Core;
using FrooxEngine;

namespace InspectorContextualActions.Utils;

public static class ValueGradientDriverHelper
{
  public static bool IsValidValueGradientDriverTarget(this IField target) =>
    typeof(ValueGradientDriver<>).MakeGenericType(target.ValueType).IsValidGenericType(validForInstantiation: true);

  public static Component AttachValueGradientDriver(this Slot slot, IField target, bool keepOriginalValue = true) =>
    (Component)typeof(ValueGradientDriverHelper)
      .GetGenericMethod(nameof(AttachValueTextFormatDriver), BindingFlags.Static | BindingFlags.Public, target.ValueType)
      .Invoke(null, [slot, target, keepOriginalValue])!;

  public static ValueGradientDriver<T> AttachValueTextFormatDriver<T>(this Slot slot, IField<T> valueTarget, bool keepOriginalValue = true)
  {
    var originalValue = valueTarget.Value;
    var driver = slot.AttachComponent<ValueGradientDriver<T>>();
    driver.Target.Target = valueTarget;
    if (keepOriginalValue)
    {
      driver.AddPoint(0, originalValue);
    }
    return driver;
  }
}
