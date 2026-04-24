# Mapeamento de Telas

## Telas da Vitrine (Portal `Project`)

- **Página Inicial (`Index.cshtml`)**
  - Hero banner com vídeo em autoplay, mensagem de valor, barra de pesquisa omni.
  - Carrossel deslizante focado em Veículos "Destaques".
  - Seções geradas via condição (apenas se populadas no DB): "Premium 0 km" e "Motos Elétricas".
  - Seção de incentivo à Venda/Troca em contato imediato via WhatsApp, seguido das Lojas listadas dinamicamente com integrações do iframe de mapa.
- **Página da Empresa ("Quem Somos" - `Empresa.cshtml`)**
  - Layout hero com proposição de valor institucional.
  - Três grandes marcadores de força extraídos pelo framework: Ano de Fundação (2012), Total de Veículos Ativos no Estoque e Unidades Físicas Ativas.
  - Painel de filiais por endereços e callouts para atendimento presencial.
- **Catálogo (`Catalogo.cshtml`)**
  - Busca facetada e filtros refinados (Marcas, Modelos, Faixas de Preço, etc).
  - Grip completo do estoque.
- **Página do Veículo (`Veiculo.cshtml`)**
  - Ficha técnica completa de uma unidade. Galeria de Midia, listagem das tags flag (Financiável, Único Dono, etc.) e simulação conversacional no WhatsApp.
- **Contato/Rotas (`Contato.cshtml`)**
  - Informações de endereço e forms base de retenção.


## Telas Gerenciais do Sistema Base (`Concessionaria`)

Esta área se dedica aos cruds da área contábil/estoque.
- Estruturadas em `/Pages`:
  - **Index de Dashboard (`Concessionaria/Pages`)** - Overview estatístico dos dados.
  - **/Veiculo**
    - `Index.cshtml`: Listagem rápida, status atual se vendido/ativo e ações administrativas.
    - `Upsert.cshtml`: Formulário hiper dinâmico agregando todos os model states para o cadastro/edição num carro e subida de imagens.
    - `ImportaJSON.cshtml` / `Importar.cshtml`: Rotinas de carga massiva de legados no banco de dados.
  - **/Loja**
    - Listagem e criação das filiais (Endereços e URLs do Maps).
  - **/Marca**
    - Gestão básica de Marcas (Montadoras) para padronização no cadastro do combo de veículos.
  - **/Vendedor**
    - Registro de representantes para gerir repasses e métricas de venda.

---
## 🔗 Conexões e Navegação
- 🛠️ Veja as funcionalidades de cada tela: [[02-Funcionalidades]]
- 📝 Conteúdo exibido nas telas: [[05-ConteudoSite]]
- 👥 Lógica institucional: [[07-QuemSomos]]
