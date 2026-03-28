using Core;
using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Core;
public class MarcaService : IMarcaService
{
    private readonly ApplicationDbContext _context;

    public MarcaService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ======================================================
    // CREATE
    // ======================================================
    public async Task<Package<int>> CriarAsync(Marca marca)
    {
        var validacao = Validar(marca);
        if (validacao.Status != PackageStatus.Success)
            return validacao;

        try
        {
            marca.Id = 0;
            marca.DataCadastro = DateTime.Now;

            _context.Marcas.Add(marca);
            await _context.SaveChangesAsync();

            return new Package<int>
            {
                Status = PackageStatus.Success,
                Data = marca.Id,
                UserMessage = "Marca cadastrada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<int>
            {
                Status = PackageStatus.Error,
                Data = 0,
                UserMessage = "Não foi possível cadastrar a marca.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // UPDATE
    // ======================================================
    public async Task<Package<bool>> EditarAsync(Marca marca)
    {
        var validacao = ValidarEdicao(marca);
        if (validacao.Status != PackageStatus.Success)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = validacao.UserMessage,
                DebugMessage = validacao.DebugMessage
            };
        }

        try
        {
            var marcaBanco = await _context.Marcas.FirstOrDefaultAsync(x => x.Id == marca.Id);

            if (marcaBanco == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Marca não encontrada.",
                    DebugMessage = $"Nenhuma marca encontrada com Id {marca.Id}."
                };
            }

            marcaBanco.Nome = marca.Nome;
            marcaBanco.LogoUrl = marca.LogoUrl;
            marcaBanco.Ativo = marca.Ativo;

            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Marca atualizada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível atualizar a marca.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // DELETE
    // ======================================================
    public async Task<Package<bool>> ExcluirAsync(int id)
    {
        if (id <= 0)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Id inválido.",
                DebugMessage = "O id informado é menor ou igual a zero."
            };
        }

        try
        {
            var marca = await _context.Marcas.FirstOrDefaultAsync(x => x.Id == id);

            if (marca == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Marca não encontrada.",
                    DebugMessage = $"Nenhuma marca encontrada com Id {id}."
                };
            }

            _context.Marcas.Remove(marca);
            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Marca excluída com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível excluir a marca.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - BY ID
    // ======================================================
    public async Task<Package<Marca>> ObterPorIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Package<Marca>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Id inválido.",
                DebugMessage = "O id informado é menor ou igual a zero."
            };
        }

        try
        {
            var marca = await _context.Marcas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (marca == null)
            {
                return new Package<Marca>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Marca não encontrada.",
                    DebugMessage = $"Nenhuma marca encontrada com Id {id}."
                };
            }

            return new Package<Marca>
            {
                Status = PackageStatus.Success,
                Data = marca,
                UserMessage = "Marca localizada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<Marca>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Não foi possível buscar a marca.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST
    // ======================================================
    public async Task<Package<List<Marca>>> ListarAsync()
    {
        try
        {
            var marcas = await _context.Marcas
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .ToListAsync();

            return new Package<List<Marca>>
            {
                Status = PackageStatus.Success,
                Data = marcas,
                UserMessage = marcas.Count > 0
                    ? "Marcas listadas com sucesso."
                    : "Nenhuma marca encontrada."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<Marca>>
            {
                Status = PackageStatus.Error,
                Data = new List<Marca>(),
                UserMessage = "Não foi possível listar as marcas.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST ACTIVE
    // ======================================================
    public async Task<Package<List<Marca>>> ListarAtivasAsync()
    {
        try
        {
            var marcas = await _context.Marcas
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .ToListAsync();

            return new Package<List<Marca>>
            {
                Status = PackageStatus.Success,
                Data = marcas,
                UserMessage = marcas.Count > 0
                    ? "Marcas ativas listadas com sucesso."
                    : "Nenhuma marca ativa encontrada."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<Marca>>
            {
                Status = PackageStatus.Error,
                Data = new List<Marca>(),
                UserMessage = "Não foi possível listar as marcas ativas.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // VALIDAÇÕES
    // ======================================================
    private Package<int> Validar(Marca marca)
    {
        if (marca == null)
            return Erro("Os dados da marca não foram informados.", "Objeto marca está nulo.");

        if (string.IsNullOrWhiteSpace(marca.Nome))
            return Erro("Informe o nome da marca.", "Campo Nome obrigatório.");

        if (marca.Nome.Length > 100)
            return Erro("O nome deve ter no máximo 100 caracteres.", "Campo Nome excedeu limite.");

        if (!string.IsNullOrWhiteSpace(marca.LogoUrl) && marca.LogoUrl.Length > 255)
            return Erro("A URL do logo deve ter no máximo 255 caracteres.", "Campo LogoUrl excedeu limite.");

        return new Package<int>
        {
            Status = PackageStatus.Success,
            Data = 0
        };
    }

    private Package<int> ValidarEdicao(Marca marca)
    {
        if (marca == null)
            return Erro("Os dados da marca não foram informados.", "Objeto marca está nulo.");

        if (marca.Id <= 0)
            return Erro("Id inválido.", "Id menor ou igual a zero.");

        return Validar(marca);
    }

    private Package<int> Erro(string user, string debug)
    {
        return new Package<int>
        {
            Status = PackageStatus.Error,
            Data = 0,
            UserMessage = user,
            DebugMessage = debug
        };
    }
}