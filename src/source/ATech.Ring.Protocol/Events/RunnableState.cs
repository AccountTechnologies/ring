namespace ATech.Ring.Protocol.Events
{
    public enum RunnableState
    {
        ZERO = 0,
        INITIATED = 1,
        STARTED = 2,
        HEALTH_CHECK =3,
        HEALTHY = 4,
        DEAD = 5,
        RECOVERING = 6
    }
}