namespace OnboardingWorker.Domain;

public class MessageEnvelope<T>
{
    public Guid MessageId { get; set; }
    public DateTime TimesTamps { get; set; }
    public string EventType { get; set; }
    public string Source { get; set; }
    public T Payload { get; set; }
}