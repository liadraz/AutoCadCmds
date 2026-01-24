using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Runtime;

namespace AutoCadCmds
{
    public class BasicCmndExapmles
    {
        [CommandMethod("HelloWorld")]
        public void HelloWorld()
        {
            // Get the current document and editor
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("Hello, AutoCAD World!");
        }

        [CommandMethod("SelectOne")]
        public void SelectOne()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an entity: ");
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                ed.WriteMessage($"\nYou selected an entity of type: {per.ObjectId.ObjectClass.Name}");
            }
            else
            {
                ed.WriteMessage("\nNo entity selected.");
            }
        }

        [CommandMethod("SelectMany")]
        public void SelectMany()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSelect Entities: ";

            PromptSelectionResult psr = ed.GetSelection(pso);

            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet ss = psr.Value;
                ed.WriteMessage($"\nYou selected {ss.Count} entities.");
            }
        }

        // Single Selection uses PromptEntityOptions with ed.GetEntity which returns PromptEntityResult containing one ObjectId
        // Multiple Selection uses PromptSelectionOptions with ed.GetSelection which returns PromptSelectionResult containing a SelectionSet of ObjectIds

        [CommandMethod("CreateLine")]
        public void CreateLine()
        {
            // Get the current database
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read. 
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write. 
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create a line that starts at (0,0,0) and ends at (100,100,0)
                Point3d startPt = new Point3d(0, 0, 0);
                Point3d endPt = new Point3d(100, 100, 0);
                Line line = new Line(startPt, endPt);

                // Add the new line to the block table record and the transaction
                btr.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);

                tr.Commit();
            }
        }

        //       BlockTable                     Each dwg has only one BlockTable. BT is the catalog-registry consits only ObjectId references
        //├── BlockTableRecord(ModelSpace)      BTR represents one block consists entities in that block
        //│    ├── Line
        //│    ├── Polyline
        //│    └── Hatch
        //├── BlockTableRecord(PaperSpace)
        //└── BlockTableRecord("MyBlock")       Block that was made using the Block command and used as a template 
        //     ├── Circle
        //     └── Line
        //                                      When inserting a block, a BlockReference is created in the current space (Model or Paper) that references the BlockTableRecord.

        // Database vs Document
        // Document represents the DWG file opened in AutoCAD application including the UI grpahic layouts. Each Document has its own Database.
        //      To interact with the user use Document and Editor.
        // Database represents the actual data structure of the DWG file (the graphics itself) including all entities, layers, styles, etc.
        //      To create objects use Database.

        // Transaction represents a unit of work that allows you to group multiple operations into a single atomic action.
        //     Transactions are essential when working with the AutoCAD database to ensure that changes are made safely and consistently.
        //      When changes are made within a transaction, they can be committed (saved) or aborted (discarded) as a single unit.

        [CommandMethod("CretaeCircle")]
        public void CretaeCircle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Prompt the user for the radius of the circle.
            //      These are the tools used to "talk" to the user and receive a decimal number (double) via the command line.
            //      The PromptDoubleOptions object acts as a filter between the user's keyboard and your code.
            //      When you set AllowNegative = false, the validation happens inside AutoCAD's internal engine
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEnter radius of the circle: ");
            pdo.AllowNegative = false;
            pdo.AllowZero = false;

            // Is a blocking method that waits for the user to input a value and press Enter.
            //      AutoCAD Engine does the validation based on the options set in pdo.
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nInvalid radius.");
                return;
            }

            double radius = pdr.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Point3d center = new Point3d(0, 0, 0);
                //      Vector3d.ZAxis - When creating a circle, AutoCAD needs to know its "Normal" (which way it faces). Usually, this is the Z-axis (0,0,1)
                Circle circle = new Circle(center, Vector3d.ZAxis, radius);

                btr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                tr.Commit();
            }
        }

        [CommandMethod("MoveObject")]
        public void MoveObject()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // select entity and validate selection
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an entity to move: ");
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //  open the selected entity for write
                Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Entity;

                //  define the move vector
                Vector3d moveVec3d = new Vector3d(50.0, 50.0, 0);

                //  create the transformation matrix
                Matrix3d moveMatrix = Matrix3d.Displacement(moveVec3d);

                ent.TransformBy(moveMatrix);

                tr.Commit();
            }
        }

        // Matrix3d is the mathematical way AutoCAD handles moving, rotating, and scaling.
        //      TransformBy(Matrix3d) is better
        //      It works on any object, It is much faster for the database to process, It handles complex math (like rotating and moving at the same time) automatically.

        // Distinction between Creating and Modifying Objects
        //      In Creation: You need the BlockTableRecord because you are adding a new member to the family.
        //      You need to tell the "container" (Model Space) to accept a new child.
        //      In Modification: The object is already "inside" the Model Space.
        //      You already have its "address" (ObjectId).
        //      Since you aren't adding or removing anything from the Model Space you only need to open the object itself.

        [CommandMethod("CloneAndRed")]
        public void CloneAndRed()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an object to clone: ");
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity originalEnt = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;

                // Clone the entity
                // Note: Cloning creates a copy of the entity in memory. To add it to the drawing, you need to append it to a BlockTableRecord.
                Entity cloneEnt = originalEnt.Clone() as Entity;

                // Set color to red (1 is red in AutoCAD color index)
                cloneEnt.ColorIndex = 1;

                Matrix3d displacement = Matrix3d.Displacement(new Vector3d(20.0, 0, 0));
                cloneEnt.TransformBy(displacement);

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                btr.AppendEntity(cloneEnt);
                tr.AddNewlyCreatedDBObject(cloneEnt, true);

                tr.Commit();
            }

            ed.WriteMessage("\nCloned entity created in red color.");
        }

        [CommandMethod("SelectOnlyLines")]
        public void SelectOnlyLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Create a TypedValue array to define the filter rules
            TypedValue[] filterValues = new TypedValue[1];
            filterValues[0] = new TypedValue((int)DxfCode.Start, "Line");

            SelectionFilter filter = new SelectionFilter(filterValues);

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSelect objects (Only lines will be picked): ";

            PromptSelectionResult psr = ed.GetSelection(pso, filter);

            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet ss = psr.Value;
                ed.WriteMessage($"\nYou selected {ss.Count} line entities.");

                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject selObj in ss)
                    {
                        // We are sure that only lines are selected due to the filter
                        Line line = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Line;
                        line.ColorIndex = 3; // Change color to green
                    }

                    tr.Commit();
                }
            }
        }

        // TypedValue is a structure that represents a single data item consisting of a type code and a value. Based on DXF codes.
        // Each object in Autocad save in DXF Drawing Exchange Format. Every information has a numeric code
        //      DxfCode.Start (code 0) represents the entity type (e.g., Line, Circle, Polyline)
        //      DxfCode.LayerName (code 8) represents the layer name
        // SelectionFilter is the filter that passed to the method GetSelection to limit the selection to specific criteria.
        // SelectionsSet is a collection of the selected ObjectsIds returned by the selection methods
        //      To open and work with the actual entities, you need to use a Transaction getObject method.

        [CommandMethod("OrderedOffset")]
        public void OrderedOffset()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSelect two nested closed polylines: ";

            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if (psr.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline p1 = tr.GetObject(psr.Value[0].ObjectId, OpenMode.ForWrite) as Polyline;
                Polyline p2 = tr.GetObject(psr.Value[1].ObjectId, OpenMode.ForWrite) as Polyline;

                Polyline outer = (p1.Area > p2.Area) ? p1 : p2;
                Polyline inner = (p1.Area > p2.Area) ? p2 : p1;

                outer.ColorIndex = 1; // Red - Index 1
                inner.ColorIndex = 3; // Green - Index 3

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                ObjectId outerOffsetId = CreateInternalOffset(outer, 2, btr, tr, 2);
                ObjectId innerOffsetId = CreateInternalOffset(inner, 2, btr, tr, 4);

                if (outerOffsetId != ObjectId.Null)
                {
                    Polyline outerOffset = tr.GetObject(outerOffsetId, OpenMode.ForRead) as Polyline;
                    CreateHatchBetween(outer, outerOffset, "SOLID", btr, tr);
                }

                if (innerOffsetId != ObjectId.Null)
                {
                    Polyline innerOffset = (Polyline)tr.GetObject(innerOffsetId, OpenMode.ForRead);
                    CreateHatchBetween(inner, innerOffset, "SOLID", btr, tr);
                }

                if (outerOffsetId != ObjectId.Null && inner != null)
                {
                    Polyline readPoly = tr.GetObject(outerOffsetId, OpenMode.ForRead) as Polyline;

                    CreateHatchBetween(readPoly, inner, "ANSI31", btr, tr);
                }

                if (innerOffsetId != ObjectId.Null)
                {
                    // 1. Get the inner-most polyline object
                    Polyline innerMostPoly = (Polyline)tr.GetObject(innerOffsetId, OpenMode.ForRead);

                    // 2. Create the hatch object
                    Hatch coreHatch = new Hatch();
                    coreHatch.Normal = innerMostPoly.Normal;
                    coreHatch.Elevation = innerMostPoly.Elevation;

                    coreHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                    coreHatch.ColorIndex = 6;

                    // 3. Add to Database
                    btr.AppendEntity(coreHatch);
                    tr.AddNewlyCreatedDBObject(coreHatch, true);

                    // 4. Create the loop (Contains only ONE object)
                    ObjectIdCollection coreLoop = new ObjectIdCollection();
                    coreLoop.Add(innerOffsetId);

                    // 5. Append and Evaluate
                    // For a single loop, HatchLoopTypes.Default works perfectly
                    coreHatch.AppendLoop(HatchLoopTypes.Default, coreLoop);
                    coreHatch.EvaluateHatch(true);
                }

                tr.Commit();
            }
        }

        // DBObjectCollection is a special array used to store entities that was created by geometric actions like Offset or Explode. 
        //      In order to use its results we need to iterate its results


        // Helper: Create an internal offset and return its ObjectId
        private ObjectId CreateInternalOffset(Polyline poly, double dist, BlockTableRecord btr, Transaction tr, int color)
        {
            DBObjectCollection collection = poly.GetOffsetCurves(dist);
            if (collection.Count == 0) return ObjectId.Null;

            Polyline offsetPoly = collection[0] as Polyline;
            if (offsetPoly.Area > poly.Area)
            {
                offsetPoly.Dispose();
                collection = poly.GetOffsetCurves(-dist);
                if (collection.Count == 0) return ObjectId.Null;
                offsetPoly = (Polyline)collection[0];
            }

            offsetPoly.ColorIndex = color;
            btr.AppendEntity(offsetPoly);
            tr.AddNewlyCreatedDBObject(offsetPoly, true);

            return offsetPoly.ObjectId;
        }

        // Helper: Create a hatch between two polylines
        private void CreateHatchBetween(Polyline outer, Polyline inner, string pattern, BlockTableRecord btr, Transaction tr)
        {
            // Create a new Hatch object
            Hatch hatch = new Hatch();
            hatch.Normal = outer.Normal;
            hatch.Elevation = outer.Elevation;

            // Note: The Hatch must be added to the BlockTableRecord before setting its properties
            btr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);

            hatch.SetHatchPattern(HatchPatternType.PreDefined, pattern);
            hatch.PatternScale = 20.0;

            // Define the boundaries for the hatch
            ObjectIdCollection outerLoop = new ObjectIdCollection();
            outerLoop.Add(outer.ObjectId);
            
            ObjectIdCollection innerLoop = new ObjectIdCollection();
            innerLoop.Add(inner.ObjectId);

            hatch.AppendLoop(HatchLoopTypes.Default, outerLoop);
            hatch.AppendLoop(HatchLoopTypes.Default, innerLoop);

            hatch.EvaluateHatch(true);
        }
    }
}