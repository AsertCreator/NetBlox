# The NetBlox Project 2
i f̵̞̼͈́͐̉̀͘ų̵͙͉̩̳̝̜̈́͂͐c̶͇̀͌̚͝k̸͍̈̓̌̅̀ȉ̷̦̙̦̝͖̾̀n̷͓̠͆g̵͕͋͌ love roblox. thats why i decided to 
dedicate my even lesser free time i have at uni on creating the clone of it on c#. it's
generation 2 now and the engine is much much better structured and is just better
to work with and it more optimized, however it currently has less features than gen1.
please don't laugh at me if the code is actually worse than i thought.

## Building
build server and client as a regular .NET projects by using `dotnet build`. then start the
server with `dotnet run` with following arguments, adjusting them accordingly: `-Port 44500`.
then start the client with following arguments, adjusting them accrodingly as well:
`-Server 127.0.0.1 -Port 44500`. you can add `-Pause` to make the client wait until a keypress
before starting. you can do the same on Visual Studio and Visual Studio Code if you know how.

## What?
as i said earlier, the project is basically a game engine, aiming to be API-compatible
with roblox. if i get a lot of free time then maybe it's gonna be compatible enough to cross-play with
native roblox clients on native roblox servers, although probably not with the modern servers
and clients. the project follows a regular structure of multiplayer games, we have
`NetBloxServer` program and `NetBloxClient`, which are the server and client of this game
respectively and i believe everything else is straightforward.

now i really have nothing to say as to why this project even exists, but i believe i created
it as a no-hope thing that i didn't have any motivation to work on. over three weeks i added
things or two and abandoned the project. then spring of 2024 came and i found this project
on my computer and decided to give it a go, and now we're here.

just like roblox, it's supposed to support physics, scripting, characters, multiplayer, nice 
rendering and its social network part. so far, little was achieved, but scripting probably
works at a level that i can call "normal". also in generation 2 physics works ok i guess,
game is much better looking visually, the characters and multiplayer are still finicky (as in
you can't even spawn as a character yet). the social network part has not started yet.

## Generation 2?

yes, generation 2. it's currently kind of uncapable of anything fun, but it's much more optimized
and i like working with it more than the first one.

## Licenses
The NetBlox Project is licensed under MIT license, check LICENSE file in the repository 
root.

### Software dependencies
- NetBlox. Copyright (c) 2024-2026, The NetBlox Project's contributors. 
- Raylib. Copyright (c) 2013-2026, Ramon Santamaria (check [license](https://github.com/raysan5/raylib/blob/master/LICENSE))
- Raylib-cs. Copyright (c) 2018-2026, ChrisDill (check [license](https://github.com/ChrisDill/Raylib-cs/blob/master/LICENSE))
- MoonSharp. Copyright (c) 2014-2016, Marco Mastropaolo (check [license](https://github.com/moonsharp-devs/moonsharp/blob/master/LICENSE))
- JitterPhysics2. Copyright (c) 2023-2026, Thorben Linneweber and contributors (check [license](https://github.com/notgiven688/jitterphysics2/blob/master/LICENSE))

For open-source licenses of software that Generation 1 depends on, check branches that start with "gen1/"

### Sound effects

Explosion sound by JohanDeecke ([profile on FreeSound](https://freesound.org/people/JohanDeecke/))