# Inspector Contextual Actions

Inspector Contextual Actions is an RML mod that add various contextual actions.

**Drive Receiver Actions**
When dropping a held reference on a field, like when driving a field, additional actions will now show up and should be read in the follow format.

field `FieldType` receives `HeldReferenceType` for `DriverType`

- field `IField<T>` receives `IValue<T>` for `ValueDriver<T>`
- field `IField<TO>` receives `IField<FROM>` for `Convertible{TO}Driver<FROM>`
  this allows for casting between types by attempting to drive them
- field `IField<TO>` receives `IField<FROM>` for `{FROM}To{TO}SwizzleDriver`
  this allows for "casting" between `float2` to `float3` by using the swizzle drivers
- field `IField<string>` receives `IField<T>` for `ValueTextFormatDriver<T>`
  this allows for "casting" between most value types to a `string`

Dropping a dynamic variable component reference on a field will attempt to drive the field with the component's dynamic variable.

- field `IField<T>` receives `IDynamicVariable<T>` for `DynamicValueDriver<T>`
- field `ISyncRef<T>` receives `IDynamicVariable<T>` for `DynamicReferenceDriver<T>`


**Component Receiver Actions** 
When dropping a held reference on an inspector panel this will typically show the "move component" or "copy coponent" actions in vanilla.

This mod adds some additional actions that follow a similar pattern that should be read in the following format.

slot receives `DroppedType` for `AttatchedComponentType`

- slot receives `IAssetProvider<ITexture2D>` for
  - `GridTexture`
  - `NoiseTexture`
  - `SimplexTexture`
  - `SolidColorTexture`
  - `UVTexture`
- slot receives `IAssetProvider<RenderTexture>` for `RenderTextureProvider`.
- slot receives `ISyncRef<IField<T>>` for `ValueField<T>`
- slot receives `ISyncRef<ISyncRef<T>>` for `ReferenceField<T>`
- slot receives `ISyncRef<T>` for
  - `DynamicReferenceVariable<T>`
  - `DynamicReference<T>`
  - `DynamicReferenceVariable<ISyncRef<T>>`
    Dynamic Reference of ISyncRef
- slot receives `SyncType` for
  - `DynamicTypeVariable`
  - `DynamicTypeField`
- slot receives `IField<T>` for
  - `DynamicValueVariable<T>`
  - `DynamicField<T>`
  - `DynamicReferenceVariable<IField<T>>`
    Dynamic Reference of IField
  - `DynamicReferenceVariable<IValue<T>>`
    Dynamic Reference of IValue