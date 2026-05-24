namespace Laundry.Patterns
{
    // Decorator Pattern - Dynamically add features to orders
    public abstract class OrderComponent
    {
        public abstract string Description { get; }
        public abstract decimal CalculateCost();
        public abstract int GetEstimatedHours();
        public abstract string GetExtrasDescription();
    }

    public class BaseLaundryOrder : OrderComponent
    {
        private readonly ILaundryService _service;

        public BaseLaundryOrder(ILaundryService service)
        {
            _service = service;
        }

        public override string Description => _service.Name;
        public override decimal CalculateCost() => _service.BasePrice;
        public override int GetEstimatedHours() => _service.EstimatedHours;
        public override string GetExtrasDescription() => "Base service";
    }

    public abstract class LaundryDecorator : OrderComponent
    {
        protected OrderComponent _order;
        protected LaundryDecorator(OrderComponent order) => _order = order;
    }

    public class FabricSoftenerDecorator : LaundryDecorator
    {
        public FabricSoftenerDecorator(OrderComponent order) : base(order) { }

        public override string Description => _order.Description;
        public override decimal CalculateCost() => _order.CalculateCost() + 5.00m;
        public override int GetEstimatedHours() => _order.GetEstimatedHours();
        public override string GetExtrasDescription() => _order.GetExtrasDescription() + " + Fabric Softener";
    }

    public class StainRemovalDecorator : LaundryDecorator
    {
        public StainRemovalDecorator(OrderComponent order) : base(order) { }

        public override string Description => _order.Description;
        public override decimal CalculateCost() => _order.CalculateCost() + 12.00m;
        public override int GetEstimatedHours() => _order.GetEstimatedHours() + 1;
        public override string GetExtrasDescription() => _order.GetExtrasDescription() + " + Stain Removal";
    }

    public class PerfumeTreatmentDecorator : LaundryDecorator
    {
        public PerfumeTreatmentDecorator(OrderComponent order) : base(order) { }

        public override string Description => _order.Description;
        public override decimal CalculateCost() => _order.CalculateCost() + 8.00m;
        public override int GetEstimatedHours() => _order.GetEstimatedHours();
        public override string GetExtrasDescription() => _order.GetExtrasDescription() + " + Perfume Treatment";
    }

    public class DelicateWashDecorator : LaundryDecorator
    {
        public DelicateWashDecorator(OrderComponent order) : base(order) { }

        public override string Description => _order.Description;
        public override decimal CalculateCost() => _order.CalculateCost() + 10.00m;
        public override int GetEstimatedHours() => _order.GetEstimatedHours() + 2;
        public override string GetExtrasDescription() => _order.GetExtrasDescription() + " + Delicate Wash";
    }

    public class ExtraFoldingDecorator : LaundryDecorator
    {
        public ExtraFoldingDecorator(OrderComponent order) : base(order) { }

        public override string Description => _order.Description;
        public override decimal CalculateCost() => _order.CalculateCost() + 7.00m;
        public override int GetEstimatedHours() => _order.GetEstimatedHours() + 1;
        public override string GetExtrasDescription() => _order.GetExtrasDescription() + " + Premium Folding";
    }

    public class HangerPackagingDecorator : LaundryDecorator
    {
        public HangerPackagingDecorator(OrderComponent order) : base(order) { }

        public override string Description => _order.Description;
        public override decimal CalculateCost() => _order.CalculateCost() + 9.00m;
        public override int GetEstimatedHours() => _order.GetEstimatedHours();
        public override string GetExtrasDescription() => _order.GetExtrasDescription() + " + Hanger Packaging";
    }
}