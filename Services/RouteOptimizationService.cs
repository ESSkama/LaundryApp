using Laundry.Patterns.Singleton;

namespace Laundry.Services
{
    public class RouteOptimizationService
    {
        private readonly Dictionary<int, DriverLocation> _driverLocations = new();
        private readonly Random _random = new();

        // Public parameterless constructor for DI
        public RouteOptimizationService()
        {
            OrderLogger.Instance.LogEvent("RouteOptimizationService initialized", EventLevel.Info);
        }

        public void UpdateDriverLocation(int driverId, double latitude, double longitude)
        {
            _driverLocations[driverId] = new DriverLocation
            {
                DriverId = driverId,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdate = DateTime.Now
            };
            OrderLogger.Instance.LogEvent($"Driver {driverId} location updated", EventLevel.Info);
        }

        public async Task<RouteInfo> OptimizeDeliveryRouteAsync(string pickupAddress, string deliveryAddress, int? driverId = null)
        {
            await Task.Delay(100);
            var distance = _random.Next(2, 30);
            var duration = TimeSpan.FromMinutes(distance * 2);

            return new RouteInfo
            {
                DistanceKm = distance,
                EstimatedDuration = duration,
                EstimatedArrivalTime = DateTime.Now.Add(duration),
                IsDriverAssigned = driverId.HasValue,
                DriverId = driverId ?? _random.Next(100, 999)
            };
        }

        public async Task<DriverProximity> GetDriverProximityAsync(int driverId, string destinationAddress)
        {
            await Task.Delay(50);
            var minutesAway = _random.Next(1, 60);

            string status = minutesAway switch
            {
                <= 1 => "Arrived",
                <= 5 => "Very Close - 5 minutes away",
                <= 15 => "En Route - On the way",
                _ => $"On the way - ETA {minutesAway} minutes"
            };

            return new DriverProximity
            {
                MinutesAway = minutesAway,
                Status = status,
                Latitude = -33.9608 + (_random.NextDouble() * 0.1),
                Longitude = 18.4602 + (_random.NextDouble() * 0.1),
                EstimatedArrival = DateTime.Now.AddMinutes(minutesAway)
            };
        }

        public DriverLocation? GetDriverLocation(int driverId)
        {
            return _driverLocations.GetValueOrDefault(driverId);
        }
    }

    public class DriverLocation
    {
        public int DriverId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? Status { get; set; }
    }

    public class RouteInfo
    {
        public double DistanceKm { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public DateTime EstimatedArrivalTime { get; set; }
        public bool IsDriverAssigned { get; set; }
        public int DriverId { get; set; }
        public List<RoutePoint> RoutePoints { get; set; } = new();
        public string TrafficCondition { get; set; } = "Normal";
    }

    public class DriverProximity
    {
        public int MinutesAway { get; set; }
        public string Status { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime EstimatedArrival { get; set; }
    }

    public class RoutePoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Order { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}