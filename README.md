# Hourai Networking

Platform based P2P networking with multiple backends.

---

## Features

 * Lobby based peer-to-peer transport, with pluggable backends. See below for
   more details.
 * Abstracts platform level constructs like matchmaking and relay networks into
   generic interfaces.
 * High performance message serialization with next to zero garbage collection
   pressure.
 * Premade network topologies for faster iteration: Host-Client, Full Mesh

## Supported Backends

Hourai Networking acts as a wrapper around these backends to provide support.
Some features may not be uniform across all platforms.

|Feature|Steam|Discord|Direct UDP|Epic Games|
|:------|:---:|:-----:|:--------:|:--------:|
|Implemenation|✔️|✔️|Planned|Planned|
|Lobbies|✔️|✔️|❌|?|
|Matchmaking|✔️|✔️|❌|?|
|Custom Metadata|✔️|✔️|?|?|
|Host Migration|✔️|✔️|?|?|

---

## Installation
Backroll is most easily installable via Unity Package Manager. In Unity 2018.3+,
add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.houraiteahouse.networking": "0.1.1"
  },
  "scopedRegistries": [
    {
      "name": "Hourai Teahouse",
      "url": "https://upm.houraiteahouse.net",
      "scopes": ["com.houraiteahouse"]
    }
  ]
}
```

## FAQ:

**Q: Why peer-to-peer? Almost everyone uses client-server nowadays!**

There are notable issues with peer-to-peer networking:

 * Inconsistent connections - need to use NAT traversals, low connection
   quality, etc.
 * Lack of authority - cheaters can easily attack P2P game systems with
   alterred clients or malicous abuse of the protocols used.
 * Difficulty to game scale - realtime games with a signifigant number of
   players (think MMOs, battle royales) have difficulty scaling up to that
   size.

These are notable issues commonly seen, which is why the majority of the game
industry nowadays aims for an authoritative client-server model of netplay.
However, the main argument against this model, espeically for low-budget games
that want netplay, is cost. Running a fleet of servers 24/7, and paying
operators to maintain those servers is simply impossible for most small
developers.

Peer-to-peer netwroking only requires multiple players to share the
same game version and an internet connection. No dedicated servers are required
on the developer's end.

**Q: Why use proprietary networks like Steam and Discord?**

One of the aforementioned issues with peer-to-peer networking is the
inconsistent connection issues. Modern consumer home networks are almost always
behind NATs (network address translation), which makes normal socket based
connections almost impossible without a whole host of hacks and workarounds
(i.e. UPnP, libjingle, STUN, etc.), none of which are perfect. The only true way
to ensure connections complete is to have a relay server network, with well
known public IPs. This returns to the issues of servers and cost. Luckily,
Steam and Discord, now some of the largest companies in PC gaming, offer free
access their relay networks to developers willing to integrate with their
systems: allowing for more consistent netplay experience in exchange for some
vendor lock-in. One of the goals of HouraiNetworking is to help allievate that
vendor lock-in by abstracting platform level differences away.

One other benefit to these solutions is use of relay services often incorproates
IP masking and encryption to help maintian player privacy.
