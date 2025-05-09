using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal Preco { get; set; } // Persistido no banco de dados
    public decimal Total { get; set; } // Persistido no banco de dados

    [JsonIgnore] // Ignora o campo durante a serialização/deserialização JSON
    [ValidateNever] // Evita que o campo seja validado durante a requisição
    public Produto Produto { get; set; } = null!;
}