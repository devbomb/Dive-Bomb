namespace FastDragon
{
    public interface IChargeable
    {
        bool CausesBonk => false;
        void OnCharged();
    }
}