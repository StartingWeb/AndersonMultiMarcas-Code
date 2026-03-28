using Core;
using Core.Dtos;
using Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces;

public interface ILojaService
{
    Task<Package<int>> CriarAsync(LojaDto loja);
    Task<Package<bool>> EditarAsync(LojaDto loja);
    Task<Package<bool>> ExcluirAsync(int id);
    Task<Package<LojaDto>> ObterPorIdAsync(int id);
    Task<Package<List<LojaDto>>> ListarAsync();
    Task<Package<List<LojaDto>>> ListarAtivasAsync();
}