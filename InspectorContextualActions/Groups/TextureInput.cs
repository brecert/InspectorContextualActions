using System;
using System.Collections;
using System.Collections.Generic;
using FrooxEngine;

class Suggestion(string Name, Type ComponentType);

static class TextureInput
{
  static bool IsOfType(Type type) => typeof(IAssetProvider<ITexture2D>).IsSubclassOf(type);

  
}