# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2019-10-23
 * Added Serializer/Deserializer overloads for reading/writing from raw byte pointers
 * Added MessageHandler overloads for getting the INetworkReciever
 * Fixed typo in Reiliabiity enum.
 * Added Message Processors as a way to post-process messages before being sent out.
   Can be disabled by setting the static variable LobbyMember.MessageProcessor to null.
   Defaults to a LZFProcessor that applies LZF compression to the messages.
 * Added the ConnectionStats struct and endpoints to monitor the stats of each connection
   in lobbies.

## [1.0.0] - 2019-10-20
 * Initial Release
