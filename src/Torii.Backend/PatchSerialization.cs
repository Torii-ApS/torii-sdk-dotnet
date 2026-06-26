using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Torii.Backend;

/// <summary>
/// Newtonsoft settings that serialize tri-state <see cref="Patch{T}"/> fields on
/// generated request models. Field-agnostic: any Patch-typed property a future
/// regen adds is handled with no further changes.
/// <list type="bullet">
///   <item><description>A property that is <c>null</c> or <c>Patch.Omit</c> is dropped from the body — the "leave unchanged" wire state.</description></item>
///   <item><description>A <c>Patch.Set(value)</c> emits the inner value; <c>Patch.Set(null)</c> emits an explicit JSON <c>null</c> (clear).</description></item>
///   <item><description>Null values <i>inside</i> a metadata bag are preserved (a null-valued key deletes that key), so the default <c>NullValueHandling</c> is kept.</description></item>
/// </list>
/// </summary>
internal static class PatchSerialization
{
    internal static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new PatchContractResolver(),
        Converters = { new PatchJsonConverter() },
    };
}

/// <summary>
/// Omits any request-body property whose value is null or a <c>Patch.Omit</c>.
/// Applied only to request serialization, so dropping top-level nulls is exactly
/// the "leave unchanged" contract; null values nested inside a serialized bag are
/// untouched (that property itself is non-null), preserving the key-delete case.
/// </summary>
internal sealed class PatchContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        var provider = property.ValueProvider;
        property.ShouldSerialize = instance =>
        {
            var value = provider?.GetValue(instance);
            if (value is null)
            {
                return false;
            }
            return value is not IPatch patch || !patch.IsOmitted;
        };
        return property;
    }
}

/// <summary>
/// Writes a present <see cref="Patch{T}"/> as its inner value (a value, or JSON
/// <c>null</c> for a cleared field); reads a present key back into a
/// <c>Patch.Set(value)</c>. The resolver guarantees omitted patches never reach
/// <see cref="WriteJson"/>; an absent key leaves the property at its default.
/// </summary>
internal sealed class PatchJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => typeof(IPatch).IsAssignableFrom(objectType);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        serializer.Serialize(writer, ((IPatch)value!).BoxedValue);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var element = objectType.GetGenericArguments()[0];
        var inner = serializer.Deserialize(reader, element);
        var set = objectType.GetMethod("Set", BindingFlags.Public | BindingFlags.Static)!;
        return set.Invoke(null, new[] { inner });
    }
}
