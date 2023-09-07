
namespace MSS.Types
{
	public enum TransformType
	{								// MapSections Updated?			Coordinates Updated		Zoom Updated	
		Home = 0,					// All new						Yes						Yes
		CalcSettingsUpdate = 1,		
		IterationUpdate = 2,		// Depends						No						No
		ColorMapUpdate = 3,			// Same							No						No
		ZoomIn = 4,					// All new						Yes						Yes
		Pan = 5,					// Some new or All new			Yes						No
		ZoomOut = 6,				// All new						Yes						Yes
		CanvasSizeUpdate_Depreciated = 7		// Depends						No						Depends
	}


	/*

	When moving from node N to node N + 1, the TransformType indicates what was updated

	Need to update the TransformType to use Flags

	Node N + 1, can differ from Node N in the following ways

	ZoomIn, ZoomOut or Pan
		with or without an updated ColorMap
		with or without an iteration update

	ColorMap update
		with or without a ZoomIn, ZoomOut or Pan
		with or without an iteration update


	Iteration update
		with or without a ZoomIn, ZoomOut or Pan
		with or without an updated ColorMap

	So each transistion will have
	One of the folowing Coordinate Change Types: None, ZoomIn, Pan ZoomOut.
	and will have an updated ColorMap or not
	and will have one of the following Iteration Change Types: Increase, Decrease or None.

	So we could have use a Flags-Type Enum with
	Home = 0
	ColorMapUpdate = 1
	ZoomIn = 2
	Pan = 4
	ZoomOut = 8
	IterationIncrease = 16
	IterationDecrease = 32

	Allowed Values are
	0	Home				(aka No Coordinate Change, No Iteration Update)
	1	Color				(aka No Coordinate Change, No Iteration Update /w Color
	2	ZoomIn
	3	ZoomIn + Color
	4	Pan
	5	Pan + Color
	8	ZoomOut
	9	ZoomOut + Color

	All Iteration Updates are also Color Updates

	With Iteration Increase
	17	II + Color				
	19	II + ZoomIn + Color
	21	II + Pan + Color
	25	II + ZoomOut + Color

	With Iteration Decrease
	33	ID + Color
	35	ID + ZoomIn + Color
	37	ID + Pan + Color
	41	ID + ZoomOut + Color

	*/

}
