namespace P3.Driver.FroniusSolar.Model
{
    public class DataResponse<T> 
    {
        public Head Head { get; set; }

        public Body<T> Body { get; set; }
    }
}
