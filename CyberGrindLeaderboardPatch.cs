using HarmonyLib;

namespace ElementalBattlegroundsMod
{
    // Elemental Battlegrounds deliberately allows arsenal combinations that are impossible in
    // vanilla ULTRAKILL. Keep Cyber Grind high scores local, but never upload an EB run to the
    // public Steam leaderboard. FinalCyberRank handles the local-best save separately after
    // this method returns, so suppressing this call affects only public submission.
    [HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.SubmitCyberGrindScore))]
    internal static class CyberGrindLeaderboardPatch
    {
        private static bool Prefix()
        {
            Plugin.LogSource?.LogInfo("Blocked Cyber Grind leaderboard submission while Elemental Battlegrounds is loaded; local high score remains enabled.");
            return false;
        }
    }
}
