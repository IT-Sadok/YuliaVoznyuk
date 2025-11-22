namespace BookingSystem.Application.DTOs;

public class AuthResultDto
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static AuthResultDto Success(string tokenOrMessage) =>
        new() { Success = true, Token = tokenOrMessage };

    public static AuthResultDto Fail(string error) =>
        new() { Success = false, Errors = new List<string> { error } };

    public static AuthResultDto Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };
}