using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer.MapDisplay.ExpermentalOrUnused
{
    internal interface IScreenSectionCollection
    {
        SizeInt CanvasSizeInBlocks { get; set; }

        VectorInt SectionIndex { get; } // Just for diagnostics.

        void Draw(PointInt position, byte[] pixels, bool offline);
        void Redraw(PointInt position);
        void Finish();

        bool Hide(MapSection mapSection);

        void HideScreenSections(bool rebuild = false);
        void Shift(VectorInt amount);

        int CurrentDrawingGroupCnt { get; }
    }
}