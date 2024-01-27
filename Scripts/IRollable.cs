namespace FastDragon
{
    public interface IRollable
    {
        bool CausesBonk => false;
        void OnRolledInto();
    }
}