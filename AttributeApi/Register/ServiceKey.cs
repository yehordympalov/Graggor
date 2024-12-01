namespace AttributeApi.Register;

public class ServiceKey(Guid key)
{
    public Guid Key { get; } = key;

    public static Guid DefaultServiceKey { get; } = Guid.CreateVersion7();
}
