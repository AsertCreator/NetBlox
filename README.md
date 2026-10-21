# The NetBlox Project 2
i f̵̞̼͈́͐̉̀͘ų̵͙͉̩̳̝̜̈́͂͐c̶͇̀͌̚͝k̸͍̈̓̌̅̀ȉ̷̦̙̦̝͖̾̀n̷͓̠͆g̵͕͋͌ love roblox. thats why i decided to 
dedicate my even lesser free time i have at uni on creating a clone of it on c#. it's
generation 2 now and the engine is much much better structured and is just better
to work with and it's more optimized, however it currently has less features than gen1.
please don't laugh at me if the code is actually worse than i thought.

## Building
build server and client as a regular .NET projects by using `dotnet build`. then start the server
with `dotnet run --project NetBloxServer`. this will automatically create a non-headless server that
listens on port 44500 and opens crossroads. you can customize the server if you use this syntax:
`dotnet run --project NetBloxServer -- -Port 44500 -Graphics -PlaceUrl rbxasset://places/CrossroadsButBetter.rbxlx`.
replace everything you want to change with what you want to put. removing `-Graphics` makes a headless server.
then start the client with following syntax, adjusting them arguments to your liking as well:
`dotnet run --project NetBloxClient -- -Server 127.0.0.1 -Port 44500`. you can add `-Pause` to make the client wait
until a keypress before starting. you can do the same on Visual Studio and Visual Studio Code if you know how.

about the `-PlaceUrl` part. you can replace the path after the `rbxasset://` with a different relative path
inside the `content` directory of the server. or you can also replace it entirely with `net-network://http://localhost/places/yadayada.rbxlx`.
this will make the server fetch the place file off of that URL and open it. replace the HTTP URL
with what you want and you can then just open stuff. don't expect like anything to work though, even
crossroads is still buggy, incomplete due to most Instance classes being unimplemented and the game
has user scripts disabled temporarily.

### Running. Alternatively
if you press G key on the server window and then press "Copy Server Ticket" button you'll get a
ticket copied into your clipboard. then you start a client with this structure `-Ticket <paste your ticket here>`.
and your client automagically connects to the server. this will supposedly make it simpler and easier.
### Modes
- `Copy Server Ticket (public)` means it will create a ticket meant for public use. it will contain your
public IP address, you would have to open up your server port for it to work. may break with VPNs.
- `Copy Server Ticket (LAN-only)` means it will create a ticket meant for local-only use. it will
contain your private LAN IP address, it will only work on the same network. breaks with VPNs.
- `Copy Server Ticket (localhost-only)` means it will create a ticket meant for localhost use. only
works with clients running on the same machine as the server.

## What?
as i said earlier, the project is basically a game engine, aiming to be API-compatible
with roblox. if i get a lot of free time then maybe it's gonna be compatible enough to cross-play with
native roblox clients on native roblox servers, although probably not with the modern ones. the project 
follows a regular structure of multiplayer games, we have
`NetBloxServer` program and `NetBloxClient`, which are the server and client of this game
respectively and i believe everything else is straightforward.

now i really have nothing to say as to why this project even exists, but i believe i made
it spontaneously and i didn't have any motivation to work with it after some time fiddling around. over 
three weeks i added a thing or two and abandoned the project. then spring of 2024 came and i found this project
on my computer and decided to give it a go, and now we're here. oh and then 2025 came, i got carried away by 
the finals, i had them finished and forgot like everything about this codebase, it was like i was looking 
at somebody else's code. that was partially a reason why i chose to rewrite this whole thing from scratch 
instead of hopelessly trying to fix the original.

just like roblox, it's supposed to support physics, scripting, characters, multiplayer, nice 
rendering and the social network part. so far, little was achieved, but scripting
works at a level that i could probably call "normal". also in generation 2 physics works ok i guess,
game is much better looking visually, the characters and multiplayer are still finicky (as in
you can't even spawn as a character yet). the social network part has not been started yet.

## Generation 2?

yes, generation 2. it's currently kind of uncapable of anything fun, but it's much more optimized, less buggy, 
and i like working with it more than with the first one.

## Licenses
The NetBlox Project is licensed under MIT license, check LICENSE file in the repository 
root.

### Software dependencies
- NetBlox. Copyright (c) 2024-2026, The NetBlox Project's contributors. 
- Raylib. Copyright (c) 2013-2026, Ramon Santamaria (check [license](https://github.com/raysan5/raylib/blob/master/LICENSE))
- Raylib-cs. Copyright (c) 2018-2026, ChrisDill (check [license](https://github.com/ChrisDill/Raylib-cs/blob/master/LICENSE))
- MoonSharp. Copyright (c) 2014-2016, Marco Mastropaolo (check [license](https://github.com/moonsharp-devs/moonsharp/blob/master/LICENSE))
- JitterPhysics2. Copyright (c) 2023-2026, Thorben Linneweber and contributors (check [license](https://github.com/notgiven688/jitterphysics2/blob/master/LICENSE))
- LRU Cache. Copyright (c) 2023, Atul Mishra (https://medium.com/@atulmishra.bhumca09/basic-lru-cache-implementation-in-c-17d4a5f0d4ba)

For open-source licenses of software that Generation 1 depends on, check branches that start with "gen1/"

### Sound effects

Explosion sound by JohanDeecke ([profile on FreeSound](https://freesound.org/people/JohanDeecke/))
