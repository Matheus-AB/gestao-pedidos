/// <summary>
/// Representa um pedido.
/// </summary>
public class Pedido
{
    public int Id { get; set; }
    public DateTime DataPedido { get; set; }
    public string Solicitante { get; set; } = string.Empty;
    public string Situacao { get; set; } = "Rascunho";
    public decimal ValorTotal { get; set; } // Persistido no banco de dados
    public List<ItemPedido> Itens { get; set; } = new();
}