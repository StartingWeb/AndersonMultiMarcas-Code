namespace Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime? DataAtualizacao { get; protected set; }

    protected void MarcarAtualizacao()
    {
        DataAtualizacao = DateTime.UtcNow;
    }
}
