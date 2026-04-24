# Regras de Negócio

## Entidade Principal: Veículo (`Veiculo.cs`)

1. **Associações Obrigatórias e Opcionais:**
   - **Obrigatório:** Todo veículo deve obrigatoriamente estar vinculado a uma `Loja` (unidade física que o detém) e a uma `Marca` (montadora).
   - **Opcional:** Um veículo pode ter vínculo direto com um `VendedorId` caso ele tenha comissionamento direto ou gestão específica do lead, além de um `IdLegado` para controle de migrações.

2. **Categorização e Flags Comerciais:**
   - O veículo tem flags boleanos críticos que acionam tags visuais e filtros no site:
     - `AceitaTroca`
     - `Financiavel`
     - `Destaque` (Fixa o carro nas primeiras seções da página inicial do site).
     - `Seminovo`
     - `MotoEletrica` (Dispara a exibição da vitrine exclusiva para Motos Elétricas na Home se tiver >= 1 no banco).

3. **Status e Fluxo de Venda:**
   - A visibilidade de um carro na loja online é controlada pelas flags `Ativo` e `Vendido`.
   - Ao concretizar uma venda (`Vendido == true`), o sistema armazena uma data de auditoria (`DataVenda`) e a identificação do usuário do sistema admin que efetivou a transação (`VendidoPorUsuarioId`).

4. **Classificação Premium:**
   - Para figurar na sessão "Premium" da página principal, há uma regra dinâmica que busca veículos que possuem flag como `ZeroKm = true` e um valor limite base alto estipulado (ex: `130.000` via Query String de filtro padrão).

5. **Dados de Mídia e Características:**
   - O negócio permite adicionar uma URL direta de Vídeo de Exposição, além de possuir uma relação 1-para-Muitos (`ICollection<VeiculoMidia>`) para fotos.
   - Detalhes adicionais extras ficam em `VeiculoCaracteristica`. Existe campo reservado à empresa de `ObservacoesInternas` focado para uso estrito do dashboard interno e comunicação entre a equipe.

---
## 🔗 Conexões e Navegação
- 🔎 Veja como os filtros usam essas regras: [[06-AnaliseFiltros]]
- 🗺️ Contexto Geral: [[01-Contextos]]
- 🚗 Detalhamento de features do Veículo: [[02-Funcionalidades]]
