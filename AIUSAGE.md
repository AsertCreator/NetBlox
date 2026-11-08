# AI usage in The NetBlox Project
The core and foundation of NetBlox is entirely human-written (original author [AsertCreator](https://github.com/asertcreator)), some parts of the default rendering pipeline and physics-related subsystems were written with AI assitance, specifically GitHub Copilot. The Lua side of the NetBlox source code was written with no AI assitance. Assets, such as images, skyboxes, audio and built-in places were created with no AI assitance, original authors are credited in the README.md as far as I could find.

## AI usage breakdown by each subsystem
| Subsystem                       | Was AI used? | Entirely? | ..how?                                       |
|---------------------------------|--------------|-----------|----------------------------------------------|
| Game manager                    | No           | No        |                                              |
| Task scheduler                  | No           | No        |                                              |
| Game logger                     | No           | No        |                                              |
| Game event routing              | No           | No        |                                              |
| Game asset loader               | No           | No        |                                              |
| Game programs (server/client)   | No           | No        |                                              |
| Game unit tests                 | No           | No        |                                              |
| Instance class + hierarchy      | No           | No        |                                              |
| Most Instance subclasses        | No           | No        |                                              |
| Primitive rendering utilities   | No*          | No        | Unless you count shadows as a part of primitive rendering, no |
| Place loader                    | No           | No        |                                              |
| Security (security identities)  | No           | No        |                                              |
| Security (script sandboxing)    | No           | No        |                                              |
| Security (AccessFlags)          | No           | No        |                                              |
| Scripting (ScriptContext)       | No           | No        |                                              |
| Scripting (marshalling)         | No           | No        |                                              |
| Scripting (data types)          | No           | No        |                                              |
| Scripting (Instance cache)      | No           | No        |                                              |
| Scripting (CoreScript)          | No           | No        |                                              |
| Networking (CompoundConnection) | No           | No        |                                              |
| Networking (server)             | No           | No        |                                              |
| Networking (client)             | No           | No        |                                              |
| Networking (replication)        | No           | No        |                                              |
| Rendering (default pipeline)    | Yes          | No        | AI was used to figure out shadowmaps, find slow spots in the code, write decal support, help with hooking up textures, normal maps. |
| Rendering (debug & auxiliary)   | No*          | No        | Some auxiliary viewports derive functionality from the default pipeline, which is written with AI assistance |
| Rendering (shaders)             | Yes          | No        | Shaders related to shadowmapping were written with AI assistance |
| Rendering (frustum culling)     | Yes          | No        | AI was used to figure out frustum calculation |
| Rendering (particles)           | No           | No        |                                              |
| Rendering (font loading)        | No           | No        |                                              |
| Rendering (GUI)                 | No           | No        |                                              |
| Physics                         | Yes          | No        | AI was used to figure out Model collision when using Humanoids |
| Physics (spatial grid)          | No           | No        |                                              |
| Physics (state synchronization) | No           | No        |                                              |
| Workspace joint maker           | Yes          | No        | AI was used to figure out all possible "surfaces of different types touching" situations |
| Resources themselves            | No           | No        |                                              |
| This Markdown file              | No           | No        | Of course not. I wouldn't write an AI usage disclosure with AI, am I dumb? |

If I missed something or lied accidentally, let me know through an Issue.

## So..

### Is this thing vibecoded?
I wouldn't call it vibecoded. Vibecoding is when you mostly or fully delegate all code writing and maintaining to an AI. I don't find that
entertaining and cheap. Of course some parts of this game engine are AI-written/assisted, but even then a human was checking and reviewing
generated code for whether it works or not. The foundation for this game was made completely manually.

I'm also not responsible for AI usage in this projects' dependencies, like Raylib, Raylib_cs, Jitter2, MoonSharp, and others. I mean if they
suddenly turn completely vibecoded I'd probably go find a replacement for them, but they won't, I know that.

### Can I avoid running AI-generated code?
Possibly, if you disable rendering on the server, use only modern Roblox places and uhh don't have physics?

### Can I submit entirely vibecoded Pull Requests?
Please review the generated code thoroughly, more thoroughly than I do (trust me that's easy). If the code is actually like working and isn't
too horrible to look at (like most of human written code here), then submit it I guess.

### Can I submit lesser vibecoded Pull Requests?
Ditto. Just in general please ensure your code works okay and doesn't bring in total foundational changes.

### What about Generation 1?
We don't talk about Generation 1.