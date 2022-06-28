using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EventRecordGraphType : ObjectGraphType<EventRecord>
{
    public EventRecordGraphType()
    {
        Field(x => x.Phase);
        Field(x => x.Event);
        Field(x => x.Topics);
    }
}