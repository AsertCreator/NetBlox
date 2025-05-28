# The Netblox Project [![.github/workflows/dotnet-desktop.yml](https://github.com/AsertCreator/NetBlox/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/AsertCreator/NetBlox/actions/workflows/dotnet-desktop.yml)
i f̵̞̼͈́͐̉̀͘ų̵͙͉̩̳̝̜̈́͂͐c̶͇̀͌̚͝k̸͍̈̓̌̅̀ȉ̷̦̙̦̝͖̾̀n̷͓̠͆g̵͕͋͌ love roblox. thats why i decided to 
dedicate my two weeks school break on creating the clone of it on c#. it lowkey
looks like some "pixel gun 3d" type of game you would find on play store. and
isn't that "project" thing way too ambitiously sounding?

## Building
build server and client as a regular .NET projects by using `dotnet build`.

now run the server with following arguments (change them to your needs):
`-ss {"f":25570,"g":60,"b":"content/places/Welcoming.rbxlx","c":"Welcoming","d":"netblox","h":"The Lord"}`
, where `f` is server port, `g` is maximum player count, `b` is a relative path to the XML
formatted place file, `c` is place's display name, `d` is place's universe name (places and
universes/experiences are different but connected entities!) and `h` is place's author. don't
ask me why the keys are so cryptically named.

finally run the client with following arguments (change them to your needs):
`-cs {"a":"http://localhost:80/","b":"NetBlox Development","e":true,"g":"127.0.0.1"}`
, where `a` is Public Service base URL (i will elaborate on this later), `b` is window's
display title, `e` is whether you want to log in as a guest (you can't turn it off yet) and
`g` is server's IP and port.
> [!IMPORTANT]
> Because Windows and JSON's double quotes, your terminal may turn arguments into literals 
and ruin the whole thing. You need to escape the quotes, or substitute them with `^^`.

## What?
as i said earlier, the project is basically a game engine, aiming to be API-compatible
with roblox. if we're lucky, then maybe it's gonna be compatible enough to cross-play with
native roblox clients on native roblox servers. the project follows traditional structure
for multiplayer games, we have `UniversalServer` program and `UniversalClient`, which are
server and client of this game respectively and i believe everything else is straightforward.

now i really have nothing to say as to why this project even exists, but i believe i created
it as a no-hope thing that i didn't have any motivation to work on. over three weeks i added
things or two and abandoned the project. then spring of 2024 came and i found this project
on my computer and decided to give it a go, and now we're here.

just like roblox, it's expected to support physics, scripting, characters, multiplayer, nice 
rendering and its social network part. so far, little was achieved, but scripting probably
works at a level that i can call "normal".

## Licenses
The NetBlox project is licensed under MIT license, check LICENSE.txt file in the repository 
root.

### Software dependencies
NetBlox. Copyright (c) 2024-2025, The NetBlox Project's contributors. <br/>
Raylib. Copyright (c) 2013-2025, Ramon Santamaria (check [license](https://github.com/raysan5/raylib/blob/master/LICENSE))<br/>
Raylib-cs. Copyright (c) 2018-2025, ChrisDill (check [license](https://github.com/ChrisDill/Raylib-cs/blob/master/LICENSE))<br/>
MoonSharp. Copyright (c) 2014-2016, Marco Mastropaolo (check [license](https://github.com/moonsharp-devs/moonsharp/blob/master/LICENSE))<br/>
Network. Copyright (c) 2024, Toemsel (check [license](https://github.com/Toemsel/Network/blob/main/LICENSE))<br/>

### Sound effects

Explosion sound by JohanDeecke ([profile on FreeSound](https://freesound.org/people/JohanDeecke/))