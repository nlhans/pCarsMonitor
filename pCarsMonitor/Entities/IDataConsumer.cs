using pCarsTelemetry.API;

namespace pCarsMonitor.Entities
{
    public interface IDataConsumer
    {
        void Push(PcarsTelemetrySample telemetrySample);
    }
}