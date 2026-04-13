:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated-Server for details

@echo on

set Name="Challenge Dedicated Server"
set Mod=challenge-ra
set Map=""
set ListenPort=1234
set AdvertiseOnline=False
set AdvertiseOnLocalNetwork=True
set Password=""
set RecordReplays=True

set RequireAuthentication=False
set ProfileIDBlacklist=""
set ProfileIDWhitelist=""

set EnableSingleplayer=True
set EnableSyncReports=False
set EnableGeoIP=False
set EnableLintChecks=True
set ShareAnonymizedIPs=True

set FloodLimitJoinCooldown=5000

set SupportDir=""

set AllowBotOnlyGames=True

REM Challenge-specific settings
set Challenge.Enabled=True
if "%~1"=="" (
    echo No MatchId provided, using auto-generated ID
    set Challenge.MatchId=
) else (
    echo Using MatchId: %~1
    set Challenge.MatchId=%~1
)

:loop

bin\OpenRA.Server.exe Engine.EngineDir=".." Game.Mod=%Mod% ^
  Server.Name=%Name% ^
  Server.Map=%Map% ^
  Server.ListenPort=%ListenPort% ^
  Server.AdvertiseOnline=%AdvertiseOnline% ^
  Server.EnableSingleplayer=%EnableSingleplayer% ^
  Server.AllowBotOnlyGames=%AllowBotOnlyGames% ^
  Server.Password=%Password% ^
  Server.RecordReplays=%RecordReplays% ^
  Server.EnableSyncReports=%EnableSyncReports% ^
  Server.EnableGeoIP=%EnableGeoIP% ^
  Server.EnableLintChecks=%EnableLintChecks% ^
  Server.ShareAnonymizedIPs=%ShareAnonymizedIPs% ^
  Server.FloodLimitJoinCooldown=%FloodLimitJoinCooldown% ^
  Engine.SupportDir=%SupportDir% ^
  Challenge.Enabled=%ChallengeEnabled% ^
  Challenge.MatchId=%ChallengeMatchId%

goto loop
