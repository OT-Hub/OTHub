using Swashbuckle.AspNetCore.Filters;

namespace OTHub.APIServer
{
    public class OfferExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return "0xd4447b2fe112e73702505cbbf0afa88c7c90440e8ea0509ae2f31d695c4af5d8";
        }
    }
}