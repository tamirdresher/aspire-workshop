using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Bookstore.AppHost;

public static class TalkingClockResourceBuilderExtensions
{
    public static IResourceBuilder<TalkingClockResource> AddTalkingClock(this IDistributedApplicationBuilder builder, string name)
    {
        var tickHandResource = new ClockHandResource(name + "-tick-hand");
        var tockHandResource = new ClockHandResource(name + "-tock-hand");
        var resource = new TalkingClockResource(name, tickHandResource, tockHandResource);

        var clockBuilder = builder.AddResource(resource)
                      .WithInitialState(new()
                      {
                          ResourceType = "TalkingClock",
                          CreationTimeStamp = DateTime.UtcNow,
                          State = KnownResourceStates.NotStarted,
                          Properties = [
                              new(CustomResourceKnownProperties.Source, "Talking Clock")
                          ]
                      })
                      .WithUrl("https://www.speaking-clock.com/", "Speaking Clock")
                      .ExcludeFromManifest();

        clockBuilder.OnInitializeResource(static async (resource, initEvent, token) =>
        {
            var log = initEvent.Logger;
            var eventing = initEvent.Eventing;
            var notification = initEvent.Notifications;
            var services = initEvent.Services;

            await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, services), token);
            await eventing.PublishAsync(new BeforeResourceStartedEvent(resource.TickHand, services), token);
            await eventing.PublishAsync(new BeforeResourceStartedEvent(resource.TockHand, services), token);

            log.LogInformation("Starting Talking Clock...");

            await notification.PublishUpdateAsync(resource, s => s with
            {
                StartTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.Running
            });
            await notification.PublishUpdateAsync(resource.TickHand, s => s with
            {
                StartTimeStamp = DateTime.UtcNow,
                State = "Waiting on clock tick"
            });
            await notification.PublishUpdateAsync(resource.TockHand, s => s with
            {
                StartTimeStamp = DateTime.UtcNow,
                State = "Waiting on clock tock"
            });

            while (!token.IsCancellationRequested)
            {
                log.LogInformation("The time is {time}", DateTime.UtcNow);

                await notification.PublishUpdateAsync(resource,
                    s => s with { State = new ResourceStateSnapshot("Tick", KnownResourceStateStyles.Success) });
                await notification.PublishUpdateAsync(resource.TickHand,
                    s => s with { State = new ResourceStateSnapshot("On", KnownResourceStateStyles.Success) });
                await notification.PublishUpdateAsync(resource.TockHand,
                    s => s with { State = new ResourceStateSnapshot("Off", KnownResourceStateStyles.Info) });

                await Task.Delay(1000, token);

                await notification.PublishUpdateAsync(resource,
                    s => s with { State = new ResourceStateSnapshot("Tock", KnownResourceStateStyles.Success) });
                await notification.PublishUpdateAsync(resource.TickHand,
                    s => s with { State = new ResourceStateSnapshot("Off", KnownResourceStateStyles.Info) });
                await notification.PublishUpdateAsync(resource.TockHand,
                    s => s with { State = new ResourceStateSnapshot("On", KnownResourceStateStyles.Success) });

                await Task.Delay(1000, token);
            }
        });

        AddHandResource(tickHandResource);
        AddHandResource(tockHandResource);

        return clockBuilder;

        void AddHandResource(ClockHandResource clockHand)
        {
            builder.AddResource(clockHand)
                .WithParentRelationship(clockBuilder)
                .WithInitialState(new()
                {
                    ResourceType = "ClockHand",
                    CreationTimeStamp = DateTime.UtcNow,
                    State = KnownResourceStates.NotStarted,
                    Properties =
                    [
                        new(CustomResourceKnownProperties.Source, "Talking Clock")
                    ]
                });
        }
    }
}
