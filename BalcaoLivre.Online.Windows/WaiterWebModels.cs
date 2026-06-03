using System.Text.Json;

namespace BalcaoLivre.Online.Windows;

public sealed class WaiterStateDto
{
    public string RestaurantName { get; set; } = "";
    public DateTime ServerTime { get; set; } = DateTime.Now;
    public List<WaiterBoardDto> Boards { get; set; } = [];
    public List<WaiterProductDto> Products { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public List<WaiterStaffDto> Staff { get; set; } = [];
}

public sealed class WaiterBoardDto
{
    public string Kind { get; set; } = "MESA";
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Notes { get; set; } = "";
    public int Waiter { get; set; }
    public decimal Total { get; set; }
    public string TotalText { get; set; } = "";
    public List<WaiterLineDto> Lines { get; set; } = [];
}

public sealed class WaiterLineDto
{
    public int Index { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string UnitPriceText { get; set; } = "";
    public decimal Total { get; set; }
    public string TotalText { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class WaiterProductDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public string PriceText { get; set; } = "";
    public decimal Stock { get; set; }
}

public sealed class WaiterStaffDto
{
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

public sealed class WaiterOpenBoardRequest
{
    public string Kind { get; set; } = "MESA";
    public string BoardNumber { get; set; } = "";
    public string WaiterNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
}

public sealed class WaiterAddProductRequest
{
    public string Kind { get; set; } = "MESA";
    public string BoardNumber { get; set; } = "";
    public string WaiterNumber { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string Note { get; set; } = "";
}

public sealed class WaiterBoardNoteRequest
{
    public string Kind { get; set; } = "MESA";
    public string BoardNumber { get; set; } = "";
    public string WaiterNumber { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class WaiterRemoveLineRequest
{
    public string Kind { get; set; } = "MESA";
    public string BoardNumber { get; set; } = "";
    public int LineIndex { get; set; }
}

public sealed class WaiterBoardRequest
{
    public string Kind { get; set; } = "MESA";
    public string BoardNumber { get; set; } = "";
    public bool Paid { get; set; }
    public string PaymentMethod { get; set; } = "DINHEIRO";
    public decimal TenderedAmount { get; set; }
    public string Payer { get; set; } = "";
}

public sealed class WaiterActionResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public WaiterStateDto? State { get; set; }

    public static WaiterActionResult Success(string message, WaiterStateDto state) => new()
    {
        Ok = true,
        Message = message,
        State = state
    };

    public static WaiterActionResult Fail(string message, WaiterStateDto state) => new()
    {
        Ok = false,
        Message = message,
        State = state
    };
}

public sealed class MobileBridgeStatus
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string LocalUrl { get; set; } = "";
    public string NetworkUrl { get; set; } = "";
    public DateTime ServerTime { get; set; } = DateTime.Now;
    public WaiterStateDto? State { get; set; }
}

public sealed class MobilePrintRequest
{
    public string Kind { get; set; } = "receipt";
    public string Content { get; set; } = "";
    public string JobName { get; set; } = "Balcao Livre Mobile";
    public bool Compact { get; set; } = true;
    public string PrinterName { get; set; } = "";
}

public sealed class MobileImportRequest
{
    public List<MobileImportEvent> Events { get; set; } = [];
}

public sealed class MobileImportEvent
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public JsonElement Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
