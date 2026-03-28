using Core;
using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Core;
public class VeiculoService : IVeiculoService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public VeiculoService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ======================================================
    // CREATE
    // ======================================================
    public async Task<Package<int>> CriarAsync(Veiculo veiculo)
    {
        var validacao = await ValidarAsync(veiculo);
        if (validacao.Status != PackageStatus.Success)
            return validacao;

        try
        {
            veiculo.Id = 0;
            veiculo.DataCadastro = DateTime.Now;
            veiculo.DataAtualizacao = null;

            if (veiculo.Vendido && !veiculo.DataVenda.HasValue)
                veiculo.DataVenda = DateTime.Now;

            _context.Veiculos.Add(veiculo);
            await _context.SaveChangesAsync();

            return new Package<int>
            {
                Status = PackageStatus.Success,
                Data = veiculo.Id,
                UserMessage = "Veículo cadastrado com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<int>
            {
                Status = PackageStatus.Error,
                Data = 0,
                UserMessage = "Não foi possível cadastrar o veículo.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // UPDATE
    // ======================================================
    public async Task<Package<bool>> EditarAsync(Veiculo veiculo)
    {
        var validacao = await ValidarEdicaoAsync(veiculo);
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
            var veiculoBanco = await _context.Veiculos
                .FirstOrDefaultAsync(x => x.Id == veiculo.Id);

            if (veiculoBanco == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Veículo não encontrado.",
                    DebugMessage = $"Nenhum veículo encontrado com Id {veiculo.Id}."
                };
            }

            veiculoBanco.LojaId = veiculo.LojaId;
            veiculoBanco.Titulo = veiculo.Titulo;
            veiculoBanco.MarcaId = veiculo.MarcaId;
            veiculoBanco.VendedorId = veiculo.VendedorId;

            veiculoBanco.Modelo = veiculo.Modelo;
            veiculoBanco.Versao = veiculo.Versao;
            veiculoBanco.AnoFabricacao = veiculo.AnoFabricacao;
            veiculoBanco.AnoModelo = veiculo.AnoModelo;
            veiculoBanco.Cor = veiculo.Cor;
            veiculoBanco.Combustivel = veiculo.Combustivel;
            veiculoBanco.Cambio = veiculo.Cambio;
            veiculoBanco.Quilometragem = veiculo.Quilometragem;

            veiculoBanco.Placa = veiculo.Placa;
            veiculoBanco.Chassi = veiculo.Chassi;
            veiculoBanco.Renavam = veiculo.Renavam;

            veiculoBanco.PrecoVenda = veiculo.PrecoVenda;
            veiculoBanco.PrecoPromocional = veiculo.PrecoPromocional;
            veiculoBanco.PrecoFipe = veiculo.PrecoFipe;

            veiculoBanco.AceitaTroca = veiculo.AceitaTroca;
            veiculoBanco.Financiavel = veiculo.Financiavel;
            veiculoBanco.Destaque = veiculo.Destaque;
            veiculoBanco.Seminovo = veiculo.Seminovo;

            veiculoBanco.Ativo = veiculo.Ativo;

            if (!veiculoBanco.Vendido && veiculo.Vendido)
            {
                veiculoBanco.Vendido = true;
                veiculoBanco.DataVenda = veiculo.DataVenda ?? DateTime.Now;
                veiculoBanco.VendidoPorUsuarioId = veiculo.VendidoPorUsuarioId ?? veiculoBanco.VendidoPorUsuarioId;
            }
            else if (veiculoBanco.Vendido && !veiculo.Vendido)
            {
                veiculoBanco.Vendido = false;
                veiculoBanco.DataVenda = null;
                veiculoBanco.VendidoPorUsuarioId = null;
            }
            else
            {
                veiculoBanco.Vendido = veiculo.Vendido;
                veiculoBanco.DataVenda = veiculo.Vendido
                    ? veiculo.DataVenda ?? veiculoBanco.DataVenda
                    : null;
                veiculoBanco.VendidoPorUsuarioId = veiculo.Vendido
                    ? veiculo.VendidoPorUsuarioId ?? veiculoBanco.VendidoPorUsuarioId
                    : null;
            }

            veiculoBanco.Descricao = veiculo.Descricao;
            veiculoBanco.UrlVideo = veiculo.UrlVideo;
            veiculoBanco.ObservacoesInternas = veiculo.ObservacoesInternas;
            veiculoBanco.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Veículo atualizado com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível atualizar o veículo.",
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
            var veiculo = await _context.Veiculos
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (veiculo == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Veículo não encontrado.",
                    DebugMessage = $"Nenhum veículo encontrado com Id {id}."
                };
            }

            if (veiculo.Caracteristica != null)
                _context.VeiculoCaracteristicas.Remove(veiculo.Caracteristica);

            if (veiculo.Midias != null && veiculo.Midias.Any())
            {
                foreach (var midia in veiculo.Midias)
                {
                    ExcluirArquivoFisicoSeExistir(midia.Url);
                }

                _context.VeiculoMidias.RemoveRange(veiculo.Midias);
            }

            _context.Veiculos.Remove(veiculo);
            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Veículo excluído com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível excluir o veículo.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - BY ID
    // ======================================================
    public async Task<Package<Veiculo>> ObterPorIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Package<Veiculo>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Id inválido.",
                DebugMessage = "O id informado é menor ou igual a zero."
            };
        }

        try
        {
            var veiculo = await _context.Veiculos
                .Include(x => x.Loja)
                .Include(x => x.Marca)
                .Include(x => x.Vendedor)
                .Include(x => x.VendidoPorUsuario)
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (veiculo == null)
            {
                return new Package<Veiculo>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Veículo não encontrado.",
                    DebugMessage = $"Nenhum veículo encontrado com Id {id}."
                };
            }

            return new Package<Veiculo>
            {
                Status = PackageStatus.Success,
                Data = veiculo,
                UserMessage = "Veículo localizado com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<Veiculo>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Não foi possível buscar o veículo.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST
    // ======================================================
    public async Task<Package<List<Veiculo>>> ListarAsync()
    {
        try
        {
            var lista = await _context.Veiculos
                .Include(x => x.Loja)
                .Include(x => x.Marca)
                .Include(x => x.Vendedor)
                .Include(x => x.VendidoPorUsuario)
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .AsNoTracking()
                .OrderByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Veículos listados com sucesso."
                    : "Nenhum veículo encontrado."
            };
        }
        catch (Exception ex)
        {
            try
            {
                var lista = await _context.Veiculos
                    .AsNoTracking()
                    .OrderByDescending(x => x.DataCadastro)
                    .ToListAsync();

                var lojas = await _context.Lojas
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id);

                var marcas = await _context.Marcas
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id);

                var vendedores = await _context.Vendedores
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id);

                var usuarios = await _context.Users
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id);

                var caracteristicas = await _context.VeiculoCaracteristicas
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.VeiculoId);

                var midiasPorVeiculo = await _context.VeiculoMidias
                    .AsNoTracking()
                    .GroupBy(x => x.VeiculoId)
                    .ToDictionaryAsync(
                        group => group.Key,
                        group => group.OrderBy(midia => midia.Ordem).ToList());

                foreach (var veiculo in lista)
                {
                    veiculo.Loja = lojas.GetValueOrDefault(veiculo.LojaId);
                    veiculo.Marca = marcas.GetValueOrDefault(veiculo.MarcaId);
                    veiculo.Vendedor = veiculo.VendedorId.HasValue
                        ? vendedores.GetValueOrDefault(veiculo.VendedorId.Value)
                        : null;
                    veiculo.VendidoPorUsuario = veiculo.VendidoPorUsuarioId.HasValue
                        ? usuarios.GetValueOrDefault(veiculo.VendidoPorUsuarioId.Value)
                        : null;
                    veiculo.Caracteristica = caracteristicas.GetValueOrDefault(veiculo.Id);
                    veiculo.Midias = midiasPorVeiculo.GetValueOrDefault(veiculo.Id) ?? [];
                }

                return new Package<List<Veiculo>>
                {
                    Status = PackageStatus.Success,
                    Data = lista,
                    UserMessage = lista.Count > 0
                        ? "Veículos listados com sucesso."
                        : "Nenhum veículo encontrado.",
                    DebugMessage = $"Fallback aplicado após falha na consulta principal: {ex.Message}"
                };
            }
            catch (Exception fallbackEx)
            {
                return new Package<List<Veiculo>>
                {
                    Status = PackageStatus.Error,
                    Data = new List<Veiculo>(),
                    UserMessage = "Não foi possível listar os veículos.",
                    DebugMessage = $"{ex.Message} | Fallback: {fallbackEx.Message}"
                };
            }
        }
    }

    // ======================================================
    // READ - LIST ACTIVE
    // ======================================================
    public async Task<Package<List<Veiculo>>> ListarAtivosAsync()
    {
        try
        {
            var lista = await _context.Veiculos
                .Include(x => x.Loja)
                .Include(x => x.Marca)
                .Include(x => x.Vendedor)
                .Include(x => x.VendidoPorUsuario)
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .AsNoTracking()
                .Where(x => x.Ativo && !x.Vendido)
                .OrderByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Veículos ativos listados com sucesso."
                    : "Nenhum veículo ativo encontrado."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Não foi possível listar os veículos ativos.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST BY LOJA
    // ======================================================
    public async Task<Package<List<Veiculo>>> ListarPorLojaAsync(int lojaId)
    {
        if (lojaId <= 0)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Loja inválida.",
                DebugMessage = "LojaId menor ou igual a zero."
            };
        }

        try
        {
            var lista = await _context.Veiculos
                .Include(x => x.Loja)
                .Include(x => x.Marca)
                .Include(x => x.Vendedor)
                .Include(x => x.VendidoPorUsuario)
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .AsNoTracking()
                .Where(x => x.LojaId == lojaId)
                .OrderByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Veículos da loja listados com sucesso."
                    : "Nenhum veículo encontrado para esta loja."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Não foi possível listar os veículos da loja.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST BY MARCA
    // ======================================================
    public async Task<Package<List<Veiculo>>> ListarPorMarcaAsync(int marcaId)
    {
        if (marcaId <= 0)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Marca inválida.",
                DebugMessage = "MarcaId menor ou igual a zero."
            };
        }

        try
        {
            var lista = await _context.Veiculos
                .Include(x => x.Loja)
                .Include(x => x.Marca)
                .Include(x => x.Vendedor)
                .Include(x => x.VendidoPorUsuario)
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .AsNoTracking()
                .Where(x => x.MarcaId == marcaId)
                .OrderByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Veículos da marca listados com sucesso."
                    : "Nenhum veículo encontrado para esta marca."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Não foi possível listar os veículos da marca.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - LIST BY VENDEDOR
    // ======================================================
    public async Task<Package<List<Veiculo>>> ListarPorVendedorAsync(int vendedorId)
    {
        if (vendedorId <= 0)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Vendedor inválido.",
                DebugMessage = "VendedorId menor ou igual a zero."
            };
        }

        try
        {
            var lista = await _context.Veiculos
                .Include(x => x.Loja)
                .Include(x => x.Marca)
                .Include(x => x.Vendedor)
                .Include(x => x.VendidoPorUsuario)
                .Include(x => x.Caracteristica)
                .Include(x => x.Midias)
                .AsNoTracking()
                .Where(x => x.VendedorId == vendedorId)
                .OrderByDescending(x => x.DataCadastro)
                .ToListAsync();

            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Success,
                Data = lista,
                UserMessage = lista.Count > 0
                    ? "Veículos do vendedor listados com sucesso."
                    : "Nenhum veículo encontrado para este vendedor."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<Veiculo>>
            {
                Status = PackageStatus.Error,
                Data = new List<Veiculo>(),
                UserMessage = "Não foi possível listar os veículos do vendedor.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // VALIDAÇÕES
    // ======================================================
    private async Task<Package<int>> ValidarAsync(Veiculo veiculo)
    {
        if (veiculo == null)
            return Erro("Os dados do veículo não foram informados.", "Objeto veículo está nulo.");

        if (veiculo.LojaId <= 0)
            return Erro("Loja inválida.", "LojaId menor ou igual a zero.");

        if (veiculo.MarcaId <= 0)
            return Erro("Marca inválida.", "MarcaId menor ou igual a zero.");

        if (string.IsNullOrWhiteSpace(veiculo.Titulo))
            return Erro("Informe o título do veículo.", "Campo Titulo obrigatório.");

        if (veiculo.Titulo.Length > 150)
            return Erro("O título deve ter no máximo 150 caracteres.", "Campo Titulo excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Modelo) && veiculo.Modelo.Length > 100)
            return Erro("O modelo deve ter no máximo 100 caracteres.", "Campo Modelo excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Versao) && veiculo.Versao.Length > 100)
            return Erro("A versão deve ter no máximo 100 caracteres.", "Campo Versao excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Cor) && veiculo.Cor.Length > 30)
            return Erro("A cor deve ter no máximo 30 caracteres.", "Campo Cor excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Combustivel) && veiculo.Combustivel.Length > 30)
            return Erro("O combustível deve ter no máximo 30 caracteres.", "Campo Combustivel excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Cambio) && veiculo.Cambio.Length > 30)
            return Erro("O câmbio deve ter no máximo 30 caracteres.", "Campo Cambio excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Placa) && veiculo.Placa.Length > 20)
            return Erro("A placa deve ter no máximo 20 caracteres.", "Campo Placa excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Chassi) && veiculo.Chassi.Length > 50)
            return Erro("O chassi deve ter no máximo 50 caracteres.", "Campo Chassi excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Renavam) && veiculo.Renavam.Length > 50)
            return Erro("O renavam deve ter no máximo 50 caracteres.", "Campo Renavam excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.Descricao) && veiculo.Descricao.Length > 1000)
            return Erro("A descrição deve ter no máximo 1000 caracteres.", "Campo Descricao excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.UrlVideo) && veiculo.UrlVideo.Length > 255)
            return Erro("A URL do vídeo deve ter no máximo 255 caracteres.", "Campo UrlVideo excedeu limite.");

        if (!string.IsNullOrWhiteSpace(veiculo.ObservacoesInternas) && veiculo.ObservacoesInternas.Length > 255)
            return Erro("As observações internas devem ter no máximo 255 caracteres.", "Campo ObservacoesInternas excedeu limite.");

        if (veiculo.AnoFabricacao.HasValue && veiculo.AnoFabricacao <= 0)
            return Erro("Ano de fabricação inválido.", "AnoFabricacao menor ou igual a zero.");

        if (veiculo.AnoModelo.HasValue && veiculo.AnoModelo <= 0)
            return Erro("Ano do modelo inválido.", "AnoModelo menor ou igual a zero.");

        if (veiculo.Quilometragem.HasValue && veiculo.Quilometragem < 0)
            return Erro("Quilometragem inválida.", "Quilometragem menor que zero.");

        if (veiculo.PrecoVenda.HasValue && veiculo.PrecoVenda < 0)
            return Erro("Preço de venda inválido.", "PrecoVenda menor que zero.");

        if (veiculo.PrecoPromocional.HasValue && veiculo.PrecoPromocional < 0)
            return Erro("Preço promocional inválido.", "PrecoPromocional menor que zero.");

        if (veiculo.PrecoFipe.HasValue && veiculo.PrecoFipe < 0)
            return Erro("Preço FIPE inválido.", "PrecoFipe menor que zero.");

        var lojaExiste = await _context.Lojas.AnyAsync(x => x.Id == veiculo.LojaId);
        if (!lojaExiste)
            return Erro("Loja não encontrada.", "FK LojaId não existe.");

        var marcaExiste = await _context.Marcas.AnyAsync(x => x.Id == veiculo.MarcaId);
        if (!marcaExiste)
            return Erro("Marca não encontrada.", "FK MarcaId não existe.");

        if (veiculo.VendedorId.HasValue)
        {
            var vendedorExiste = await _context.Vendedores.AnyAsync(x => x.Id == veiculo.VendedorId.Value);
            if (!vendedorExiste)
                return Erro("Vendedor não encontrado.", "FK VendedorId não existe.");
        }

        if (veiculo.Vendido && !veiculo.DataVenda.HasValue)
        {
            // permitido; data será preenchida automaticamente no create/update
        }

        return new Package<int>
        {
            Status = PackageStatus.Success,
            Data = 0,
            UserMessage = "Validação concluída com sucesso."
        };
    }

    private async Task<Package<int>> ValidarEdicaoAsync(Veiculo veiculo)
    {
        if (veiculo == null)
            return Erro("Os dados do veículo não foram informados.", "Objeto veículo está nulo.");

        if (veiculo.Id <= 0)
            return Erro("Id inválido.", "Id do veículo menor ou igual a zero.");

        return await ValidarAsync(veiculo);
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

    private void ExcluirArquivoFisicoSeExistir(string? urlRelativa)
    {
        if (string.IsNullOrWhiteSpace(urlRelativa))
            return;

        try
        {
            var relative = urlRelativa.TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            var path = Path.Combine(_environment.WebRootPath, relative);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignora falha de exclusão física para não impedir a remoção do registro.
        }
    }
}
