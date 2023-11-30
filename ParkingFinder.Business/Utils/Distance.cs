namespace ParkingFinder.Business.Utils;

public static class Distance
{
    public static double Calculate(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371008.8;

        var latDiff = lat2 - lat1;
        var lonDiff = lon2 - lon1;

        var latAvg = lat2 + lat1 / 2;

        var latDist = (double)latDiff * radius;
        var lonDist = (double)lonDiff * radius * Math.Cos((double)latAvg);
        
        double distance = Math.Sqrt(latDist * latDist + lonDist * lonDist);

        return distance;
    }
}