﻿using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Job
	{
		public ObjectId Id { get; init; }
		public Job? ParentJob { get; set; }
		public Project Project { get; set; }
		public Subdivision Subdivision { get; init; }
		public string? Label { get; init; }

		public TransformType TransformType { get; init; }
		public RectangleInt NewArea { get; init; }

		public MSetInfo MSetInfo { get; init; }
		public SizeInt CanvasSizeInBlocks { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public Job(Job? parentJob, Project project, Subdivision subdivision, string? label, TransformType transformType, RectangleInt newArea, MSetInfo mSetInfo, 
			SizeInt canvasSizeInBlocks, BigVector mapBlockOffset, VectorInt canvasControlOffset)
			: this(ObjectId.GenerateNewId(), parentJob, project, subdivision, label, transformType, newArea, mSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset)
		{ }


		public Job(
			ObjectId id,
			Job? parentJob,
			Project project,
			Subdivision subdivision,
			string? label,

			TransformType transformType,
			RectangleInt newArea,

			MSetInfo mSetInfo,
			SizeInt canvasSizeInBlocks,
			BigVector mapBlockOffset,
			VectorInt canvasControlOffset
			)
		{
			Id = id;
			ParentJob = parentJob;
			Project = project ?? throw new ArgumentNullException(nameof(project));
			Subdivision = subdivision;
			Label = label;

			TransformType = transformType;
			NewArea = newArea;

			MSetInfo = mSetInfo;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
