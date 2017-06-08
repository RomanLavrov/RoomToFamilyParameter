
#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Documents;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

#endregion

namespace RoomToFamily
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            int counter = 0;
            foreach (Room item in GetRooms(app))
            {
                ApplyParameterToDevice(item, doc);
                counter++;
            }
            TaskDialog.Show("Finished", "Modification provided in " + counter +"rooms.");
            return Result.Succeeded;
        }

        public List<Room> GetRooms(Application app)
        {
            List<Room> roomList = new List<Room>();
            
            foreach (Document d in app.Documents)
            {
                if (d.IsLinked)
                {
                    FilteredElementCollector roomLinkedCollector = new FilteredElementCollector(d).OfCategory(BuiltInCategory.OST_Rooms);

                    string tempLinkedRoom = string.Empty;
                    foreach (Room e in roomLinkedCollector)
                    {
                        roomList.Add(e);
                        tempLinkedRoom += e.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString() + " " + e.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() + "\n";
                    }
                }
            }
            return roomList;
        }
       
        private List<FamilyInstance> GetCategoryDevices(Document doc, Room room, BuiltInCategory category )
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> instances = new List<FamilyInstance>();
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                .OfCategory(category)
                .WherePasses(filter);

            foreach (FamilyInstance e in roomCollector)
            {
                if (FilterPosition(room, e))
                {
                    instances.Add(e);
                }
            }
           
            return instances;
        }

        private void ApplyParameterToDevice(Room room, Document doc)
        {
            List<FamilyInstance> devicesList = new List<FamilyInstance>();
            BoundingBoxXYZ box = room.get_BoundingBox(null);
            if (box != null)
            {
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_ElectricalEquipment));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_ElectricalFixtures));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_LightingDevices));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_LightingFixtures));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_FireAlarmDevices));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_CommunicationDevices));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_TelephoneDevices));
                
                {                       
                    using (Transaction trans = new Transaction(doc, "Paramaters Adding"))
                    {
                        trans.Start();                        
                        foreach (FamilyInstance instance in devicesList)
                        {
                            
                            Parameter param1 = instance.LookupParameter("RaumNummer");
                            Parameter param2 = instance.LookupParameter("RaumName");
                            if (param1.HasValue)
                            {
                                param1.Set("empty data");
                            }

                            if (param2.HasValue)
                            {
                                param2.Set("empty data");
                            }
                            param1.Set(room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString());
                            param2.Set(room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
                        }                           
                        trans.Commit();
                    }
                }
            }
        }

        private PointF GetLocation(FamilyInstance instance)
        {
            PointF location = new PointF();
            Location locationInstance = instance.Location;
            LocationPoint point = locationInstance as LocationPoint; 
            location.X = (float) (point.Point.X * 25.4 * 12);
            location.Y = (float) (point.Point.Y * 25.4 * 12);

            return location;
        }

        private BoundingBoxIntersectsFilter Filter(Room room)
        {
            BoundingBoxXYZ box = room.get_BoundingBox(null);
            if (box != null)
            {
                Outline outline = new Outline(box.Min, box.Max);
                BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
                return filter;
            }
            return null;
        }
        

        public bool FilterPosition(Room room, FamilyInstance instance)
        {
            PointF location = GetLocation(instance);
            List<System.Windows.Shapes.Line> revitWalls = GetWalls(room);
            int revitOutterCheckpoint = 100000000;
           
                int counter = 0;
                System.Windows.Shapes.Line check = DrawCheckline(location, revitOutterCheckpoint, revitOutterCheckpoint);
                foreach (var wall in revitWalls)
                {
                    if (CheckIntersection(wall, check))
                        counter++;
                }
                if (counter % 2 != 0)
                    return true;
                else return false;
        }

        private List<System.Windows.Shapes.Line> GetWalls(Room room)
        {
            SpatialElementBoundaryOptions boundaryOption = new SpatialElementBoundaryOptions();
            boundaryOption.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;

            IList<IList<BoundarySegment>> boundary = room.GetBoundarySegments(boundaryOption);
            List<System.Windows.Shapes.Line> wallCoord = new List<System.Windows.Shapes.Line>();

            foreach (IList<BoundarySegment> walls in boundary)
            {
                foreach (BoundarySegment segment in walls)
                {
                    System.Windows.Shapes.Line wall = new System.Windows.Shapes.Line();

                    var segmentStart = segment.GetCurve().GetEndPoint(0);

                    wall.X1 = (segmentStart.X * 25.4 * 12);
                    wall.Y1 = (segmentStart.Y * 25.4 * 12);

                    var segmentEnd = segment.GetCurve().GetEndPoint(1);

                    wall.X2 = (segmentEnd.X * 25.4 * 12);
                    wall.Y2 = (segmentEnd.Y * 25.4 * 12);

                    wallCoord.Add(wall);
                }
            }
            return wallCoord;
        }

        private System.Windows.Shapes.Line DrawCheckline(PointF point, int outterX, int outterY)
        {
            System.Windows.Shapes.Line checkLine = new System.Windows.Shapes.Line();
            checkLine.X1 = outterX;
            checkLine.Y1 = outterY;
            checkLine.X2 = point.X;
            checkLine.Y2 = point.Y;
            return checkLine;
        }

        private bool CheckIntersection(System.Windows.Shapes.Line first, System.Windows.Shapes.Line second)
        {
            PointF intersection = GetIntersectionD(first, second);

            if ((float.IsInfinity(intersection.X)) || (float.IsInfinity(intersection.Y)))
            {
                return false;
            }

            bool belongFirst = CheckIfPointBelongToLine(first, intersection);
            bool belongSecond = CheckIfPointBelongToLine(second, intersection);

            if (belongFirst && belongSecond)
            {
                return true;
            }
            return false;
        }

        bool CheckIfPointBelongToLine(System.Windows.Shapes.Line line, PointF point)
        {
            System.Windows.Shapes.Line check1 = new System.Windows.Shapes.Line();
            check1.X1 = line.X1;
            check1.Y1 = line.Y1;
            check1.X2 = point.X;
            check1.Y2 = point.Y;

            System.Windows.Shapes.Line check2 = new System.Windows.Shapes.Line();
            check2.X1 = line.X2;
            check2.Y1 = line.Y2;
            check2.X2 = point.X;
            check2.Y2 = point.Y;
            double summ = GetLength(check1) + GetLength(check2);
            double length = GetLength(line);

            double tolerance = Math.Abs(length * .00001);
            if (Math.Abs(length - summ) < tolerance)
            {
                return true;
            }
            return false;
        }

        public PointF GetIntersectionD(System.Windows.Shapes.Line box, System.Windows.Shapes.Line wall)
        {
            List<double> wallCoefs = LineEquation(wall);
            double a1 = wallCoefs[0];
            double b1 = wallCoefs[1];
            double c1 = wallCoefs[2];

            List<double> boxCoefs = LineEquation(box);
            double a2 = boxCoefs[0];
            double b2 = boxCoefs[1];
            double c2 = boxCoefs[2];

            PointF intersection = new PointF();
            {
                double x = (c1 * b2 - c2 * b1) / (a2 * b1 - a1 * b2);
                double y = 0;
                if (b1.Equals(0))
                {
                    y = (int)(-c2 - a2 * x) / b2;
                }
                else if (b2.Equals(0))
                    y = (-c1 - a1 * x) / b1;

                else
                    y = (-c2 - (a2 * x)) / b2;

                if (x.Equals(Double.NaN))
                {
                    x = float.PositiveInfinity;
                }
                if (y.Equals(Double.NaN))
                {
                    y = float.PositiveInfinity;
                }
                intersection.X = (float)x;
                intersection.Y = (float)y;
            }
            return intersection;
        }

        public List<double> LineEquation(System.Windows.Shapes.Line line)
        {
            List<double> result = new List<double>();
            double a = line.Y2 - line.Y1;
            double b = line.X1 - line.X2;
            double c = line.Y1 * (line.X1 - line.X2) - line.X1 * (line.Y1 - line.Y2);
            result.Add(a);
            result.Add(b);
            result.Add(-c);
            return result;
        }

        private double GetLength(System.Windows.Shapes.Line line)
        {
            return Math.Sqrt(Math.Pow((line.X1 - line.X2), 2) + Math.Pow((line.Y1 - line.Y2), 2));
        }
    }
}
