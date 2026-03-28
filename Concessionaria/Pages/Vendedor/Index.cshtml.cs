using Core.Dtos;
using Core.Enums;
using Core.Interfaces;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Concessionaria.Pages.Vendedor
{
    public class IndexModel : PageModel
    {
        private readonly IVendedorService _vendedorService;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            IVendedorService vendedorService,
            ApplicationDbContext context)
        {
            _vendedorService = vendedorService;
            _context = context;
        }

        public List<VendedorDto> Vendedores { get; set; } = new();
        public List<LojaDto> LojasFiltro { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Filtro { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Ordem { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LojaId { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarLojasFiltroAsync();
            await CarregarVendedoresAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _vendedorService.ExcluirAsync(id);

            TempData[result.Status == PackageStatus.Success ? "Success" : "Error"] =
                result.UserMessage ?? (result.Status == PackageStatus.Success
                    ? "Vendedor excluído com sucesso."
                    : "Não foi possível excluir o vendedor.");

            return RedirectToPage("./Index", new
            {
                Filtro,
                Ordem,
                LojaId
            });
        }

        private async Task CarregarLojasFiltroAsync()
        {
            LojasFiltro = await _context.Lojas
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .Select(x => new LojaDto
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Cidade = x.Cidade,
                    Uf = x.Uf,
                    Endereco = x.Endereco,
                    Telefone = x.Telefone,
                    Email = x.Email
                })
                .ToListAsync();
        }

        private async Task CarregarVendedoresAsync()
        {
            var resultado = LojaId.HasValue && LojaId.Value > 0
                ? await _vendedorService.ListarPorLojaAsync(LojaId.Value)
                : await _vendedorService.ListarAsync();

            if (resultado.Status != PackageStatus.Success || resultado.Data == null)
            {
                Vendedores = new List<VendedorDto>();
                return;
            }

            var query = resultado.Data
                .Select(x => new VendedorDto
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Email = x.Email,
                    Telefone = x.Telefone,
                    Whatsapp = x.Whatsapp,
                    Cpf = x.Cpf,
                    FotoUrl = x.FotoUrl,
                    Cargo = x.Cargo,
                    Ativo = x.Ativo,
                    LojaId = x.LojaId,
                    LojaNome = x.Loja != null ? x.Loja.Nome : string.Empty,
                    DataCadastro = x.DataCadastro
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Filtro))
            {
                var filtro = Filtro.Trim().ToLower();

                query = query.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.Nome) && x.Nome.ToLower().Contains(filtro)) ||
                    (!string.IsNullOrWhiteSpace(x.Email) && x.Email.ToLower().Contains(filtro)) ||
                    (!string.IsNullOrWhiteSpace(x.Telefone) && x.Telefone.ToLower().Contains(filtro)) ||
                    (!string.IsNullOrWhiteSpace(x.Whatsapp) && x.Whatsapp.ToLower().Contains(filtro)) ||
                    (!string.IsNullOrWhiteSpace(x.Cpf) && x.Cpf.ToLower().Contains(filtro)) ||
                    (!string.IsNullOrWhiteSpace(x.Cargo) && x.Cargo.ToLower().Contains(filtro)) ||
                    (!string.IsNullOrWhiteSpace(x.LojaNome) && x.LojaNome.ToLower().Contains(filtro))
                );
            }

            query = Ordem switch
            {
                "nome_desc" => query.OrderByDescending(x => x.Nome),
                "loja" => query.OrderBy(x => x.LojaNome).ThenBy(x => x.Nome),
                "recentes" => query.OrderByDescending(x => x.DataCadastro),
                _ => query.OrderBy(x => x.Nome)
            };

            Vendedores = query.ToList();
        }
    }
}
