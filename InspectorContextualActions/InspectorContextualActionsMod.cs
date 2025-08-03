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

  private static string HarmonyId => $"dev.bree.{ModAssembly.GetName()}";
  private static readonly Harmony harmony = new(HarmonyId);

  private static ModConfiguration? Config;

  private static HashSet<string> Categories = [];

  InspectorContextualActionsMod()
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

    Config = GetConfiguration()!;
    Config.OnThisConfigurationChanged += OnConfigChanged;

    PatchCategories();
  }

#if DEBUG
  static void BeforeHotReload()
  {
    UnpatchCategories();
  }

  static void OnHotReload(ResoniteMod modInstance)
  {
    PatchCategories();
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