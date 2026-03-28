using Core;
using Domain;

namespace Core.Interfaces;
public interface IMarcaService
{
    Task<Package<int>> CriarAsync(Marca marca);
    Task<Package<bool>> EditarAsync(Marca marca);
    Task<Package<bool>> ExcluirAsync(int id);
    Task<Package<Marca>> ObterPorIdAsync(int id);
    Task<Package<List<Marca>>> ListarAsync();
    Task<Package<List<Marca>>> ListarAtivasAsync();
}