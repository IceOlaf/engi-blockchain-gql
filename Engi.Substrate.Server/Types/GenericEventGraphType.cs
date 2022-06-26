using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class GenericEventGraphType : ObjectGraphType<GenericEvent>
{
    public GenericEventGraphType()
    {
        Field(x => x.Section);
        Field(x => x.Method);
        Field(x => x.Index);
        Field(x => x.Data, type: typeof(DataType));
    }

    public class DataType : ScalarGraphType
    {
        public override object? ParseValue(object? value) => throw new NotImplementedException();

        public override object? Serialize(object? value)
        {
            return value;
        }
    }
}