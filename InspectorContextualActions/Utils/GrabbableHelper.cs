using System.Collections.Generic;
using System.Linq;
using FrooxEngine;

namespace InspectorContextualActions.Utils;

public static class GrabbableHelper
{
  public static IEnumerable<IWorldElement> GetGrabbedReferences(IEnumerable<IGrabbable> items) =>
    items.SelectMany(grabbable => grabbable.Slot.GetComponentsInChildren<ReferenceProxy>()).Select(p => p.Reference.Target);
}