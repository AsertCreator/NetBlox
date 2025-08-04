namespace NetBlox.Network
{
	public enum NetworkPacketTypeEnum
	{
		NPClientDisconnection, NPClientIntroduction, NPServerIntroduction, NPReplication, NPChat, 
		NPRemoteEvent, NPUpdatePlayerBufferZone, NPUpdatePlayerOwnership, NPPhysicsReplication,
		NPCharacterReset, NPCallbackOnInstanceArrival, NPSetPlayableCharacter
	}
}
