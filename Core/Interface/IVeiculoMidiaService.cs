using Domain;
namespace Core.Interfaces;
public interface IVeiculoMidiaService
{
    Task<Package<int>> CriarAsync(VeiculoMidia midia);
    Task<Package<bool>> EditarAsync(VeiculoMidia midia);
    Task<Package<bool>> ExcluirAsync(int id);
    Task<Package<VeiculoMidia>> ObterPorIdAsync(int id);
    Task<Package<List<VeiculoMidia>>> ListarAsync();
    Task<Package<List<VeiculoMidia>>> ListarPorVeiculoAsync(int veiculoId);
    Task<Package<List<VeiculoMidia>>> ListarAtivasPorVeiculoAsync(int veiculoId);
}