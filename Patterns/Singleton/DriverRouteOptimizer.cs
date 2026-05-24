using Laundry.Patterns.Singleton;
namespace FLaundry.Patterns.Singleton
{
    // Bonus: Driver Route Optimization Singleton
    public sealed class DriverRouteOptimizer
    {
        private static readonly Lazy<DriverRouteOptimizer> _instance = new(() => new DriverRouteOptimizer());
        private readonly Dictionary<string, Dictionary<string, int>> _distanceMatrix = new();

        private DriverRouteOptimizer()
        {
            // Initialize distance matrix (simulated)
            InitializeDistances();
        }

        public static DriverRouteOptimizer Instance => _instance.Value;

        private void InitializeDistances()
        {
            // Simulated distances between suburbs (in minutes)
            var suburbs = new[] { "CBD", "Northern Suburbs", "Southern Suburbs", "Eastern Suburbs", "Western Suburbs" };

            foreach (var from in suburbs)
            {
                _distanceMatrix[from] = new Dictionary<string, int>();
                foreach (var to in suburbs)
                {
                    if (from == to)
                        _distanceMatrix[from][to] = 0;
                    else
                        _distanceMatrix[from][to] = new Random(from.GetHashCode() + to.GetHashCode()).Next(15, 60);
                }
            }
        }

        public List<string> OptimizeRoute(List<string> addresses, string startingPoint)
        {
            // Simple nearest neighbor algorithm for route optimization
            var optimized = new List<string>();
            var remaining = new List<string>(addresses);
            var current = startingPoint;

            optimized.Add(current);
            remaining.Remove(current);

            while (remaining.Any())
            {
                var nearest = remaining.OrderBy(a => GetDistance(current, a)).First();
                optimized.Add(nearest);
                remaining.Remove(nearest);
                current = nearest;
            }

            OrderLogger.Instance.LogEvent($"Route optimized: {string.Join(" → ", optimized)}");
            return optimized;
        }

        public int GetDistance(string from, string to)
        {
            // Extract suburb from address (simplified)
            var fromSuburb = ExtractSuburb(from);
            var toSuburb = ExtractSuburb(to);

            if (_distanceMatrix.ContainsKey(fromSuburb) && _distanceMatrix[fromSuburb].ContainsKey(toSuburb))
                return _distanceMatrix[fromSuburb][toSuburb];

            return 30; // Default 30 minutes
        }

        private string ExtractSuburb(string address)
        {
            // Simplified suburb extraction
            if (address.Contains("CBD")) return "CBD";
            if (address.Contains("North")) return "Northern Suburbs";
            if (address.Contains("South")) return "Southern Suburbs";
            if (address.Contains("East")) return "Eastern Suburbs";
            if (address.Contains("West")) return "Western Suburbs";
            return "CBD";
        }

        public int CalculateEstimatedDeliveryTime(string pickupAddress, string deliveryAddress)
        {
            return GetDistance(pickupAddress, deliveryAddress);
        }
    }
}