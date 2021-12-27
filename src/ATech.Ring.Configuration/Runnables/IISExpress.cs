namespace ATech.Ring.Configuration.Runnables;

public class IISExpress : CsProjRunnable
{
    public override bool Equals(object obj) => obj is IISExpress express && CsProj == express.CsProj;

    public override int GetHashCode()
    {
        return -576574704 + CsProj.GetHashCode();
    }
}
