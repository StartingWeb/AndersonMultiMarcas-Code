using Core;
using Core.Dtos;
using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class LojaService : ILojaService
{
    private readonly ApplicationDbContext _context;

    public LojaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Package<int>> CriarAsync(LojaDto loja)
    {
        var validacao = ValidarCriacao(loja);
        if (validacao.Status != PackageStatus.Success)
            return validacao;

        try
        {
            var entidade = MapToEntity(loja!);
            entidade.Id = 0;
            entidade.DataCadastro = DateTime.Now;

            _context.Lojas.Add(entidade);
            await _context.SaveChangesAsync();

            return new Package<int>
            {
                Status = PackageStatus.Success,
                Data = entidade.Id,
                UserMessage = "Loja cadastrada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<int>
            {
                Status = PackageStatus.Error,
                Data = 0,
                UserMessage = "Não foi possível cadastrar a loja.",
                DebugMessage = ex.Message
            };
        }
    }

    public async Task<Package<bool>> EditarAsync(LojaDto loja)
    {
        var validacao = ValidarEdicao(loja);
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
            var lojaBanco = await _context.Lojas.FirstOrDefaultAsync(x => x.Id == loja!.Id);

            if (lojaBanco is null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Loja não encontrada.",
                    DebugMessage = $"Nenhuma loja encontrada com Id {loja.Id}."
                };
            }

            AtualizarEntidade(lojaBanco, loja);

            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Loja atualizada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível atualizar a loja.",
                DebugMessage = ex.Message
            };
        }
    }

    public async Task<Package<bool>> ExcluirAsync(int id)
    {
        if (id <= 0)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Id inválido.",
                DebugMessage = "O id informado deve ser maior que zero."
            };
        }

        try
        {
            var loja = await _context.Lojas.FirstOrDefaultAsync(x => x.Id == id);

            if (loja is null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Loja não encontrada.",
                    DebugMessage = $"Nenhuma loja encontrada com Id {id}."
                };
            }

            _context.Lojas.Remove(loja);
            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Loja excluída com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Não foi possível excluir a loja.",
                DebugMessage = ex.Message
            };
        }
    }

    public async Task<Package<LojaDto>> ObterPorIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Package<LojaDto>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Id inválido.",
                DebugMessage = "O id informado deve ser maior que zero."
            };
        }

        try
        {
            var loja = await _context.Lojas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (loja is null)
            {
                return new Package<LojaDto>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Loja não encontrada.",
                    DebugMessage = $"Nenhuma loja encontrada com Id {id}."
                };
            }

            return new Package<LojaDto>
            {
                Status = PackageStatus.Success,
                Data = MapToDto(loja),
                UserMessage = "Loja localizada com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<LojaDto>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Não foi possível buscar a loja.",
                DebugMessage = ex.Message
            };
        }
    }

    public async Task<Package<List<LojaDto>>> ListarAsync()
    {
        try
        {
            var lojas = await _context.Lojas
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .ToListAsync();

            var lojasDto = lojas.Select(MapToDto).ToList();

            return new Package<List<LojaDto>>
            {
                Status = PackageStatus.Success,
                Data = lojasDto,
                UserMessage = lojasDto.Count > 0
                    ? "Lojas listadas com sucesso."
                    : "Nenhuma loja encontrada."
            };
        }
        catch (Exception ex)
        {
            try
            {
                var lojas = await _context.Lojas
                    .AsNoTracking()
                    .ToListAsync();

                var lojasDto = lojas
                    .OrderBy(x => x.Nome)
                    .Select(MapToDto)
                    .ToList();

                return new Package<List<LojaDto>>
                {
                    Status = PackageStatus.Success,
                    Data = lojasDto,
                    UserMessage = lojasDto.Count > 0
                        ? "Lojas listadas com sucesso."
                        : "Nenhuma loja encontrada.",
                    DebugMessage = $"Fallback simples aplicado após falha na consulta principal: {ex.Message}"
                };
            }
            catch (Exception fallbackEx)
            {
                return new Package<List<LojaDto>>
                {
                    Status = PackageStatus.Error,
                    Data = new List<LojaDto>(),
                    UserMessage = "Não foi possível listar as lojas.",
                    DebugMessage = $"{ex.Message} | Fallback: {fallbackEx.Message}"
                };
            }
        }
    }

    public async Task<Package<List<LojaDto>>> ListarAtivasAsync()
    {
        try
        {
            var lojas = await _context.Lojas
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .ToListAsync();

            var lojasDto = lojas.Select(MapToDto).ToList();

            return new Package<List<LojaDto>>
            {
                Status = PackageStatus.Success,
                Data = lojasDto,
                UserMessage = lojasDto.Count > 0
                    ? "Lojas ativas listadas com sucesso."
                    : "Nenhuma loja ativa encontrada."
            };
        }
        catch (Exception ex)
        {
            return new Package<List<LojaDto>>
            {
                Status = PackageStatus.Error,
                Data = new List<LojaDto>(),
                UserMessage = "Não foi possível listar as lojas ativas.",
                DebugMessage = ex.Message
            };
        }
    }

    private Package<int> ValidarCriacao(LojaDto? loja)
    {
        return Validar(loja);
    }

    private Package<int> ValidarEdicao(LojaDto? loja)
    {
        if (loja is null)
            return ErroInt("Os dados da loja não foram informados.", "Objeto loja está nulo.");

        if (loja.Id <= 0)
            return ErroInt("Id da loja inválido.", "O campo Id deve ser maior que zero para edição.");

        return Validar(loja);
    }

    private Package<int> Validar(LojaDto? loja)
    {
        if (loja is null)
            return ErroInt("Os dados da loja não foram informados.", "Objeto loja está nulo.");

        loja.Nome = loja.Nome?.Trim() ?? string.Empty;
        loja.RazaoSocial = loja.RazaoSocial?.Trim();
        loja.Cnpj = loja.Cnpj?.Trim();
        loja.Email = loja.Email?.Trim();
        loja.Telefone = loja.Telefone?.Trim();
        loja.Endereco = loja.Endereco?.Trim();
        loja.Numero = loja.Numero?.Trim();
        loja.Complemento = loja.Complemento?.Trim();
        loja.Bairro = loja.Bairro?.Trim();
        loja.Cidade = loja.Cidade?.Trim();
        loja.Uf = loja.Uf?.Trim().ToUpper();
        loja.Cep = loja.Cep?.Trim();

        if (string.IsNullOrWhiteSpace(loja.Nome))
            return ErroInt("Informe o nome da loja.", "O campo Nome é obrigatório.");

        if (loja.Nome.Length > 150)
            return ErroInt("O nome da loja deve ter no máximo 150 caracteres.", "Campo Nome excedeu o limite de 150 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.RazaoSocial) && loja.RazaoSocial.Length > 200)
            return ErroInt("A razão social deve ter no máximo 200 caracteres.", "Campo RazaoSocial excedeu o limite de 200 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Cnpj) && loja.Cnpj.Length > 20)
            return ErroInt("O CNPJ deve ter no máximo 20 caracteres.", "Campo Cnpj excedeu o limite de 20 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Email))
        {
            if (loja.Email.Length > 150)
                return ErroInt("O e-mail deve ter no máximo 150 caracteres.", "Campo Email excedeu o limite de 150 caracteres.");

            if (!EhEmailValido(loja.Email))
                return ErroInt("Informe um e-mail válido.", "Campo Email possui formato inválido.");
        }

        if (!string.IsNullOrWhiteSpace(loja.Telefone) && loja.Telefone.Length > 20)
            return ErroInt("O telefone deve ter no máximo 20 caracteres.", "Campo Telefone excedeu o limite de 20 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Endereco) && loja.Endereco.Length > 200)
            return ErroInt("O endereço deve ter no máximo 200 caracteres.", "Campo Endereco excedeu o limite de 200 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Numero) && loja.Numero.Length > 20)
            return ErroInt("O número deve ter no máximo 20 caracteres.", "Campo Numero excedeu o limite de 20 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Complemento) && loja.Complemento.Length > 100)
            return ErroInt("O complemento deve ter no máximo 100 caracteres.", "Campo Complemento excedeu o limite de 100 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Bairro) && loja.Bairro.Length > 100)
            return ErroInt("O bairro deve ter no máximo 100 caracteres.", "Campo Bairro excedeu o limite de 100 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Cidade) && loja.Cidade.Length > 100)
            return ErroInt("A cidade deve ter no máximo 100 caracteres.", "Campo Cidade excedeu o limite de 100 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Uf) && loja.Uf.Length != 2)
            return ErroInt("A UF deve ter exatamente 2 caracteres.", "Campo Uf deve possuir exatamente 2 caracteres.");

        if (!string.IsNullOrWhiteSpace(loja.Cep) && loja.Cep.Length > 10)
            return ErroInt("O CEP deve ter no máximo 10 caracteres.", "Campo Cep excedeu o limite de 10 caracteres.");

        return new Package<int>
        {
            Status = PackageStatus.Success,
            Data = 0,
            UserMessage = "Validação concluída com sucesso."
        };
    }

    private static Loja MapToEntity(LojaDto dto)
    {
        return new Loja
        {
            Id = dto.Id,
            Nome = dto.Nome?.Trim() ?? string.Empty,
            RazaoSocial = dto.RazaoSocial?.Trim(),
            Cnpj = dto.Cnpj?.Trim(),
            Email = dto.Email?.Trim(),
            Telefone = dto.Telefone?.Trim(),
            Endereco = dto.Endereco?.Trim(),
            Numero = dto.Numero?.Trim(),
            Complemento = dto.Complemento?.Trim(),
            Bairro = dto.Bairro?.Trim(),
            Cidade = dto.Cidade?.Trim(),
            Uf = dto.Uf?.Trim().ToUpper(),
            Cep = dto.Cep?.Trim(),
            Ativo = dto.Ativo,
            DataCadastro = dto.DataCadastro
        };
    }

    private static LojaDto MapToDto(Loja loja)
    {
        return new LojaDto
        {
            Id = loja.Id,
            Nome = loja.Nome,
            RazaoSocial = loja.RazaoSocial,
            Cnpj = loja.Cnpj,
            Email = loja.Email,
            Telefone = loja.Telefone,
            Endereco = loja.Endereco,
            Numero = loja.Numero,
            Complemento = loja.Complemento,
            Bairro = loja.Bairro,
            Cidade = loja.Cidade,
            Uf = loja.Uf,
            Cep = loja.Cep,
            Ativo = loja.Ativo,
            DataCadastro = loja.DataCadastro
        };
    }

    private static void AtualizarEntidade(Loja destino, LojaDto origem)
    {
        destino.Nome = origem.Nome?.Trim() ?? string.Empty;
        destino.RazaoSocial = origem.RazaoSocial?.Trim();
        destino.Cnpj = origem.Cnpj?.Trim();
        destino.Email = origem.Email?.Trim();
        destino.Telefone = origem.Telefone?.Trim();
        destino.Endereco = origem.Endereco?.Trim();
        destino.Numero = origem.Numero?.Trim();
        destino.Complemento = origem.Complemento?.Trim();
        destino.Bairro = origem.Bairro?.Trim();
        destino.Cidade = origem.Cidade?.Trim();
        destino.Uf = origem.Uf?.Trim().ToUpper();
        destino.Cep = origem.Cep?.Trim();
        destino.Ativo = origem.Ativo;
    }

    private static bool EhEmailValido(string email)
    {
        try
        {
            var endereco = new System.Net.Mail.MailAddress(email);
            return endereco.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static Package<int> ErroInt(string userMessage, string debugMessage)
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
