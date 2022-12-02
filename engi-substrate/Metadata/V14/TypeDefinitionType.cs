namespace Engi.Substrate.Metadata.V14;

public enum TypeDefinitionType
{
    /// A composite type (e.g. a struct or a tuple)
    Composite = 0,
    /// A variant type (e.g. an enum)
    Variant = 1,
    /// A sequence type with runtime known length.
    Sequence = 2,
    /// An array type with compile-time known length.
    Array = 3,
    /// A tuple type.
    Tuple = 4,
    /// A Rust primitive type.
    Primitive = 5,
    /// A type using the [`Compact`] encoding
    Compact = 6,
    /// A type representing a sequence of bits.
    BitSequence = 7,
    Void = 8
}