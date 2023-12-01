namespace ParkingFinder.Business.Utils;

public enum Mode
{
    Realtime,
    Simulation
}

public static class Global
{
    private static DateTime _time;
    public static DateTime Time
    {
        get
        {
            return Mode switch
            {
                Mode.Realtime => DateTime.Now,
                Mode.Simulation => _time,
                _ => default
            };
        }
        set
        {
            if (Mode == Mode.Simulation)
                _time = value;
        }
    }

    public static Mode Mode { get; set; } = Mode.Realtime;
}