using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class VeiculoCaracteristica : BaseEntity
{
    public int VeiculoId { get; private set; }

    public bool ArCondicionado { get; private set; }
    public bool ArQuente { get; private set; }
    public bool DirecaoHidraulica { get; private set; }
    public bool DirecaoEletrica { get; private set; }
    public bool VidroEletrico { get; private set; }
    public bool TravaEletrica { get; private set; }
    public bool RetrovisorEletrico { get; private set; }
    public bool BancoDeCouro { get; private set; }
    public bool AjusteEletricoBancos { get; private set; }
    public bool AquecimentoBancos { get; private set; }
    public bool VolanteMultifuncional { get; private set; }
    public bool PilotoAutomatico { get; private set; }
    public bool ControleAutomaticoVelocidade { get; private set; }
    public bool LimitadorVelocidade { get; private set; }
    public bool ComputadorBordo { get; private set; }
    public bool ChavePresencial { get; private set; }
    public bool PartidaBotao { get; private set; }
    public bool SensorChuva { get; private set; }
    public bool SensorCrepuscular { get; private set; }
    public bool TetoSolar { get; private set; }
    public bool TetoPanoramico { get; private set; }
    public bool AirbagMotorista { get; private set; }
    public bool AirbagPassageiro { get; private set; }
    public bool AirbagLateral { get; private set; }
    public bool AirbagCortina { get; private set; }
    public bool FreiosAbs { get; private set; }
    public bool ControleTracao { get; private set; }
    public bool ControleEstabilidade { get; private set; }
    public bool AssistentePartidaRampa { get; private set; }
    public bool Isofix { get; private set; }
    public bool Alarme { get; private set; }
    public bool CameraDeRe { get; private set; }
    public bool SensorEstacionamentoDianteiro { get; private set; }
    public bool SensorEstacionamentoTraseiro { get; private set; }
    public bool FarolNeblina { get; private set; }
    public bool FarolLed { get; private set; }
    public bool FarolMilha { get; private set; }
    public bool CentralMultimidia { get; private set; }
    public bool Som { get; private set; }
    public bool Bluetooth { get; private set; }
    public bool Usb { get; private set; }
    public bool EntradaAuxiliar { get; private set; }
    public bool Radio { get; private set; }
    public bool GPS { get; private set; }
    public bool CarregadorInducao { get; private set; }
    public bool AppleCarPlay { get; private set; }
    public bool AndroidAuto { get; private set; }
    public bool RodaLigaLeve { get; private set; }
    public bool KitMultimidia { get; private set; }
    public bool Engate { get; private set; }
    public bool Bagageiro { get; private set; }
    public bool CapotaMaritima { get; private set; }
    public bool Estribo { get; private set; }
    public bool SantoAntonio { get; private set; }
    public bool ProtetorCacamba { get; private set; }
    public bool PortaMalasEletrico { get; private set; }
    public bool TerceiraFileira { get; private set; }
    public bool CambioAutomatico { get; private set; }
    public bool CambioManual { get; private set; }
    public bool CambioCvt { get; private set; }
    public bool CambioAutomatizado { get; private set; }
    public bool TracaoDianteira { get; private set; }
    public bool TracaoTraseira { get; private set; }
    public bool TracaoIntegral { get; private set; }
    public bool StartStop { get; private set; }
    public bool Turbo { get; private set; }
    public bool Hibrido { get; private set; }
    public bool Eletrico { get; private set; }

    public Veiculo Veiculo { get; private set; } = null!;

    private VeiculoCaracteristica() { }

    public VeiculoCaracteristica(int veiculoId)
    {
        VeiculoId = veiculoId;
    }

    public IReadOnlyCollection<TipoVeiculoOpcional> OpcionaisAtivos()
    {
        var ativos = new List<TipoVeiculoOpcional>();
        foreach (var opcional in Enum.GetValues<TipoVeiculoOpcional>())
        {
            if (PossuiOpcional(opcional)) ativos.Add(opcional);
        }
        return ativos;
    }

    public bool PossuiOpcional(TipoVeiculoOpcional opcional) => opcional switch
    {
        TipoVeiculoOpcional.ArCondicionado => ArCondicionado,
        TipoVeiculoOpcional.ArQuente => ArQuente,
        TipoVeiculoOpcional.DirecaoHidraulica => DirecaoHidraulica,
        TipoVeiculoOpcional.DirecaoEletrica => DirecaoEletrica,
        TipoVeiculoOpcional.VidroEletrico => VidroEletrico,
        TipoVeiculoOpcional.TravaEletrica => TravaEletrica,
        TipoVeiculoOpcional.RetrovisorEletrico => RetrovisorEletrico,
        TipoVeiculoOpcional.BancoDeCouro => BancoDeCouro,
        TipoVeiculoOpcional.AjusteEletricoBancos => AjusteEletricoBancos,
        TipoVeiculoOpcional.AquecimentoBancos => AquecimentoBancos,
        TipoVeiculoOpcional.VolanteMultifuncional => VolanteMultifuncional,
        TipoVeiculoOpcional.PilotoAutomatico => PilotoAutomatico,
        TipoVeiculoOpcional.ControleAutomaticoVelocidade => ControleAutomaticoVelocidade,
        TipoVeiculoOpcional.LimitadorVelocidade => LimitadorVelocidade,
        TipoVeiculoOpcional.ComputadorBordo => ComputadorBordo,
        TipoVeiculoOpcional.ChavePresencial => ChavePresencial,
        TipoVeiculoOpcional.PartidaBotao => PartidaBotao,
        TipoVeiculoOpcional.SensorChuva => SensorChuva,
        TipoVeiculoOpcional.SensorCrepuscular => SensorCrepuscular,
        TipoVeiculoOpcional.TetoSolar => TetoSolar,
        TipoVeiculoOpcional.TetoPanoramico => TetoPanoramico,
        TipoVeiculoOpcional.AirbagMotorista => AirbagMotorista,
        TipoVeiculoOpcional.AirbagPassageiro => AirbagPassageiro,
        TipoVeiculoOpcional.AirbagLateral => AirbagLateral,
        TipoVeiculoOpcional.AirbagCortina => AirbagCortina,
        TipoVeiculoOpcional.FreiosAbs => FreiosAbs,
        TipoVeiculoOpcional.ControleTracao => ControleTracao,
        TipoVeiculoOpcional.ControleEstabilidade => ControleEstabilidade,
        TipoVeiculoOpcional.AssistentePartidaRampa => AssistentePartidaRampa,
        TipoVeiculoOpcional.Isofix => Isofix,
        TipoVeiculoOpcional.Alarme => Alarme,
        TipoVeiculoOpcional.CameraDeRe => CameraDeRe,
        TipoVeiculoOpcional.SensorEstacionamentoDianteiro => SensorEstacionamentoDianteiro,
        TipoVeiculoOpcional.SensorEstacionamentoTraseiro => SensorEstacionamentoTraseiro,
        TipoVeiculoOpcional.FarolNeblina => FarolNeblina,
        TipoVeiculoOpcional.FarolLed => FarolLed,
        TipoVeiculoOpcional.FarolMilha => FarolMilha,
        TipoVeiculoOpcional.CentralMultimidia => CentralMultimidia,
        TipoVeiculoOpcional.Som => Som,
        TipoVeiculoOpcional.Bluetooth => Bluetooth,
        TipoVeiculoOpcional.Usb => Usb,
        TipoVeiculoOpcional.EntradaAuxiliar => EntradaAuxiliar,
        TipoVeiculoOpcional.Radio => Radio,
        TipoVeiculoOpcional.GPS => GPS,
        TipoVeiculoOpcional.CarregadorInducao => CarregadorInducao,
        TipoVeiculoOpcional.AppleCarPlay => AppleCarPlay,
        TipoVeiculoOpcional.AndroidAuto => AndroidAuto,
        TipoVeiculoOpcional.RodaLigaLeve => RodaLigaLeve,
        TipoVeiculoOpcional.KitMultimidia => KitMultimidia,
        TipoVeiculoOpcional.Engate => Engate,
        TipoVeiculoOpcional.Bagageiro => Bagageiro,
        TipoVeiculoOpcional.CapotaMaritima => CapotaMaritima,
        TipoVeiculoOpcional.Estribo => Estribo,
        TipoVeiculoOpcional.SantoAntonio => SantoAntonio,
        TipoVeiculoOpcional.ProtetorCacamba => ProtetorCacamba,
        TipoVeiculoOpcional.PortaMalasEletrico => PortaMalasEletrico,
        TipoVeiculoOpcional.TerceiraFileira => TerceiraFileira,
        TipoVeiculoOpcional.CambioAutomatico => CambioAutomatico,
        TipoVeiculoOpcional.CambioManual => CambioManual,
        TipoVeiculoOpcional.CambioCvt => CambioCvt,
        TipoVeiculoOpcional.CambioAutomatizado => CambioAutomatizado,
        TipoVeiculoOpcional.TracaoDianteira => TracaoDianteira,
        TipoVeiculoOpcional.TracaoTraseira => TracaoTraseira,
        TipoVeiculoOpcional.TracaoIntegral => TracaoIntegral,
        TipoVeiculoOpcional.StartStop => StartStop,
        TipoVeiculoOpcional.Turbo => Turbo,
        TipoVeiculoOpcional.Hibrido => Hibrido,
        TipoVeiculoOpcional.Eletrico => Eletrico,
        _ => false
    };

    public void AdicionarOpcional(TipoVeiculoOpcional opcional) => DefinirOpcional(opcional, true);

    public void RemoverOpcional(TipoVeiculoOpcional opcional) => DefinirOpcional(opcional, false);

    private void DefinirOpcional(TipoVeiculoOpcional opcional, bool ativo)
    {
        switch (opcional)
        {
            case TipoVeiculoOpcional.ArCondicionado: ArCondicionado = ativo; break;
            case TipoVeiculoOpcional.ArQuente: ArQuente = ativo; break;
            case TipoVeiculoOpcional.DirecaoHidraulica: DirecaoHidraulica = ativo; break;
            case TipoVeiculoOpcional.DirecaoEletrica: DirecaoEletrica = ativo; break;
            case TipoVeiculoOpcional.VidroEletrico: VidroEletrico = ativo; break;
            case TipoVeiculoOpcional.TravaEletrica: TravaEletrica = ativo; break;
            case TipoVeiculoOpcional.RetrovisorEletrico: RetrovisorEletrico = ativo; break;
            case TipoVeiculoOpcional.BancoDeCouro: BancoDeCouro = ativo; break;
            case TipoVeiculoOpcional.AjusteEletricoBancos: AjusteEletricoBancos = ativo; break;
            case TipoVeiculoOpcional.AquecimentoBancos: AquecimentoBancos = ativo; break;
            case TipoVeiculoOpcional.VolanteMultifuncional: VolanteMultifuncional = ativo; break;
            case TipoVeiculoOpcional.PilotoAutomatico: PilotoAutomatico = ativo; break;
            case TipoVeiculoOpcional.ControleAutomaticoVelocidade: ControleAutomaticoVelocidade = ativo; break;
            case TipoVeiculoOpcional.LimitadorVelocidade: LimitadorVelocidade = ativo; break;
            case TipoVeiculoOpcional.ComputadorBordo: ComputadorBordo = ativo; break;
            case TipoVeiculoOpcional.ChavePresencial: ChavePresencial = ativo; break;
            case TipoVeiculoOpcional.PartidaBotao: PartidaBotao = ativo; break;
            case TipoVeiculoOpcional.SensorChuva: SensorChuva = ativo; break;
            case TipoVeiculoOpcional.SensorCrepuscular: SensorCrepuscular = ativo; break;
            case TipoVeiculoOpcional.TetoSolar: TetoSolar = ativo; break;
            case TipoVeiculoOpcional.TetoPanoramico: TetoPanoramico = ativo; break;
            case TipoVeiculoOpcional.AirbagMotorista: AirbagMotorista = ativo; break;
            case TipoVeiculoOpcional.AirbagPassageiro: AirbagPassageiro = ativo; break;
            case TipoVeiculoOpcional.AirbagLateral: AirbagLateral = ativo; break;
            case TipoVeiculoOpcional.AirbagCortina: AirbagCortina = ativo; break;
            case TipoVeiculoOpcional.FreiosAbs: FreiosAbs = ativo; break;
            case TipoVeiculoOpcional.ControleTracao: ControleTracao = ativo; break;
            case TipoVeiculoOpcional.ControleEstabilidade: ControleEstabilidade = ativo; break;
            case TipoVeiculoOpcional.AssistentePartidaRampa: AssistentePartidaRampa = ativo; break;
            case TipoVeiculoOpcional.Isofix: Isofix = ativo; break;
            case TipoVeiculoOpcional.Alarme: Alarme = ativo; break;
            case TipoVeiculoOpcional.CameraDeRe: CameraDeRe = ativo; break;
            case TipoVeiculoOpcional.SensorEstacionamentoDianteiro: SensorEstacionamentoDianteiro = ativo; break;
            case TipoVeiculoOpcional.SensorEstacionamentoTraseiro: SensorEstacionamentoTraseiro = ativo; break;
            case TipoVeiculoOpcional.FarolNeblina: FarolNeblina = ativo; break;
            case TipoVeiculoOpcional.FarolLed: FarolLed = ativo; break;
            case TipoVeiculoOpcional.FarolMilha: FarolMilha = ativo; break;
            case TipoVeiculoOpcional.CentralMultimidia: CentralMultimidia = ativo; break;
            case TipoVeiculoOpcional.Som: Som = ativo; break;
            case TipoVeiculoOpcional.Bluetooth: Bluetooth = ativo; break;
            case TipoVeiculoOpcional.Usb: Usb = ativo; break;
            case TipoVeiculoOpcional.EntradaAuxiliar: EntradaAuxiliar = ativo; break;
            case TipoVeiculoOpcional.Radio: Radio = ativo; break;
            case TipoVeiculoOpcional.GPS: GPS = ativo; break;
            case TipoVeiculoOpcional.CarregadorInducao: CarregadorInducao = ativo; break;
            case TipoVeiculoOpcional.AppleCarPlay: AppleCarPlay = ativo; break;
            case TipoVeiculoOpcional.AndroidAuto: AndroidAuto = ativo; break;
            case TipoVeiculoOpcional.RodaLigaLeve: RodaLigaLeve = ativo; break;
            case TipoVeiculoOpcional.KitMultimidia: KitMultimidia = ativo; break;
            case TipoVeiculoOpcional.Engate: Engate = ativo; break;
            case TipoVeiculoOpcional.Bagageiro: Bagageiro = ativo; break;
            case TipoVeiculoOpcional.CapotaMaritima: CapotaMaritima = ativo; break;
            case TipoVeiculoOpcional.Estribo: Estribo = ativo; break;
            case TipoVeiculoOpcional.SantoAntonio: SantoAntonio = ativo; break;
            case TipoVeiculoOpcional.ProtetorCacamba: ProtetorCacamba = ativo; break;
            case TipoVeiculoOpcional.PortaMalasEletrico: PortaMalasEletrico = ativo; break;
            case TipoVeiculoOpcional.TerceiraFileira: TerceiraFileira = ativo; break;
            case TipoVeiculoOpcional.CambioAutomatico: CambioAutomatico = ativo; break;
            case TipoVeiculoOpcional.CambioManual: CambioManual = ativo; break;
            case TipoVeiculoOpcional.CambioCvt: CambioCvt = ativo; break;
            case TipoVeiculoOpcional.CambioAutomatizado: CambioAutomatizado = ativo; break;
            case TipoVeiculoOpcional.TracaoDianteira: TracaoDianteira = ativo; break;
            case TipoVeiculoOpcional.TracaoTraseira: TracaoTraseira = ativo; break;
            case TipoVeiculoOpcional.TracaoIntegral: TracaoIntegral = ativo; break;
            case TipoVeiculoOpcional.StartStop: StartStop = ativo; break;
            case TipoVeiculoOpcional.Turbo: Turbo = ativo; break;
            case TipoVeiculoOpcional.Hibrido: Hibrido = ativo; break;
            case TipoVeiculoOpcional.Eletrico: Eletrico = ativo; break;
            default: break;
        }
    }
}
