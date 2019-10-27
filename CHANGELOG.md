# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.2] - 2019-10-xx
 * Renamed LobbyBase -> Lobby
 * Fix: Added member metadata function implementations for both Discord and Steam
 * Added more events to Lobby and LobbyMember.
 * Added a high performance CRC32 implemenation for ease of use.
 * Added shortcut IntegrationManager to simplify setup.

## [0.1.1] - 2019-10-23
 * Added Serializer/Deserializer overloads for reading/writing from raw byte pointers
 * Added MessageHandler overloads for getting the INetworkReciever
 * Fixed typo in Reiliabity enum.
 * Added Message Processors as a way to post-process messages before being sent out.
   Can be disabled by setting the static variable LobbyMember.MessageProcessor to null.
   Defaults to a LZFProcessor that applies LZF compression to the messages.
 * Added the ConnectionStats struct and endpoints to monitor the stats of each connection
   in lobbies.

## [0.1.0] - 2019-10-20
 * Initial Release
