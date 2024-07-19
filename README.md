# netblox [![.github/workflows/dotnet-desktop.yml](https://github.com/AsertCreator/NetBlox/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/AsertCreator/NetBlox/actions/workflows/dotnet-desktop.yml)
i f̵̞̼͈́͐̉̀͘ų̵͙͉̩̳̝̜̈́͂͐c̶͇̀͌̚͝k̸͍̈̓̌̅̀ȉ̷̦̙̦̝͖̾̀n̷͓̠͆g̵͕͋͌ love roblox. thats why i decided to 
dedicate my two weeks school break on creating the clone of it on c#. it lowkey
looks like some "pixel gun 3d" type of game you would find on play store.

## Features
well there are instances, scripting(maybe), networking, physics(maybe). roblox offers the 
same, but a lot better, netblox is very poorly made. also there's physics engine named qu3e-sharp,
which i believe is licensed same as qu3e, so zlib. the lua engine used is a modified version of
moonsharp (so no luau for you). you can read my "blog" [here](https://asertcreator.github.io/netblox-blog/)

## System requirements
anything that can run raylib and .net. it consumes a bunch of memory becuse of everything,
around ~296 MB (on studio its half a gigabyte 💀), so make sure you have enough.

## How to play?
a weird question you have there, but to play this, you need to download installer, good luck
finding it tho. jk, its in github actions section, you unpack it, start it, then client should
open. the hard part is finding with who to play with.

## Architecture
`AppManager` class is static and holds within task scheduler (i.e. game processor), a lot
of common things. `GameManager` is some sort of sandboxed game, it holds one `DataModel`, and
also `Instance`s must be assigned to one `GameManager`, and there's no way for them to switch
ones. entire networking is managed by `NetworkManager` object, reference to which is in
`GameManager`. entire rendering is managed by `RenderManager`, which referenced the same way.
nothing else to describe.

that UniversalDuoHost project you see is not meant to be played by the player, its for development
only. it hosts both server and client.

## Contributing
all contributions should ideally adhere to the "constitution" thing i wrote bc idk what to do
with this project. the thing lies in `docs` folder

## Licenses
netblox. Copyright (c) 2024, AsertCreator. <br/>
raylib. Copyright (c) 2013-2024, Ramon Santamaria (check [license](https://github.com/raysan5/raylib/blob/master/LICENSE))<br/>
raylib_cs. Copyright (c) 2018-2024, ChrisDill (check [license](https://github.com/ChrisDill/Raylib-cs/blob/master/LICENSE))<br/>
MoonSharp. Copyright (c) 2014-2016, Marco Mastropaolo (check [license](https://github.com/moonsharp-devs/moonsharp/blob/master/LICENSE))<br/>
Qu3e-Sharp. Copyright (c) 2017, Wildan M (check [license](https://github.com/willnode/Qu3e-Sharp))<br/>
Network. Copyright (c) 2024, Toemsel (check [license](https://github.com/Toemsel/Network/blob/main/LICENSE))<br/>

netblox is licensed under MIT license. check LICENSE.txt file in repository root.
