
using System.Reflection;
using Elements.Core;
using FrooxEngine;

namespace InspectorContextualActions.Utils;

public static class TextFormatDriverHelper
{
  public static void AttatchValueTextFormatDriver(this Slot slot, IField source, IField<string> target, string format) =>
    typeof(TextFormatDriverHelper)
      .GetGenericMethod(nameof(AttatchValueTextFormatDriver), BindingFlags.Static | BindingFlags.Public, source.ValueType)
      .Invoke(null, [slot, source, target, format]);

  public static void AttatchValueTextFormatDriver<T>(this Slot slot, IField sourceValue, IField<string> textTarget, string format)
  {
    var driver = slot.AttachComponent<ValueTextFormatDriver<T>>();
    driver.Source.TrySet(sourceValue);
    driver.Format.Value = format;
    driver.Text.Target = textTarget;
  }
}