# Mapeamento de Contextos (Context Map)

A plataforma Anderson Multimarcas está arquitetada para separar claramente o acesso ao cliente final (vitrine e captação de leads) do uso interno da concessionária (gestão e administração).

## 1. Web Public (Frontend Vitrine - Projeto "Project")
Este é o contexto focado na experiência do usuário final, captação de leads e SEO.
- **Responsabilidade:** Apresentação do estoque, busca de veículos, exposição institucional e redirecionamento de intenção de compra via WhatsApp.
- **Consumidores:** Clientes buscando veículos novos ou seminovos, clientes pesquisando histórico da empresa.
- **Integração:** Exibe os dados de `Veiculo`, `Loja`, etc. apenas em modo leitura e renderização otimizada para o SEO.

## 2. Admin Web (Backoffice - Projeto "Concessionaria")
Este é o painel de controle administrativo, gerenciando o portfólio da concessionária.
- **Responsabilidade:** CRUD de Veículos, Marcas, Lojas e Vendedores. Automação de entrada de estoque, importações em massa (JSON), importação de mídias e gestão de status de veículos (Ex: Marcar como Vendido).
- **Consumidores:** Gerentes, vendedores e setor administrativo da Anderson Multimarcas.

## 3. Core / Domain (Regras de Negócio e Entidades)
Este contexto centraliza a lógica de negócios da concessionária de veículos, sendo compartilhado pelos apps.
- **Responsabilidade:** Garantir integridade de domínios centrais: `Veiculo`, `Loja`, `Marca`, `Vendedor`, validações de estado do veículo (e.g., Ativo, Vendido), informações de documentação e mídias do veículo.

## 4. Data / Infraestrutura
A camada baseada em Entity Framework para abstrair o banco de dados.
- **Responsabilidade:** Persistência no banco relacional, rotinas de Migration (IdentitySeed), infraestrutura técnica.

---
## 🔗 Conexões e Navegação
- ➡️ Base de funcionalidades: [[02-Funcionalidades]]
- 📊 Interface visual: [[04-Telas]]
- ⚙️ Regras centrais: [[03-RegrasDeNegocio]]
