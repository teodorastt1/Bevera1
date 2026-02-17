namespace Bevera.Helpers
{
    public static class OrderStates
    {
        // Must match enum names in Models/Enums/OrderStatus.cs
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string Preparing = "Preparing";
        public const string ReadyForPickup = "ReadyForPickup";
        public const string Delivered = "Delivered";
        public const string Received = "Received";
        public const string Cancelled = "Cancelled";
    }

    public static class PaymentStates
    {
        public const string Unpaid = "Unpaid";
        public const string Paid = "Paid";
    }
}
