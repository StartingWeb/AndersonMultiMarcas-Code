using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

[Table("VeiculoCaracteristica")]
public class VeiculoCaracteristica
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }

    // Conforto
    public bool ArCondicionado { get; set; }
    public bool ArQuente { get; set; }
    public bool DirecaoHidraulica { get; set; }
    public bool DirecaoEletrica { get; set; }
    public bool VidroEletrico { get; set; }
    public bool TravaEletrica { get; set; }
    public bool RetrovisorEletrico { get; set; }
    public bool BancoDeCouro { get; set; }
    public bool AjusteEletricoBancos { get; set; }
    public bool AquecimentoBancos { get; set; }
    public bool VolanteMultifuncional { get; set; }
    public bool PilotoAutomatico { get; set; }
    public bool ControleAutomaticoVelocidade { get; set; }
    public bool LimitadorVelocidade { get; set; }
    public bool ComputadorBordo { get; set; }
    public bool ChavePresencial { get; set; }
    public bool PartidaBotao { get; set; }
    public bool SensorChuva { get; set; }
    public bool SensorCrepuscular { get; set; }
    public bool TetoSolar { get; set; }
    public bool TetoPanoramico { get; set; }

    // Segurança
    public bool AirbagMotorista { get; set; }
    public bool AirbagPassageiro { get; set; }
    public bool AirbagLateral { get; set; }
    public bool AirbagCortina { get; set; }
    public bool FreiosAbs { get; set; }
    public bool ControleTracao { get; set; }
    public bool ControleEstabilidade { get; set; }
    public bool AssistentePartidaRampa { get; set; }
    public bool Isofix { get; set; }
    public bool Alarme { get; set; }
    public bool CameraDeRe { get; set; }
    public bool SensorEstacionamentoDianteiro { get; set; }
    public bool SensorEstacionamentoTraseiro { get; set; }
    public bool FarolNeblina { get; set; }
    public bool FarolLed { get; set; }
    public bool FarolMilha { get; set; }

    // Multimídia e tecnologia
    public bool CentralMultimidia { get; set; }
    public bool Som { get; set; }
    public bool Bluetooth { get; set; }
    public bool Usb { get; set; }
    public bool EntradaAuxiliar { get; set; }
    public bool Radio { get; set; }
    public bool GPS { get; set; }
    public bool CarregadorInducao { get; set; }
    public bool AppleCarPlay { get; set; }
    public bool AndroidAuto { get; set; }

    // Estrutura e utilidade
    public bool RodaLigaLeve { get; set; }
    public bool KitMultimidia { get; set; }
    public bool Engate { get; set; }
    public bool Bagageiro { get; set; }
    public bool CapotaMaritima { get; set; }
    public bool Estribo { get; set; }
    public bool SantoAntonio { get; set; }
    public bool ProtetorCacamba { get; set; }
    public bool PortaMalasEletrico { get; set; }
    public bool TerceiraFileira { get; set; }

    // Mecânica / tração
    public bool CambioAutomatico { get; set; }
    public bool CambioManual { get; set; }
    public bool CambioCvt { get; set; }
    public bool CambioAutomatizado { get; set; }
    public bool TracaoDianteira { get; set; }
    public bool TracaoTraseira { get; set; }
    public bool TracaoIntegral { get; set; }
    public bool StartStop { get; set; }
    public bool Turbo { get; set; }

    // Veículos eletrificados
    public bool Hibrido { get; set; }
    public bool Eletrico { get; set; }
}