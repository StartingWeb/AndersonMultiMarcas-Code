# Funcionalidades Gerais do Sistema

O ecossistema Anderson Multimarcas fornece suporte de ponta a ponta na divulgação institucional e gestão de estoque.

## Plataforma Pública (Vitrine Automotiva)
- **Busca Omnichannel (Search Engine):** Caixa de busca para consultar por marca, modelo, versão ou ano.
- **Listagem de Estoque e Catálogo:** Grid de catálogo para veículos com suporte a filtros dinâmicos (por Preço Mínimo, Condição como 0Km, Moto Elétrica).
- **Sessões Segmentadas Inteligentes:**
  - **Destaques:** Carros sinalizados pelos administradores como prioridades de venda.
  - **Premium 0km:** Listagem automática de automóveis de luxo (filtro de Preço mínimo R$ 130.000 + 0Km).
  - **Duas Rodas:** Sessão dedicada que aparece automaticamente caso existam Motos Elétricas no estoque.
- **Apresentação Institucional:** Página da empresa exibindo contagem dinâmica de lojas e veículos ativos em tempo real, além de mapas do Google embedados.
- **Integração Lead-WhatsApp:** Call-To-Actions espalhados pelo site para iniciar conversa com vendedores (tanto botão flutuante quanto botões atrelados diretamente a veículos e avaliações de troca).

## Plataforma Admin (Backoffice Concessionária)
- **Gestão de Lojas e Marcas:** Criação e parametrização das bases físicas da concessionária (endereços e links do Google Maps) e cadastro das montadoras.
- **Gestão de Equipe (Vendedores):** Cadastramento da força de vendas para vincular um veículo a um vendedor específico.
- **CRUD e Ficha Completa de Veículos:** Cadastro de veículos incluindo abas técnicas:
  - Identificação (Modelo, Versão, Anos, Cor, Combustível).
  - Documentação (Placas).
  - Condições Comerciais (Aceita Troca, Financiamento, Seminovo).
  - Descrição Rica, Links de Vídeo, anexação de Múltiplas Imagens (Midias).
- **Módulo de Importação em Massa:**
  - `ImportaJSON`: Funcionalidade de subida de arquivo para criação acelerada de estoque legado ou integração de feirões.
  - Sincronização de mídia (`ImportarMidia`).

---
## 🔗 Conexões e Navegação
- 🗺️ Contextos da Aplicação: [[01-Contextos]]
- 🧠 Lógica de Filtros: [[06-AnaliseFiltros]]
- 💻 Mapeamento de Telas: [[04-Telas]]
