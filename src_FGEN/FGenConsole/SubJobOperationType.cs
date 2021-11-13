namespace FGenConsole
{
	internal enum SubJobOperationType
	{
		Unknown,
		Fetch,
		Build,
		IncreaseIterations,
		DecreaseIterations,
		None // Parent Job is closed
	}
}
