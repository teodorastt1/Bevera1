namespace Bevera.Models.Enums
{
    public enum OrderStatus
    {
        Draft = 0,          // в количка / не е пратена
        Submitted = 1,      // клиент прати
        Preparing = 2,      // работник подготвя
        ReadyForPickup = 3, // готова
        Delivered = 4,      // доставена/пристигнала
        Received = 5,       // клиент потвърди получаване
        Cancelled = 6
    }
}
