namespace NetBlox.Network
{
	public enum NetworkPacketTypeEnum
	{
		NPClientDisconnection, NPClientIntroduction, NPServerIntroduction, NPStartReplication, 
		NPReplication, NPChat, NPRemoteEvent, NPUpdatePlayerBufferZone, NPUpdatePlayerOwnership, 
		NPPhysicsReplication, NPCharacterReset, NPCallbackOnInstanceArrival, NPSetPlayableCharacter,
		NPWaitForSubjectAndSetCamera
	}
}
