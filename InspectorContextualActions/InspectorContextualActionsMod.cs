using System.Linq;
using System.Reflection;
using ResoniteModLoader;
using HarmonyLib;
using System;
using System.Collections.Generic;


#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace InspectorContextualActions;

public class InspectorContextualActionsMod : ResoniteMod
{
  private static Assembly ModAssembly => typeof(InspectorContextualActionsMod).Assembly;

  public override string Name => ModAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
  public override string Author => ModAssembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
  public override string Version => ModAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
  public override string Link => ModAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(meta => meta.Key == "RepositoryUrl").Value;

  internal static string HarmonyId => $"dev.bree.{ModAssembly.GetName()}";
  internal static readonly Harmony harmony = new(HarmonyId);

  internal static ModConfiguration? Config;

  private static HashSet<string> Categories = [];

  public InspectorContextualActionsMod()
  {
    Categories = AccessTools.GetTypesFromAssembly(ModAssembly)
      .Select(t => t.GetCustomAttribute<HarmonyPatchCategory>()?.info.category)
      .OfType<string>()
      .ToHashSet();
  }

  public override void OnEngineInit()
  {
#if DEBUG
    HotReloader.RegisterForHotReload(this);
#endif

    // Config = GetConfiguration()!;
    // Config.OnThisConfigurationChanged += OnConfigChanged;

    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }

#if DEBUG
  static void BeforeHotReload()
  {
    harmony.UnpatchAll(HarmonyId);
    ComponentSubtypePatches.SeenTypes.Clear();
  }

  static void OnHotReload(ResoniteMod modInstance)
  {
    PatchCategories();
    harmony.PatchAllUncategorized(ModAssembly);
  }
#endif

  private static void UnpatchCategories()
  {
    foreach (var category in Categories)
    {
      harmony.UnpatchCategory(ModAssembly, category);
    }
  }

  private static void PatchCategories()
  {
    foreach (var category in Categories)
    {
      harmony.PatchCategory(ModAssembly, category);
    }
  }

  void OnConfigChanged(ConfigurationChangedEvent changed)
  {

  }
}