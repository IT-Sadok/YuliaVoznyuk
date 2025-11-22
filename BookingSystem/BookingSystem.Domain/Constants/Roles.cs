namespace BookingSystem.Domain.Constants;

public static class Roles
{
    public const string Client = "Client";
    public const string Host = "Host";
    public const string Admin = "Admin";

    public static readonly string[] All = { Client, Host, Admin };
}