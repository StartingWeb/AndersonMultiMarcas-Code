using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Application;

public class AspNetMenu
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public string? Icone { get; set; }

    public string? Url { get; set; }

    public int Ordem { get; set; }

    public bool Ativo { get; set; }

    public Guid? MenuPaiId { get; set; }

    public AspNetMenu? MenuPai { get; set; }

    public ICollection<AspNetMenu> SubMenus { get; set; } = new List<AspNetMenu>();
    public ICollection<AspNetMenuRole> MenuRoles { get; set; } = new List<AspNetMenuRole>();
}
