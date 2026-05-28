namespace Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime DataCadastro { get; protected set; }
    public bool Ativo { get; protected set; }

    protected BaseEntity()
    {
        DataCadastro = DateTime.UtcNow;
        Ativo = true;
    }

    public virtual void Ativar() => Ativo = true;
    public virtual void Desativar() => Ativo = false;
}
