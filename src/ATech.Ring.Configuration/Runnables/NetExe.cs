namespace ATech.Ring.Configuration.Runnables
{
    public class NetExe : CsProjRunnable
    {     
        public string[] Args { get; set; } = { };
        public override bool Equals(object obj) => obj is NetExe exe && CsProj == exe.CsProj;

        public override int GetHashCode()
        {
            return -576574704 + CsProj.GetHashCode();
        }
    }
}