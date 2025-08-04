// TODO: This interface should be moved to Application layer or use Domain entities
// Domain layer should not depend on Application layer DTOs

// using QR_Menu.Application.Payments.DTOs;

// namespace QR_Menu.Domain.Common.Interfaces;

// public interface IPayTRService
// {
//     Task<(bool Success, string Message, PayTRDirectPaymentResponse? Response)> ProcessDirectPaymentAsync(PayTRDirectPaymentRequest request);
//     Task<(bool Success, string Message, PayTRCreateLinkResponse? Response)> CreatePaymentLinkAsync(PayTRCreateLinkRequest request);
//     Task<(bool Success, string Message)> DeletePaymentLinkAsync(string linkId);
//     Task<(bool Success, string Message)> ProcessCallbackAsync(PayTRCallbackRequest callback);
//     string GenerateToken(string concatenatedFields);
//     string ConcatenateFieldsForDirectPayment(PayTRDirectPaymentRequest request);
//     string ConcatenateFieldsForCreateLink(PayTRCreateLinkRequest request);
//     string ConcatenateFieldsForDeleteLink(string linkId);
//     string ConcatenateFieldsForCallback(PayTRCallbackRequest callback);
// } 