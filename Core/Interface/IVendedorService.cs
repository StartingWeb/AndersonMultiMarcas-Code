using Core;
using Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces;
public interface IVendedorService
{
    Task<Package<int>> CriarAsync(Vendedor vendedor);

    Task<Package<bool>> EditarAsync(Vendedor vendedor);

    Task<Package<bool>> ExcluirAsync(int id);

    Task<Package<Vendedor>> ObterPorIdAsync(int id);

    Task<Package<List<Vendedor>>> ListarAsync();

    Task<Package<List<Vendedor>>> ListarPorLojaAsync(int lojaId);
}