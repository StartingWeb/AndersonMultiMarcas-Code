using Core;
using Core.Dtos;
using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace Concessionaria.Pages.Loja;

public class IndexModel : PageModel
{
    private readonly ILojaService _lojaService;

    public IndexModel(ILojaService lojaService)
    {
        _lojaService = lojaService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filtro { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Ordem { get; set; }

    public List<LojaDto> Lojas { get; set; } = new();

    public string? Mensagem { get; set; }

    public async Task OnGetAsync()
    {
        var retorno = await _lojaService.ListarAsync();

        if (retorno == null)
        {
            Lojas = new List<LojaDto>();
            Mensagem = "Não foi possível carregar as lojas.";
            return;
        }

        if (retorno.Status != Core.Enums.PackageStatus.Success)
        {
            Lojas = retorno.Data ?? new List<LojaDto>();
            Mensagem = retorno.UserMessage;
            return;
        }

        var query = (retorno.Data ?? new List<LojaDto>()).AsQueryable();

        if (!string.IsNullOrWhiteSpace(Filtro))
        {
            var filtro = Filtro.Trim().ToLower();

            query = query.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Nome) && x.Nome.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.RazaoSocial) && x.RazaoSocial.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Cnpj) && x.Cnpj.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Email) && x.Email.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Telefone) && x.Telefone.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Cidade) && x.Cidade.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Uf) && x.Uf.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Bairro) && x.Bairro.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Endereco) && x.Endereco.ToLower().Contains(filtro)) ||
                (!string.IsNullOrWhiteSpace(x.Cep) && x.Cep.ToLower().Contains(filtro))
            );
        }

        query = Ordem switch
        {
            "nome_desc" => query.OrderByDescending(x => x.Nome),
            "cidade" => query.OrderBy(x => x.Cidade).ThenBy(x => x.Nome),
            "recentes" => query.OrderByDescending(x => x.DataCadastro),
            "antigas" => query.OrderBy(x => x.DataCadastro),
            _ => query.OrderBy(x => x.Nome)
        };

        Lojas = query.ToList();
    }
}