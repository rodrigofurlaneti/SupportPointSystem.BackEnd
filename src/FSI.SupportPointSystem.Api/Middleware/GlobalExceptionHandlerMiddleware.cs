using FluentValidation;
using FSI.SupportPointSystem.Domain.Exceptions;
using System.Text.Json;

namespace FSI.SupportPointSystem.Api.Middleware;

/// <summary>
/// Middleware global para tratamento de exceções.
/// Converte exceções de domínio nos status HTTP corretos.
/// Garante que nunca vaze stack trace para o cliente em produção.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Falha de validação: {Errors}", ex.Errors.Select(e => e.ErrorMessage));
            await WriteResponse(context, StatusCodes.Status422UnprocessableEntity, new
            {
                code = "VALIDATION_FAILED",
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Recurso não encontrado: {Message}", ex.Message);
            await WriteResponse(context, StatusCodes.Status404NotFound, new
            {
                code = "NOT_FOUND",
                description = ex.Message
            });
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning("Violação de regra de negócio [{Rule}]: {Message}", ex.RuleName, ex.Message);
            await WriteResponse(context, StatusCodes.Status422UnprocessableEntity, new
            {
                code = ex.RuleName,
                description = ex.Message
            });
        }
        catch (DomainValidationException ex)
        {
            logger.LogWarning("Validação de domínio: {Message}", ex.Message);
            await WriteResponse(context, StatusCodes.Status400BadRequest, new
            {
                code = "DOMAIN_VALIDATION",
                description = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado na requisição {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteResponse(context, StatusCodes.Status500InternalServerError, new
            {
                code = "INTERNAL_ERROR",
                description = "Ocorreu um erro interno. Tente novamente mais tarde."
            });
        }
    }

    private static async Task WriteResponse(HttpContext context, int statusCode, object body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
