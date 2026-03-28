using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Core;

public class VeiculoMidiaService : IVeiculoMidiaService
{
    private readonly ApplicationDbContext _context;

    public VeiculoMidiaService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ======================================================
    // CREATE
    // ======================================================
    public async Task<Package<int>> CriarAsync(VeiculoMidia midia)
    {
        var validacao = await ValidarAsync(midia);
        if (validacao.Status != PackageStatus.Success)
            return validacao;

        try
        {
            midia.Id = 0;
            midia.DataCadastro = DateTime.Now;

            if (midia.Capa)
            {
                var midiasCapa = await _context.VeiculoMidias
                    .Where(x => x.VeiculoId == midia.VeiculoId && x.Capa)
                    .ToListAsync();

                if (midiasCapa.Any())
                {
                    foreach (var item in midiasCapa)
                        item.Capa = false;
                }
            }

            _context.VeiculoMidias.Add(midia);
            await _context.SaveChangesAsync();

            return new Package<int>
            {
                Status = PackageStatus.Success,
                Data = midia.Id,
                UserMessage = "Mídia cadastrada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<int>
            {
                Status = PackageStatus.Error,
                Data = 0,
                UserMessage = "Não foi possível cadastrar a mídia.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // UPDATE
    // ======================================================
    public async Task<Package<bool>> EditarAsync(VeiculoMidia midia)
    {
        var validacao = await ValidarEdicaoAsync(midia);
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
            var midiaBanco = await _context.VeiculoMidias
                .FirstOrDefaultAsync(x => x.Id == midia.Id);

            if (midiaBanco == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Mídia não encontrada.",
                    DebugMessage = $"Nenhuma mídia encontrada com Id {midia.Id}."
                };
            }

            if (midia.Capa)
            {
                var outrasCapas = await _context.VeiculoMidias
                    .Where(x => x.VeiculoId == midia.VeiculoId && x.Id != midia.Id && x.Capa)
                    .ToListAsync();

                if (outrasCapas.Any())
                {
                    foreach (var item in outrasCapas)
                        item.Capa = false;
                }
            }

            midiaBanco.VeiculoId = midia.VeiculoId;
            midiaBanco.NomeArquivo = midia.NomeArquivo;
            midiaBanco.Url = midia.Url;
            midiaBanco.BlobName = midia.BlobName;
            midiaBanco.Container = midia.Container;
            midiaBanco.Tipo = midia.Tipo;
            midiaBanco.ContentType = midia.ContentType;
            midiaBanco.TamanhoBytes = midia.TamanhoBytes;
            midiaBanco.Capa = midia.Capa;
            midiaBanco.Ordem = midia.Ordem;
            midiaBanco.Ativo = midia.Ativo;

            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Mídia atualizada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível atualizar a mídia.",
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
            var midia = await _context.VeiculoMidias
                .FirstOrDefaultAsync(x => x.Id == id);

            if (midia == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Mídia não encontrada.",
                    DebugMessage = $"Nenhuma mídia encontrada com Id {id}."
                };
            }

            _context.VeiculoMidias.Remove(midia);
            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Mídia excluída com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível excluir a mídia.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - BY ID
    // ======================================================
    public async Task<Package<VeiculoMidia>> ObterPorIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Package<VeiculoMidia>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Id inválido.",
                DebugMessage = "O id informado é menor ou igual a zero."
            };
        }

        try
        {
            var midia = await _context.VeiculoMidias
                .Include(x => x.Veiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (midia == null)
            {
                return new Package<VeiculoMidia>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Mídia não encontrada.",
                    DebugMessage = $"Nenhuma mídia encontrada com Id {id}."
                };
            }

            return new Package<VeiculoMidia>
            {
                Status = PackageStatus.Success,
                Data = midia,
                UserMessage = "Mídia localizada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<VeiculoMidia>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Não foi possível buscar a mídia.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST
    // ======================================================
    public async Task<Package<List<VeiculoMidia>>> ListarAsync()
    {
        try
        {
            var lista = await _context.VeiculoMidias
                .Include(x => x.Veiculo)
                .AsNoTracking()
                .OrderBy(x => x.VeiculoId)
                .ThenBy(x => x.Ordem)
                .ThenByDescending(x => x.Capa)
                .ThenByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Mídias listadas com sucesso."
                    : "Nenhuma mídia encontrada."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Error,
                Data = new List<VeiculoMidia>(),
                UserMessage = "Não foi possível listar as mídias.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST BY VEICULO
    // ======================================================
    public async Task<Package<List<VeiculoMidia>>> ListarPorVeiculoAsync(int veiculoId)
    {
        if (veiculoId <= 0)
        {
            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Error,
                Data = new List<VeiculoMidia>(),
                UserMessage = "Veículo inválido.",
                DebugMessage = "VeiculoId menor ou igual a zero."
            };
        }

        try
        {
            var lista = await _context.VeiculoMidias
                .Include(x => x.Veiculo)
                .AsNoTracking()
                .Where(x => x.VeiculoId == veiculoId)
                .OrderByDescending(x => x.Capa)
                .ThenBy(x => x.Ordem)
                .ThenByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Mídias do veículo listadas com sucesso."
                    : "Nenhuma mídia encontrada para este veículo."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Error,
                Data = new List<VeiculoMidia>(),
                UserMessage = "Não foi possível listar as mídias do veículo.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST ACTIVE BY VEICULO
    // ======================================================
    public async Task<Package<List<VeiculoMidia>>> ListarAtivasPorVeiculoAsync(int veiculoId)
    {
        if (veiculoId <= 0)
        {
            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Error,
                Data = new List<VeiculoMidia>(),
                UserMessage = "Veículo inválido.",
                DebugMessage = "VeiculoId menor ou igual a zero."
            };
        }

        try
        {
            var lista = await _context.VeiculoMidias
                .Include(x => x.Veiculo)
                .AsNoTracking()
                .Where(x => x.VeiculoId == veiculoId && x.Ativo)
                .OrderByDescending(x => x.Capa)
                .ThenBy(x => x.Ordem)
                .ThenByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Mídias ativas do veículo listadas com sucesso."
                    : "Nenhuma mídia ativa encontrada para este veículo."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<VeiculoMidia>>
            {
                Status = PackageStatus.Error,
                Data = new List<VeiculoMidia>(),
                UserMessage = "Não foi possível listar as mídias ativas do veículo.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // VALIDAÇÕES
    // ======================================================
    private async Task<Package<int>> ValidarAsync(VeiculoMidia midia)
    {
        if (midia == null)
            return Erro("Os dados da mídia não foram informados.", "Objeto mídia está nulo.");

        if (midia.VeiculoId <= 0)
            return Erro("Veículo inválido.", "VeiculoId menor ou igual a zero.");

        if (string.IsNullOrWhiteSpace(midia.NomeArquivo))
            return Erro("Informe o nome do arquivo.", "Campo NomeArquivo obrigatório.");

        if (midia.NomeArquivo.Length > 255)
            return Erro("O nome do arquivo deve ter no máximo 255 caracteres.", "Campo NomeArquivo excedeu limite.");

        if (string.IsNullOrWhiteSpace(midia.Url))
            return Erro("Informe a URL da mídia.", "Campo Url obrigatório.");

        if (midia.Url.Length > 500)
            return Erro("A URL da mídia deve ter no máximo 500 caracteres.", "Campo Url excedeu limite.");

        if (!string.IsNullOrWhiteSpace(midia.BlobName) && midia.BlobName.Length > 200)
            return Erro("O BlobName deve ter no máximo 200 caracteres.", "Campo BlobName excedeu limite.");

        if (!string.IsNullOrWhiteSpace(midia.Container) && midia.Container.Length > 100)
            return Erro("O Container deve ter no máximo 100 caracteres.", "Campo Container excedeu limite.");

        if (!string.IsNullOrWhiteSpace(midia.Tipo) && midia.Tipo.Length > 20)
            return Erro("O tipo deve ter no máximo 20 caracteres.", "Campo Tipo excedeu limite.");

        if (!string.IsNullOrWhiteSpace(midia.ContentType) && midia.ContentType.Length > 100)
            return Erro("O ContentType deve ter no máximo 100 caracteres.", "Campo ContentType excedeu limite.");

        if (midia.TamanhoBytes.HasValue && midia.TamanhoBytes < 0)
            return Erro("O tamanho do arquivo é inválido.", "Campo TamanhoBytes menor que zero.");

        if (midia.Ordem < 0)
            return Erro("A ordem é inválida.", "Campo Ordem menor que zero.");

        if (!string.IsNullOrWhiteSpace(midia.Tipo))
        {
            var tipo = midia.Tipo.Trim().ToLower();

            if (tipo != "imagem" && tipo != "video")
                return Erro("O tipo da mídia deve ser 'imagem' ou 'video'.", "Campo Tipo possui valor inválido.");
        }

        var veiculoExiste = await _context.Veiculos.AnyAsync(x => x.Id == midia.VeiculoId);
        if (!veiculoExiste)
            return Erro("Veículo não encontrado.", "FK VeiculoId não existe.");

        return new Package<int>
        {
            Status = PackageStatus.Success,
            Data = 0,
            UserMessage = "Validação concluída com sucesso."
        };
    }

    private async Task<Package<int>> ValidarEdicaoAsync(VeiculoMidia midia)
    {
        if (midia == null)
            return Erro("Os dados da mídia não foram informados.", "Objeto mídia está nulo.");

        if (midia.Id <= 0)
            return Erro("Id inválido.", "Id da mídia menor ou igual a zero.");

        return await ValidarAsync(midia);
    }

    private Package<int> Erro(string userMessage, string debugMessage)
    {
        return new Package<int>
        {
            Status = PackageStatus.Error,
            Data = 0,
            UserMessage = userMessage,
            DebugMessage = debugMessage
        };
    }
}