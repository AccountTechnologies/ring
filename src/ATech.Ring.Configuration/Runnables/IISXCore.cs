namespace ATech.Ring.Configuration.Runnables
{
    public class IISXCore : CsProjRunnable
    {
        public override bool Equals(object obj) => obj is IISXCore express && CsProj == express.CsProj;

        public override int GetHashCode()
        {
            return -576574704 + CsProj.GetHashCode();
        }
    }
}