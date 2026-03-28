using Core;
using Domain;
using System.Threading.Tasks;

namespace Core.Interfaces;
public interface IVeiculoCaracteristicaService
{
    Task<Package<int>> CriarOuAtualizarAsync(VeiculoCaracteristica model);
    Task<Package<VeiculoCaracteristica>> ObterPorVeiculoAsync(int veiculoId);
    Task<Package<bool>> ExcluirPorVeiculoAsync(int veiculoId);
}