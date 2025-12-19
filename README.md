# WORK IN PROGRESS - USE AT OWN RISK

# Derail Valley Random Events mod

A mod for the game [Derail Valley](https://store.steampowered.com/app/588030/Derail_Valley/) that makes random events occur as you're driving your train through the world.

## Install

**Depends on mod [Comms Radio API](https://www.nexusmods.com/derailvalley/mods/813?tab=files) and [DerailValleyModToolbar](https://www.nexusmods.com/derailvalley/mods/1367)**

Download the zip and use Unity Mod Manager to install it.

## Known issues

- obstacle physics may spaz out
- cows/animal AI is very basic and glitchy

## Events

| Event    | Chance        | Description                                                          | Resolution                                                        |
| -------- | ------------- | -------------------------------------------------------------------- | ----------------------------------------------------------------- |
| Obstacle | 25% (per sec) | Spawn an obstacle or collection of obstacles in front of your train. | Use the comms radio to instantly clear any obstacles in your way. |

### Obstacles

Clear an obstacle by using your Comm Radio and selecting the "Clear Obstacle" option and using it on an obstacle.

| Obstacle  | Biome         | Description                                                  |
| --------- | ------------- | ------------------------------------------------------------ |
| Rockslide | Rock          | A group of heavy boulders are all over the track.            |
| Treefall  | Forest        | A tree has fallen onto the track.                            |
| Cows      | Meadow, Field | Some clumsy cows have walked onto the track. Honk your horn! |

Some obstacles can only be manually spawned from the panel:

| Obstacle      | Description                               |
| ------------- | ----------------------------------------- |
| FunRamp       | Make it fly!                              |
| Other animals | A variety of animals already in the game. |

## When it happens

Every second it checks some conditions:

- has there been enough time after your game has started
- has a minimum time past OR has a maximum time past
- are you in the correct biome
- are you unlucky

If this happens, a random obstacle will spawn ahead of your train which you can clear away using your comm radio (or a little nudge!).

## Creating your own obstacles

Create a JSON file under `Dependencies/Obstacles` and define all of your obstacle parameters.

In Unity create any number of prefabs and place them in an [AssetBundle](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetBundlesIntro.html) inside `Dependencies/AssetBundles`. The mod will randomly pick a prefab.

That should be it. If the player is in the correct biome and the conditions are met, your custom obstacle will be emitted. You can test it using the panel.

### Exploding

Make sure your prefab is set up exactly like this:

```
MyPrefabNamedWhatever
  [Base]
    ...anything you want to hide when it explodes...
  [Explode]
    ...anything you want to show when it explodes...
```

The mod will automatically hide everything inside of `[Explode]`.

On explosion:

- Anything inside of `[Base]` will be hidden

- Anything inside of `[Explode]` will be shown

- Any particle systems inside of `[Explode]` will be force played **once** for explosion effects

- Any rigidbodies inside of `[Explode]` will be launched in every direction

Note the separate meshes/rigidbodies are still considered part of the obstacle so cleaning up one bit will clean it all up.

## Development

Created in VSCode (with C# and C# Dev Kit extensions) and MSBuild.

1. Run `msbuild` in root to build

Template from https://github.com/derail-valley-modding/template-umm

## Publishing

1. Run `.\package.ps1`
