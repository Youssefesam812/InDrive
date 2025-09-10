using Microsoft.AspNetCore.SignalR;
using Snap.APIs.DTOs;
using Snap.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Snap.APIs.Hubs
{
    public class LocationHub : Hub
    {
        private readonly SnapDbContext _context;
        private static readonly ConcurrentDictionary<int, DriverLocationResponseDto> _onlineDrivers = new();
        private static readonly ConcurrentDictionary<string, int> _connectionToDriverMap = new();

        public LocationHub(SnapDbContext context)
        {
            _context = context;
        }

        // Driver connects and starts sharing location
        public async Task ConnectDriver(int driverId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");
            
            // Map connection ID to driver ID for tracking
            _connectionToDriverMap[Context.ConnectionId] = driverId;
            
            // Get driver info from database
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
            if (driver != null)
            {
                var driverLocation = new DriverLocationResponseDto
                {
                    DriverId = driverId,
                    DriverName = driver.DriverFullname,
                    Lat = 0,
                    Lng = 0,
                    LastUpdate = DateTime.UtcNow,
                    IsOnline = true
                };
                
                _onlineDrivers.AddOrUpdate(driverId, driverLocation, (key, oldValue) => driverLocation);
                
                // Notify all clients that a new driver is online
                await Clients.All.SendAsync("DriverConnected", driverLocation);
            }
        }

        // User/admin connects to receive location updates
        public async Task ConnectClient()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Clients");
            
            // Send current online drivers to the newly connected client
            var onlineDrivers = _onlineDrivers.Values.ToList();
            await Clients.Caller.SendAsync("OnlineDrivers", onlineDrivers);
        }

        // Driver updates their location
        public async Task UpdateLocation(LocationUpdateDto location)
        {
            // Get driver ID from connection mapping
            if (_connectionToDriverMap.TryGetValue(Context.ConnectionId, out var driverId))
            {
                if (_onlineDrivers.TryGetValue(driverId, out var driverLocation))
                {
                    // Update location
                    driverLocation.Lat = location.Lat;
                    driverLocation.Lng = location.Lng;
                    driverLocation.LastUpdate = location.Timestamp;
                    driverLocation.IsOnline = true;

                    // Broadcast location update to all clients
                    await Clients.Group("Clients").SendAsync("LocationUpdate", driverLocation);
                    
                    // Also broadcast to other drivers if needed
                    await Clients.GroupExcept("Drivers", Context.ConnectionId).SendAsync("DriverLocationUpdate", driverLocation);
                }
            }
        }

        // Update location with driver ID (alternative method)
        public async Task UpdateLocationWithDriverId(int driverId, LocationUpdateDto location)
        {
            if (_onlineDrivers.TryGetValue(driverId, out var driverLocation))
            {
                // Update location
                driverLocation.Lat = location.Lat;
                driverLocation.Lng = location.Lng;
                driverLocation.LastUpdate = location.Timestamp;
                driverLocation.IsOnline = true;

                // Broadcast location update to all clients
                await Clients.Group("Clients").SendAsync("LocationUpdate", driverLocation);
                
                // Also broadcast to other drivers if needed
                await Clients.Group("Drivers").SendAsync("DriverLocationUpdate", driverLocation);
            }
        }

        // Get all online drivers
        public async Task GetOnlineDrivers()
        {
            var onlineDrivers = _onlineDrivers.Values.Where(d => d.IsOnline).ToList();
            await Clients.Caller.SendAsync("OnlineDrivers", onlineDrivers);
        }

        // Get specific driver location
        public async Task GetDriverLocation(int driverId)
        {
            if (_onlineDrivers.TryGetValue(driverId, out var driverLocation))
            {
                await Clients.Caller.SendAsync("DriverLocation", driverLocation);
            }
            else
            {
                await Clients.Caller.SendAsync("DriverNotFound", driverId);
            }
        }

        // Driver disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Get driver ID from connection mapping
            if (_connectionToDriverMap.TryRemove(Context.ConnectionId, out var driverId))
            {
                if (_onlineDrivers.TryGetValue(driverId, out var driverLocation))
                {
                    driverLocation.IsOnline = false;
                    driverLocation.LastUpdate = DateTime.UtcNow;
                    
                    // Notify all clients that driver went offline
                    await Clients.All.SendAsync("DriverDisconnected", driverLocation);
                    
                    // Remove from online drivers after some time (optional)
                    // _onlineDrivers.TryRemove(driverId, out _);
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Ping to keep connection alive
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }
    }
}