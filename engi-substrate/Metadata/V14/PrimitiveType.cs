using System.Runtime.Serialization;

namespace Engi.Substrate.Metadata.V14;

public enum PrimitiveType
{
    [EnumMember(Value = "bool")]
    Bool = 0,

    [EnumMember(Value = "char")]
    Char = 1,

    [EnumMember(Value = "str")]
    String = 2,

    [EnumMember(Value = "u8")]
    UInt8 = 3,

    [EnumMember(Value = "u16")]
    UInt16 = 4,

    [EnumMember(Value = "u32")]
    UInt32 = 5,

    [EnumMember(Value = "u64")]
    UInt64 = 6,

    [EnumMember(Value = "u128")]
    UInt128 = 7,

    [EnumMember(Value = "u256")]
    UInt256 = 8,

    [EnumMember(Value = "i8")]
    Int8 = 9,

    [EnumMember(Value = "i32")]
    Int32 = 10,

    [EnumMember(Value = "i64")]
    Int64 = 11,

    [EnumMember(Value = "i128")]
    Int128 = 12,

    [EnumMember(Value = "i256")]
    Int256 = 13
}