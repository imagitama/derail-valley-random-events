# WORK IN PROGRESS - USE AT OWN RISK (DO NOT USE IN YOUR CAREER SAVES)

# Derail Valley Random Events mod

A mod for the game [Derail Valley](https://store.steampowered.com/app/588030/Derail_Valley/) that makes random events occur as you're driving your train through the world.

**Depends on mod [Comms Radio API](https://www.nexusmods.com/derailvalley/mods/813?tab=files)**

### Known issues

- only relies on your current biome so may spawn in a train station or weird place
- may spawn in tunnels
- cows are silly :)

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

## Install

Download the zip and use Unity Mod Manager to install it.

## How it works

It checks every second if an event needs to be emitted.

Then there is a percentage chance of the event occuring.

An initial delay of X seconds after you load into a game before anything is checked.

At least Y seconds between events.

No more than Z seconds between events.

## Adding/editing a new obstacle mesh

1. In Unity 2019 create your scene
2. Create a new gameobject called whatever and add a mesh inside it at 0,0,0 (use whatever materials/components you like)\
   **Ensure your FBX/OBJ has "Read/write" enabled!**
3. Create a prefab of your obstacle
4. Assign it to an assetbundle that corresponds with your Obstacle definition eg. `trees` or `rocks`
5. Export assetbundle
6. Copy assetbundle into the mod's dependencies & launch game

If your gameobject doesn't have a collider or rigidbody component it will be added for you (using the first mesh it finds as a MeshCollider).

## Development

Created in VSCode (with C# and C# Dev Kit extensions) and MSBuild.

1. Run `msbuild` in root to build

Template from https://github.com/derail-valley-modding/template-umm

## Publishing

1. Run `.\package.ps1`
