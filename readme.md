# **Documentação do Projeto - Sistema de Gestão de Pedidos**

## **Descrição do Projeto**
Este projeto é um sistema de gestão de pedidos desenvolvido em **ASP.NET Core** com **Entity Framework Core**. Ele permite criar, atualizar, listar, cancelar e concluir pedidos, além de gerenciar itens e produtos associados. O sistema foi projetado para atender a regras de negócio específicas, como controle de estoque e validação de valores máximos para pedidos.

---

## **Tecnologias Utilizadas**
- **ASP.NET Core 8.0**: Framework para desenvolvimento de APIs RESTful.
- **Entity Framework Core**: ORM para manipulação de dados no banco de dados.
- **SQL Server**: Banco de dados relacional utilizado no projeto.

---

## **Estrutura do Projeto**

### **1. Controllers**
Os controladores são responsáveis por gerenciar as requisições HTTP e implementar as regras de negócio.

#### **PedidosController**
- **Rota Base**: `/api/pedidos`
- Gerencia as operações relacionadas aos pedidos, como criação, atualização, cancelamento e conclusão.

---

### **2. Models**
As classes de modelo representam as entidades do sistema e são mapeadas para tabelas no banco de dados.

#### **Pedido**
Representa um pedido no sistema.

- **Propriedades**:
  - `Id` (int): Identificador único do pedido.
  - `DataPedido` (DateTime): Data em que o pedido foi criado.
  - `Solicitante` (string): Nome do solicitante do pedido.
  - `Situacao` (string): Situação atual do pedido (`Rascunho`, `Finalizado`, `Cancelado`).
  - `ValorTotal` (decimal): Valor total do pedido.
  - `Itens` (List<ItemPedido>): Lista de itens associados ao pedido.

#### **ItemPedido**
Representa um item dentro de um pedido.

- **Propriedades**:
  - `Id` (int): Identificador único do item.
  - `PedidoId` (int): Identificador do pedido ao qual o item pertence.
  - `ProdutoId` (int): Identificador do produto associado ao item.
  - `Quantidade` (int): Quantidade do produto no item.
  - `Preco` (decimal): Preço unitário do produto no momento do pedido.
  - `Total` (decimal): Valor total do item (calculado como `Quantidade * Preco`).

#### **Produto**
Representa um produto disponível no sistema.

- **Propriedades**:
  - `Id` (int): Identificador único do produto.
  - `Nome` (string): Nome do produto.
  - `Preco` (decimal): Preço unitário do produto.
  - `EstoqueAtual` (int): Quantidade disponível no estoque.

---

### **3. Data**
A camada de dados utiliza o **Entity Framework Core** para gerenciar o banco de dados.

#### **AppDbContext**
O contexto do banco de dados que gerencia as entidades e suas relações.

- **DbSets**:
  - `Pedidos`: Representa a tabela de pedidos.
  - `ItensPedido`: Representa a tabela de itens de pedidos.
  - `Produtos`: Representa a tabela de produtos.

- **Configurações de Relacionamento**:
  - Um pedido pode ter muitos itens (`Pedido -> ItensPedido`).
  - Um item está associado a um único produto (`ItemPedido -> Produto`).
  - Exclusão em cascata é configurada para itens de pedidos ao excluir um pedido.

---

## **Endpoints da API**

### **1. Pedidos**

#### **1.1. Listar Pedidos**
- **Rota**: `GET /api/pedidos`
- **Descrição**: Retorna a lista de todos os pedidos.
- **Resposta**:
  - `200 OK`: Lista de pedidos.

#### **1.2. Obter Pedido por ID**
- **Rota**: `GET /api/pedidos/{id}`
- **Descrição**: Retorna os detalhes de um pedido específico.
- **Resposta**:
  - `200 OK`: Detalhes do pedido.
  - `404 Not Found`: Pedido não encontrado.

#### **1.3. Criar Pedido**
- **Rota**: `POST /api/pedidos`
- **Descrição**: Cria um novo pedido com itens.
- **Regras de Negócio**:
  - O campo `Solicitante` é obrigatório.
  - A data do pedido não pode ser no futuro.
  - O pedido deve conter pelo menos um item.
  - O valor total do pedido não pode exceder R$10.000.
- **Resposta**:
  - `201 Created`: Pedido criado com sucesso.
  - `400 Bad Request`: Erro de validação.

#### **1.4. Atualizar Pedido**
- **Rota**: `PUT /api/pedidos/{id}`
- **Descrição**: Atualiza o solicitante e a quantidade dos itens de um pedido.
- **Regras de Negócio**:
  - Apenas pedidos em `Rascunho` podem ser atualizados.
  - Apenas o campo `Solicitante` e as quantidades dos itens podem ser alterados.
  - A quantidade de itens deve ser maior que zero e não pode exceder o estoque disponível.
  - O valor total do pedido não pode exceder R$10.000.
- **Resposta**:
  - `204 No Content`: Pedido atualizado com sucesso.
  - `400 Bad Request`: Erro de validação.
  - `404 Not Found`: Pedido não encontrado.

#### **1.5. Cancelar Pedido**
- **Rota**: `DELETE /api/pedidos/{id}`
- **Descrição**: Cancela um pedido, alterando sua situação para `Cancelado`.
- **Regras de Negócio**:
  - Apenas pedidos em `Rascunho` podem ser cancelados.
- **Resposta**:
  - `204 No Content`: Pedido cancelado com sucesso.
  - `400 Bad Request`: Erro de validação.
  - `404 Not Found`: Pedido não encontrado.

#### **1.6. Concluir Pedido**
- **Rota**: `POST /api/pedidos/{id}/concluir`
- **Descrição**: Conclui um pedido, atualizando o estoque dos produtos e alterando a situação para `Finalizado`.
- **Regras de Negócio**:
  - Apenas pedidos em `Rascunho` podem ser concluídos.
  - O estoque dos produtos deve ser suficiente para atender às quantidades solicitadas.
- **Resposta**:
  - `204 No Content`: Pedido concluído com sucesso.
  - `400 Bad Request`: Erro de validação.
  - `404 Not Found`: Pedido não encontrado.

---

### **2. Itens de Pedido**

#### **2.1. Adicionar Item ao Pedido**
- **Rota**: `POST /api/pedidos/{pedidoId}/itens`
- **Descrição**: Adiciona um novo item a um pedido existente.
- **Regras de Negócio**:
  - Apenas pedidos em `Rascunho` podem ser alterados.
  - O produto deve existir no sistema.
  - A quantidade do item deve ser maior que zero e não pode exceder o estoque disponível.
  - O valor total do pedido não pode exceder R$10.000.
- **Resposta**:
  - `200 OK`: Item adicionado com sucesso.
  - `400 Bad Request`: Erro de validação.
  - `404 Not Found`: Pedido ou produto não encontrado.

#### **2.2. Remover Item do Pedido**
- **Rota**: `DELETE /api/pedidos/{pedidoId}/itens/{itemId}`
- **Descrição**: Remove um item de um pedido.
- **Regras de Negócio**:
  - Apenas pedidos em `Rascunho` podem ser alterados.
- **Resposta**:
  - `200 OK`: Item removido com sucesso.
  - `400 Bad Request`: Erro de validação.
  - `404 Not Found`: Pedido ou item não encontrado.

---

## **Regras de Negócio**
1. **Pedidos**:
   - Apenas pedidos em `Rascunho` podem ser alterados ou cancelados.
   - O valor total do pedido não pode exceder R$10.000.

2. **Itens de Pedido**:
   - A quantidade de itens deve ser maior que zero.
   - A quantidade de itens não pode exceder o estoque disponível.

3. **Produtos**:
   - O estoque é atualizado apenas na conclusão do pedido.

---

## **Configuração do Banco de Dados**
O banco de dados utiliza o **Entity Framework Core** para gerenciar as tabelas e relações. Certifique-se de configurar a string de conexão no arquivo appsettings.json:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=PedidosDB;Trusted_Connection=True;"
}
```

---
