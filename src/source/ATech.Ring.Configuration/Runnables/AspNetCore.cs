namespace ATech.Ring.Configuration.Runnables
{
    public class AspNetCore : CsProjRunnable
    {
        public string[] Urls { get; set; } = { };
        public override bool Equals(object obj) => obj is AspNetCore core && CsProj == core.CsProj;
        public override int GetHashCode() => -576574704 + CsProj.GetHashCode();
    }
}