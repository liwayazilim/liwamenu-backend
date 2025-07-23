namespace QR_Menu.Application.Common;

public class ResponsBase
{
    public string? Message_TR { get; set; } = string.Empty;
    public string? Message_EN { get; set; } = string.Empty;
    public string? StatusCode { get; set; } = string.Empty;
    public object? Data { get; set; } = null;

    public static ResponsBase Create(string trMessage = "", string enMessage = "", string code = "", object? data = null)
    {
        return new ResponsBase
        {
            Message_TR = trMessage,
            Message_EN = enMessage,
            StatusCode = code,
            Data = data
        };
    }
} 