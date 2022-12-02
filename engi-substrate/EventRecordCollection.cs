using System.Collections.ObjectModel;

namespace Engi.Substrate;

public class EventRecordCollection : ReadOnlyCollection<EventRecord>
{
    public EventRecordCollection(IList<EventRecord> list) 
        : base(list)
    {
    }

    public GenericEvent Find(string section, string method)
    {
        return this.First(
                x => x.Event.Section == section && x.Event.Method == method)
            .Event;
    }
}