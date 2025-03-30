namespace ClassNotes.API.Services.Distance
{
    //DD: Codigo para validar si encuentra dentro del rango establecido 
    public class DistanceService
    {
        public double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371e3; // Radio de la Tierra en metros
            double dLat = (lat2 - lat1) * (Math.PI / 180);
            double dLon = (lon2 - lon1) * (Math.PI / 180);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
