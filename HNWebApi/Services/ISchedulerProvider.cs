using System.Reactive.Concurrency;

namespace HNWebApi.Services;

public interface ISchedulerProvider
{
    IScheduler TaskPoolScheduler { get; }
}