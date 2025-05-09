CREATE TABLE Produtos (
    Id INT PRIMARY KEY IDENTITY,
    Nome NVARCHAR(100) NOT NULL,
    Preco DECIMAL(18, 2) NOT NULL,
    EstoqueAtual INT NOT NULL
);

CREATE TABLE Pedidos (
    Id INT PRIMARY KEY IDENTITY,
    DataPedido DATETIME2 NOT NULL,
    Solicitante NVARCHAR(100) NOT NULL,
    Situacao NVARCHAR(50) NOT NULL DEFAULT 'Rascunho',
    ValorTotal DECIMAL(18, 2) NOT NULL
);

CREATE TABLE ItensPedido (
    Id INT PRIMARY KEY IDENTITY,
    PedidoId INT NOT NULL,
    ProdutoId INT NOT NULL,
    Quantidade INT NOT NULL,
    Preco DECIMAL(18, 2) NOT NULL,
    Total DECIMAL(18, 2) NOT NULL,
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
);



INSERT INTO Produtos (Nome, Preco, EstoqueAtual) VALUES
('Caneta Azul', 1.50, 100),
('Caderno Universitário', 15.90, 50),
('Lápis HB', 0.80, 200),
('Borracha Branca', 1.20, 150),
('Apontador Duplo', 2.50, 75),
('Régua 30cm', 3.00, 60),
('Marcador de Texto', 4.50, 40),
('Grampeador Pequeno', 12.00, 30),
('Papel Sulfite A4 (500 folhas)', 25.00, 25),
('Pasta Plástica', 2.00, 100),
('Projetor 4K', 5000.00, 100);