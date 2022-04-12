using System.Runtime.Serialization;

namespace Engi.Substrate.Metadata.V14;

public enum PrimitiveType
{
    Bool = 0,
    
    Char = 1,

    [EnumMember(Value = "Str")]
    String = 2,

    [EnumMember(Value = "U8")]
    UInt8 = 3,

    [EnumMember(Value = "U16")]
    UInt16 = 4,

    [EnumMember(Value = "U32")]
    UInt32 = 5,

    [EnumMember(Value = "U64")]
    UInt64 = 6,

    [EnumMember(Value = "U128")]
    UInt128 = 7,

    [EnumMember(Value = "U256")]
    UInt256 = 8,

    [EnumMember(Value = "I8")]
    Int8 = 9,

    [EnumMember(Value = "I32")]
    Int32 = 10,

    [EnumMember(Value = "I64")]
    Int64 = 11,

    [EnumMember(Value = "I128")]
    Int128 = 12,

    [EnumMember(Value = "I256")]
    Int256 = 13
}