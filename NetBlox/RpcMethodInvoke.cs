namespace NetBlox
{
	public enum RpcMethodType
	{
		Kick, InformClientConnect, InformClientDisconnect
	}
    public struct RpcMethodInvoke
    {
		public RpcMethodType MethodType;
		public byte[] MethodArguments;

		public string MethodName => MethodType.ToString();
    }
}
