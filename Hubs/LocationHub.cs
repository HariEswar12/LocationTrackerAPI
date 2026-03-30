namespace LocationTrackerAPI.Hubs
{
    using Microsoft.AspNetCore.SignalR;

    public class LocationHub : Hub
    {
        public async Task SendLocation(string userId, double lat, double lng)
        {
            await Clients.All.SendAsync("ReceiveLocation", userId, lat, lng);
        }
    }
}
