namespace Spirometry.Web.Models
{
    public sealed class TestDetailsViewModel
    {

        public required TestListItemViewModel Test { get; init; }
        public List<MeasurementViewModel> Measurements { get; init; } = [];
    }

    public sealed class MeasurementViewModel
    {
       
        public required string Code { get; init; }

      
        public decimal Value { get; init; }

        
        public required string Unit { get; init; }

        
        public decimal? PredictedValue { get; init; }

       
        public decimal? LowerLimitOfNormal { get; init; }
    }
}


