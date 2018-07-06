namespace LeagueFPSBoost.IO
{
    interface ILeagueClientFileInfo : IBasicFileInfo
    {
        string PatchVersion { get; }
    }
}
