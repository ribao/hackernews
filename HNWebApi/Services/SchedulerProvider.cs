using System.Reactive.Concurrency;

namespace HNWebApi.Services;

public class SchedulerProvider : ISchedulerProvider
{
    public IScheduler TaskPoolScheduler => System.Reactive.Concurrency.TaskPoolScheduler.Default;
}