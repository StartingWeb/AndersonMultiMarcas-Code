using Core;
using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Core;
public class VeiculoCaracteristicaService : IVeiculoCaracteristicaService
{
    private readonly ApplicationDbContext _context;

    public VeiculoCaracteristicaService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ======================================================
    // CREATE (ou UPDATE automático se já existir)
    // ======================================================
    public async Task<Package<int>> CriarOuAtualizarAsync(VeiculoCaracteristica model)
    {
        var validacao = await ValidarAsync(model);
        if (validacao.Status != PackageStatus.Success)
            return validacao;

        try
        {
            var existente = await _context.VeiculoCaracteristicas
                .FirstOrDefaultAsync(x => x.VeiculoId == model.VeiculoId);

            if (existente == null)
            {
                model.Id = 0;
                _context.VeiculoCaracteristicas.Add(model);
                await _context.SaveChangesAsync();

                return new Package<int>
                {
                    Status = PackageStatus.Success,
                    Data = model.Id,
                    UserMessage = "Características cadastradas com sucesso."
                };
            }

            // UPDATE automático
            AtualizarCampos(existente, model);

            await _context.SaveChangesAsync();

            return new Package<int>
            {
                Status = PackageStatus.Success,
                Data = existente.Id,
                UserMessage = "Características atualizadas com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<int>
            {
                Status = PackageStatus.Error,
                Data = 0,
                UserMessage = "Erro ao salvar características.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // READ - BY VEICULO
    // ======================================================
    public async Task<Package<VeiculoCaracteristica>> ObterPorVeiculoAsync(int veiculoId)
    {
        if (veiculoId <= 0)
        {
            return new Package<VeiculoCaracteristica>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Veículo inválido."
            };
        }

        try
        {
            var item = await _context.VeiculoCaracteristicas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.VeiculoId == veiculoId);

            if (item == null)
            {
                return new Package<VeiculoCaracteristica>
                {
                    Status = PackageStatus.Error,
                    Data = null,
                    UserMessage = "Características não encontradas."
                };
            }

            return new Package<VeiculoCaracteristica>
            {
                Status = PackageStatus.Success,
                Data = item,
                UserMessage = "Características localizadas com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<VeiculoCaracteristica>
            {
                Status = PackageStatus.Error,
                Data = null,
                UserMessage = "Erro ao buscar características.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // DELETE
    // ======================================================
    public async Task<Package<bool>> ExcluirPorVeiculoAsync(int veiculoId)
    {
        if (veiculoId <= 0)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Veículo inválido."
            };
        }

        try
        {
            var item = await _context.VeiculoCaracteristicas
                .FirstOrDefaultAsync(x => x.VeiculoId == veiculoId);

            if (item == null)
            {
                return new Package<bool>
                {
                    Status = PackageStatus.Error,
                    Data = false,
                    UserMessage = "Características não encontradas."
                };
            }

            _context.VeiculoCaracteristicas.Remove(item);
            await _context.SaveChangesAsync();

            return new Package<bool>
            {
                Status = PackageStatus.Success,
                Data = true,
                UserMessage = "Características excluídas com sucesso."
            };
        }
        catch (Exception ex)
        {
            return new Package<bool>
            {
                Status = PackageStatus.Error,
                Data = false,
                UserMessage = "Erro ao excluir características.",
                DebugMessage = ex.Message
            };
        }
    }

    // ======================================================
    // VALIDAÇÃO
    // ======================================================
    private async Task<Package<int>> ValidarAsync(VeiculoCaracteristica model)
    {
        if (model == null)
            return Erro("Dados não informados.", "Objeto nulo.");

        if (model.VeiculoId <= 0)
            return Erro("Veículo inválido.", "VeiculoId inválido.");

        var existeVeiculo = await _context.Veiculos
            .AnyAsync(x => x.Id == model.VeiculoId);

        if (!existeVeiculo)
            return Erro("Veículo não encontrado.", "FK VeiculoId inválida.");

        return new Package<int>
        {
            Status = PackageStatus.Success,
            Data = 0
        };
    }

    // ======================================================
    // MAPEAMENTO
    // ======================================================
    private void AtualizarCampos(VeiculoCaracteristica destino, VeiculoCaracteristica origem)
    {
        // Conforto
        destino.ArCondicionado = origem.ArCondicionado;
        destino.ArQuente = origem.ArQuente;
        destino.DirecaoHidraulica = origem.DirecaoHidraulica;
        destino.DirecaoEletrica = origem.DirecaoEletrica;
        destino.VidroEletrico = origem.VidroEletrico;
        destino.TravaEletrica = origem.TravaEletrica;
        destino.RetrovisorEletrico = origem.RetrovisorEletrico;
        destino.BancoDeCouro = origem.BancoDeCouro;
        destino.AjusteEletricoBancos = origem.AjusteEletricoBancos;
        destino.AquecimentoBancos = origem.AquecimentoBancos;
        destino.VolanteMultifuncional = origem.VolanteMultifuncional;
        destino.PilotoAutomatico = origem.PilotoAutomatico;
        destino.ControleAutomaticoVelocidade = origem.ControleAutomaticoVelocidade;
        destino.LimitadorVelocidade = origem.LimitadorVelocidade;
        destino.ComputadorBordo = origem.ComputadorBordo;
        destino.ChavePresencial = origem.ChavePresencial;
        destino.PartidaBotao = origem.PartidaBotao;
        destino.SensorChuva = origem.SensorChuva;
        destino.SensorCrepuscular = origem.SensorCrepuscular;
        destino.TetoSolar = origem.TetoSolar;
        destino.TetoPanoramico = origem.TetoPanoramico;

        // Segurança
        destino.AirbagMotorista = origem.AirbagMotorista;
        destino.AirbagPassageiro = origem.AirbagPassageiro;
        destino.AirbagLateral = origem.AirbagLateral;
        destino.AirbagCortina = origem.AirbagCortina;
        destino.FreiosAbs = origem.FreiosAbs;
        destino.ControleTracao = origem.ControleTracao;
        destino.ControleEstabilidade = origem.ControleEstabilidade;
        destino.AssistentePartidaRampa = origem.AssistentePartidaRampa;
        destino.Isofix = origem.Isofix;
        destino.Alarme = origem.Alarme;
        destino.CameraDeRe = origem.CameraDeRe;
        destino.SensorEstacionamentoDianteiro = origem.SensorEstacionamentoDianteiro;
        destino.SensorEstacionamentoTraseiro = origem.SensorEstacionamentoTraseiro;
        destino.FarolNeblina = origem.FarolNeblina;
        destino.FarolLed = origem.FarolLed;
        destino.FarolMilha = origem.FarolMilha;

        // Multimídia
        destino.CentralMultimidia = origem.CentralMultimidia;
        destino.Som = origem.Som;
        destino.Bluetooth = origem.Bluetooth;
        destino.Usb = origem.Usb;
        destino.EntradaAuxiliar = origem.EntradaAuxiliar;
        destino.Radio = origem.Radio;
        destino.GPS = origem.GPS;
        destino.CarregadorInducao = origem.CarregadorInducao;
        destino.AppleCarPlay = origem.AppleCarPlay;
        destino.AndroidAuto = origem.AndroidAuto;

        // Estrutura
        destino.RodaLigaLeve = origem.RodaLigaLeve;
        destino.KitMultimidia = origem.KitMultimidia;
        destino.Engate = origem.Engate;
        destino.Bagageiro = origem.Bagageiro;
        destino.CapotaMaritima = origem.CapotaMaritima;
        destino.Estribo = origem.Estribo;
        destino.SantoAntonio = origem.SantoAntonio;
        destino.ProtetorCacamba = origem.ProtetorCacamba;
        destino.PortaMalasEletrico = origem.PortaMalasEletrico;
        destino.TerceiraFileira = origem.TerceiraFileira;

        // Mecânica
        destino.CambioAutomatico = origem.CambioAutomatico;
        destino.CambioManual = origem.CambioManual;
        destino.CambioCvt = origem.CambioCvt;
        destino.CambioAutomatizado = origem.CambioAutomatizado;
        destino.TracaoDianteira = origem.TracaoDianteira;
        destino.TracaoTraseira = origem.TracaoTraseira;
        destino.TracaoIntegral = origem.TracaoIntegral;
        destino.StartStop = origem.StartStop;
        destino.Turbo = origem.Turbo;

        // Elétricos
        destino.Hibrido = origem.Hibrido;
        destino.Eletrico = origem.Eletrico;
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
}