# Árvore de Processamento Web (Comportamentos, Filtros e Inteligência do Site)

Uma análise aprofundada dos algoritmos e inteligências executados pelo portal de vitrine (`Project`).

## 1. Engine de Busca e Autocomplete (`Index.cshtml.cs`)
O site possui um sistema avançado de Autocomplete (SearchSuggestions). 
- **Feedback Visual:** É acionado quando a string da busca passa de 2 caracteres.
- **Sugestões Tipadas:** Em vez de retornar "strings secas", o backend divide as sugestões em **3 Grupos Semânticos**:
  1. **Nome:** Combinação exata de Marca + Modelo / Título do Carro (máx. 6 amostras).
  2. **Categoria:** Sugere saltar para filtros rápidos, identificando se a sua digitação tem a ver com "Combustível" ou "Câmbio" (ex: você digita "Flex", ele sugere clicar direto na categoria gasolina/flex).
  3. **Marca:** Sugere o shortcut para visualizar as marcas.
- **Normalização Textual (`IsElectricVehicle` e MotoEletrica):** A index possui código em background para caçar palavras-chave ligadas a motos ("SCOOTER", "BIZ", "POP") e a elétricos ("BYD", "LEAF", "E-TRON"). Mas no banco, os filtros usam com prioridade as flags ativadas no painel.

## 2. Catálogo e Filtros Estritos (`Catalogo.cshtml.cs`)
Os parâmetros que abastecem as consultas dinâmicas têm os seguintes tratamentos:
- **Busca Aberta Omni:** Puxa via `LIKE %val%` dados da tabela em campos combinados (Modelo, Título, Versão, Combustível, Cor, Ano, Marca). É resiliente.
- **Prevenção de Quebra de Preço:** Se o usuário jogar na URL um `PrecoMinimo` maior que o `PrecoMaximo`, o sistema capta e inverte fisicamente as variáveis antes de bater no ORM.
- **Ordem de Relevância:** A "caixa registradora" do portal exibe prioritamente o que está como `Destaque = true` -> Seguido do Vínculo de Ano Mais Recente -> Data de Cadastro Mais Recente (para forçar novidades no topo).
- Se a página não tiver nenhum desses filtros aplicados, ela tira os 3 Veículos mais recentes dessa query e os batiza como "Veículos Recentes", separando-os visualmente dos demais, agindo como chamariz.

## 3. Lógica Semântica na Página de Detalhe (`Veiculo.cshtml.cs`)
A exibição da página específica de um lead contém processamentos dedicados:
- **Inteligência de Veículos Relacionados (RelatedVehicles):** Para segurar o usuário na loja, a página constrói uma lista de recomendações em lote utilizando uma regra restrita de similaridade:
   1. Procura primeiro veículos Ativos/Não-vendidos.
   2. Ordena priorizando a **Mesma Marca** do veículo que está sendo visualizado.
   3. Aplica tolerância de Preço: Ele puxa carros cujo "GAP" de preço (tanto pra mais caro quanto pra mais barato) não passe de uma **diferença de R$ 25.000,00** comparado ao carro aberto. É um algoritmo focado na condição de compra do poder aquisitivo do lead.
- **Sorteio e Contatos Vendedores:** Ele resgata os contatos da loja aleatoriamente ou globalmente, limpando números e ajustando sempre para o prefixo do Brasil "55", empurrando pra URLs amigáveis com template de string "Quero saber do veículo [TITULO]".
- **Otimização de Imagens Edge:** Possui um script interno (`OtimizarUrlImagemExterna`) caso as fotos venham de provedores de Nuvem (Cloudinary, Imgix, ImageKit). Ele acopla on-the-fly manipuladores de query string (`fit=crop`, `q=70`, `w=120`, `h=120`) para não sobrecarregar as thumbnails dos vendedores na página.
- **Display de Setup/Features:** Avalia entre \~50 boolean flags (Abs, Airbags, Piloto Automático, etc) provindas do `VeiculoCaracteristica` e materializa numa listagem em forma de string.

---
## 🔗 Conexões e Navegação
- ⚙️ Regras do Veículo: [[03-RegrasDeNegocio]]
- 🚀 Onde isso é acionado (Telas): [[04-Telas]]
- 🗺️ Estrutura do Sistema: [[01-Contextos]]

---
## Atualizacao de Correcoes (2026-04-15)

### 1) Permissao e menu do usuario
- **Problema:** Menu lateral sumindo para alguns perfis e abas inacessiveis com erro.
- **Causa raiz:** O seed de identidade removia mapeamentos de `MenuRoles` a cada inicializacao, sobrescrevendo permissoes configuradas; em cenarios sem links, o menu ficava vazio.
- **Solucao:** Seed passou a ser **aditivo** (nao remove mapeamentos existentes) e o `AdminMenuService` ganhou fallback por perfil para evitar menu vazio.
- **Arquivos alterados:** `Data/IdentitySeed.cs`, `Project/Navigation/AdminMenuService.cs`, `Project/Pages/AccessDenied.cshtml`.
- **Impacto:** Regressao de permissao mitigada, navegacao admin mais resiliente e resposta amigavel para acesso negado.

### 2) Home - motos eletricas
- **Problema:** Sessao de motos eletricas nao aparecia em casos com classificacao por texto/tag.
- **Causa raiz:** Home dependia exclusivamente da flag `MotoEletrica`.
- **Solucao:** Filtro da Home passou a considerar `MotoEletrica` **ou** combinacao semantica (duas rodas + eletrico), mantendo prioridade da flag.
- **Arquivos alterados:** `Project/Pages/Index.cshtml.cs`.
- **Impacto:** Maior cobertura da sessao sem depender apenas de cadastro perfeito da flag.

### 3) Catalogo / Premium / 0km
- **Problema:** Divergencia de resultados 0km na transicao Home -> Catalogo.
- **Causa raiz:** Regra da Home para Premium usava apenas `!Seminovo`, diferente da regra aplicada no Catalogo.
- **Solucao:** Home Premium passou a usar a mesma leitura de 0km (`novo` ou `km = 0`/nao informado) para consistencia entre telas.
- **Arquivos alterados:** `Project/Pages/Index.cshtml.cs`.
- **Impacto:** Consistencia de listagem entre vitrine e catalogo.

### 4) Dashboard zerado apos relogar
- **Problema:** Dashboard admin carregava vazio com erro em novos logins.
- **Causa raiz:** `Task.WhenAll` executava tres services em paralelo compartilhando o mesmo `DbContext` scoped (concorrencia nao suportada no EF Core).
- **Solucao:** Carregamento do dashboard foi serializado (consultas sequenciais).
- **Arquivos alterados:** `Project/Pages/Admin/Index.cshtml.cs`.
- **Impacto:** Elimina erro intermitente de contexto e estabiliza os indicadores apos relogin.

### 5) Destaques da home
- **Problema:** Bloco de destaques misturava itens novos sem tag de destaque.
- **Causa raiz:** Query concatenava `Destaque=true` com fallback de ultimos cadastrados.
- **Solucao:** Bloco passou a listar apenas `Destaque=true`.
- **Arquivos alterados:** `Project/Pages/Index.cshtml.cs`.
- **Impacto:** Sessao respeita estritamente a regra de negocio de destaque.

### 6) Botoes de WhatsApp
- **Problema:** Fluxo fragmentado (parte abria link direto, parte modal).
- **Causa raiz:** Implementacao distribuida por tela com comportamentos diferentes.
- **Solucao:** Criado modal compartilhado de vendedores no layout publico e padronizados botoes para abrir esse modal.
- **Arquivos alterados:** `Project/ViewComponents/SellerContactModalViewComponent.cs`, `Project/Pages/Shared/Components/SellerContactModal/Default.cshtml`, `Project/Pages/Shared/_Layout.cshtml`, `Project/Pages/Shared/_CatalogVehicleCardDb.cshtml`, `Project/Pages/Shared/_CatalogVehicleCard.cshtml`, `Project/Pages/Index.cshtml`, `Project/Pages/Empresa.cshtml`, `Project/Pages/Veiculo.cshtml`, `Project/wwwroot/js/site.js`.
- **Impacto:** Jornada unica de contato com foto/telefone do vendedor e mensagem contextual por veiculo.

### 7) Modal mobile
- **Problema:** Densidade visual e CTA do modal no celular.
- **Causa raiz:** Grid do modal pouco adaptado para telas pequenas.
- **Solucao:** Ajustes de layout mobile (card em duas linhas, foto menor, CTA full-width, espacemento e altura de rolagem).
- **Arquivos alterados:** `Project/wwwroot/css/Layout.css`, `Project/wwwroot/css/Veiculo.css`.
- **Impacto:** Melhor usabilidade e legibilidade no mobile sem alterar desktop.

### Ajuste de Regra (2026-04-15 - revisao)
- **Moto eletrica na Home:** a sessao agora exige estritamente `MotoEletrica = true` (sem inferencia por texto/tag).
- **Zero km:** removida a inferencia por quilometragem vazia/zero. A regra passa a ser somente `Seminovo = false`.
- **Arquivos atualizados na revisao:** `Project/Pages/Index.cshtml.cs`, `Project/Pages/Catalogo.cshtml.cs`, `Project/Pages/Shared/_CatalogVehicleCardDb.cshtml`.
- **Ajuste (2026-04-15):** Botao "Ver premium" na Home passou a navegar para o Catalogo sem `precoMinimo` (removeu corte de R$ 130 mil), mantendo apenas o filtro de condicao (`zeroKm=true`).
- **Ajuste de performance (2026-04-15):** Home passou a fazer prefetch assincrono (idle) das fotos de vendedores ativos para aquecer cache do navegador e reduzir sensacao de lentidao ao abrir contato/WhatsApp.
- **Implementacao:** lista de URLs no `IndexModel` + prefetch em lote com `requestIdleCallback` (fallback `setTimeout`), sem bloquear render inicial.
- **Arquivos:** `Project/Pages/Index.cshtml.cs`, `Project/Pages/Index.cshtml`.
- **Ajuste de performance (2026-04-15 - modal vendedores):** scroll do modal otimizado com thumbnails de foto (URL otimizada para provedores externos) e tuning de rolagem (`-webkit-overflow-scrolling`, `overscroll-behavior`, `contain`, `content-visibility`).
- **Arquivos:** `Project/ViewComponents/SellerContactModalViewComponent.cs`, `Project/wwwroot/css/Layout.css`.
- **Ajuste de regra (2026-04-15 - destaques):** remocao do filtro obrigatorio de preco nos destaques da Home. Veiculos com `Destaque = true` agora aparecem mesmo sem `PrecoVenda`, exibindo `Consulte` no card.
- **Arquivo:** `Project/Pages/Index.cshtml.cs`.
- **Ajuste de UX (2026-04-15 - Catalogo):** filtro de condicao no Catalogo agora exibe `0Km` e `Seminovo` (removeu label "Novo").
- **Regra backend:** `Condicao=zerokm` filtra `Seminovo = false` (mantida compatibilidade com `Condicao=novo`).
- **Arquivos:** `Project/Pages/Catalogo.cshtml`, `Project/Pages/Catalogo.cshtml.cs`.
