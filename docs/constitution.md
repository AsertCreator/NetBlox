# Constitution of NetBlox

thats a clickbait actually i just realized. here i say what i actually want this to be. so, 
netblox is a clone of roblox, but written in c# because i feel like it, and because i know 
little about c/c++.

netblox is a modular system, extensible. there should be a master manager, and its "slaves"
managers. they are already implemented in `AppManager` and `GameManager`s respectively. the
slaves manage local things, like `Instance`s within them, networking and rendering (if granted
a permission to do so). all that, while settings and script management is the job of master.

> every instance has ID which is basically a GUID. used by runtime and networking

## Networking

networking is so weird, that i might switch to do using upper-case letters to distinguish
sentences. So, `NetworkManager` is not a static class which means, that its managed by
`GameManager` (a.k.a slave). Network system consists of a server and clients. Server manages
things that client should not have audacity to do, like replicating to other clients, or
filtering the chat message (or delivering them at all). Clients manage gameplay stuff, how
player gets to move a character (which is created by the server, along with `Player` instance).

### Protocol

The network library I'm using here is literally called "Network", but it kinda gets the job 
done?? Packets that are sent through the boundary have a tag attached to them, saying what
kind of packet it is. 

> Let's assume that you cannot disable `FilteringEnabled`, like in Roblox.

#### Handshake

Anyways, the handshake is initiated by the client, and client sends to server this 
information:

- the client's version
- the client's login token / username in offline mode
- the client's flags of which there are none currently.

> The login token is given by the <b>Public Service</b> which runs the server, so client must be
aware of it, but if the server is in offline mode, which means that its not assigned to any
Public Service, it accepts a username (the character has deafult appearance). 

> If server and client versions are different, the connection does not live any longer (what), 
server just disconnects the client and goes away.

> Here server disallows creating any new instances.

Finally, after client has sent the server info and after server has created all necessary 
instances and builds a list of instances to replicate, it returns this information:

- the server's version (should be identical to client's as said before),
- the amount of `Instance`s to replicate (after that number is almost finished the client can 
close loading screen and let player, well, play)
- the expected ID of `Player` instance (client must assign it to be a `LocalPlayer`)
- the expected ID of `Character`/`Model` instance (client must assign it, well, as the character)
- the expected ID of `DataModel` instance
- name of experience
- name of place
- author of place
- max player count
- current player count including the connecting player
- the client's unique id
- error code and message (if any error occurred besides the version mismatch, then server
must tell it there, like when server is full of people)

> The `DataModel` itself which is on server is NOT replicated to clients, but clients must
set their `DataModel`'s local ID to whatever server had sent.

> Each client has its own unique ID, which is basically just incremented 32-bit singed number.
<b>CLIENT'S ID IS NOT EQUAL TO PLAYER INSTANCE ID!</b>

Client does its initialization things, and then it sends a confirmation packet with virtually
no data, saying that everything's okay and server should start replicating stuff to the client. 
The initial replication starts.

> Instance creation on server is still disabled.

#### Replication

The initial replication, as well as any other type of replication sequence consists of replication
packets (please count how many times I said "replication"). A replication packet is a normal
packet and has three types:

- *new instance* (instance that has just been created/didn't exist on client)
- *property change* (a property/properties of an already existing instance has just changed) 
- *reparent* (a parent of an instance has just changed)

All three must reference an instance with its ID/GUID. First two carry serialized properties
and their names, while third one carries just new parent's ID.

> Let's assume that *new instance* packet is a bit delayed, so scripts could change its
properties quick enough, so all changes would be delivered in one packet, rather than five or 
six. Client must apply all properties of a *new instance* packet and not just leave new instance
in default state.

Why did I say so much? Anyway, after each replication packet has been sent by the server, it
waits for client to respond, and which it can send another replication packet, so there is no
race condition if there could potentially be one. After everything has been sent, client and
server just sit silently and do not do anything.

> Instance creation is enabled.

#### Network ownership

An instance can be owned either by the server or by a client, the ownership is given and revoked
by the server and confirmed with corresponding packet, <b>which is sent ONLY to the new
owner</b>, while previous owner gets a ownership removal packet. Both carry target instance IDs.

> By default all instances are given server ownership (client id -1). When a character is
created its given ownership of a respective client. `Player` instance is still owned by the
server.

If one's an owner of an instance, then it gets to modify its properties however its likes.
Client can send *property change* and ONLY *property change* packets to the server. When
server recieves one, it applies it to itself.

> No restriction by property yet...

#### Property change

When server detects a property change, it puts the instance to re-replication with exact
property specification. It replicates the instance to every single client. But if the change
was made by the network owner, server replicates the instance to everybody but the owner.

#### Replication queue

All replication packets are sent to this queue with recievers explicitly specified. When the time
comes, server takes one packet out of there and sends it to the clients.