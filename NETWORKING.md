# Networking

Fisherman now uses UNet's `[Command]`, `[ClientRpc]`, `[SyncVar]` and `[Server]` attributes. These only do something once the built dll has been run through the **UNet weaver**, which generates the code behind them.

**If the dll isn't weaved, nothing errors.** Commands just run on the client instead of the server, and multiplayer quietly breaks again. Always weave.

Every networking-related change in the code has a `// NETWORKING:` comment explaining why. Search for that to find them all.

## Building

### With the csproj (recommended)

`FishermanMod/FishermanMod.csproj` has a `WeaveNetworking` target that runs after every build, the same setup Aetherium uses:

```
cd FishermanMod
dotnet build -c Release
```

The weaved dll is `FishermanMod/bin/Release/netstandard2.1/FishermanMod.dll`. The weaver itself lives in `FishermanMod/Weaver/`.

The csproj was gitignored by the Unity-style `.gitignore` (`*.csproj`). There is now an exception for this one file so the weave step gets shared.

### From Unity / ThunderKit

When Unity compiles the scripts itself, the dll is **probably not weaved**. Unity only weaves automatically when the `com.unity.multiplayer-hlapi` *package* is installed in the project. A ThunderKit RoR2 import brings in the game's hlapi dll instead, which doesn't weave anything.

To weave a Unity-built dll, run the weaver on it by hand:

```
FishermanMod\Weaver\Unity.UNetWeaver.exe ^
  "<RoR2>\Risk of Rain 2_Data\Managed\UnityEngine.CoreModule.dll" ^
  "<RoR2>\Risk of Rain 2_Data\Managed\com.unity.multiplayer-hlapi.Runtime.dll" ^
  "<folder the dll is in>" ^
  "<folder the dll is in>\FishermanMod.dll" ^
  "<folder containing every referenced dll>"
```

The last argument has to be one folder holding everything the mod references: the game's `Managed` folder contents plus BepInEx, MMHOOK and the R2API module dlls. The csproj does this by copying all references into `obj/.../WeaverLibs`.

### Checking that it worked

Open the dll in ILSpy or dnSpy and look at `SkillObjectTracker`. A weaved build has generated methods like `CallCmdRecallHooks` and `InvokeCmdCmdRecallHooks`. If those are missing, the dll wasn't weaved.

## What runs where

| Thing | Where it runs | How it gets there |
|---|---|---|
| Recall hooks (Secondary) | Server | `SkillObjectTracker.RecallHooksAuthority` → `CmdRecallHooks` |
| Hook catching and throwing objects/allies/bombs, recall physics | Server | Hook trigger/collision callbacks bail out on clients |
| Hook recall line flash | All clients | `FishHookController.RpcOnRecalled` |
| Hook hitstop | The hook owner's machine | `FishHookController.RpcApplyHitStop` |
| Hook-fail jolt | The hook owner's machine | `SkillObjectTracker.RpcHookFeedback` |
| Hook screen shake | All clients | `SkillObjectTracker.RpcHookFeedback` |
| Hook bomb stock count | Server | `CmdSetPendingHookBombStocks`, sent before the bomb is fired (same reliable channel, so it arrives first) |
| Detonate hook bombs (Special recast) | Server | `CmdHookAllBombs` |
| Tether line visual | All clients | `HookBombController.RpcAddTetherVisual` |
| Tether debuff add/remove | Server | Already server-only (damage hooks), plus guards on the cleanup paths |
| Summon platform | Server | `CmdSummonPlatform`, because `MasterSummon.Perform` only works on the server |
| Direct platform / set enemy / destroy platform | Server (BaseAI is server-only) | `CmdDirectPlatforms`, `CmdDestroyAllPlatforms` |
| Platform command point and aim target indicators | The platform owner's machine | `PlatformMinionController` SyncVars |
| Bomblet spread | Everywhere, same result | Randomness is seeded from the bomblet's `netId` |
| Bottle hidden after throwing, recall animations | All clients | Moved out of `isAuthority` checks |

Most of these follow vanilla's convention: `XAuthority()` runs `X()` directly on the server, or sends `CmdX()` from a client.

## Things to check on the Unity side

I couldn't see the asset bundle, so these are assumptions:

- **Projectile prefabs.** `FishermanHookProjectile` and `FishermanFloatingBombProjectile` come from the bundle. They need a `NetworkIdentity` and a `ProjectileNetworkTransform`. Without the network transform, clients won't see the hook fly back on recall, because recall physics now only runs on the server. The hook bomb is cloned from the Loader pylon, which already has one.
- **`ShantyMinionBody`.** It needs its `NetworkIdentity`. `PlatformMinionController` is now a NetworkBehaviour that gets added in code before the prefab is registered.
- **Assembly name.** The csproj builds `FishermanMod.dll`. If anything in the bundle references a script from this assembly (for example `ShantyMoveShootState` on an AI master), the assembly name must match whatever the bundle was built against.

## Not touched

- `SummonWhale` / `DepricatedWhaleProjectile`: the skill isn't in the loadout, and it still calls `MasterSummon` on the client. If it comes back, route it through the server the same way `SummonPlatformAuthority` does.
- `NervesDrinkState.AddBuffForeachDebuff` is never called. If it gets used, it has to run on the server because it adds and removes buffs.
