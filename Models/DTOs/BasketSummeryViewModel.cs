namespace SecureApi.Models.DTOs
{
    public class BasketSummeryViewModel
    {
        public int BasketCount { get; set; }

        public decimal BasketTotal { get; set; }

        public BasketSummeryViewModel()
        {

        }
        public BasketSummeryViewModel(int basketCount, decimal basketTotal)
        {
            BasketCount = basketCount;
            BasketTotal = basketTotal;
        }
    }
}
