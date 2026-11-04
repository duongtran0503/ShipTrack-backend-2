namespace Logistics.Api.Hubs;

/// <summary>
/// Tập trung khai báo path các SignalR Hub.
/// Dùng chung ở JWT event và MapHub để tránh lệch path.
/// </summary>
public static class HubPaths
{
    public const string Logistics = "/hubs/logistics";
}