using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace cs2_glow_plugin;

public class Plugin : BasePlugin
{
    public override string ModuleName => "CS2 glow plugin";

    public override string ModuleVersion => "1.0";

    private ulong lastBenefittingPlayerId;

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"cs2-glow-plugin {(hotReload ? "HOT" : "COLD")} loaded!");
        base.Load(hotReload);
    }

    [ConsoleCommand("give_wallhack", "gives a player wallhacks")]
    [CommandHelper(minArgs: 1, usage: "[target]", CommandUsage.SERVER_ONLY)]
    public void GiveWalls(CCSPlayerController? CommandSendingPlayer, CommandInfo command)
    {
        if (CommandSendingPlayer is not null)
        {
            Console.WriteLine("Command was sent by player, ignoring.");
            return;
        }

        var target = command.GetArg(1);
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        Console.WriteLine($"Attempting to give wallhack to {target}");
        var AllPlayers = GetPlayers();
        CCSPlayerController? BenefittingPlayer = AllPlayers.Where(p => p.PlayerName.Contains(target, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        if (BenefittingPlayer is null)
        {
            Console.WriteLine("Player not found");
            return;
        }

        ApplyGlowToAllOtherPlayers(BenefittingPlayer.SteamID);



        Console.WriteLine("Custom command called.");
    }


    private void ApplyGlowToAllOtherPlayers(ulong playerId)
    {

        var AllPlayers = GetPlayers();

        var sufferingPlayers = AllPlayers.Where(p => p.SteamID != playerId);
        var benefittingPlayer = AllPlayers.First(p =>  p.SteamID == playerId);

        foreach (CCSPlayerController suffering in sufferingPlayers)
        {
            Console.WriteLine($"going to add glow to player {suffering.PlayerName}");
            if (suffering is not null)
            {
                SetGlowing(suffering, benefittingPlayer.TeamNum);
            }
        }
        lastBenefittingPlayerId = benefittingPlayer.SteamID;

        Console.WriteLine($"benefitting player {benefittingPlayer.PlayerName} ({benefittingPlayer.SteamID})");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart gameEvent, GameEventInfo info)
    {
        if (lastBenefittingPlayerId != null)
        {
            ApplyGlowToAllOtherPlayers(lastBenefittingPlayerId);
        }

        return HookResult.Continue;
    }


    IEnumerable<CCSPlayerController> GetPlayers()
    {
        Console.WriteLine("Looking through players");
        List<CCSPlayerController> output = new();
        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            var p = Utilities.GetPlayerFromSlot(i);
            if (p is not null)
            {
                Console.WriteLine($"found player {p.PlayerName}");
                output.Add(p);
            }
        }
        return output;
    }

    void SetGlowing(CCSPlayerController player, int team)
    {
        Console.WriteLine($"Adding glow to {player.PlayerName}");
        var pawn = player.PlayerPawn.Value;
        if (pawn is null)
        {
            return;
        }

        CBaseModelEntity? modelGlow = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        CBaseModelEntity? modelRelay = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (modelGlow == null || modelRelay == null)
        {
            return;
        }

        string modelName = pawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();

        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.DispatchSpawn();

        modelGlow.Glow.GlowColorOverride = Color.Blue;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = team;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 100;

        modelRelay.AcceptInput("FollowEntity", pawn, modelRelay, "!activator");
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

    }
}
