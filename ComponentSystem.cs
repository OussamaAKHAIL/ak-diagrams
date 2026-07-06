using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace AKDiagrams
{
    [DataContract]
    public class ComponentDefinition
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string PreviewBase64 { get; set; }

        [DataMember]
        public string PreviewExtension { get; set; }

        [DataMember]
        public List<FloatPoint> ConnectionPoints { get; set; }

        public ComponentDefinition()
        {
            Name = string.Empty;
            Category = "Custom";
            PreviewBase64 = string.Empty;
            PreviewExtension = "png";
            ConnectionPoints = new List<FloatPoint>();
        }
    }

    [DataContract]
    public class ComponentPackage
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public List<ComponentDefinition> Components { get; set; }

        public ComponentPackage()
        {
            Name = "ak-diagrams components";
            Version = AppInfo.Version;
            Components = new List<ComponentDefinition>();
        }
    }

    public static class ComponentRepository
    {
        private const string PackageEntryName = "package.json";

        public static string LibraryFolder
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "components"); }
        }

        public static List<ComponentDefinition> LoadInstalledComponents()
        {
            var loadedComponents = new List<ComponentDefinition>();
            try
            {
                if (!Directory.Exists(LibraryFolder))
                {
                    Directory.CreateDirectory(LibraryFolder);
                }

                foreach (var zipPath in Directory.GetFiles(LibraryFolder, "*.zip"))
                {
                    loadedComponents.AddRange(LoadPackage(zipPath));
                }
            }
            catch
            {
            }

            return loadedComponents;
        }

        public static List<ComponentDefinition> LoadPackage(string zipPath)
        {
            var components = new List<ComponentDefinition>();
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            {
                return components;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entry = archive.GetEntry(PackageEntryName);
                    if (entry == null)
                    {
                        return components;
                    }

                    using (var stream = entry.Open())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(ComponentPackage));
                        var package = serializer.ReadObject(stream) as ComponentPackage;
                        if (package != null && package.Components != null)
                        {
                            components.AddRange(package.Components.Where(component => component != null));
                        }
                    }
                }
            }
            catch
            {
            }

            return components;
        }

        public static void SavePackage(string zipPath, string packageName, IEnumerable<ComponentDefinition> components)
        {
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var package = new ComponentPackage
            {
                Name = string.IsNullOrWhiteSpace(packageName) ? "ak-diagrams components" : packageName,
                Components = components == null ? new List<ComponentDefinition>() : components.ToList()
            };

            var serializer = new DataContractJsonSerializer(typeof(ComponentPackage));
            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, package);
                memoryStream.Position = 0;

                using (var archiveStream = File.Create(zipPath))
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create))
                {
                    var entry = archive.CreateEntry(PackageEntryName, CompressionLevel.Optimal);
                    using (var entryStream = entry.Open())
                    {
                        memoryStream.CopyTo(entryStream);
                    }
                }
            }
        }

        public static string GetDefaultPackagePath()
        {
            return Path.Combine(LibraryFolder, "custom-components.zip");
        }
    }

    public enum ComponentEditorTool
    {
        Select,
        Rectangle,
        Ellipse,
        Line,
        Freehand,
        Connection
    }

    public enum ComponentElementKind
    {
        Rectangle,
        Ellipse,
        Line,
        Freehand
    }

    public class ComponentEditorElement
    {
        public ComponentElementKind Kind { get; set; }
        public List<PointF> Points { get; private set; }
        public Color StrokeColor { get; set; }
        public Color FillColor { get; set; }
        public float StrokeWidth { get; set; }
        public bool Filled { get; set; }

        public ComponentEditorElement()
        {
            Points = new List<PointF>();
            StrokeColor = Color.Black;
            FillColor = Color.Transparent;
            StrokeWidth = 2f;
            Filled = false;
        }

        public ComponentEditorElement Clone()
        {
            var clone = new ComponentEditorElement();
            clone.Kind = Kind;
            clone.StrokeColor = StrokeColor;
            clone.FillColor = FillColor;
            clone.StrokeWidth = StrokeWidth;
            clone.Filled = Filled;
            clone.Points.AddRange(Points.Select(point => new PointF(point.X, point.Y)));
            return clone;
        }

        public RectangleF GetBounds()
        {
            if (Points.Count == 0)
            {
                return RectangleF.Empty;
            }

            if (Kind == ComponentElementKind.Freehand)
            {
                var minX = Points.Min(point => point.X);
                var minY = Points.Min(point => point.Y);
                var maxX = Points.Max(point => point.X);
                var maxY = Points.Max(point => point.Y);
                return RectangleF.FromLTRB(minX, minY, maxX, maxY);
            }

            if (Points.Count < 2)
            {
                return new RectangleF(Points[0].X, Points[0].Y, 1f, 1f);
            }

            return RectangleF.FromLTRB(
                Math.Min(Points[0].X, Points[1].X),
                Math.Min(Points[0].Y, Points[1].Y),
                Math.Max(Points[0].X, Points[1].X),
                Math.Max(Points[0].Y, Points[1].Y));
        }

        public void MoveBy(float dx, float dy)
        {
            for (var i = 0; i < Points.Count; i++)
            {
                Points[i] = new PointF(Points[i].X + dx, Points[i].Y + dy);
            }
        }

        public void SetBounds(RectangleF rect)
        {
            if (Kind == ComponentElementKind.Freehand)
            {
                ScaleFreehand(rect);
                return;
            }

            EnsurePairPoints();
            Points[0] = new PointF(rect.Left, rect.Top);
            Points[1] = new PointF(rect.Right, rect.Bottom);
        }

        public PointF GetHandlePoint(string handle)
        {
            if (Kind == ComponentElementKind.Line)
            {
                EnsurePairPoints();
                if (string.Equals(handle, "START", StringComparison.OrdinalIgnoreCase))
                {
                    return Points[0];
                }
                return Points[1];
            }

            var bounds = GetBounds();
            if (string.Equals(handle, "NW", StringComparison.OrdinalIgnoreCase))
            {
                return new PointF(bounds.Left, bounds.Top);
            }
            if (string.Equals(handle, "NE", StringComparison.OrdinalIgnoreCase))
            {
                return new PointF(bounds.Right, bounds.Top);
            }
            if (string.Equals(handle, "SE", StringComparison.OrdinalIgnoreCase))
            {
                return new PointF(bounds.Right, bounds.Bottom);
            }
            return new PointF(bounds.Left, bounds.Bottom);
        }

        public void SetHandlePoint(string handle, PointF point)
        {
            if (Kind == ComponentElementKind.Line)
            {
                EnsurePairPoints();
                if (string.Equals(handle, "START", StringComparison.OrdinalIgnoreCase))
                {
                    Points[0] = point;
                }
                else
                {
                    Points[1] = point;
                }
                return;
            }

            var bounds = GetBounds();
            if (string.Equals(handle, "NW", StringComparison.OrdinalIgnoreCase))
            {
                SetBounds(RectangleF.FromLTRB(point.X, point.Y, bounds.Right, bounds.Bottom));
            }
            else if (string.Equals(handle, "NE", StringComparison.OrdinalIgnoreCase))
            {
                SetBounds(RectangleF.FromLTRB(bounds.Left, point.Y, point.X, bounds.Bottom));
            }
            else if (string.Equals(handle, "SE", StringComparison.OrdinalIgnoreCase))
            {
                SetBounds(RectangleF.FromLTRB(bounds.Left, bounds.Top, point.X, point.Y));
            }
            else if (string.Equals(handle, "SW", StringComparison.OrdinalIgnoreCase))
            {
                SetBounds(RectangleF.FromLTRB(point.X, bounds.Top, bounds.Right, point.Y));
            }
        }

        public bool HitTest(PointF point)
        {
            if (Kind == ComponentElementKind.Line && Points.Count >= 2)
            {
                return DistanceToSegment(point, Points[0], Points[1]) <= Math.Max(StrokeWidth + 5f, 8f);
            }

            if (Kind == ComponentElementKind.Freehand && Points.Count >= 2)
            {
                for (var i = 0; i < Points.Count - 1; i++)
                {
                    if (DistanceToSegment(point, Points[i], Points[i + 1]) <= Math.Max(StrokeWidth + 5f, 8f))
                    {
                        return true;
                    }
                }
                return false;
            }

            var bounds = GetBounds();
            bounds.Inflate(4f, 4f);
            return bounds.Contains(point);
        }

        public void Draw(Graphics graphics)
        {
            using (var strokePen = new Pen(StrokeColor, Math.Max(1f, StrokeWidth)))
            using (var fillBrush = new SolidBrush(FillColor))
            {
                strokePen.StartCap = LineCap.Round;
                strokePen.EndCap = LineCap.Round;
                strokePen.LineJoin = LineJoin.Round;

                if (Kind == ComponentElementKind.Line && Points.Count >= 2)
                {
                    graphics.DrawLine(strokePen, Points[0], Points[1]);
                    return;
                }

                if (Kind == ComponentElementKind.Freehand && Points.Count >= 2)
                {
                    graphics.DrawLines(strokePen, Points.ToArray());
                    return;
                }

                var rect = GetBounds();
                if (Filled && FillColor.A > 0)
                {
                    if (Kind == ComponentElementKind.Ellipse)
                    {
                        graphics.FillEllipse(fillBrush, rect);
                    }
                    else
                    {
                        graphics.FillRectangle(fillBrush, rect);
                    }
                }

                if (Kind == ComponentElementKind.Ellipse)
                {
                    graphics.DrawEllipse(strokePen, rect);
                }
                else
                {
                    graphics.DrawRectangle(strokePen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        private void EnsurePairPoints()
        {
            if (Points.Count == 0)
            {
                Points.Add(new PointF(0f, 0f));
                Points.Add(new PointF(1f, 1f));
            }
            else if (Points.Count == 1)
            {
                Points.Add(new PointF(Points[0].X + 1f, Points[0].Y + 1f));
            }
        }

        private void ScaleFreehand(RectangleF rect)
        {
            if (Points.Count < 2)
            {
                return;
            }

            var current = GetBounds();
            if (current.Width < 0.001f || current.Height < 0.001f)
            {
                return;
            }

            for (var i = 0; i < Points.Count; i++)
            {
                var xRatio = (Points[i].X - current.Left) / current.Width;
                var yRatio = (Points[i].Y - current.Top) / current.Height;
                Points[i] = new PointF(rect.Left + xRatio * rect.Width, rect.Top + yRatio * rect.Height);
            }
        }

        private float DistanceToSegment(PointF point, PointF a, PointF b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            if (Math.Abs(dx) < 0.001f && Math.Abs(dy) < 0.001f)
            {
                return Distance(point, a);
            }

            var t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0f, Math.Min(1f, t));
            var projection = new PointF(a.X + t * dx, a.Y + t * dy);
            return Distance(point, projection);
        }

        private float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class ComponentEditorForm : Form
    {
        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly Panel canvas = new Panel();
        private readonly Panel propertiesPanel = new Panel();
        private readonly TextBox nameBox = new TextBox();
        private readonly TextBox categoryBox = new TextBox();
        private readonly NumericUpDown xBox = new NumericUpDown();
        private readonly NumericUpDown yBox = new NumericUpDown();
        private readonly NumericUpDown widthBox = new NumericUpDown();
        private readonly NumericUpDown heightBox = new NumericUpDown();
        private readonly NumericUpDown thicknessBox = new NumericUpDown();
        private readonly Label statusLabel = new Label();
        private readonly Button saveButton = new Button();
        private readonly Button cancelButton = new Button();
        private readonly Button deleteButton = new Button();
        private readonly Button clearButton = new Button();
        private readonly Dictionary<ComponentEditorTool, ToolStripButton> toolButtons = new Dictionary<ComponentEditorTool, ToolStripButton>();
        private readonly List<ComponentEditorElement> elements = new List<ComponentEditorElement>();
        private readonly List<PointF> connectionPoints = new List<PointF>();

        private ComponentEditorTool currentTool = ComponentEditorTool.Select;
        private ComponentEditorElement selectedElement;
        private ComponentEditorElement activeElement;
        private PointF dragStart;
        private PointF creationStart;
        private string activeHandle = string.Empty;
        private bool isDragging;
        private bool isCreating;
        private bool isFreehandDrawing;
        private bool updatingProperties;

        public ComponentDefinition Result { get; private set; }

        public ComponentEditorForm()
        {
            Text = "Component Studio";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Width = 1160;
            Height = 760;
            MinimizeBox = false;
            MaximizeBox = true;

            BuildTopBar();
            BuildCanvas();
            BuildPropertiesPanel();
            BuildFooter();

            AcceptButton = saveButton;
            CancelButton = cancelButton;
            SetTool(ComponentEditorTool.Select);
            UpdatePropertiesFromSelection();
        }

        private void BuildTopBar()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 76;
            topPanel.Padding = new Padding(12, 10, 12, 8);
            Controls.Add(topPanel);

            var nameLabel = new Label();
            nameLabel.Text = "Name";
            nameLabel.AutoSize = true;
            nameLabel.Location = new Point(14, 14);
            topPanel.Controls.Add(nameLabel);

            nameBox.SetBounds(64, 10, 170, 26);
            nameBox.Text = "Component";
            topPanel.Controls.Add(nameBox);

            var categoryLabel = new Label();
            categoryLabel.Text = "Category";
            categoryLabel.AutoSize = true;
            categoryLabel.Location = new Point(250, 14);
            topPanel.Controls.Add(categoryLabel);

            categoryBox.SetBounds(320, 10, 160, 26);
            categoryBox.Text = "Custom";
            topPanel.Controls.Add(categoryBox);

            toolStrip.Dock = DockStyle.Bottom;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Stretch = false;
            toolStrip.Padding = new Padding(0);
            toolStrip.RenderMode = ToolStripRenderMode.System;
            topPanel.Controls.Add(toolStrip);

            AddToolButton("Select", ComponentEditorTool.Select);
            AddToolButton("Rect", ComponentEditorTool.Rectangle);
            AddToolButton("Ellipse", ComponentEditorTool.Ellipse);
            AddToolButton("Line", ComponentEditorTool.Line);
            AddToolButton("Freehand", ComponentEditorTool.Freehand);
            AddToolButton("Connect", ComponentEditorTool.Connection);

            clearButton.Text = "Clear";
            clearButton.AutoSize = true;
            clearButton.Click += delegate
            {
                elements.Clear();
                connectionPoints.Clear();
                selectedElement = null;
                activeElement = null;
                isDragging = false;
                isCreating = false;
                isFreehandDrawing = false;
                canvas.Invalidate();
                UpdatePropertiesFromSelection();
            };
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripControlHost(clearButton));
        }

        private void BuildCanvas()
        {
            canvas.Dock = DockStyle.Fill;
            canvas.BackColor = Color.White;
            canvas.BorderStyle = BorderStyle.FixedSingle;
            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            canvas.MouseEnter += delegate { canvas.Focus(); };
            Controls.Add(canvas);
        }

        private void BuildPropertiesPanel()
        {
            propertiesPanel.Dock = DockStyle.Right;
            propertiesPanel.Width = 270;
            propertiesPanel.Padding = new Padding(12);
            propertiesPanel.BackColor = Color.FromArgb(246, 246, 246);
            Controls.Add(propertiesPanel);

            var title = new Label();
            title.Text = "Properties";
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(12, 12);
            propertiesPanel.Controls.Add(title);

            var y = 48;
            AddNumericRow("X", ref y, xBox);
            AddNumericRow("Y", ref y, yBox);
            AddNumericRow("Width", ref y, widthBox);
            AddNumericRow("Height", ref y, heightBox);
            AddNumericRow("Thickness", ref y, thicknessBox);

            xBox.ValueChanged += delegate { ApplyProperties(); };
            yBox.ValueChanged += delegate { ApplyProperties(); };
            widthBox.ValueChanged += delegate { ApplyProperties(); };
            heightBox.ValueChanged += delegate { ApplyProperties(); };
            thicknessBox.ValueChanged += delegate { ApplyProperties(); };

            statusLabel.AutoSize = false;
            statusLabel.SetBounds(12, y + 2, 240, 38);
            statusLabel.Text = "Select an element to edit it.";
            propertiesPanel.Controls.Add(statusLabel);

            var addConnectionButton = new Button();
            addConnectionButton.Text = "Connection Tool";
            addConnectionButton.SetBounds(12, y + 46, 236, 30);
            addConnectionButton.Click += delegate { SetTool(ComponentEditorTool.Connection); };
            propertiesPanel.Controls.Add(addConnectionButton);

            deleteButton.Text = "Delete Selected";
            deleteButton.SetBounds(12, y + 82, 236, 30);
            deleteButton.Click += delegate { DeleteSelected(); };
            propertiesPanel.Controls.Add(deleteButton);
        }

        private void BuildFooter()
        {
            var footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 54;
            footer.Padding = new Padding(12, 10, 12, 10);
            Controls.Add(footer);

            saveButton.Text = "Save";
            saveButton.Width = 90;
            saveButton.Click += delegate { SaveComponent(); };
            footer.Controls.Add(saveButton);

            cancelButton.Text = "Cancel";
            cancelButton.Width = 90;
            cancelButton.Left = 96;
            cancelButton.DialogResult = DialogResult.Cancel;
            footer.Controls.Add(cancelButton);
        }

        private void AddToolButton(string text, ComponentEditorTool tool)
        {
            var button = new ToolStripButton(text);
            button.CheckOnClick = true;
            button.Click += delegate { SetTool(tool); };
            toolButtons[tool] = button;
            toolStrip.Items.Add(button);
        }

        private void SetTool(ComponentEditorTool tool)
        {
            currentTool = tool;
            foreach (var pair in toolButtons)
            {
                pair.Value.Checked = pair.Key == tool;
            }

            canvas.Cursor = tool == ComponentEditorTool.Select ? Cursors.Arrow : Cursors.Cross;
            statusLabel.Text = tool == ComponentEditorTool.Connection
                ? "Click the canvas to add connection points."
                : "Tool: " + tool;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            PaintCheckerboard(e.Graphics, canvas.ClientRectangle);

            foreach (var element in elements)
            {
                element.Draw(e.Graphics);
            }

            DrawConnectionPoints(e.Graphics);

            if (selectedElement != null)
            {
                DrawSelection(e.Graphics, selectedElement);
            }

            if (isCreating && activeElement != null)
            {
                DrawSelection(e.Graphics, activeElement);
            }
        }

        private void PaintCheckerboard(Graphics graphics, Rectangle bounds)
        {
            graphics.Clear(Color.White);
            using (var light = new SolidBrush(Color.FromArgb(244, 244, 244)))
            using (var dark = new SolidBrush(Color.FromArgb(232, 232, 232)))
            {
                const int tile = 20;
                for (var y = 0; y < bounds.Height; y += tile)
                {
                    for (var x = 0; x < bounds.Width; x += tile)
                    {
                        var useDark = ((x / tile) + (y / tile)) % 2 == 0;
                        graphics.FillRectangle(useDark ? light : dark, x, y, tile, tile);
                    }
                }
            }
        }

        private void DrawConnectionPoints(Graphics graphics)
        {
            foreach (var point in connectionPoints)
            {
                graphics.FillEllipse(Brushes.DarkRed, point.X - 4f, point.Y - 4f, 8f, 8f);
                graphics.DrawEllipse(Pens.White, point.X - 4f, point.Y - 4f, 8f, 8f);
            }
        }

        private void DrawSelection(Graphics graphics, ComponentEditorElement element)
        {
            var bounds = element.GetBounds();
            using (var pen = new Pen(Color.FromArgb(42, 115, 217), 1.5f))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(pen, bounds.X - 4f, bounds.Y - 4f, bounds.Width + 8f, bounds.Height + 8f);
            }

            using (var borderPen = new Pen(Color.FromArgb(42, 115, 217), 1f))
            using (var fillBrush = new SolidBrush(Color.White))
            {
                foreach (var handle in GetHandles(element))
                {
                    var handleRect = new RectangleF(handle.Value.X - 4f, handle.Value.Y - 4f, 8f, 8f);
                    graphics.FillRectangle(fillBrush, handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height);
                    graphics.DrawRectangle(borderPen, handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height);
                }
            }
        }

        private Dictionary<string, PointF> GetHandles(ComponentEditorElement element)
        {
            var handles = new Dictionary<string, PointF>();
            if (element == null)
            {
                return handles;
            }

            if (element.Kind == ComponentElementKind.Line)
            {
                handles["START"] = element.GetHandlePoint("START");
                handles["END"] = element.GetHandlePoint("END");
                return handles;
            }

            var bounds = element.GetBounds();
            handles["NW"] = new PointF(bounds.Left, bounds.Top);
            handles["NE"] = new PointF(bounds.Right, bounds.Top);
            handles["SE"] = new PointF(bounds.Right, bounds.Bottom);
            handles["SW"] = new PointF(bounds.Left, bounds.Bottom);
            return handles;
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var point = e.Location;

            if (currentTool == ComponentEditorTool.Connection)
            {
                connectionPoints.Add(point);
                canvas.Invalidate();
                return;
            }

            if (currentTool == ComponentEditorTool.Freehand)
            {
                isCreating = true;
                isFreehandDrawing = true;
                activeElement = new ComponentEditorElement();
                activeElement.Kind = ComponentElementKind.Freehand;
                activeElement.Points.Add(point);
                elements.Add(activeElement);
                selectedElement = activeElement;
                UpdatePropertiesFromSelection();
                canvas.Invalidate();
                return;
            }

            if (currentTool == ComponentEditorTool.Rectangle || currentTool == ComponentEditorTool.Ellipse || currentTool == ComponentEditorTool.Line)
            {
                isCreating = true;
                creationStart = point;
                activeElement = new ComponentEditorElement();
                activeElement.Kind = currentTool == ComponentEditorTool.Line
                    ? ComponentElementKind.Line
                    : currentTool == ComponentEditorTool.Rectangle
                        ? ComponentElementKind.Rectangle
                        : ComponentElementKind.Ellipse;
                activeElement.Points.Add(point);
                activeElement.Points.Add(point);
                elements.Add(activeElement);
                selectedElement = activeElement;
                UpdatePropertiesFromSelection();
                canvas.Invalidate();
                return;
            }

            SelectElementAt(point);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.Location;

            if (isCreating && activeElement != null)
            {
                if (isFreehandDrawing)
                {
                    if (activeElement.Points.Count == 0 || Distance(activeElement.Points[activeElement.Points.Count - 1], point) >= 1.5f)
                    {
                        activeElement.Points.Add(point);
                    }
                }
                else if (activeElement.Kind == ComponentElementKind.Line)
                {
                    activeElement.Points[1] = point;
                }
                else
                {
                    activeElement.SetBounds(MakeRect(creationStart, point));
                }

                UpdatePropertiesFromSelection();
                canvas.Invalidate();
                return;
            }

            if (isDragging && selectedElement != null)
            {
                if (string.IsNullOrWhiteSpace(activeHandle))
                {
                    var dx = point.X - dragStart.X;
                    var dy = point.Y - dragStart.Y;
                    selectedElement.MoveBy(dx, dy);
                    dragStart = point;
                }
                else
                {
                    selectedElement.SetHandlePoint(activeHandle, point);
                }

                UpdatePropertiesFromSelection();
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isCreating)
            {
                isCreating = false;
                isFreehandDrawing = false;

                if (activeElement != null && activeElement.Kind != ComponentElementKind.Freehand)
                {
                    var bounds = activeElement.GetBounds();
                    if (bounds.Width < 2f && bounds.Height < 2f)
                    {
                        elements.Remove(activeElement);
                    }
                }

                activeElement = null;
                UpdatePropertiesFromSelection();
                canvas.Invalidate();
                return;
            }

            if (isDragging)
            {
                isDragging = false;
                activeHandle = string.Empty;
                activeElement = null;
                UpdatePropertiesFromSelection();
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (currentTool == ComponentEditorTool.Connection)
            {
                connectionPoints.Add(e.Location);
                canvas.Invalidate();
            }
        }

        private void SelectElementAt(PointF point)
        {
            for (var i = elements.Count - 1; i >= 0; i--)
            {
                var element = elements[i];
                var handle = GetHandleHit(element, point);
                if (handle != null)
                {
                    selectedElement = element;
                    activeElement = element;
                    activeHandle = handle.Name;
                    dragStart = point;
                    isDragging = true;
                    UpdatePropertiesFromSelection();
                    canvas.Invalidate();
                    return;
                }

                if (element.HitTest(point))
                {
                    selectedElement = element;
                    activeElement = element;
                    activeHandle = string.Empty;
                    dragStart = point;
                    isDragging = true;
                    UpdatePropertiesFromSelection();
                    canvas.Invalidate();
                    return;
                }
            }

            selectedElement = null;
            activeElement = null;
            UpdatePropertiesFromSelection();
            canvas.Invalidate();
        }

        private HandleHit GetHandleHit(ComponentEditorElement element, PointF point)
        {
            foreach (var handle in GetHandles(element))
            {
                if (Distance(handle.Value, point) <= 8f)
                {
                    return new HandleHit(handle.Key, 0);
                }
            }
            return null;
        }

        private void AddNumericRow(string text, ref int y, NumericUpDown box)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(12, y + 5);
            propertiesPanel.Controls.Add(label);

            box.SetBounds(95, y, 156, 26);
            box.Minimum = -10000;
            box.Maximum = 10000;
            box.DecimalPlaces = 0;
            box.Enabled = false;
            propertiesPanel.Controls.Add(box);
            y += 34;
        }

        private void UpdatePropertiesFromSelection()
        {
            updatingProperties = true;
            try
            {
                var hasSelection = selectedElement != null;
                xBox.Enabled = hasSelection;
                yBox.Enabled = hasSelection;
                widthBox.Enabled = hasSelection;
                heightBox.Enabled = hasSelection;
                thicknessBox.Enabled = hasSelection;
                deleteButton.Enabled = hasSelection;

                if (!hasSelection)
                {
                    statusLabel.Text = "Select an element to edit it.";
                    return;
                }

                var bounds = selectedElement.GetBounds();
                xBox.Value = ClampNumeric(bounds.X);
                yBox.Value = ClampNumeric(bounds.Y);
                widthBox.Value = ClampNumeric(bounds.Width);
                heightBox.Value = ClampNumeric(bounds.Height);
                thicknessBox.Value = ClampNumeric(Math.Max(1f, selectedElement.StrokeWidth));
                statusLabel.Text = selectedElement.Kind.ToString() + " selected";
            }
            finally
            {
                updatingProperties = false;
            }
        }

        private void ApplyProperties()
        {
            if (updatingProperties || selectedElement == null)
            {
                return;
            }

            var rect = MakeRect(
                new PointF((float)xBox.Value, (float)yBox.Value),
                new PointF((float)xBox.Value + (float)widthBox.Value, (float)yBox.Value + (float)heightBox.Value));

            selectedElement.SetBounds(rect);
            selectedElement.StrokeWidth = Math.Max(1f, (float)thicknessBox.Value);
            canvas.Invalidate();
        }

        private decimal ClampNumeric(float value)
        {
            var clamped = Math.Max(-10000f, Math.Min(10000f, value));
            return Convert.ToDecimal(Math.Round(clamped));
        }

        private RectangleF MakeRect(PointF start, PointF end)
        {
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var right = Math.Max(start.X, end.X);
            var bottom = Math.Max(start.Y, end.Y);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        private void DeleteSelected()
        {
            if (selectedElement == null)
            {
                return;
            }

            elements.Remove(selectedElement);
            selectedElement = null;
            activeElement = null;
            isDragging = false;
            activeHandle = string.Empty;
            UpdatePropertiesFromSelection();
            canvas.Invalidate();
        }

        private void SaveComponent()
        {
            var contentBounds = GetContentBounds();
            var preview = RenderPreview(contentBounds);
            var definition = new ComponentDefinition
            {
                Name = string.IsNullOrWhiteSpace(nameBox.Text) ? "Component" : nameBox.Text.Trim(),
                Category = string.IsNullOrWhiteSpace(categoryBox.Text) ? "Custom" : categoryBox.Text.Trim(),
                PreviewBase64 = Convert.ToBase64String(preview),
                PreviewExtension = "png",
                ConnectionPoints = connectionPoints.Select(point => new FloatPoint(
                    contentBounds.Width <= 0f ? 0f : Clamp((point.X - contentBounds.X) / contentBounds.Width, 0f, 1f),
                    contentBounds.Height <= 0f ? 0f : Clamp((point.Y - contentBounds.Y) / contentBounds.Height, 0f, 1f))).ToList()
            };

            Result = definition;
            DialogResult = DialogResult.OK;
            Close();
        }

        private RectangleF GetContentBounds()
        {
            RectangleF bounds = RectangleF.Empty;

            foreach (var element in elements)
            {
                var rect = element.GetBounds();
                if (rect.Width < 1f)
                {
                    rect.Width = 1f;
                }
                if (rect.Height < 1f)
                {
                    rect.Height = 1f;
                }

                if (bounds == RectangleF.Empty)
                {
                    bounds = rect;
                }
                else
                {
                    bounds = RectangleF.Union(bounds, rect);
                }
            }

            if (bounds == RectangleF.Empty)
            {
                bounds = new RectangleF(0f, 0f, Math.Max(1f, canvas.Width), Math.Max(1f, canvas.Height));
            }

            bounds.Inflate(24f, 24f);
            return bounds;
        }

        private byte[] RenderPreview(RectangleF contentBounds)
        {
            var width = Math.Max(1, (int)Math.Ceiling(contentBounds.Width));
            var height = Math.Max(1, (int)Math.Ceiling(contentBounds.Height));

            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                bitmap.SetResolution(96f, 96f);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.Clear(Color.Transparent);
                    graphics.TranslateTransform(-contentBounds.X, -contentBounds.Y);

                    foreach (var element in elements)
                    {
                        element.Draw(graphics);
                    }
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        private float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
