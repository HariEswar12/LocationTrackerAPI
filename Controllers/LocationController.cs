using LocationTrackerAPI.Hubs;
using LocationTrackerAPI.Models;
using LocationTrackerAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace LocationTrackerAPI.Controllers
{
    [ApiController]
    [Route("api/location")]
    public class LocationController : ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly IHubContext<LocationHub> _hub;

        public LocationController(MongoService mongo, IHubContext<LocationHub> hub)
        {
            _mongo = mongo;
            _hub = hub;
        }

        // 🚀 TRACK LOCATION (SAVE + REALTIME)
        //[Authorize]
        //[HttpPost("track")]
        //public async Task<IActionResult> Track(Location loc)
        //{
        //    var userId = User.FindFirst("id")?.Value;
        //    var username = User.Identity?.Name ?? "Unknown";

        //    if (userId == null)
        //        return Unauthorized();

        //    loc.UserId = userId;
        //    loc.Username = username;
        //    loc.Timestamp = DateTime.UtcNow;

        //    await _mongo.Locations.InsertOneAsync(loc);

        //    // 🔴 Send to all clients (admin + users)
        //    await _hub.Clients.All.SendAsync(
        //        "ReceiveLocation",
        //        userId,
        //        username,
        //        loc.Latitude,
        //        loc.Longitude,
        //        loc.Timestamp
        //    );

        //    return Ok(new { message = "Location saved" });
        //}
        [Authorize]
        [HttpPost("track")]
        public async Task<IActionResult> Track(TrackDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            var username = User.FindFirst("username")?.Value;

            var loc = new Location
            {
                UserId = userId,
                Username = username,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Speed = dto.Speed,
                Timestamp = DateTime.UtcNow
            };

            await _mongo.Locations.InsertOneAsync(loc);

            await _hub.Clients.All.SendAsync(
                "ReceiveLocation",
                userId,
                username,
                loc.Latitude,
                loc.Longitude,
                loc.Speed
            );

            return Ok(new { message = "Location saved" });
        }
        // 📍 GET MY HISTORY
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> My()
        {
            var userId = User.FindFirst("id")?.Value;

            if (userId == null)
                return Unauthorized();

            var history = await _mongo.Locations
                .Find(x => x.UserId == userId)
                .SortBy(x => x.Timestamp)
                .ToListAsync();

            return Ok(history);
        }

        // 👨‍💼 ADMIN: GET ALL USERS HISTORY
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> All()
        {
            var data = await _mongo.Locations
                .Find(_ => true)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();

            return Ok(data);
        }

        // 👨‍💼 ADMIN: GET HISTORY BY USER
        [Authorize(Roles = "Admin")]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserHistory(string userId)
        {
            var history = await _mongo.Locations
                .Find(x => x.UserId == userId)
                .SortBy(x => x.Timestamp)
                .ToListAsync();

            return Ok(history);
        }

        // 📊 FILTER BY DATE RANGE
        [Authorize]
        [HttpGet("range")]
        public async Task<IActionResult> GetByRange(DateTime from, DateTime to)
        {
            var userId = User.FindFirst("id")?.Value;

            var history = await _mongo.Locations
                .Find(x =>
                    x.UserId == userId &&
                    x.Timestamp >= from &&
                    x.Timestamp <= to
                )
                .SortBy(x => x.Timestamp)
                .ToListAsync();

            return Ok(history);
        }

        // 📍 GET LATEST LOCATION OF ALL USERS (ADMIN DASHBOARD)
        [Authorize(Roles = "Admin")]
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestLocations()
        {
            var latest = await _mongo.Locations
                .Aggregate()
                .SortByDescending(x => x.Timestamp)
                .Group(x => x.UserId, g => new
                {
                    UserId = g.Key,
                    Location = g.First()
                })
                .ToListAsync();

            return Ok(latest);
        }
    }
}