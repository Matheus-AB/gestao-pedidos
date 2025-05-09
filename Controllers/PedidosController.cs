using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PedidosController> _logger;

    public PedidosController(AppDbContext context, ILogger<PedidosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os pedidos.
    /// </summary>
    /// <returns>Lista de pedidos</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos()
    {
        _logger.LogInformation("Listando todos os pedidos");
        return await _context.Pedidos.Include(p => p.Itens).ToListAsync();
    }

    /// <summary>
    /// Obtém um pedido pelo ID.
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Pedido encontrado ou 404</returns>
    [ProducesResponseType(typeof(Pedido), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<ActionResult<Pedido>> GetPedido(int id)
    {
        _logger.LogInformation("Buscando pedido com ID {Id}", id);
        var pedido = await _context.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Produto).FirstOrDefaultAsync(p => p.Id == id);
        return pedido == null ? NotFound() : pedido;
    }

    /// <summary>
    /// Cria um novo pedido com itens.
    /// </summary>
    /// <param name="pedido">Dados do pedido a ser criado, incluindo itens.</param>
    /// <returns>Pedido criado</returns>
    [HttpPost]
    public async Task<ActionResult> CreatePedido(Pedido pedido)
    {
        if (string.IsNullOrWhiteSpace(pedido.Solicitante))
            return BadRequest("O campo 'Solicitante' é obrigatório.");

        if (pedido.DataPedido > DateTime.Now)
            return BadRequest("A data do pedido não pode ser no futuro.");

        if (pedido.Itens == null || !pedido.Itens.Any())
            return BadRequest("O pedido deve conter pelo menos um item.");

        decimal valorTotal = 0;

        foreach (var item in pedido.Itens)
        {
            var produto = await _context.Produtos.FindAsync(item.ProdutoId);
            if (produto == null)
                return BadRequest($"Produto com ID {item.ProdutoId} não encontrado.");

            if (produto.EstoqueAtual < item.Quantidade)
                return BadRequest($"Estoque insuficiente para o produto '{produto.Nome}'. Disponível: {produto.EstoqueAtual}, Solicitado: {item.Quantidade}.");

            item.Preco = produto.Preco;
            item.Total = item.Quantidade * item.Preco;
            valorTotal += item.Total;
        }

        // Valida se o valor total do pedido excede o limite de R$10.000
        if (valorTotal > 10000)
            return BadRequest("Valor total do pedido excede o limite de R$10.000.");

        pedido.ValorTotal = valorTotal;
        pedido.Situacao = "Rascunho";

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido criado com sucesso: {Id}", pedido.Id);
        return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedido);
    }

    /// <summary>
    /// Adiciona um item ao pedido.
    /// </summary>
    /// <param name="pedidoId">ID do pedido</param>
    /// <param name="item">Dados do item a ser adicionado</param>
    /// <returns>Pedido atualizado</returns>
    [HttpPost("{pedidoId}/itens")]
    public async Task<IActionResult> AdicionarItem(int pedidoId, ItemPedido item)
    {
        var pedido = await _context.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido == null) return NotFound("Pedido não encontrado.");
        if (pedido.Situacao != "Rascunho") return BadRequest("Não é possível alterar um pedido finalizado ou cancelado.");

        if (item.Quantidade <= 0)
            return BadRequest("A quantidade do item deve ser maior que zero.");

        var produto = await _context.Produtos.FindAsync(item.ProdutoId);
        if (produto == null)
            return BadRequest($"Produto com ID {item.ProdutoId} não encontrado.");

        if (pedido.Itens.Any(i => i.ProdutoId == item.ProdutoId))
            return BadRequest("O produto já foi adicionado ao pedido. Atualize a quantidade do item existente.");

        if (produto.EstoqueAtual < item.Quantidade)
            return BadRequest($"Estoque insuficiente para o produto '{produto.Nome}'. Disponível: {produto.EstoqueAtual}, Solicitado: {item.Quantidade}.");

        item.Preco = produto.Preco;
        item.Total = item.Quantidade * item.Preco;

        if (pedido.ValorTotal + item.Total > 10000)
            return BadRequest("Valor total do pedido excede o limite de R$10.000.");

        pedido.Itens.Add(item);
        pedido.ValorTotal += item.Total;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Item {ItemId} adicionado ao pedido {PedidoId}", item.Id, pedidoId);
        return Ok(pedido);
    }

    /// <summary>
    /// Atualiza o solicitante do pedido e a quantidade dos itens enviados, desde que o pedido esteja em rascunho.
    /// </summary>
    /// <param name="id">ID do pedido a ser atualizado.</param>
    /// <param name="pedido">Dados atualizados do pedido, incluindo apenas o solicitante e as quantidades dos itens.</param>
    /// <returns>Sem conteúdo (204) se atualizado com sucesso, erro se inválido.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePedido(int id, Pedido pedido)
    {
        if (id != pedido.Id)
            return BadRequest("O ID da URL não corresponde ao ID do corpo da requisição.");

        var original = await _context.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Produto).FirstOrDefaultAsync(p => p.Id == id);
        if (original == null)
            return NotFound("Pedido não encontrado.");

        if (original.Situacao != "Rascunho")
            return BadRequest("Não é possível editar um pedido que não esteja em rascunho.");

        // Atualiza apenas o campo Solicitante
        original.Solicitante = pedido.Solicitante;

        // Atualiza apenas os itens enviados
        decimal valorTotal = 0;
        foreach (var originalItem in original.Itens)
        {
            var updatedItem = pedido.Itens.FirstOrDefault(i => i.Id == originalItem.Id);
            if (updatedItem != null)
            {
                if (updatedItem.Quantidade <= 0)
                    return BadRequest($"A quantidade do item com ID {originalItem.Id} deve ser maior que zero.");

                if (originalItem.Produto.EstoqueAtual < updatedItem.Quantidade)
                    return BadRequest($"Estoque insuficiente para o produto '{originalItem.Produto.Nome}'. Disponível: {originalItem.Produto.EstoqueAtual}, Solicitado: {updatedItem.Quantidade}.");

                // Atualiza a quantidade e recalcula o total do item
                originalItem.Quantidade = updatedItem.Quantidade;
                originalItem.Total = originalItem.Quantidade * originalItem.Preco;
            }

            // Recalcula o valor total do pedido
            valorTotal += originalItem.Total;
        }

        // Valida se o valor total do pedido excede o limite de R$10.000
        if (valorTotal > 10000)
            return BadRequest("Valor total do pedido excede o limite de R$10.000.");

        original.ValorTotal = valorTotal;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Pedido atualizado: {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Cancela um pedido.
    /// </summary>
    /// <param name="id">ID do pedido a ser cancelado</param>
    /// <returns>Sem conteúdo (204) se cancelado com sucesso, erro se inválido.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelarPedido(int id)
    {
        var pedido = await _context.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Produto).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null) return NotFound("Pedido não encontrado.");
        if (pedido.Situacao != "Rascunho") return BadRequest("Somente pedidos em rascunho podem ser cancelados.");

        // Apenas altera a situação do pedido para "Cancelado"
        pedido.Situacao = "Cancelado";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido cancelado: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Remove um item de um pedido.
    /// </summary>
    /// <param name="pedidoId">ID do pedido</param>
    /// <param name="itemId">ID do item a ser removido</param>
    /// <returns>Pedido atualizado ou erro se inválido.</returns>
    [HttpDelete("{pedidoId}/itens/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverItem(int pedidoId, int itemId)
    {
        var pedido = await _context.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido == null) return NotFound("Pedido não encontrado.");
        if (pedido.Situacao != "Rascunho") return BadRequest("Não é possível alterar um pedido finalizado ou cancelado.");

        var item = pedido.Itens.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound("Item não encontrado.");

        // Remove o item do pedido
        _context.ItensPedido.Remove(item);

        // Recalcula o valor total do pedido
        pedido.ValorTotal = pedido.Itens.Where(i => i.Id != itemId).Sum(i => i.Total);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Item {ItemId} removido do pedido {PedidoId}", itemId, pedidoId);
        return Ok(pedido);
    }

    /// <summary>
    /// Conclui um pedido, atualizando o estoque dos produtos e alterando a situação para "Finalizado".
    /// </summary>
    /// <param name="id">ID do pedido a ser concluído</param>
    /// <returns>Sem conteúdo (204) se concluído com sucesso, erro se inválido.</returns>
    [HttpPost("{id}/concluir")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConcluirPedido(int id)
    {
        var pedido = await _context.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Produto).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null) return NotFound("Pedido não encontrado.");
        if (pedido.Situacao != "Rascunho") return BadRequest("Somente pedidos em rascunho podem ser concluídos.");

        foreach (var item in pedido.Itens)
        {
            if (item.Produto.EstoqueAtual < item.Quantidade)
            {
                return BadRequest($"Estoque insuficiente para o produto '{item.Produto.Nome}'. Disponível: {item.Produto.EstoqueAtual}, Solicitado: {item.Quantidade}.");
            }
            item.Produto.EstoqueAtual -= item.Quantidade; // Atualiza o estoque
        }

        pedido.Situacao = "Finalizado"; // Altera a situação do pedido
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido concluído: {Id}", id);
        return NoContent();
    }        
}