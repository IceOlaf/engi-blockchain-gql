using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class PhaseGraphType : ScalarGraphType
{
    public override object? ParseValue(object? value) => throw new NotImplementedException();

    public override object? Serialize(object? value)
    {
        var phase = (Phase)value!;

        if (phase.Value == PhaseType.ApplyExtrinsic)
        {
            return new { ApplyExtrinsic = phase.Data };
        }

        return phase.Value;
    }
}