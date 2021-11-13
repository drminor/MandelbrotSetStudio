using MqMessages;
using qdDotNet;
using System;
using PointInt = MqMessages.PointInt;

namespace FGenConsole
{
	internal class SubJob
	{
		public SubJob(Job parentJob, KPoint position)
		{
			ParentJob = parentJob ?? throw new ArgumentNullException(nameof(parentJob));
			Position = position;
			SubJobResult = null;
			OperationType = SubJobOperationType.Unknown;
		}

		public readonly Job ParentJob;
		public readonly KPoint Position;
		public SubJobResult SubJobResult { get; set; }
		public SubJobOperationType OperationType { get; set; }

		public FJobResult GetResultFromSubJob(bool isFinalResult)
		{
			PointInt resultPos = new PointInt(Position.X * FGenerator.BLOCK_WIDTH, Position.Y * FGenerator.BLOCK_HEIGHT);

			MqMessages.SizeInt resultSize = new MqMessages.SizeInt(FGenerator.BLOCK_WIDTH, FGenerator.BLOCK_HEIGHT);
			MqMessages.RectangleInt area = new MqMessages.RectangleInt(resultPos, resultSize);

			FJobResult fJobResult = new FJobResult(ParentJob.JobId, area, SubJobResult.Counts, isFinalResult);

			return fJobResult;
		}
	}
}
