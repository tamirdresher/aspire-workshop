using Aspire.Hosting;

namespace Bookstore.AppHost;

public sealed class TalkingClockResource(string name, ClockHandResource tickHand, ClockHandResource tockHand) : Resource(name)
{
    public ClockHandResource TickHand { get; } = tickHand;
    public ClockHandResource TockHand { get; } = tockHand;
}

public sealed class ClockHandResource(string name) : Resource(name);
