using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace AKDiagrams
{
    public enum ToolKind
    {
        Select,
        Block,
        Device,
        Wire,
        Text
    }

    [DataContract]
    public class FloatPoint
    {
        [DataMember]
        public float X { get; set; }

        [DataMember]
        public float Y { get; set; }

        public FloatPoint()
        {
        }

        public FloatPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public PointF ToPointF()
        {
            return new PointF(X, Y);
        }

        public static FloatPoint FromPointF(PointF point)
        {
            return new FloatPoint(point.X, point.Y);
        }
    }

    [DataContract]
    public class DiagramItem
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public List<FloatPoint> Points { get; set; }

        [DataMember]
        public string Label { get; set; }

        [DataMember]
        public string FillColor { get; set; }

        [DataMember]
        public string LineColor { get; set; }

        [DataMember]
        public string TextColor { get; set; }

        [DataMember]
        public float LineWidth { get; set; }

        [DataMember]
        public bool Arrow { get; set; }

        public DiagramItem()
        {
            Type = string.Empty;
            Points = new List<FloatPoint>();
            Label = string.Empty;
            FillColor = "#F2F5FF";
            LineColor = "#111111";
            TextColor = "#111111";
            LineWidth = 2f;
            Arrow = false;
        }
    }

    [DataContract]
    public class DiagramDocument
    {
        [DataMember]
        public string App { get; set; }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public List<DiagramItem> Items { get; set; }

        public DiagramDocument()
        {
            App = "ak-diagrams";
            Version = 1;
            Items = new List<DiagramItem>();
        }
    }

    public class DiagramCanvas : Panel
    {
        public DiagramCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(248, 248, 248);
            Cursor = Cursors.Cross;
        }
    }

    public static class DotEnvLoader
    {
        public static void Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            var lines = File.ReadAllLines(filePath);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            }
        }
    }

    public class DiagramForm : Form
    {
        private readonly DiagramCanvas canvas = new DiagramCanvas();
        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly StatusStrip statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();
        private readonly Dictionary<ToolKind, ToolStripButton> toolButtons = new Dictionary<ToolKind, ToolStripButton>();
        private readonly List<DiagramItem> items = new List<DiagramItem>();

        private readonly ToolStripComboBox widthCombo = new ToolStripComboBox();
        private readonly ToolStripButton fillColorButton = new ToolStripButton("Fill");
        private readonly ToolStripButton lineColorButton = new ToolStripButton("Line");
        private readonly ToolStripButton textColorButton = new ToolStripButton("Text");
        private readonly ToolStripButton arrowToggleButton = new ToolStripButton("Arrow");
        private readonly ToolStripButton snapToggleButton = new ToolStripButton("Snap");
        private readonly ToolStripButton gridToggleButton = new ToolStripButton("Grid");

        private ToolKind currentTool = ToolKind.Select;
        private DiagramItem selectedItem;

        private bool snapToGrid = true;
        private bool showGrid = true;
        private bool wireArrow = true;
        private readonly int gridSize = 20;

        private Color fillColor = Color.FromArgb(242, 245, 255);
        private Color lineColor = Color.FromArgb(17, 17, 17);
        private Color textColor = Color.FromArgb(17, 17, 17);
        private float lineWidth = 2f;

        private PointF? drawStart;
        private PointF currentPointer;
        private bool dragging;
        private bool movedInDrag;
        private PointF dragStart;
        private PointF? wireStart;
        private string currentPath = string.Empty;
        private readonly string defaultDialogDirectory;

        public DiagramForm()
        {
            Text = "ak-diagrams";
            Width = 1440;
            Height = 900;
            MinimumSize = new Size(1080, 720);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DotEnvLoader.Load(Path.Combine(appDirectory, ".env"));
            defaultDialogDirectory = ResolveDefaultDialogDirectory(appDirectory);

            InitializeMenuAndToolbar();
            InitializeCanvas();
            InitializeStatusBar();
            UpdateColorButtons();
            SetStatus("Ready");
        }

        private void InitializeMenuAndToolbar()
        {
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("New", null, (s, e) => NewFile());
            fileMenu.DropDownItems.Add("Open", null, (s, e) => OpenFile());
            fileMenu.DropDownItems.Add("Save", null, (s, e) => SaveFile());
            fileMenu.DropDownItems.Add("Save As", null, (s, e) => SaveFileAs());
            fileMenu.DropDownItems.Add("Export SVG", null, (s, e) => ExportSvg());
            fileMenu.DropDownItems.Add("-");
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
            menuStrip.Items.Add(fileMenu);

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Dock = DockStyle.Top;

            AddToolStripButton("New", (s, e) => NewFile());
            AddToolStripButton("Open", (s, e) => OpenFile());
            AddToolStripButton("Save", (s, e) => SaveFile());
            AddToolStripButton("Save As", (s, e) => SaveFileAs());
            AddToolStripButton("Export SVG", (s, e) => ExportSvg());

            toolStrip.Items.Add(new ToolStripSeparator());

            AddToolButton("Select", ToolKind.Select);
            AddToolButton("Block", ToolKind.Block);
            AddToolButton("Device", ToolKind.Device);
            AddToolButton("Wire", ToolKind.Wire);
            AddToolButton("Text", ToolKind.Text);

            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripLabel("Width"));

            widthCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            widthCombo.Items.AddRange(new object[] { "1", "2", "3", "4", "6", "8", "10" });
            widthCombo.SelectedIndex = 1;
            widthCombo.AutoSize = false;
            widthCombo.Width = 60;
            widthCombo.SelectedIndexChanged += (s, e) =>
            {
                float value;
                if (float.TryParse(Convert.ToString(widthCombo.SelectedItem), out value))
                {
                    lineWidth = value;
                }
            };
            toolStrip.Items.Add(widthCombo);

            fillColorButton.Click += (s, e) => PickColor("fill");
            lineColorButton.Click += (s, e) => PickColor("line");
            textColorButton.Click += (s, e) => PickColor("text");
            toolStrip.Items.Add(fillColorButton);
            toolStrip.Items.Add(lineColorButton);
            toolStrip.Items.Add(textColorButton);

            AddToolStripButton("Apply Style", (s, e) => ApplyStyleToSelected());

            toolStrip.Items.Add(new ToolStripSeparator());

            arrowToggleButton.CheckOnClick = true;
            arrowToggleButton.Checked = wireArrow;
            arrowToggleButton.CheckedChanged += (s, e) => wireArrow = arrowToggleButton.Checked;
            toolStrip.Items.Add(arrowToggleButton);

            snapToggleButton.CheckOnClick = true;
            snapToggleButton.Checked = snapToGrid;
            snapToggleButton.CheckedChanged += (s, e) => snapToGrid = snapToggleButton.Checked;
            toolStrip.Items.Add(snapToggleButton);

            gridToggleButton.CheckOnClick = true;
            gridToggleButton.Checked = showGrid;
            gridToggleButton.CheckedChanged += (s, e) =>
            {
                showGrid = gridToggleButton.Checked;
                canvas.Invalidate();
            };
            toolStrip.Items.Add(gridToggleButton);

            AddToolStripButton("Rename", (s, e) => RenameSelected());
            AddToolStripButton("Delete", (s, e) => DeleteSelected());

            Controls.Add(toolStrip);
        }

        private void InitializeCanvas()
        {
            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.DoubleClick += Canvas_DoubleClick;
            canvas.Resize += (s, e) => canvas.Invalidate();
            Controls.Add(canvas);
            canvas.BringToFront();
        }

        private void InitializeStatusBar()
        {
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.Items.Add(statusLabel);
            Controls.Add(statusStrip);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelected();
                e.Handled = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveFile();
                e.Handled = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.O)
            {
                OpenFile();
                e.Handled = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.N)
            {
                NewFile();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                drawStart = null;
                wireStart = null;
                dragging = false;
                canvas.Invalidate();
            }
        }

        private void AddToolStripButton(string text, EventHandler click)
        {
            var button = new ToolStripButton(text);
            button.Click += click;
            toolStrip.Items.Add(button);
        }

        private void AddToolButton(string text, ToolKind kind)
        {
            var button = new ToolStripButton(text);
            button.CheckOnClick = true;
            button.Checked = kind == ToolKind.Select;
            button.Click += (s, e) => SetTool(kind);
            toolButtons[kind] = button;
            toolStrip.Items.Add(button);
        }

        private void SetTool(ToolKind kind)
        {
            currentTool = kind;
            foreach (var pair in toolButtons)
            {
                pair.Value.Checked = pair.Key == kind;
            }
            drawStart = null;
            wireStart = null;
            dragging = false;
            canvas.Cursor = kind == ToolKind.Select ? Cursors.Arrow : Cursors.Cross;
            SetStatus("Tool: " + kind);
            canvas.Invalidate();
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        private void PickColor(string target)
        {
            using (var dialog = new ColorDialog())
            {
                if (target == "fill") dialog.Color = fillColor;
                if (target == "line") dialog.Color = lineColor;
                if (target == "text") dialog.Color = textColor;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (target == "fill") fillColor = dialog.Color;
                if (target == "line") lineColor = dialog.Color;
                if (target == "text") textColor = dialog.Color;
                UpdateColorButtons();
                SetStatus(target + " color updated");
            }
        }

        private void UpdateColorButtons()
        {
            fillColorButton.BackColor = fillColor;
            lineColorButton.BackColor = lineColor;
            textColorButton.BackColor = textColor;
            fillColorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            lineColorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            textColorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        }

        private PointF Snap(PointF point)
        {
            if (!snapToGrid)
            {
                return point;
            }
            return new PointF(
                (float)Math.Round(point.X / gridSize) * gridSize,
                (float)Math.Round(point.Y / gridSize) * gridSize
            );
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            var rawPoint = new PointF(e.X, e.Y);
            currentPointer = rawPoint;
            var point = Snap(rawPoint);

            if (currentTool == ToolKind.Select)
            {
                selectedItem = FindItemAt(rawPoint);
                dragging = selectedItem != null;
                dragStart = rawPoint;
                movedInDrag = false;
                if (selectedItem == null)
                {
                    SetStatus("No selection");
                }
                else
                {
                    SetStatus("Selected " + selectedItem.Type);
                }
                canvas.Invalidate();
                return;
            }

            if (currentTool == ToolKind.Block || currentTool == ToolKind.Device)
            {
                drawStart = point;
                canvas.Invalidate();
                return;
            }

            if (currentTool == ToolKind.Wire)
            {
                if (wireStart == null)
                {
                    wireStart = point;
                    SetStatus("Wire start set. Click end point.");
                }
                else
                {
                    var points = BuildOrthogonalPoints(wireStart.Value, point);
                    items.Add(new DiagramItem
                    {
                        Type = "wire",
                        Points = points.Select(FloatPoint.FromPointF).ToList(),
                        LineColor = ColorTranslator.ToHtml(lineColor),
                        LineWidth = lineWidth,
                        Arrow = wireArrow
                    });
                    wireStart = null;
                    SetStatus("Wire created");
                }
                canvas.Invalidate();
                return;
            }

            if (currentTool == ToolKind.Text)
            {
                var text = PromptDialog.Show("Enter text:", "Add Text");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    items.Add(new DiagramItem
                    {
                        Type = "text",
                        Points = new List<FloatPoint> { FloatPoint.FromPointF(point) },
                        Label = text,
                        TextColor = ColorTranslator.ToHtml(textColor)
                    });
                    SetStatus("Text added");
                    canvas.Invalidate();
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var rawPoint = new PointF(e.X, e.Y);
            currentPointer = rawPoint;

            if (currentTool == ToolKind.Select && dragging && selectedItem != null)
            {
                var dx = rawPoint.X - dragStart.X;
                var dy = rawPoint.Y - dragStart.Y;
                if (Math.Abs(dx) > 0.01f || Math.Abs(dy) > 0.01f)
                {
                    MoveItem(selectedItem, dx, dy);
                    dragStart = rawPoint;
                    movedInDrag = true;
                    canvas.Invalidate();
                }
                return;
            }

            if (drawStart != null || wireStart != null)
            {
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            var point = Snap(new PointF(e.X, e.Y));

            if (currentTool == ToolKind.Select)
            {
                if (dragging && movedInDrag)
                {
                    SetStatus("Moved selected object");
                }
                dragging = false;
                movedInDrag = false;
                return;
            }

            if ((currentTool == ToolKind.Block || currentTool == ToolKind.Device) && drawStart != null)
            {
                var start = drawStart.Value;
                drawStart = null;
                var rect = NormalizeRect(start, point);
                if (rect.Width < 14f || rect.Height < 14f)
                {
                    SetStatus("Shape too small");
                    canvas.Invalidate();
                    return;
                }
                var type = currentTool == ToolKind.Block ? "rect" : "ellipse";
                var label = currentTool == ToolKind.Block ? "Component" : "Device";
                items.Add(new DiagramItem
                {
                    Type = type,
                    Points = new List<FloatPoint>
                    {
                        new FloatPoint(rect.Left, rect.Top),
                        new FloatPoint(rect.Right, rect.Bottom)
                    },
                    Label = label,
                    FillColor = ColorTranslator.ToHtml(fillColor),
                    LineColor = ColorTranslator.ToHtml(lineColor),
                    TextColor = ColorTranslator.ToHtml(textColor),
                    LineWidth = lineWidth
                });
                SetStatus((type == "rect" ? "Block" : "Device") + " created");
                canvas.Invalidate();
            }
        }

        private void Canvas_DoubleClick(object sender, EventArgs e)
        {
            if (currentTool != ToolKind.Select || selectedItem == null)
            {
                return;
            }
            RenameSelected();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (showGrid)
            {
                DrawGrid(graphics);
            }

            foreach (var item in items)
            {
                DrawItem(graphics, item);
            }

            DrawPreview(graphics);
            DrawSelection(graphics);
        }

        private void DrawGrid(Graphics graphics)
        {
            using (var pen = new Pen(Color.FromArgb(232, 232, 232), 1f))
            {
                for (var x = 0; x <= canvas.Width; x += gridSize)
                {
                    graphics.DrawLine(pen, x, 0, x, canvas.Height);
                }
                for (var y = 0; y <= canvas.Height; y += gridSize)
                {
                    graphics.DrawLine(pen, 0, y, canvas.Width, y);
                }
            }
        }

        private void DrawItem(Graphics graphics, DiagramItem item)
        {
            if (item.Type == "rect" && item.Points.Count >= 2)
            {
                DrawRect(graphics, item);
                return;
            }
            if (item.Type == "ellipse" && item.Points.Count >= 2)
            {
                DrawEllipse(graphics, item);
                return;
            }
            if (item.Type == "wire" && item.Points.Count >= 2)
            {
                DrawWire(graphics, item);
                return;
            }
            if (item.Type == "text" && item.Points.Count >= 1)
            {
                DrawText(graphics, item);
            }
        }

        private void DrawRect(Graphics graphics, DiagramItem item)
        {
            var rect = GetRect(item);
            using (var brush = new SolidBrush(ParseColor(item.FillColor, Color.White)))
            using (var pen = new Pen(ParseColor(item.LineColor, Color.Black), item.LineWidth))
            using (var textBrush = new SolidBrush(ParseColor(item.TextColor, Color.Black)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                graphics.FillRectangle(brush, rect);
                graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                graphics.DrawString(item.Label ?? string.Empty, new Font("Times New Roman", 15f, FontStyle.Bold), textBrush, rect, format);
            }
        }

        private void DrawEllipse(Graphics graphics, DiagramItem item)
        {
            var rect = GetRect(item);
            using (var brush = new SolidBrush(ParseColor(item.FillColor, Color.White)))
            using (var pen = new Pen(ParseColor(item.LineColor, Color.Black), item.LineWidth))
            using (var textBrush = new SolidBrush(ParseColor(item.TextColor, Color.Black)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                graphics.FillEllipse(brush, rect);
                graphics.DrawEllipse(pen, rect);
                graphics.DrawString(item.Label ?? string.Empty, new Font("Times New Roman", 15f, FontStyle.Bold), textBrush, rect, format);
            }
        }

        private void DrawWire(Graphics graphics, DiagramItem item)
        {
            var points = item.Points.Select(point => point.ToPointF()).ToArray();
            using (var pen = new Pen(ParseColor(item.LineColor, Color.Black), item.LineWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                if (item.Arrow)
                {
                    pen.CustomEndCap = new AdjustableArrowCap(4f, 6f, true);
                }
                graphics.DrawLines(pen, points);
            }
        }

        private void DrawText(Graphics graphics, DiagramItem item)
        {
            var point = item.Points[0].ToPointF();
            var text = item.Label ?? string.Empty;
            using (var brush = new SolidBrush(ParseColor(item.TextColor, Color.Black)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                graphics.DrawString(text, new Font("Times New Roman", 15f, FontStyle.Regular), brush, point, format);
            }
        }

        private void DrawPreview(Graphics graphics)
        {
            var snappedPointer = Snap(currentPointer);
            using (var pen = new Pen(lineColor, Math.Max(1f, lineWidth)))
            {
                pen.DashStyle = DashStyle.Dash;
                if (currentTool == ToolKind.Block && drawStart != null)
                {
                    var rect = NormalizeRect(drawStart.Value, snappedPointer);
                    graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    return;
                }
                if (currentTool == ToolKind.Device && drawStart != null)
                {
                    var rect = NormalizeRect(drawStart.Value, snappedPointer);
                    graphics.DrawEllipse(pen, rect);
                    return;
                }
                if (currentTool == ToolKind.Wire && wireStart != null)
                {
                    var points = BuildOrthogonalPoints(wireStart.Value, snappedPointer).ToArray();
                    if (wireArrow)
                    {
                        pen.CustomEndCap = new AdjustableArrowCap(4f, 6f, true);
                    }
                    graphics.DrawLines(pen, points);
                }
            }
        }

        private void DrawSelection(Graphics graphics)
        {
            if (selectedItem == null)
            {
                return;
            }
            var rect = GetBounds(selectedItem);
            rect.Inflate(6f, 6f);
            using (var pen = new Pen(Color.FromArgb(53, 118, 216), 2f))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private RectangleF GetRect(DiagramItem item)
        {
            return NormalizeRect(item.Points[0].ToPointF(), item.Points[1].ToPointF());
        }

        private RectangleF GetBounds(DiagramItem item)
        {
            if (item.Type == "rect" || item.Type == "ellipse")
            {
                return GetRect(item);
            }
            if (item.Type == "wire")
            {
                var points = item.Points.Select(point => point.ToPointF()).ToList();
                var minX = points.Min(point => point.X);
                var maxX = points.Max(point => point.X);
                var minY = points.Min(point => point.Y);
                var maxY = points.Max(point => point.Y);
                var pad = Math.Max(6f, item.LineWidth + 2f);
                return new RectangleF(minX - pad, minY - pad, (maxX - minX) + 2f * pad, (maxY - minY) + 2f * pad);
            }
            if (item.Type == "text")
            {
                var center = item.Points[0].ToPointF();
                var width = Math.Max(40f, (item.Label ?? string.Empty).Length * 9f);
                return new RectangleF(center.X - width / 2f, center.Y - 14f, width, 28f);
            }
            return new RectangleF();
        }

        private void MoveItem(DiagramItem item, float dx, float dy)
        {
            for (var i = 0; i < item.Points.Count; i++)
            {
                var point = item.Points[i];
                point.X += dx;
                point.Y += dy;
            }
        }

        private DiagramItem FindItemAt(PointF point)
        {
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (HitTest(items[i], point))
                {
                    return items[i];
                }
            }
            return null;
        }

        private bool HitTest(DiagramItem item, PointF point)
        {
            if (item.Type == "rect")
            {
                return GetRect(item).Contains(point);
            }
            if (item.Type == "ellipse")
            {
                var rect = GetRect(item);
                var cx = rect.Left + rect.Width / 2f;
                var cy = rect.Top + rect.Height / 2f;
                var rx = rect.Width / 2f;
                var ry = rect.Height / 2f;
                if (rx <= 0f || ry <= 0f)
                {
                    return false;
                }
                var dx = (point.X - cx) / rx;
                var dy = (point.Y - cy) / ry;
                return (dx * dx + dy * dy) <= 1.0f;
            }
            if (item.Type == "wire")
            {
                var points = item.Points.Select(value => value.ToPointF()).ToList();
                var threshold = Math.Max(6f, item.LineWidth + 2f);
                for (var i = 0; i < points.Count - 1; i++)
                {
                    var distance = DistanceToSegment(point, points[i], points[i + 1]);
                    if (distance <= threshold)
                    {
                        return true;
                    }
                }
                return false;
            }
            if (item.Type == "text")
            {
                return GetBounds(item).Contains(point);
            }
            return false;
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
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            var projection = new PointF(a.X + t * dx, a.Y + t * dy);
            return Distance(point, projection);
        }

        private float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private RectangleF NormalizeRect(PointF first, PointF second)
        {
            var left = Math.Min(first.X, second.X);
            var top = Math.Min(first.Y, second.Y);
            var right = Math.Max(first.X, second.X);
            var bottom = Math.Max(first.Y, second.Y);
            return new RectangleF(left, top, right - left, bottom - top);
        }

        private List<PointF> BuildOrthogonalPoints(PointF start, PointF end)
        {
            if (Math.Abs(end.X - start.X) >= Math.Abs(end.Y - start.Y))
            {
                return new List<PointF>
                {
                    start,
                    new PointF(end.X, start.Y),
                    end
                };
            }
            return new List<PointF>
            {
                start,
                new PointF(start.X, end.Y),
                end
            };
        }

        private void RenameSelected()
        {
            if (selectedItem == null)
            {
                SetStatus("Select an object first");
                return;
            }
            if (selectedItem.Type == "wire")
            {
                SetStatus("Wire has no label");
                return;
            }
            var value = PromptDialog.Show("Enter label:", "Rename", selectedItem.Label ?? string.Empty);
            if (value == null)
            {
                return;
            }
            selectedItem.Label = value;
            SetStatus("Label updated");
            canvas.Invalidate();
        }

        private void DeleteSelected()
        {
            if (selectedItem == null)
            {
                return;
            }
            items.Remove(selectedItem);
            selectedItem = null;
            SetStatus("Deleted selection");
            canvas.Invalidate();
        }

        private void ApplyStyleToSelected()
        {
            if (selectedItem == null)
            {
                SetStatus("Select an object first");
                return;
            }
            if (selectedItem.Type == "rect" || selectedItem.Type == "ellipse")
            {
                selectedItem.FillColor = ColorTranslator.ToHtml(fillColor);
                selectedItem.LineColor = ColorTranslator.ToHtml(lineColor);
                selectedItem.TextColor = ColorTranslator.ToHtml(textColor);
                selectedItem.LineWidth = lineWidth;
            }
            else if (selectedItem.Type == "wire")
            {
                selectedItem.LineColor = ColorTranslator.ToHtml(lineColor);
                selectedItem.LineWidth = lineWidth;
                selectedItem.Arrow = wireArrow;
            }
            else if (selectedItem.Type == "text")
            {
                selectedItem.TextColor = ColorTranslator.ToHtml(textColor);
            }
            SetStatus("Style applied");
            canvas.Invalidate();
        }

        private void NewFile()
        {
            if (items.Count > 0)
            {
                var result = MessageBox.Show(
                    this,
                    "Start a new diagram? Unsaved changes may be lost.",
                    "ak-diagrams",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }
            items.Clear();
            selectedItem = null;
            drawStart = null;
            wireStart = null;
            dragging = false;
            currentPath = string.Empty;
            SetStatus("New diagram");
            canvas.Invalidate();
        }

        private string ResolveDefaultDialogDirectory(string appDirectory)
        {
            var configuredPath = Environment.GetEnvironmentVariable("AK_DIAGRAMS_DEFAULT_DIR");
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            try
            {
                string resolvedPath;
                if (Path.IsPathRooted(configuredPath))
                {
                    resolvedPath = Path.GetFullPath(configuredPath);
                }
                else
                {
                    resolvedPath = Path.GetFullPath(Path.Combine(appDirectory, configuredPath));
                }

                if (!Directory.Exists(resolvedPath))
                {
                    Directory.CreateDirectory(resolvedPath);
                }

                return resolvedPath;
            }
            catch
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        private string GetDialogInitialDirectory()
        {
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var folder = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                {
                    return folder;
                }
            }

            if (!string.IsNullOrWhiteSpace(defaultDialogDirectory) && Directory.Exists(defaultDialogDirectory))
            {
                return defaultDialogDirectory;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void ConfigureDialogInitialDirectory(FileDialog dialog)
        {
            var initialDirectory = GetDialogInitialDirectory();
            if (!string.IsNullOrWhiteSpace(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }
        }

        private void OpenFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "AK Diagrams (*.akd;*.json)|*.akd;*.json|All files (*.*)|*.*";
                dialog.Title = "Open ak-diagrams file";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    using (var stream = File.OpenRead(dialog.FileName))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(DiagramDocument));
                        var document = serializer.ReadObject(stream) as DiagramDocument;
                        if (document == null)
                        {
                            throw new InvalidDataException("Invalid diagram file.");
                        }
                        items.Clear();
                        items.AddRange(document.Items ?? new List<DiagramItem>());
                        selectedItem = null;
                        currentPath = dialog.FileName;
                        SetStatus("Opened " + Path.GetFileName(dialog.FileName));
                        canvas.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not open file:\n" + ex.Message, "ak-diagrams", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                SaveFileAs();
                return;
            }
            WriteDocument(currentPath);
        }

        private void SaveFileAs()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "AK Diagrams (*.akd)|*.akd|JSON (*.json)|*.json|All files (*.*)|*.*";
                dialog.Title = "Save ak-diagrams file";
                dialog.DefaultExt = "akd";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                WriteDocument(dialog.FileName);
            }
        }

        private void WriteDocument(string path)
        {
            try
            {
                var document = new DiagramDocument
                {
                    App = "ak-diagrams",
                    Version = 1,
                    Items = items
                };
                using (var stream = File.Create(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(DiagramDocument));
                    serializer.WriteObject(stream, document);
                }
                currentPath = path;
                SetStatus("Saved " + Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save file:\n" + ex.Message, "ak-diagrams", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportSvg()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "SVG (*.svg)|*.svg";
                dialog.Title = "Export SVG";
                dialog.DefaultExt = "svg";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    File.WriteAllText(dialog.FileName, BuildSvg(), Encoding.UTF8);
                    SetStatus("Exported " + Path.GetFileName(dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not export SVG:\n" + ex.Message, "ak-diagrams", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string BuildSvg()
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.AppendLine(string.Format(
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0}\" height=\"{1}\" viewBox=\"0 0 {0} {1}\">",
                canvas.Width,
                canvas.Height
            ));
            builder.AppendLine("<defs>");
            builder.AppendLine("<marker id=\"ak-arrow\" markerWidth=\"10\" markerHeight=\"8\" refX=\"9\" refY=\"4\" orient=\"auto\" markerUnits=\"strokeWidth\">");
            builder.AppendLine("<path d=\"M0,0 L10,4 L0,8 Z\" fill=\"context-stroke\"/>");
            builder.AppendLine("</marker>");
            builder.AppendLine("</defs>");

            foreach (var item in items)
            {
                if (item.Type == "rect" && item.Points.Count >= 2)
                {
                    var rect = GetRect(item);
                    builder.AppendLine(string.Format(
                        "<rect x=\"{0:F2}\" y=\"{1:F2}\" width=\"{2:F2}\" height=\"{3:F2}\" fill=\"{4}\" stroke=\"{5}\" stroke-width=\"{6:F2}\"/>",
                        rect.X, rect.Y, rect.Width, rect.Height, item.FillColor, item.LineColor, item.LineWidth
                    ));
                    builder.AppendLine(string.Format(
                        "<text x=\"{0:F2}\" y=\"{1:F2}\" fill=\"{2}\" font-family=\"Times New Roman\" font-size=\"18\" text-anchor=\"middle\" dominant-baseline=\"middle\">{3}</text>",
                        rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, item.TextColor, EscapeXml(item.Label ?? string.Empty)
                    ));
                    continue;
                }
                if (item.Type == "ellipse" && item.Points.Count >= 2)
                {
                    var rect = GetRect(item);
                    var cx = rect.X + rect.Width / 2f;
                    var cy = rect.Y + rect.Height / 2f;
                    builder.AppendLine(string.Format(
                        "<ellipse cx=\"{0:F2}\" cy=\"{1:F2}\" rx=\"{2:F2}\" ry=\"{3:F2}\" fill=\"{4}\" stroke=\"{5}\" stroke-width=\"{6:F2}\"/>",
                        cx, cy, rect.Width / 2f, rect.Height / 2f, item.FillColor, item.LineColor, item.LineWidth
                    ));
                    builder.AppendLine(string.Format(
                        "<text x=\"{0:F2}\" y=\"{1:F2}\" fill=\"{2}\" font-family=\"Times New Roman\" font-size=\"18\" text-anchor=\"middle\" dominant-baseline=\"middle\">{3}</text>",
                        cx, cy, item.TextColor, EscapeXml(item.Label ?? string.Empty)
                    ));
                    continue;
                }
                if (item.Type == "wire" && item.Points.Count >= 2)
                {
                    var points = string.Join(" ", item.Points.Select(point => string.Format("{0:F2},{1:F2}", point.X, point.Y)));
                    var marker = item.Arrow ? " marker-end=\"url(#ak-arrow)\"" : string.Empty;
                    builder.AppendLine(string.Format(
                        "<polyline points=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"{2:F2}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{3}/>",
                        points, item.LineColor, item.LineWidth, marker
                    ));
                    continue;
                }
                if (item.Type == "text" && item.Points.Count >= 1)
                {
                    builder.AppendLine(string.Format(
                        "<text x=\"{0:F2}\" y=\"{1:F2}\" fill=\"{2}\" font-family=\"Times New Roman\" font-size=\"18\" text-anchor=\"middle\" dominant-baseline=\"middle\">{3}</text>",
                        item.Points[0].X, item.Points[0].Y, item.TextColor, EscapeXml(item.Label ?? string.Empty)
                    ));
                }
            }

            builder.AppendLine("</svg>");
            return builder.ToString();
        }

        private string EscapeXml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private Color ParseColor(string value, Color fallback)
        {
            try
            {
                return ColorTranslator.FromHtml(value);
            }
            catch
            {
                return fallback;
            }
        }
    }

    public static class PromptDialog
    {
        public static string Show(string text, string caption, string defaultValue = "")
        {
            using (var form = new Form())
            using (var textLabel = new Label())
            using (var inputBox = new TextBox())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                form.Text = caption;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(420, 130);
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = false;

                textLabel.Text = text;
                textLabel.SetBounds(10, 12, 390, 20);

                inputBox.Text = defaultValue ?? string.Empty;
                inputBox.SetBounds(10, 38, 390, 24);

                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.SetBounds(230, 80, 80, 28);

                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.SetBounds(320, 80, 80, 28);

                form.Controls.AddRange(new Control[] { textLabel, inputBox, okButton, cancelButton });
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                return form.ShowDialog() == DialogResult.OK ? inputBox.Text : null;
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DiagramForm());
        }
    }
}
