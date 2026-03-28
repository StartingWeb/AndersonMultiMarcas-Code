using Core;
using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Core
{
    public class VendedorService : IVendedorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public VendedorService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ======================================================
        // CREATE
        // ======================================================
        public async Task<Package<int>> CriarAsync(Vendedor vendedor)
        {
            var validacao = await ValidarAsync(vendedor);
            if (validacao.Status != PackageStatus.Success)
                return validacao;

            try
            {
                vendedor.Id = 0;
                vendedor.DataCadastro = DateTime.Now;

                _context.Vendedores.Add(vendedor);
                await _context.SaveChangesAsync();

                return new Package<int>
                {
                    Status = PackageStatus.Success,
                    Data = vendedor.Id,
                    UserMessage = "Vendedor cadastrado com sucesso."
                };
            }
            catch (Exception ex)
            {
                return new Package<int>
                {
                    Status = PackageStatus.Error,
                    Data = 0,
                    UserMessage = "Não foi possível cadastrar o vendedor.",
                    DebugMessage = ex.Message
                };
            }
        }

        // ======================================================
        // UPDATE
        // ======================================================
        public async Task<Package<bool>> EditarAsync(Vendedor vendedor)
        {
            var validacao = await ValidarEdicaoAsync(vendedor);
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
                var vendedorBanco = await _context.Vendedores
                    .FirstOrDefaultAsync(x => x.Id == vendedor.Id);

                if (vendedorBanco == null)
                {
                    return new Package<bool>
                    {
                        Status = PackageStatus.Error,
                        Data = false,
                        UserMessage = "Vendedor não encontrado.",
                        DebugMessage = $"Nenhum vendedor encontrado com Id {vendedor.Id}."
                    };
                }

                vendedorBanco.Nome = vendedor.Nome;
                vendedorBanco.Email = vendedor.Email;
                vendedorBanco.Telefone = vendedor.Telefone;
                vendedorBanco.Whatsapp = vendedor.Whatsapp;
                vendedorBanco.Cpf = vendedor.Cpf;
                vendedorBanco.FotoUrl = vendedor.FotoUrl;
                vendedorBanco.Cargo = vendedor.Cargo;
                vendedorBanco.Ativo = vendedor.Ativo;
                vendedorBanco.LojaId = vendedor.LojaId;

                await _context.SaveChangesAsync();

                return new Package<bool>
                {
                    Status = PackageStatus.Success,
                    Data = true,
                    UserMessage = "Vendedor atualizado com sucesso."
                };
            }
            catch (Exception ex)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Não foi possível atualizar o vendedor.",
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
                    DebugMessage = "Id menor ou igual a zero."
                };
            }

            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(x => x.Id == id);

                if (vendedor == null)
                {
                    return new Package<bool>
                    {
                        Status = PackageStatus.Error,
                        Data = false,
                        UserMessage = "Vendedor não encontrado.",
                        DebugMessage = $"Nenhum vendedor encontrado com Id {id}."
                    };
                }

                ExcluirArquivoFisicoSeExistir(vendedor.FotoUrl);
                _context.Vendedores.Remove(vendedor);
                await _context.SaveChangesAsync();

                return new Package<bool>
                {
                    Status = PackageStatus.Success,
                    Data = true,
                    UserMessage = "Vendedor excluído com sucesso."
                };
            }
            catch (Exception ex)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Não foi possível excluir o vendedor.",
                    DebugMessage = ex.Message
                };
            }
        }

        // ======================================================
        // READ - BY ID
        // ======================================================
        public async Task<Package<Vendedor>> ObterPorIdAsync(int id)
        {
            if (id <= 0)
            {
                return new Package<Vendedor>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Id inválido."
                };
            }

            try
            {
                var vendedor = await _context.Vendedores
                    .Include(x => x.Loja)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (vendedor == null)
                {
                    return new Package<Vendedor>
                    {
                        Status = PackageStatus.Error,
                        Data = null,
                        UserMessage = "Vendedor não encontrado."
                    };
                }

                return new Package<Vendedor>
                {
                    Status = PackageStatus.Success,
                    Data = vendedor,
                    UserMessage = "Vendedor localizado com sucesso."
                };
            }
            catch (Exception ex)
            {
                return new Package<Vendedor>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Erro ao buscar vendedor.",
                    DebugMessage = ex.Message
                };
            }
        }

        // ======================================================
        // READ - LIST
        // ======================================================
        public async Task<Package<List<Vendedor>>> ListarAsync()
        {
            try
            {
                var lista = await _context.Vendedores
                    .Include(x => x.Loja)
                    .AsNoTracking()
                    .OrderBy(x => x.Nome)
                    .ToListAsync();

                return new Package<List<Vendedor>>
                {
                    Status = PackageStatus.Success,
                    Data = lista,
                    UserMessage = lista.Count > 0
                        ? "Vendedores listados com sucesso."
                        : "Nenhum vendedor encontrado."
                };
            }
            catch (Exception ex)
            {
                try
                {
                    var vendedores = await _context.Vendedores
                        .AsNoTracking()
                        .OrderBy(x => x.Nome)
                        .ToListAsync();

                    var lojas = await _context.Lojas
                        .AsNoTracking()
                        .ToDictionaryAsync(x => x.Id);

                    foreach (var vendedor in vendedores)
                    {
                        if (lojas.TryGetValue(vendedor.LojaId, out var loja))
                        {
                            vendedor.Loja = loja;
                        }
                    }

                    return new Package<List<Vendedor>>
                    {
                        Status = PackageStatus.Success,
                        Data = vendedores,
                        UserMessage = vendedores.Count > 0
                            ? "Vendedores listados com sucesso."
                            : "Nenhum vendedor encontrado.",
                        DebugMessage = $"Fallback sem Include utilizado após falha: {ex.Message}"
                    };
                }
                catch (Exception fallbackEx)
                {
                    return new Package<List<Vendedor>>
                    {
                        Status = PackageStatus.Error,
                        Data = new List<Vendedor>(),
                        UserMessage = "Erro ao listar vendedores.",
                        DebugMessage = $"Consulta principal: {ex.Message} | Fallback: {fallbackEx.Message}"
                    };
                }
            }
        }

        // ======================================================
        // READ - LIST BY LOJA
        // ======================================================
        public async Task<Package<List<Vendedor>>> ListarPorLojaAsync(int lojaId)
        {
            if (lojaId <= 0)
            {
                return new Package<List<Vendedor>>
                {
                    Status = PackageStatus.Error,
                    Data = new List<Vendedor>(),
                    UserMessage = "Loja inválida."
                };
            }

            try
            {
                var lista = await _context.Vendedores
                    .Where(x => x.LojaId == lojaId)
                    .AsNoTracking()
                    .OrderBy(x => x.Nome)
                    .ToListAsync();

                return new Package<List<Vendedor>>
                {
                    Status = PackageStatus.Success,
                    Data = lista,
                    UserMessage = lista.Count > 0
                        ? "Vendedores da loja listados com sucesso."
                        : "Nenhum vendedor encontrado para esta loja."
                };
            }
            catch (Exception ex)
            {
                return new Package<List<Vendedor>>
                {
                    Status = PackageStatus.Error,
                    Data = new List<Vendedor>(),
                    UserMessage = "Erro ao listar vendedores.",
                    DebugMessage = ex.Message
                };
            }
        }

        // ======================================================
        // VALIDAÇÕES
        // ======================================================
        private async Task<Package<int>> ValidarAsync(Vendedor vendedor)
        {
            if (vendedor == null)
                return Erro("Dados não informados.", "Objeto nulo.");

            if (string.IsNullOrWhiteSpace(vendedor.Nome))
                return Erro("Informe o nome do vendedor.", "Campo Nome obrigatório.");

            if (vendedor.Nome.Length > 150)
                return Erro("Nome deve ter no máximo 150 caracteres.", "Nome excedeu limite.");

            if (vendedor.LojaId <= 0)
                return Erro("Loja inválida.", "LojaId inválido.");

            var lojaExiste = await _context.Lojas.AnyAsync(x => x.Id == vendedor.LojaId);
            if (!lojaExiste)
                return Erro("Loja não encontrada.", "FK LojaId não existe.");

            if (!string.IsNullOrWhiteSpace(vendedor.Email) && vendedor.Email.Length > 150)
                return Erro("Email deve ter no máximo 150 caracteres.", "Email excedeu limite.");

            if (!string.IsNullOrWhiteSpace(vendedor.Telefone) && vendedor.Telefone.Length > 20)
                return Erro("Telefone inválido.", "Telefone excedeu limite.");

            if (!string.IsNullOrWhiteSpace(vendedor.Whatsapp) && vendedor.Whatsapp.Length > 20)
                return Erro("Whatsapp inválido.", "Whatsapp excedeu limite.");

            if (!string.IsNullOrWhiteSpace(vendedor.Cpf) && vendedor.Cpf.Length > 20)
                return Erro("CPF inválido.", "Cpf excedeu limite.");

            if (!string.IsNullOrWhiteSpace(vendedor.FotoUrl) && vendedor.FotoUrl.Length > 255)
                return Erro("URL da foto inválida.", "FotoUrl excedeu limite.");

            if (!string.IsNullOrWhiteSpace(vendedor.Cargo) && vendedor.Cargo.Length > 100)
                return Erro("Cargo inválido.", "Cargo excedeu limite.");

            return new Package<int>
            {
                Status = PackageStatus.Success,
                Data = 0
            };
        }

        private async Task<Package<int>> ValidarEdicaoAsync(Vendedor vendedor)
        {
            if (vendedor == null)
                return Erro("Dados não informados.", "Objeto nulo.");

            if (vendedor.Id <= 0)
                return Erro("Id inválido.", "Id menor ou igual a zero.");

            return await ValidarAsync(vendedor);
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
                // Ignora falha do arquivo físico para não bloquear a exclusão do vendedor.
            }
        }
    }
}
