using Core;
using Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces;
public interface IVeiculoService
{
    Task<Package<int>> CriarAsync(Veiculo veiculo);
    Task<Package<bool>> EditarAsync(Veiculo veiculo);
    Task<Package<bool>> ExcluirAsync(int id);
    Task<Package<Veiculo>> ObterPorIdAsync(int id);
    Task<Package<List<Veiculo>>> ListarAsync();
    Task<Package<List<Veiculo>>> ListarAtivosAsync();
    Task<Package<List<Veiculo>>> ListarPorLojaAsync(int lojaId);
    Task<Package<List<Veiculo>>> ListarPorMarcaAsync(int marcaId);
    Task<Package<List<Veiculo>>> ListarPorVendedorAsync(int vendedorId);
}