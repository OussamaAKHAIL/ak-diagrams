using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace AKDiagrams
{
    public static class AppInfo
    {
        public const string Name = "ak-diagrams";
        public const string Version = "3.0.1";
    }

    public enum ToolKind
    {
        Select,
        Block,
        Device,
        Wire,
        Text,
        Image,
        ColorPicker
    }

    public enum DragKind
    {
        None,
        Move,
        Resize,
        WirePoint
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
    public class FloatRect
    {
        [DataMember]
        public float X { get; set; }

        [DataMember]
        public float Y { get; set; }

        [DataMember]
        public float Width { get; set; }

        [DataMember]
        public float Height { get; set; }

        public FloatRect()
        {
        }

        public FloatRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectangleF ToRectangleF()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        public static FloatRect FromRectangleF(RectangleF rect)
        {
            return new FloatRect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    [DataContract]
    public class WireConnection
    {
        [DataMember]
        public string ElementId { get; set; }

        [DataMember]
        public float AnchorX { get; set; }

        [DataMember]
        public float AnchorY { get; set; }

        [DataMember]
        public string Side { get; set; }

        [DataMember]
        public int AnchorIndex { get; set; }

        public WireConnection()
        {
            ElementId = string.Empty;
            Side = string.Empty;
            AnchorIndex = -1;
        }

        public WireConnection(string elementId, float anchorX, float anchorY, string side)
        {
            ElementId = elementId;
            AnchorX = anchorX;
            AnchorY = anchorY;
            Side = side;
            AnchorIndex = -1;
        }
    }

    [DataContract]
    public class DiagramItem
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public FloatRect Bounds { get; set; }

        [DataMember]
        public List<FloatPoint> Points { get; set; }

        [DataMember]
        public string Label { get; set; }

        [DataMember]
        public string FillColor { get; set; }

        [DataMember]
        public string OutlineColor { get; set; }

        [DataMember]
        public string LineColor { get; set; }

        [DataMember]
        public string TextColor { get; set; }

        [DataMember]
        public float LineWidth { get; set; }

        [DataMember]
        public bool Arrow { get; set; }

        [DataMember]
        public string LineStyle { get; set; }

        [DataMember]
        public string FontFamily { get; set; }

        [DataMember]
        public string WireMode { get; set; }

        [DataMember]
        public string ImageDataBase64 { get; set; }

        [DataMember]
        public string ImageExtension { get; set; }

        [DataMember]
        public string SourceFileName { get; set; }

        [DataMember]
        public WireConnection StartConnection { get; set; }

        [DataMember]
        public WireConnection EndConnection { get; set; }

        [DataMember]
        public List<FloatPoint> ConnectionPoints { get; set; }

        public DiagramItem()
        {
            Id = string.Empty;
            Type = string.Empty;
            Category = "General";
            Bounds = new FloatRect();
            Points = new List<FloatPoint>();
            Label = string.Empty;
            FillColor = "#F2F5FF";
            OutlineColor = "#111111";
            LineColor = "#111111";
            TextColor = "#111111";
            LineWidth = 2f;
            Arrow = false;
            LineStyle = "solid";
            FontFamily = "Times New Roman";
            WireMode = "orthogonal";
            ImageDataBase64 = string.Empty;
            ImageExtension = "png";
            SourceFileName = string.Empty;
            StartConnection = null;
            EndConnection = null;
            ConnectionPoints = new List<FloatPoint>();
        }
    }

    [DataContract]
    public class DiagramDocument
    {
        [DataMember]
        public string App { get; set; }

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public string BackgroundColor { get; set; }

        [DataMember]
        public List<DiagramItem> Items { get; set; }

        public DiagramDocument()
        {
            App = AppInfo.Name;
            Version = AppInfo.Version;
            BackgroundColor = "#F8F8F8";
            Items = new List<DiagramItem>();
        }
    }

    public class ElementDefinition
    {
        public string Name { get; private set; }
        public string Category { get; private set; }
        public ToolKind Tool { get; private set; }

        public ElementDefinition(string name, string category, ToolKind tool)
        {
            Name = name;
            Category = category;
            Tool = tool;
        }
    }

    public class ComponentButtonDefinition
    {
        public string Text { get; private set; }
        public EventHandler Click { get; private set; }

        public ComponentButtonDefinition(string text, EventHandler click)
        {
            Text = text;
            Click = click;
        }
    }

    public static class ElementLibrary
    {
        public static readonly List<ElementDefinition> Items = new List<ElementDefinition>
        {
            new ElementDefinition("Block", "Core", ToolKind.Block),
            new ElementDefinition("Device", "Core", ToolKind.Device),
            new ElementDefinition("Wire", "Core", ToolKind.Wire),
            new ElementDefinition("Text", "Annotation", ToolKind.Text),
            new ElementDefinition("Image", "Reference", ToolKind.Image)
        };
    }

    public class DiagramCanvas : Panel
    {
        public DiagramCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(248, 248, 248);
            Cursor = Cursors.Arrow;
        }
    }

    public static class DotEnvLoader
    {
        public static bool Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
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
                if (key.Length == 0)
                {
                    continue;
                }

                if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            }
            return true;
        }
    }

    public class DiagramForm : Form
    {
        private readonly DiagramCanvas canvas = new DiagramCanvas();
        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly StatusStrip statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();
        private readonly Panel componentsPanel = new Panel();
        private readonly Panel propertiesPanel = new Panel();
        private readonly Dictionary<ToolKind, ToolStripButton> toolButtons = new Dictionary<ToolKind, ToolStripButton>();
        private readonly List<DiagramItem> items = new List<DiagramItem>();
        private readonly List<ComponentDefinition> customComponents = new List<ComponentDefinition>();
        private readonly Dictionary<string, Image> imageCache = new Dictionary<string, Image>();

        private readonly ToolStripButton fillColorButton = new ToolStripButton("Fill");
        private readonly ToolStripButton outlineColorButton = new ToolStripButton("Outline");
        private readonly ToolStripButton lineColorButton = new ToolStripButton("Line");
        private readonly ToolStripButton textColorButton = new ToolStripButton("Text");
        private readonly ToolStripButton backgroundColorButton = new ToolStripButton("Background");
        private readonly ToolStripButton undoButton = new ToolStripButton("←");
        private readonly ToolStripButton redoButton = new ToolStripButton("→");
        private readonly ToolStripButton arrowToggleButton = new ToolStripButton("Arrow");
        private readonly ToolStripButton snapToggleButton = new ToolStripButton("Snap");
        private readonly ToolStripButton gridToggleButton = new ToolStripButton("Grid");
        private FlowLayoutPanel componentsListPanel;

        private NumericUpDown xBox;
        private NumericUpDown yBox;
        private NumericUpDown widthBox;
        private NumericUpDown heightBox;
        private NumericUpDown lineWidthBox;
        private ComboBox lineStyleCombo;
        private ComboBox fontCombo;
        private Button fillPanelButton;
        private Button outlinePanelButton;
        private Button linePanelButton;
        private Button textPanelButton;
        private Button backgroundPanelButton;

        private ToolKind currentTool = ToolKind.Select;
        private DiagramItem selectedItem;
        private DragKind dragKind = DragKind.None;
        private string resizeHandle = string.Empty;
        private int activeWirePointIndex = -1;
        private PointF dragStart;
        private PointF currentPointer;
        private PointF? drawStart;
        private bool movedInDrag;
        private bool updatingProperties;
        private ToolKind toolBeforeColorPick = ToolKind.Select;
        private string activeColorPickTarget = "fill";

        private readonly List<FloatPoint> pendingWirePoints = new List<FloatPoint>();
        private WireConnection pendingStartConnection;
        private DiagramItem copiedItem;
        private readonly List<string> undoStack = new List<string>();
        private readonly List<string> redoStack = new List<string>();
        private const int MaxHistoryStates = 60;
        private bool restoringHistory;

        private bool snapToGrid = true;
        private bool showGrid = true;
        private bool wireArrow = true;
        private bool forceSquareShape;
        private readonly int gridSize = 20;
        private int nextItemNumber = 1;

        private Color fillColor = Color.FromArgb(242, 245, 255);
        private Color outlineColor = Color.FromArgb(17, 17, 17);
        private Color lineColor = Color.FromArgb(17, 17, 17);
        private Color textColor = Color.FromArgb(17, 17, 17);
        private Color backgroundColor = Color.FromArgb(248, 248, 248);
        private float lineWidth = 2f;
        private string lineStyle = "solid";
        private string fontFamily = "Times New Roman";
        private string currentPath = string.Empty;
        private readonly string defaultDialogDirectory;
        private float viewScale = 1f;
        private PointF viewOffset = new PointF(0f, 0f);

        public DiagramForm()
        {
            Text = AppInfo.Name + " " + AppInfo.Version;
            Width = 1440;
            Height = 900;
            MinimumSize = new Size(1120, 720);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            LoadApplicationIcon();

            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var envBaseDirectory = appDirectory;
            var appEnvPath = Path.Combine(appDirectory, ".env");
            if (DotEnvLoader.Load(appEnvPath))
            {
                envBaseDirectory = appDirectory;
            }

            var workingDirectory = Directory.GetCurrentDirectory();
            var workingEnvPath = Path.Combine(workingDirectory, ".env");
            if (!string.Equals(Path.GetFullPath(appEnvPath), Path.GetFullPath(workingEnvPath), StringComparison.OrdinalIgnoreCase) && DotEnvLoader.Load(workingEnvPath))
            {
                envBaseDirectory = workingDirectory;
            }

            defaultDialogDirectory = ResolveDefaultDialogDirectory(envBaseDirectory);

            InitializeMenuAndToolbar();
            InitializeComponentsPanel();
            LoadCustomComponents();
            InitializeCanvas();
            InitializePropertiesPanel();
            InitializeStatusBar();
            UpdateColorButtons();
            UpdatePropertiesPanel();
            ClearHistory();
            SetStatus("Ready");
        }

        private void InitializeMenuAndToolbar()
        {
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("New", null, delegate { NewFile(); });
            fileMenu.DropDownItems.Add("Open", null, delegate { OpenFile(); });
            fileMenu.DropDownItems.Add("Save", null, delegate { SaveFile(); });
            fileMenu.DropDownItems.Add("Save As", null, delegate { SaveFileAs(); });
            fileMenu.DropDownItems.Add("-");
            fileMenu.DropDownItems.Add("Export SVG", null, delegate { ExportSvg(); });
            fileMenu.DropDownItems.Add("Export PNG", null, delegate { ExportPng(); });
            fileMenu.DropDownItems.Add("Export PDF", null, delegate { ExportPdf(); });
            fileMenu.DropDownItems.Add("-");
            fileMenu.DropDownItems.Add("Exit", null, delegate { Close(); });
            menuStrip.Items.Add(fileMenu);

            var insertMenu = new ToolStripMenuItem("Insert");
            insertMenu.DropDownItems.Add("Image", null, delegate { InsertImageAtCenter(); });
            menuStrip.Items.Add(insertMenu);

            var componentsMenu = new ToolStripMenuItem("Components");
            componentsMenu.CheckOnClick = true;
            componentsMenu.Checked = true;
            componentsMenu.CheckedChanged += delegate { ToggleComponentsPanel(componentsMenu.Checked); };
            menuStrip.Items.Add(componentsMenu);

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Dock = DockStyle.Top;

            AddToolStripButton("New", delegate { NewFile(); }, "Start a new diagram");
            AddToolStripButton("Open", delegate { OpenFile(); }, "Open an ak-diagrams project");
            AddToolStripButton("Save", delegate { SaveFile(); }, "Save this project");
            AddToolStripButton("SVG", delegate { ExportSvg(); }, "Export vector SVG");
            AddToolStripButton("PNG", delegate { ExportPng(); }, "Export PNG image");
            AddToolStripButton("PDF", delegate { ExportPdf(); }, "Export PDF");

            toolStrip.Items.Add(new ToolStripSeparator());

            undoButton.ToolTipText = "Undo";
            undoButton.Enabled = false;
            undoButton.Click += delegate { Undo(); };
            redoButton.ToolTipText = "Redo";
            redoButton.Enabled = false;
            redoButton.Click += delegate { Redo(); };
            toolStrip.Items.Add(undoButton);
            toolStrip.Items.Add(redoButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            AddToolButton("Select", ToolKind.Select);
            foreach (var definition in ElementLibrary.Items)
            {
                AddToolButton(definition.Name, definition.Tool);
            }

            toolStrip.Items.Add(new ToolStripSeparator());
            AddToolStripButton("Finish Wire", delegate { FinishPendingWire(); }, "Finish the active wire");
            AddToolStripButton("Cancel Wire", delegate { CancelPendingWire(); }, "Cancel the active wire");

            toolStrip.Items.Add(new ToolStripSeparator());

            fillColorButton.Click += delegate { ShowColorMenu("fill", fillColorButton); };
            outlineColorButton.Click += delegate { ShowColorMenu("outline", outlineColorButton); };
            lineColorButton.Click += delegate { ShowColorMenu("line", lineColorButton); };
            textColorButton.Click += delegate { ShowColorMenu("text", textColorButton); };
            backgroundColorButton.Click += delegate { ShowColorMenu("background", backgroundColorButton); };
            toolStrip.Items.Add(fillColorButton);
            toolStrip.Items.Add(outlineColorButton);
            toolStrip.Items.Add(lineColorButton);
            toolStrip.Items.Add(textColorButton);
            toolStrip.Items.Add(backgroundColorButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            arrowToggleButton.CheckOnClick = true;
            arrowToggleButton.Checked = wireArrow;
            arrowToggleButton.CheckedChanged += delegate { wireArrow = arrowToggleButton.Checked; };
            arrowToggleButton.ToolTipText = "Toggle arrows for new and selected wires";
            toolStrip.Items.Add(arrowToggleButton);

            snapToggleButton.CheckOnClick = true;
            snapToggleButton.Checked = snapToGrid;
            snapToggleButton.CheckedChanged += delegate { snapToGrid = snapToggleButton.Checked; };
            snapToggleButton.ToolTipText = "Snap points to grid";
            toolStrip.Items.Add(snapToggleButton);

            gridToggleButton.CheckOnClick = true;
            gridToggleButton.Checked = showGrid;
            gridToggleButton.CheckedChanged += delegate
            {
                showGrid = gridToggleButton.Checked;
                canvas.Invalidate();
            };
            gridToggleButton.ToolTipText = "Show grid";
            toolStrip.Items.Add(gridToggleButton);

            Controls.Add(toolStrip);
        }

        private void InitializePropertiesPanel()
        {
            propertiesPanel.Dock = DockStyle.Right;
            propertiesPanel.Width = 290;
            propertiesPanel.Padding = new Padding(12);
            propertiesPanel.BackColor = Color.FromArgb(245, 245, 245);
            Controls.Add(propertiesPanel);

            var title = new Label();
            title.Text = "Properties";
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(12, 12);
            propertiesPanel.Controls.Add(title);

            var y = 48;
            xBox = AddNumberField("X", ref y);
            yBox = AddNumberField("Y", ref y);
            widthBox = AddNumberField("Width", ref y);
            heightBox = AddNumberField("Height", ref y);
            lineWidthBox = AddNumberField("Line width", ref y);
            lineWidthBox.Minimum = 1;
            lineWidthBox.Maximum = 24;

            var lineStyleLabel = new Label();
            lineStyleLabel.Text = "Line style";
            lineStyleLabel.AutoSize = true;
            lineStyleLabel.Location = new Point(12, y + 5);
            propertiesPanel.Controls.Add(lineStyleLabel);

            lineStyleCombo = new ComboBox();
            lineStyleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            lineStyleCombo.Items.AddRange(new object[] { "Solid", "Dashed", "Dotted" });
            lineStyleCombo.SelectedIndex = 0;
            lineStyleCombo.SetBounds(95, y, 175, 26);
            lineStyleCombo.SelectedIndexChanged += delegate
            {
                if (updatingProperties)
                {
                    return;
                }

                var style = GetSelectedLineStyle();
                if (selectedItem != null)
                {
                    selectedItem.LineStyle = style;
                    canvas.Invalidate();
                    RecordHistory();
                }
                else
                {
                    lineStyle = style;
                }
            };
            propertiesPanel.Controls.Add(lineStyleCombo);
            y += 38;

            var fontLabel = new Label();
            fontLabel.Text = "Font";
            fontLabel.AutoSize = true;
            fontLabel.Location = new Point(12, y + 5);
            propertiesPanel.Controls.Add(fontLabel);

            fontCombo = new ComboBox();
            fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            fontCombo.Items.AddRange(new object[] { "Times New Roman", "Cambria", "Georgia" });
            fontCombo.SelectedIndex = 0;
            fontCombo.SetBounds(95, y, 175, 26);
            fontCombo.SelectedIndexChanged += delegate
            {
                if (updatingProperties)
                {
                    return;
                }
                fontFamily = Convert.ToString(fontCombo.SelectedItem);
                if (selectedItem != null && selectedItem.Type != "wire" && selectedItem.Type != "image")
                {
                    selectedItem.FontFamily = fontFamily;
                    canvas.Invalidate();
                }
            };
            propertiesPanel.Controls.Add(fontCombo);
            y += 38;

            fillPanelButton = AddPanelColorButton("Fill", ref y, "fill");
            outlinePanelButton = AddPanelColorButton("Outline", ref y, "outline");
            linePanelButton = AddPanelColorButton("Line", ref y, "line");
            textPanelButton = AddPanelColorButton("Text", ref y, "text");
            backgroundPanelButton = AddPanelColorButton("Background", ref y, "background");

            var renameButton = new Button();
            renameButton.Text = "Rename";
            renameButton.SetBounds(12, y + 8, 126, 30);
            renameButton.Click += delegate { RenameSelected(); };
            propertiesPanel.Controls.Add(renameButton);

            var deleteButton = new Button();
            deleteButton.Text = "Delete";
            deleteButton.SetBounds(144, y + 8, 126, 30);
            deleteButton.Click += delegate { DeleteSelected(); };
            propertiesPanel.Controls.Add(deleteButton);
        }

        private void InitializeComponentsPanel()
        {
            componentsPanel.Dock = DockStyle.Left;
            componentsPanel.Width = 280;
            componentsPanel.Padding = new Padding(10);
            componentsPanel.BackColor = Color.FromArgb(246, 246, 246);
            componentsPanel.Visible = true;
            componentsPanel.BorderStyle = BorderStyle.FixedSingle;
            componentsPanel.AutoScroll = true;

            var headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 36;
            headerPanel.BackColor = componentsPanel.BackColor;

            var label = new Label();
            label.Text = "Components";
            label.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            label.AutoSize = true;
            label.Location = new Point(10, 8);
            headerPanel.Controls.Add(label);
            componentsPanel.Controls.Add(headerPanel);

            componentsListPanel = new FlowLayoutPanel();
            componentsListPanel.FlowDirection = FlowDirection.TopDown;
            componentsListPanel.WrapContents = false;
            componentsListPanel.AutoScroll = true;
            componentsListPanel.Dock = DockStyle.Fill;
            componentsListPanel.Padding = new Padding(0, 0, 0, 0);
            componentsPanel.Controls.Add(componentsListPanel);

            Controls.Add(componentsPanel);
            componentsPanel.BringToFront();
            RefreshComponentsPanel();
        }

        private void ToggleComponentsPanel(bool visible)
        {
            componentsPanel.Visible = visible;
            canvas.Invalidate();
        }

        private void LoadCustomComponents()
        {
            customComponents.Clear();
            customComponents.AddRange(ComponentRepository.LoadInstalledComponents());
            RefreshComponentsPanel();
        }

        private void SaveCustomComponents()
        {
            try
            {
                var defaultPath = ComponentRepository.GetDefaultPackagePath();
                ComponentRepository.SavePackage(defaultPath, "ak-diagrams custom components", customComponents);
            }
            catch
            {
            }
        }

        private void RefreshComponentsPanel()
        {
            if (componentsListPanel == null)
            {
                return;
            }

            componentsListPanel.SuspendLayout();
            componentsListPanel.Controls.Clear();
            AddComponentsSection("Shapes", new[]
            {
                new ComponentButtonDefinition("Rectangle", delegate
                {
                    forceSquareShape = false;
                    SetTool(ToolKind.Block);
                }),
                new ComponentButtonDefinition("Circle", delegate
                {
                    forceSquareShape = false;
                    SetTool(ToolKind.Device);
                }),
                new ComponentButtonDefinition("Square", delegate
                {
                    SetTool(ToolKind.Block, true);
                    forceSquareShape = true;
                })
            });

            AddComponentsSection("Lines", new[]
            {
                new ComponentButtonDefinition("Solid Line", delegate
                {
                    SetTool(ToolKind.Wire);
                    SetSelectedLineStyle("solid");
                }),
                new ComponentButtonDefinition("Dashed Line", delegate
                {
                    SetTool(ToolKind.Wire);
                    SetSelectedLineStyle("dash");
                }),
                new ComponentButtonDefinition("Dotted Line", delegate
                {
                    SetTool(ToolKind.Wire);
                    SetSelectedLineStyle("dot");
                })
            });

            AddComponentsSection("Custom", new[]
            {
                new ComponentButtonDefinition("Add New...", delegate
                {
                    AddCustomComponent();
                }),
                new ComponentButtonDefinition("Import Zip...", delegate
                {
                    ImportCustomComponents();
                }),
                new ComponentButtonDefinition("Export Zip...", delegate
                {
                    ExportCustomComponents();
                })
            });

            if (customComponents.Count > 0)
            {
                foreach (var categoryGroup in customComponents.GroupBy(component => string.IsNullOrWhiteSpace(component.Category) ? "Custom" : component.Category).OrderBy(group => group.Key))
                {
                    var categoryButtons = new List<ComponentButtonDefinition>();
                    foreach (var component in categoryGroup.OrderBy(component => component.Name))
                    {
                        var captured = component;
                        categoryButtons.Add(new ComponentButtonDefinition(component.Name, delegate
                        {
                            InsertCustomComponent(captured, ScreenToDiagram(new PointF(canvas.Width / 2f, canvas.Height / 2f)));
                        }));
                    }
                    AddComponentsSection(categoryGroup.Key, categoryButtons.ToArray());
                }
            }

            if (customComponents.Count == 0)
            {
                var hint = new Label();
                hint.Text = "No custom components yet.";
                hint.AutoSize = true;
                hint.Padding = new Padding(4, 10, 4, 4);
                componentsListPanel.Controls.Add(hint);
            }

            componentsListPanel.ResumeLayout();
        }

        private void AddComponentsSection(string title, ComponentButtonDefinition[] buttons)
        {
            var group = new GroupBox();
            group.Text = title;
            group.AutoSize = true;
            group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            group.Width = Math.Max(220, componentsPanel.ClientSize.Width - 24);
            group.Padding = new Padding(10);
            group.Margin = new Padding(0, 0, 0, 10);

            var panel = new FlowLayoutPanel();
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.AutoSize = true;
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(0);

            foreach (var entry in buttons)
            {
                var button = new Button();
                button.Text = entry.Text;
                button.Width = Math.Max(180, componentsPanel.ClientSize.Width - 56);
                button.Height = 30;
                button.TextAlign = ContentAlignment.MiddleLeft;
                button.Click += entry.Click;
                panel.Controls.Add(button);
            }

            group.Controls.Add(panel);
            componentsListPanel.Controls.Add(group);
        }

        private void AddCustomComponent()
        {
            using (var editor = new ComponentEditorForm())
            {
                if (editor.ShowDialog(this) != DialogResult.OK || editor.Result == null)
                {
                    return;
                }

                customComponents.Add(editor.Result);
                SaveCustomComponents();
                RefreshComponentsPanel();
                SetStatus("Custom component added");
            }
        }

        private void ImportCustomComponents()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Component package (*.zip)|*.zip|All files (*.*)|*.*";
                dialog.Title = "Import component package";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var imported = ComponentRepository.LoadPackage(dialog.FileName);
                if (imported.Count == 0)
                {
                    MessageBox.Show(this, "No components were found in that package.", AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                foreach (var component in imported)
                {
                    customComponents.Add(component);
                }

                SaveCustomComponents();
                RefreshComponentsPanel();
                SetStatus("Imported " + imported.Count + " component(s)");
            }
        }

        private void ExportCustomComponents()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Component package (*.zip)|*.zip";
                dialog.Title = "Export component package";
                dialog.DefaultExt = "zip";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ComponentRepository.SavePackage(dialog.FileName, "ak-diagrams custom components", customComponents);
                SetStatus("Exported components");
            }
        }

        private NumericUpDown AddNumberField(string label, ref int y)
        {
            var textLabel = new Label();
            textLabel.Text = label;
            textLabel.AutoSize = true;
            textLabel.Location = new Point(12, y + 5);
            propertiesPanel.Controls.Add(textLabel);

            var number = new NumericUpDown();
            number.DecimalPlaces = 0;
            number.Minimum = -10000;
            number.Maximum = 10000;
            number.Increment = 1;
            number.SetBounds(95, y, 175, 26);
            number.ValueChanged += delegate
            {
                if (!updatingProperties)
                {
                    ApplyNumericProperties();
                }
            };
            propertiesPanel.Controls.Add(number);
            y += 34;
            return number;
        }

        private Button AddPanelColorButton(string label, ref int y, string target)
        {
            var textLabel = new Label();
            textLabel.Text = label;
            textLabel.AutoSize = true;
            textLabel.Location = new Point(12, y + 6);
            propertiesPanel.Controls.Add(textLabel);

            var button = new Button();
            button.Text = "Choose";
            button.SetBounds(95, y, 175, 28);
            button.Click += delegate { ShowColorMenu(target, button); };
            propertiesPanel.Controls.Add(button);
            y += 34;
            return button;
        }

        private void InitializeCanvas()
        {
            canvas.TabStop = true;
            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.DoubleClick += Canvas_DoubleClick;
            canvas.MouseEnter += delegate { canvas.Focus(); };
            canvas.Resize += delegate { canvas.Invalidate(); };
            Controls.Add(canvas);
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
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                Redo();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveFile();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                OpenFile();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                NewFile();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelected();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteCopied();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                CopySelected();
                PasteCopied();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.D0)
            {
                ResetZoom();
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
            {
                ZoomAt(new PointF(canvas.Width / 2f, canvas.Height / 2f), 1.15f);
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                ZoomAt(new PointF(canvas.Width / 2f, canvas.Height / 2f), 1f / 1.15f);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && pendingWirePoints.Count >= 2)
            {
                FinishPendingWire();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelActiveGesture();
                e.Handled = true;
            }
        }

        private void AddToolStripButton(string text, EventHandler click, string tooltip)
        {
            var button = new ToolStripButton(text);
            button.ToolTipText = tooltip;
            button.Click += click;
            toolStrip.Items.Add(button);
        }

        private void AddToolButton(string text, ToolKind kind)
        {
            if (toolButtons.ContainsKey(kind))
            {
                return;
            }

            var button = new ToolStripButton(text);
            button.CheckOnClick = true;
            button.Checked = kind == ToolKind.Select;
            button.ToolTipText = text;
            button.Click += delegate { SetTool(kind); };
            toolButtons[kind] = button;
            toolStrip.Items.Add(button);
        }

        private void SetTool(ToolKind kind)
        {
            SetTool(kind, false);
        }

        private void SetTool(ToolKind kind, bool preserveShapePreset)
        {
            if (!preserveShapePreset)
            {
                forceSquareShape = false;
            }
            currentTool = kind;
            foreach (var pair in toolButtons)
            {
                pair.Value.Checked = pair.Key == kind;
            }
            drawStart = null;
            dragKind = DragKind.None;
            resizeHandle = string.Empty;
            activeWirePointIndex = -1;
            canvas.Cursor = kind == ToolKind.Select ? Cursors.Arrow : Cursors.Cross;
            SetStatus("Tool: " + kind);
            canvas.Invalidate();
        }

        private bool IsShapeTool(ToolKind kind)
        {
            return kind == ToolKind.Block || kind == ToolKind.Device;
        }

        private RectangleF NormalizeSquareRect(PointF start, PointF end)
        {
            var rect = NormalizeRect(start, end);
            var size = Math.Min(rect.Width, rect.Height);
            if (size <= 0f)
            {
                return rect;
            }

            var x = rect.X;
            var y = rect.Y;
            if (end.X < start.X)
            {
                x = start.X - size;
            }
            if (end.Y < start.Y)
            {
                y = start.Y - size;
            }
            return new RectangleF(x, y, size, size);
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        private void LoadApplicationIcon()
        {
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ak-diagrams.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                    return;
                }
            }
            catch
            {
            }
        }

        private string CaptureSnapshot()
        {
            var document = new DiagramDocument();
            document.App = AppInfo.Name;
            document.Version = AppInfo.Version;
            document.BackgroundColor = ColorTranslator.ToHtml(backgroundColor);
            document.Items = items;
            var serializer = new DataContractJsonSerializer(typeof(DiagramDocument));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, document);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private void RestoreSnapshot(string snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshot))
            {
                return;
            }

            restoringHistory = true;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(snapshot)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(DiagramDocument));
                    var document = serializer.ReadObject(stream) as DiagramDocument;
                    if (document == null)
                    {
                        return;
                    }
                    ClearImages();
                    items.Clear();
                    items.AddRange(document.Items ?? new List<DiagramItem>());
                    backgroundColor = ParseColor(document.BackgroundColor, Color.FromArgb(248, 248, 248));
                    selectedItem = null;
                    pendingWirePoints.Clear();
                    pendingStartConnection = null;
                    dragKind = DragKind.None;
                    currentPath = string.Empty;
                    nextItemNumber = GetNextItemNumber();
                    UpdateColorButtons();
                    UpdatePropertiesPanel();
                    canvas.Invalidate();
                    UpdateUndoRedoButtons();
                }
            }
            finally
            {
                restoringHistory = false;
            }
        }

        private void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
            if (!restoringHistory)
            {
                undoStack.Add(CaptureSnapshot());
            }
            UpdateUndoRedoButtons();
        }

        private void RecordHistory()
        {
            if (restoringHistory)
            {
                return;
            }

            var snapshot = CaptureSnapshot();
            if (undoStack.Count > 0 && undoStack[undoStack.Count - 1] == snapshot)
            {
                UpdateUndoRedoButtons();
                return;
            }

            undoStack.Add(snapshot);
            if (undoStack.Count > MaxHistoryStates)
            {
                undoStack.RemoveAt(0);
            }
            redoStack.Clear();
            UpdateUndoRedoButtons();
        }

        private void Undo()
        {
            if (undoStack.Count <= 1)
            {
                return;
            }

            var current = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            redoStack.Add(current);
            RestoreSnapshot(undoStack[undoStack.Count - 1]);
            SetStatus("Undo");
        }

        private void Redo()
        {
            if (redoStack.Count == 0)
            {
                return;
            }

            var snapshot = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            undoStack.Add(snapshot);
            RestoreSnapshot(snapshot);
            SetStatus("Redo");
        }

        private void UpdateUndoRedoButtons()
        {
            if (undoButton != null)
            {
                undoButton.Enabled = undoStack.Count > 1;
            }
            if (redoButton != null)
            {
                redoButton.Enabled = redoStack.Count > 0;
            }
        }

        private PointF ScreenToDiagram(PointF point)
        {
            return new PointF((point.X - viewOffset.X) / viewScale, (point.Y - viewOffset.Y) / viewScale);
        }

        private PointF DiagramToScreen(PointF point)
        {
            return new PointF(point.X * viewScale + viewOffset.X, point.Y * viewScale + viewOffset.Y);
        }

        private void ZoomAt(PointF screenPoint, float factor)
        {
            var oldScale = viewScale;
            var newScale = Clamp(viewScale * factor, 0.25f, 4f);
            if (Math.Abs(newScale - oldScale) < 0.001f)
            {
                return;
            }

            var diagramPoint = ScreenToDiagram(screenPoint);
            viewScale = newScale;
            viewOffset = new PointF(screenPoint.X - diagramPoint.X * viewScale, screenPoint.Y - diagramPoint.Y * viewScale);
            SetStatus("Zoom " + Math.Round(viewScale * 100f) + "%");
            canvas.Invalidate();
        }

        private void ResetZoom()
        {
            viewScale = 1f;
            viewOffset = new PointF(0f, 0f);
            SetStatus("Zoom reset");
            canvas.Invalidate();
        }

        private string CreateItemId()
        {
            var id = "item-" + nextItemNumber.ToString("0000");
            nextItemNumber++;
            return id;
        }

        private DiagramItem FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            foreach (var item in items)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }
            return null;
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            canvas.Focus();
            var screenPoint = new PointF(e.X, e.Y);
            var rawPoint = ScreenToDiagram(screenPoint);
            currentPointer = rawPoint;
            var point = Snap(rawPoint);

            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(screenPoint, point);
                return;
            }

            if (currentTool == ToolKind.ColorPicker)
            {
                var picked = PickCanvasColor(rawPoint);
                ApplyPickedColor(picked);
                return;
            }

            if (currentTool == ToolKind.Select)
            {
                BeginSelectionDrag(rawPoint);
                return;
            }

            if (IsShapeTool(currentTool))
            {
                drawStart = point;
                canvas.Invalidate();
                return;
            }

            if (currentTool == ToolKind.Wire)
            {
                AddPendingWirePoint(point);
                canvas.Invalidate();
                return;
            }

            if (currentTool == ToolKind.Text)
            {
                var text = PromptDialog.Show("Enter text:", "Add Text");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AddText(point, text);
                }
                return;
            }

            if (currentTool == ToolKind.Image)
            {
                InsertImageAt(point);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var rawPoint = ScreenToDiagram(new PointF(e.X, e.Y));
            currentPointer = rawPoint;

            if (dragKind != DragKind.None && selectedItem != null)
            {
                var snapped = Snap(rawPoint);
                if (dragKind == DragKind.Move)
                {
                    var dx = rawPoint.X - dragStart.X;
                    var dy = rawPoint.Y - dragStart.Y;
                    if (Math.Abs(dx) > 0.01f || Math.Abs(dy) > 0.01f)
                    {
                        MoveItem(selectedItem, dx, dy);
                        dragStart = rawPoint;
                        movedInDrag = true;
                        UpdateConnectedWires(selectedItem);
                        UpdatePropertiesPanel();
                        canvas.Invalidate();
                    }
                }
                else if (dragKind == DragKind.Resize)
                {
                    ResizeSelected(snapped);
                    movedInDrag = true;
                    UpdateConnectedWires(selectedItem);
                    UpdatePropertiesPanel();
                    canvas.Invalidate();
                }
                else if (dragKind == DragKind.WirePoint)
                {
                    MoveWirePoint(selectedItem, activeWirePointIndex, snapped);
                    movedInDrag = true;
                    UpdatePropertiesPanel();
                    canvas.Invalidate();
                }
                return;
            }

            if (drawStart != null || pendingWirePoints.Count > 0)
            {
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            var point = Snap(ScreenToDiagram(new PointF(e.X, e.Y)));

            if (IsShapeTool(currentTool) && drawStart != null)
            {
                var rect = forceSquareShape ? NormalizeSquareRect(drawStart.Value, point) : NormalizeRect(drawStart.Value, point);
                drawStart = null;
                if (rect.Width >= 14f && rect.Height >= 14f)
                {
                    AddShape(currentTool == ToolKind.Block ? "rect" : "ellipse", rect);
                }
                canvas.Invalidate();
                return;
            }

            if (dragKind == DragKind.WirePoint && selectedItem != null && selectedItem.Type == "wire")
            {
                TryReconnectDraggedWireTip(point);
            }

            if (dragKind != DragKind.None && movedInDrag)
            {
                RecordHistory();
                SetStatus("Updated selection");
            }

            dragKind = DragKind.None;
            resizeHandle = string.Empty;
            activeWirePointIndex = -1;
            movedInDrag = false;
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            var steps = e.Delta / 120f;
            if (Math.Abs(steps) < 0.01f)
            {
                steps = e.Delta > 0 ? 0.25f : -0.25f;
            }

            var factor = (float)Math.Pow(1.12, steps);
            ZoomAt(new PointF(e.X, e.Y), factor);
        }

        private void Canvas_DoubleClick(object sender, EventArgs e)
        {
            var cursorPoint = canvas.PointToClient(Cursor.Position);
            var point = Snap(ScreenToDiagram(new PointF(cursorPoint.X, cursorPoint.Y)));
            if (currentTool == ToolKind.Wire && pendingWirePoints.Count >= 2)
            {
                FinishPendingWire();
                return;
            }

            if (currentTool != ToolKind.Select)
            {
                return;
            }

            if (selectedItem != null && selectedItem.Type == "wire")
            {
                if (InsertWireTurnAt(selectedItem, point))
                {
                    RecordHistory();
                    SetStatus("Wire turn added");
                    canvas.Invalidate();
                    return;
                }
            }

            var hit = FindItemAt(point);
            if (hit != null)
            {
                selectedItem = hit;
                RenameSelected();
            }
        }

        private void BeginSelectionDrag(PointF rawPoint)
        {
            if (selectedItem != null)
            {
                var handle = HitHandle(selectedItem, rawPoint);
                if (handle != null)
                {
                    if (selectedItem.Type == "wire")
                    {
                        activeWirePointIndex = handle.Index;
                        DetachWirePoint(selectedItem, activeWirePointIndex);
                        dragKind = DragKind.WirePoint;
                    }
                    else
                    {
                        resizeHandle = handle.Name;
                        dragKind = DragKind.Resize;
                    }
                    dragStart = rawPoint;
                    movedInDrag = false;
                    return;
                }
            }

            selectedItem = FindItemAt(rawPoint);
            UpdatePropertiesPanel();
            if (selectedItem == null)
            {
                dragKind = DragKind.None;
                SetStatus("No selection");
            }
            else
            {
                dragKind = DragKind.Move;
                dragStart = rawPoint;
                movedInDrag = false;
                SetStatus("Selected " + selectedItem.Type);
            }
            canvas.Invalidate();
        }

        private void ShowContextMenu(PointF screenPoint, PointF diagramPoint)
        {
            if (pendingWirePoints.Count >= 2)
            {
                var pendingMenu = new ContextMenuStrip();
                pendingMenu.Items.Add("Finish Wire", null, delegate { FinishPendingWire(); });
                pendingMenu.Items.Add("Cancel Wire", null, delegate { CancelPendingWire(); });
                pendingMenu.Show(canvas, new Point((int)screenPoint.X, (int)screenPoint.Y));
                return;
            }

            var hit = FindItemAt(diagramPoint);
            if (hit != null)
            {
                selectedItem = hit;
                UpdatePropertiesPanel();
                canvas.Invalidate();
            }
            else
            {
                selectedItem = null;
                UpdatePropertiesPanel();
                canvas.Invalidate();
            }

            var menu = new ContextMenuStrip();
            if (selectedItem != null)
            {
                if (selectedItem.Type != "wire" && selectedItem.Type != "image")
                {
                    menu.Items.Add("Rename", null, delegate { RenameSelected(); });
                }

                menu.Items.Add("Copy", null, delegate { CopySelected(); });
                menu.Items.Add("Duplicate", null, delegate { CopySelected(); PasteCopied(); });

                if (selectedItem.Type == "wire")
                {
                    menu.Items.Add(new ToolStripSeparator());
                    var wireModeMenu = new ToolStripMenuItem("Wire Mode");
                    wireModeMenu.DropDownItems.Add("Orthogonal Turns", null, delegate { SetSelectedWireMode("orthogonal"); });
                    wireModeMenu.DropDownItems.Add("Flexible Angles", null, delegate { SetSelectedWireMode("angled"); });
                    wireModeMenu.DropDownItems.Add("Extra Flexible Curves", null, delegate { SetSelectedWireMode("curved"); });
                    menu.Items.Add(wireModeMenu);
                    var lineStyleMenu = new ToolStripMenuItem("Line Style");
                    lineStyleMenu.DropDownItems.Add("Solid", null, delegate { SetSelectedLineStyle("solid"); });
                    lineStyleMenu.DropDownItems.Add("Dashed", null, delegate { SetSelectedLineStyle("dash"); });
                    lineStyleMenu.DropDownItems.Add("Dotted", null, delegate { SetSelectedLineStyle("dot"); });
                    menu.Items.Add(lineStyleMenu);
                    menu.Items.Add("Add Turn Here", null, delegate
                    {
                        if (InsertWireTurnAt(selectedItem, diagramPoint))
                        {
                            canvas.Invalidate();
                        }
                    });
                    menu.Items.Add(selectedItem.Arrow ? "Remove Arrow" : "Add Arrow", null, delegate
                    {
                        selectedItem.Arrow = !selectedItem.Arrow;
                        canvas.Invalidate();
                    });
                    menu.Items.Add("Disconnect Ends", null, delegate
                    {
                        selectedItem.StartConnection = null;
                        selectedItem.EndConnection = null;
                        canvas.Invalidate();
                    });
                    menu.Items.Add("Line Color", null, delegate { ShowColorMenu("line", canvas, screenPoint); });
                }
                else
                {
                    menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add("Fill Color", null, delegate { ShowColorMenu("fill", canvas, screenPoint); });
                    menu.Items.Add("Outline Color", null, delegate { ShowColorMenu("outline", canvas, screenPoint); });
                    menu.Items.Add("Line Color", null, delegate { ShowColorMenu("line", canvas, screenPoint); });
                    menu.Items.Add("Text Color", null, delegate { ShowColorMenu("text", canvas, screenPoint); });
                    menu.Items.Add("Bring To Front", null, delegate { BringSelectedToFront(); });
                    menu.Items.Add("Send To Back", null, delegate { SendSelectedToBack(); });
                }

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Delete", null, delegate { DeleteSelected(); });
            }
            else
            {
                menu.Items.Add("Paste", null, delegate { PasteCopiedAt(diagramPoint); }).Enabled = copiedItem != null;
                menu.Items.Add("Background Color", null, delegate { ShowColorMenu("background", canvas, screenPoint); });
            }

            menu.Show(canvas, new Point((int)screenPoint.X, (int)screenPoint.Y));
        }

        private void SetSelectedLineStyle(string style)
        {
            var normalized = NormalizeLineStyle(style);
            if (selectedItem != null)
            {
                selectedItem.LineStyle = normalized;
                canvas.Invalidate();
                RecordHistory();
                return;
            }

            lineStyle = normalized;
            if (lineStyleCombo != null)
            {
                lineStyleCombo.SelectedItem = StyleDisplayName(lineStyle);
            }
        }

        private void ShowColorMenu(string target, Control control, PointF screenPoint)
        {
            var menu = BuildColorMenu(target);
            menu.Show(control, new Point((int)screenPoint.X, (int)screenPoint.Y));
        }

        private void CopySelected()
        {
            if (selectedItem == null)
            {
                SetStatus("Nothing selected to copy");
                return;
            }

            copiedItem = CloneItem(selectedItem, false);
            SetStatus("Copied " + selectedItem.Type);
        }

        private void PasteCopied()
        {
            var center = ScreenToDiagram(new PointF(canvas.Width / 2f, canvas.Height / 2f));
            PasteCopiedAt(center);
        }

        private void PasteCopiedAt(PointF point)
        {
            if (copiedItem == null)
            {
                SetStatus("Nothing copied");
                return;
            }

            var pasted = CloneItem(copiedItem, true);
            MovePastedItemNear(pasted, point);
            items.Add(pasted);
            selectedItem = pasted;
            if (pasted.Type == "image")
            {
                var image = GetImage(pasted);
                if (image != null)
                {
                    imageCache[pasted.Id] = image;
                }
            }
            UpdatePropertiesPanel();
            SetStatus("Pasted " + pasted.Type);
            canvas.Invalidate();
            RecordHistory();
        }

        private DiagramItem CloneItem(DiagramItem source, bool assignNewId)
        {
            var clone = new DiagramItem();
            clone.Id = assignNewId ? CreateItemId() : source.Id;
            clone.Type = source.Type;
            clone.Category = source.Category;
            clone.Bounds = new FloatRect(source.Bounds.X, source.Bounds.Y, source.Bounds.Width, source.Bounds.Height);
            clone.Points = ClonePoints(source.Points);
            clone.Label = source.Label;
            clone.FillColor = source.FillColor;
            clone.OutlineColor = source.OutlineColor;
            clone.LineColor = source.LineColor;
            clone.TextColor = source.TextColor;
            clone.LineWidth = source.LineWidth;
            clone.Arrow = source.Arrow;
            clone.LineStyle = source.LineStyle;
            clone.FontFamily = source.FontFamily;
            clone.WireMode = string.IsNullOrWhiteSpace(source.WireMode) ? "orthogonal" : source.WireMode;
            clone.ImageDataBase64 = source.ImageDataBase64;
            clone.ImageExtension = source.ImageExtension;
            clone.SourceFileName = source.SourceFileName;
            clone.ConnectionPoints = ClonePoints(source.ConnectionPoints);
            clone.StartConnection = null;
            clone.EndConnection = null;
            return clone;
        }

        private void MovePastedItemNear(DiagramItem item, PointF target)
        {
            var bounds = GetBounds(item);
            if (bounds == RectangleF.Empty)
            {
                MoveItem(item, 24f, 24f);
                return;
            }

            var center = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            MoveItem(item, target.X - center.X + 24f, target.Y - center.Y + 24f);
        }

        private void BringSelectedToFront()
        {
            if (selectedItem == null)
            {
                return;
            }
            items.Remove(selectedItem);
            items.Add(selectedItem);
            canvas.Invalidate();
            RecordHistory();
        }

        private void SendSelectedToBack()
        {
            if (selectedItem == null)
            {
                return;
            }
            items.Remove(selectedItem);
            items.Insert(0, selectedItem);
            canvas.Invalidate();
            RecordHistory();
        }

        private void SetSelectedWireMode(string mode)
        {
            if (selectedItem == null || selectedItem.Type != "wire")
            {
                return;
            }
            selectedItem.WireMode = mode;
            SetStatus("Wire mode: " + mode);
            canvas.Invalidate();
            RecordHistory();
        }

        private void AddShape(string type, RectangleF rect)
        {
            var item = new DiagramItem();
            item.Id = CreateItemId();
            item.Type = type;
            item.Category = "Core";
            item.Bounds = FloatRect.FromRectangleF(rect);
            item.Label = type == "rect" ? "Component" : "Device";
            item.FillColor = ColorTranslator.ToHtml(fillColor);
            item.OutlineColor = ColorTranslator.ToHtml(outlineColor);
            item.TextColor = ColorTranslator.ToHtml(textColor);
            item.LineWidth = lineWidth;
            item.LineStyle = lineStyle;
            item.FontFamily = fontFamily;
            items.Add(item);
            selectedItem = item;
            UpdatePropertiesPanel();
            SetStatus((type == "rect" ? "Block" : "Device") + " added");
            RecordHistory();
        }

        private void AddText(PointF point, string text)
        {
            var item = new DiagramItem();
            item.Id = CreateItemId();
            item.Type = "text";
            item.Category = "Annotation";
            item.Points.Add(FloatPoint.FromPointF(point));
            item.Label = text;
            item.TextColor = ColorTranslator.ToHtml(textColor);
            item.FontFamily = fontFamily;
            item.LineStyle = lineStyle;
            items.Add(item);
            selectedItem = item;
            UpdatePropertiesPanel();
            SetStatus("Text added");
            canvas.Invalidate();
            RecordHistory();
        }

        private void AddPendingWirePoint(PointF point)
        {
            var connection = CreateConnectionAt(point);
            var actualPoint = point;
            if (connection != null)
            {
                actualPoint = GetConnectionPoint(connection);
            }

            if (pendingWirePoints.Count == 0)
            {
                pendingStartConnection = connection;
                pendingWirePoints.Add(FloatPoint.FromPointF(actualPoint));
                SetStatus("Wire started");
                return;
            }

            var lastPoint = pendingWirePoints[pendingWirePoints.Count - 1].ToPointF();
            if (Distance(lastPoint, actualPoint) < 2f)
            {
                return;
            }

            pendingWirePoints.Add(FloatPoint.FromPointF(actualPoint));
            SetStatus("Wire point added");
        }

        private void FinishPendingWire()
        {
            if (pendingWirePoints.Count < 2)
            {
                CancelPendingWire();
                return;
            }

            var item = new DiagramItem();
            item.Id = CreateItemId();
            item.Type = "wire";
            item.Category = "Core";
            item.Points = ClonePoints(pendingWirePoints);
            item.LineColor = ColorTranslator.ToHtml(lineColor);
            item.LineWidth = lineWidth;
            item.Arrow = wireArrow;
            item.WireMode = "orthogonal";
            item.LineStyle = lineStyle;
            item.StartConnection = pendingStartConnection;
            item.EndConnection = CreateConnectionAt(item.Points[item.Points.Count - 1].ToPointF());
            if (item.EndConnection != null)
            {
                item.Points[item.Points.Count - 1] = FloatPoint.FromPointF(GetConnectionPoint(item.EndConnection));
            }

            items.Add(item);
            selectedItem = item;
            pendingWirePoints.Clear();
            pendingStartConnection = null;
            UpdatePropertiesPanel();
            SetStatus("Wire added");
            canvas.Invalidate();
            RecordHistory();
        }

        private void CancelPendingWire()
        {
            pendingWirePoints.Clear();
            pendingStartConnection = null;
            canvas.Invalidate();
            SetStatus("Wire canceled");
        }

        private void InsertImageAtCenter()
        {
            InsertImageAt(ScreenToDiagram(new PointF(canvas.Width / 2f, canvas.Height / 2f)));
        }

        private void InsertImageAt(PointF point)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
                dialog.Title = "Insert image";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var bytes = File.ReadAllBytes(dialog.FileName);
                    Image image;
                    using (var stream = new MemoryStream(bytes))
                    {
                        using (var loadedImage = Image.FromStream(stream))
                        {
                            image = new Bitmap(loadedImage);
                        }
                    }

                    var maxWidth = 360f;
                    var maxHeight = 260f;
                    var width = (float)image.Width;
                    var height = (float)image.Height;
                    var scale = Math.Min(1f, Math.Min(maxWidth / width, maxHeight / height));
                    width *= scale;
                    height *= scale;

                    var item = new DiagramItem();
                    item.Id = CreateItemId();
                    item.Type = "image";
                    item.Category = "Reference";
                    item.Bounds = new FloatRect(point.X - width / 2f, point.Y - height / 2f, width, height);
                    item.SourceFileName = Path.GetFileName(dialog.FileName);
                    item.ImageExtension = NormalizeImageExtension(Path.GetExtension(dialog.FileName));
                    item.ImageDataBase64 = Convert.ToBase64String(bytes);
                    item.LineColor = ColorTranslator.ToHtml(outlineColor);
                    item.LineWidth = 1f;
                    item.LineStyle = lineStyle;
                    items.Add(item);
                    imageCache[item.Id] = image;
                    selectedItem = item;
                    UpdatePropertiesPanel();
                    SetStatus("Image added");
                    canvas.Invalidate();
                    RecordHistory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not insert image:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InsertCustomComponent(ComponentDefinition component, PointF point)
        {
            if (component == null)
            {
                return;
            }

            try
            {
                var previewImage = DecodeComponentPreview(component);
                if (previewImage == null)
                {
                    return;
                }

                var maxWidth = 320f;
                var maxHeight = 220f;
                var width = (float)previewImage.Width;
                var height = (float)previewImage.Height;
                var scale = Math.Min(1f, Math.Min(maxWidth / width, maxHeight / height));
                width *= scale;
                height *= scale;

                var item = new DiagramItem();
                item.Id = CreateItemId();
                item.Type = "image";
                item.Category = string.IsNullOrWhiteSpace(component.Category) ? "Custom" : component.Category;
                item.Bounds = new FloatRect(point.X - width / 2f, point.Y - height / 2f, width, height);
                item.SourceFileName = component.Name;
                item.ImageExtension = "png";
                item.ImageDataBase64 = component.PreviewBase64;
                item.LineColor = ColorTranslator.ToHtml(outlineColor);
                item.LineWidth = 1f;
                item.LineStyle = "solid";
                item.ConnectionPoints = ClonePoints(component.ConnectionPoints);

                items.Add(item);
                imageCache[item.Id] = previewImage;
                selectedItem = item;
                UpdatePropertiesPanel();
                SetStatus("Component added");
                canvas.Invalidate();
                RecordHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not add component:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Image DecodeComponentPreview(ComponentDefinition component)
        {
            if (component == null || string.IsNullOrWhiteSpace(component.PreviewBase64))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(component.PreviewBase64);
                using (var stream = new MemoryStream(bytes))
                {
                    using (var loadedImage = Image.FromStream(stream))
                    {
                        return new Bitmap(loadedImage);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private string NormalizeImageExtension(string extension)
        {
            var value = extension.Replace(".", "").ToLowerInvariant();
            if (value == "jpg")
            {
                return "jpeg";
            }
            if (value == "bmp")
            {
                return "bmp";
            }
            if (value == "jpeg")
            {
                return "jpeg";
            }
            return "png";
        }

        private List<FloatPoint> ClonePoints(List<FloatPoint> source)
        {
            var clone = new List<FloatPoint>();
            if (source == null)
            {
                return clone;
            }
            foreach (var point in source)
            {
                clone.Add(new FloatPoint(point.X, point.Y));
            }
            return clone;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            RenderDiagram(e.Graphics, canvas.ClientSize, true, 1f);
        }

        private void RenderDiagram(Graphics graphics, Size size, bool includeEditorUi, float scale)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var backgroundBrush = new SolidBrush(backgroundColor))
            {
                graphics.FillRectangle(backgroundBrush, 0, 0, size.Width * scale, size.Height * scale);
            }

            if (includeEditorUi)
            {
                graphics.TranslateTransform(viewOffset.X, viewOffset.Y);
                graphics.ScaleTransform(viewScale, viewScale);
            }
            else
            {
                graphics.ScaleTransform(scale, scale);
            }

            if (includeEditorUi && showGrid)
            {
                DrawGrid(graphics);
            }

            foreach (var item in items)
            {
                DrawItem(graphics, item);
            }

            if (includeEditorUi)
            {
                DrawPreview(graphics);
                DrawSelection(graphics);
            }
        }

        private void DrawGrid(Graphics graphics)
        {
            using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1f))
            {
                var topLeft = ScreenToDiagram(new PointF(0f, 0f));
                var bottomRight = ScreenToDiagram(new PointF(canvas.Width, canvas.Height));
                var startX = (int)Math.Floor(topLeft.X / gridSize) * gridSize;
                var endX = (int)Math.Ceiling(bottomRight.X / gridSize) * gridSize;
                var startY = (int)Math.Floor(topLeft.Y / gridSize) * gridSize;
                var endY = (int)Math.Ceiling(bottomRight.Y / gridSize) * gridSize;

                for (var x = startX; x <= endX; x += gridSize)
                {
                    graphics.DrawLine(pen, x, startY, x, endY);
                }
                for (var y = startY; y <= endY; y += gridSize)
                {
                    graphics.DrawLine(pen, startX, y, endX, y);
                }
            }
        }

        private void DrawItem(Graphics graphics, DiagramItem item)
        {
            if (item.Type == "rect")
            {
                DrawShape(graphics, item, false);
            }
            else if (item.Type == "ellipse")
            {
                DrawShape(graphics, item, true);
            }
            else if (item.Type == "wire")
            {
                DrawWire(graphics, item);
            }
            else if (item.Type == "text")
            {
                DrawText(graphics, item);
            }
            else if (item.Type == "image")
            {
                DrawImage(graphics, item);
            }
        }

        private void DrawShape(Graphics graphics, DiagramItem item, bool ellipse)
        {
            var rect = item.Bounds.ToRectangleF();
            using (var brush = new SolidBrush(ParseColor(item.FillColor, Color.White)))
            using (var pen = new Pen(ParseColor(item.OutlineColor, Color.Black), item.LineWidth))
            using (var textBrush = new SolidBrush(ParseColor(item.TextColor, Color.Black)))
            using (var font = CreateFont(item.FontFamily, 15f, FontStyle.Bold))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                ApplyLineStyle(pen, item.LineStyle);

                if (ellipse)
                {
                    graphics.FillEllipse(brush, rect);
                    graphics.DrawEllipse(pen, rect);
                }
                else
                {
                    graphics.FillRectangle(brush, rect);
                    graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }

                graphics.DrawString(item.Label ?? string.Empty, font, textBrush, rect, format);
            }
        }

        private void DrawWire(Graphics graphics, DiagramItem item)
        {
            if (item.Points.Count < 2)
            {
                return;
            }

            UpdateWireConnectionPoints(item);
            var points = GetWireDisplayPoints(item).ToArray();
            using (var pen = new Pen(ParseColor(item.LineColor, Color.Black), item.LineWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                ApplyLineStyle(pen, item.LineStyle);
                if (item.Arrow)
                {
                    pen.CustomEndCap = new AdjustableArrowCap(4f, 6f, true);
                }
                DrawWirePath(graphics, pen, points, IsCurvedWire(item));
            }
        }

        private void DrawText(Graphics graphics, DiagramItem item)
        {
            if (item.Points.Count == 0)
            {
                return;
            }

            var point = item.Points[0].ToPointF();
            using (var brush = new SolidBrush(ParseColor(item.TextColor, Color.Black)))
            using (var font = CreateFont(item.FontFamily, 15f, FontStyle.Regular))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                graphics.DrawString(item.Label ?? string.Empty, font, brush, point, format);
            }
        }

        private List<PointF> GetWireDisplayPoints(DiagramItem wire)
        {
            var controlPoints = wire.Points.Select(p => p.ToPointF()).ToList();
            if (IsOrthogonalWire(wire))
            {
                return BuildOrthogonalDisplayPoints(
                    controlPoints,
                    wire.StartConnection == null ? null : wire.StartConnection.Side,
                    wire.EndConnection == null ? null : wire.EndConnection.Side);
            }
            return controlPoints;
        }

        private List<PointF> BuildOrthogonalDisplayPoints(List<PointF> controlPoints, string startSide, string endSide)
        {
            var displayPoints = new List<PointF>();
            if (controlPoints.Count == 0)
            {
                return displayPoints;
            }

            displayPoints.Add(controlPoints[0]);

            if (!string.IsNullOrWhiteSpace(startSide) && controlPoints.Count > 1)
            {
                displayPoints.Add(OffsetFromSide(controlPoints[0], startSide, 14f));
            }

            for (var i = 1; i < controlPoints.Count - 1; i++)
            {
                AddOrthogonalStep(displayPoints, controlPoints[i]);
            }

            if (!string.IsNullOrWhiteSpace(endSide) && controlPoints.Count > 1)
            {
                AddOrthogonalStep(displayPoints, OffsetFromSide(controlPoints[controlPoints.Count - 1], endSide, 14f));
                AddDistinctPoint(displayPoints, controlPoints[controlPoints.Count - 1]);
            }
            else if (controlPoints.Count > 1)
            {
                AddOrthogonalStep(displayPoints, controlPoints[controlPoints.Count - 1]);
            }

            return displayPoints;
        }

        private void AddOrthogonalStep(List<PointF> points, PointF target)
        {
            if (points.Count == 0)
            {
                points.Add(target);
                return;
            }

            var previous = points[points.Count - 1];
            if (Math.Abs(previous.X - target.X) < 0.5f || Math.Abs(previous.Y - target.Y) < 0.5f)
            {
                AddDistinctPoint(points, target);
                return;
            }

            var elbow = new PointF(target.X, previous.Y);
            AddDistinctPoint(points, elbow);
            AddDistinctPoint(points, target);
        }

        private PointF OffsetFromSide(PointF point, string side, float distance)
        {
            var normalized = string.IsNullOrWhiteSpace(side) ? string.Empty : side.ToLowerInvariant();
            if (normalized == "left")
            {
                return new PointF(point.X - distance, point.Y);
            }
            if (normalized == "right")
            {
                return new PointF(point.X + distance, point.Y);
            }
            if (normalized == "top")
            {
                return new PointF(point.X, point.Y - distance);
            }
            if (normalized == "bottom")
            {
                return new PointF(point.X, point.Y + distance);
            }
            return point;
        }

        private void AddDistinctPoint(List<PointF> points, PointF point)
        {
            if (points.Count == 0 || Distance(points[points.Count - 1], point) > 0.5f)
            {
                points.Add(point);
            }
        }

        private bool IsOrthogonalWire(DiagramItem wire)
        {
            return wire == null || string.IsNullOrWhiteSpace(wire.WireMode) || wire.WireMode == "orthogonal";
        }

        private bool IsCurvedWire(DiagramItem wire)
        {
            return wire != null && wire.WireMode == "curved";
        }

        private void DrawImage(Graphics graphics, DiagramItem item)
        {
            var image = GetImage(item);
            if (image == null)
            {
                return;
            }

            var rect = item.Bounds.ToRectangleF();
            graphics.DrawImage(image, rect);
            using (var pen = new Pen(ParseColor(item.LineColor, Color.Gray), Math.Max(1f, item.LineWidth)))
            {
                ApplyLineStyle(pen, item.LineStyle);
                graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private void DrawPreview(Graphics graphics)
        {
            var snappedPointer = Snap(currentPointer);
            using (var pen = new Pen(lineColor, Math.Max(1f, lineWidth)))
            {
                ApplyLineStyle(pen, lineStyle);
                if (currentTool == ToolKind.Block && drawStart != null)
                {
                    var rect = forceSquareShape ? NormalizeSquareRect(drawStart.Value, snappedPointer) : NormalizeRect(drawStart.Value, snappedPointer);
                    graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else if (currentTool == ToolKind.Device && drawStart != null)
                {
                    var rect = NormalizeRect(drawStart.Value, snappedPointer);
                    graphics.DrawEllipse(pen, rect);
                }
                else if (currentTool == ToolKind.Wire && pendingWirePoints.Count > 0)
                {
                    var points = ClonePoints(pendingWirePoints);
                    points.Add(FloatPoint.FromPointF(snappedPointer));
                    var displayPoints = BuildOrthogonalDisplayPoints(points.Select(p => p.ToPointF()).ToList(), null, null);
                    if (wireArrow)
                    {
                        pen.CustomEndCap = new AdjustableArrowCap(4f, 6f, true);
                    }
                    DrawWirePath(graphics, pen, displayPoints.ToArray(), false);
                }
            }
        }

        private void DrawWirePath(Graphics graphics, Pen pen, PointF[] points, bool curved)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            if (curved && points.Length >= 3)
            {
                graphics.DrawCurve(pen, points, 0.35f);
            }
            else
            {
                graphics.DrawLines(pen, points);
            }
        }

        private void ApplyLineStyle(Pen pen, string style)
        {
            var normalized = NormalizeLineStyle(style);
            if (normalized == "dash")
            {
                pen.DashStyle = DashStyle.Dash;
            }
            else if (normalized == "dot")
            {
                pen.DashStyle = DashStyle.Dot;
            }
            else
            {
                pen.DashStyle = DashStyle.Solid;
            }
        }

        private void DrawSelection(Graphics graphics)
        {
            if (selectedItem == null)
            {
                return;
            }

            if (selectedItem.Type == "wire")
            {
                DrawWireHandles(graphics, selectedItem);
                return;
            }

            var rect = GetBounds(selectedItem);
            rect.Inflate(5f, 5f);
            using (var pen = new Pen(Color.FromArgb(42, 115, 217), 2f))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }

            foreach (var handle in GetResizeHandles(rect))
            {
                using (var brush = new SolidBrush(Color.White))
                using (var pen = new Pen(Color.FromArgb(42, 115, 217), 1f))
                {
                    graphics.FillRectangle(brush, handle.Value);
                    graphics.DrawRectangle(pen, handle.Value.X, handle.Value.Y, handle.Value.Width, handle.Value.Height);
                }
            }
        }

        private void DrawWireHandles(Graphics graphics, DiagramItem wire)
        {
            for (var i = 0; i < wire.Points.Count; i++)
            {
                var point = wire.Points[i].ToPointF();
                var connected = (i == 0 && wire.StartConnection != null) || (i == wire.Points.Count - 1 && wire.EndConnection != null);
                var fill = connected ? Color.FromArgb(0, 150, 90) : Color.White;
                var rect = new RectangleF(point.X - 5f, point.Y - 5f, 10f, 10f);
                using (var brush = new SolidBrush(fill))
                using (var pen = new Pen(Color.FromArgb(42, 115, 217), 1.5f))
                {
                    graphics.FillEllipse(brush, rect);
                    graphics.DrawEllipse(pen, rect);
                }
            }
        }

        private Font CreateFont(string family, float size, FontStyle style)
        {
            try
            {
                return new Font(string.IsNullOrWhiteSpace(family) ? "Times New Roman" : family, size, style);
            }
            catch
            {
                return new Font("Times New Roman", size, style);
            }
        }

        private Image GetImage(DiagramItem item)
        {
            if (imageCache.ContainsKey(item.Id))
            {
                return imageCache[item.Id];
            }
            if (string.IsNullOrWhiteSpace(item.ImageDataBase64))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(item.ImageDataBase64);
                using (var stream = new MemoryStream(bytes))
                {
                    Image image;
                    using (var loadedImage = Image.FromStream(stream))
                    {
                        image = new Bitmap(loadedImage);
                    }
                    imageCache[item.Id] = image;
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<string, RectangleF> GetResizeHandles(RectangleF rect)
        {
            var size = 8f;
            var half = size / 2f;
            var handles = new Dictionary<string, RectangleF>();
            handles["NW"] = new RectangleF(rect.Left - half, rect.Top - half, size, size);
            handles["N"] = new RectangleF(rect.Left + rect.Width / 2f - half, rect.Top - half, size, size);
            handles["NE"] = new RectangleF(rect.Right - half, rect.Top - half, size, size);
            handles["E"] = new RectangleF(rect.Right - half, rect.Top + rect.Height / 2f - half, size, size);
            handles["SE"] = new RectangleF(rect.Right - half, rect.Bottom - half, size, size);
            handles["S"] = new RectangleF(rect.Left + rect.Width / 2f - half, rect.Bottom - half, size, size);
            handles["SW"] = new RectangleF(rect.Left - half, rect.Bottom - half, size, size);
            handles["W"] = new RectangleF(rect.Left - half, rect.Top + rect.Height / 2f - half, size, size);
            return handles;
        }

        private HandleHit HitHandle(DiagramItem item, PointF point)
        {
            if (item.Type == "wire")
            {
                for (var i = 0; i < item.Points.Count; i++)
                {
                    if (Distance(item.Points[i].ToPointF(), point) <= 8f)
                    {
                        return new HandleHit("WIRE", i);
                    }
                }
                return null;
            }

            if (!HasResizableBounds(item))
            {
                return null;
            }

            var rect = GetBounds(item);
            rect.Inflate(5f, 5f);
            foreach (var handle in GetResizeHandles(rect))
            {
                if (handle.Value.Contains(point))
                {
                    return new HandleHit(handle.Key, -1);
                }
            }
            return null;
        }

        private void MoveItem(DiagramItem item, float dx, float dy)
        {
            if (item.Type == "rect" || item.Type == "ellipse" || item.Type == "image")
            {
                item.Bounds.X += dx;
                item.Bounds.Y += dy;
            }
            else if (item.Type == "text" || item.Type == "wire")
            {
                if (item.Type == "wire")
                {
                    item.StartConnection = null;
                    item.EndConnection = null;
                }
                foreach (var point in item.Points)
                {
                    point.X += dx;
                    point.Y += dy;
                }
            }
        }

        private void ResizeSelected(PointF point)
        {
            if (selectedItem == null || !HasResizableBounds(selectedItem))
            {
                return;
            }

            var rect = selectedItem.Bounds.ToRectangleF();
            var left = rect.Left;
            var top = rect.Top;
            var right = rect.Right;
            var bottom = rect.Bottom;

            if (resizeHandle.Contains("W")) left = Math.Min(point.X, right - 20f);
            if (resizeHandle.Contains("E")) right = Math.Max(point.X, left + 20f);
            if (resizeHandle.Contains("N")) top = Math.Min(point.Y, bottom - 20f);
            if (resizeHandle.Contains("S")) bottom = Math.Max(point.Y, top + 20f);

            selectedItem.Bounds = new FloatRect(left, top, right - left, bottom - top);
        }

        private void MoveWirePoint(DiagramItem wire, int index, PointF point)
        {
            if (wire == null || wire.Type != "wire" || index < 0 || index >= wire.Points.Count)
            {
                return;
            }

            if (IsOrthogonalWire(wire) && index > 0 && index < wire.Points.Count - 1)
            {
                var previous = wire.Points[index - 1].ToPointF();
                var current = wire.Points[index].ToPointF();
                var next = wire.Points[index + 1].ToPointF();
                var keepHorizontal = Math.Abs(previous.Y - current.Y) < 0.5f || Math.Abs(next.Y - current.Y) < 0.5f;
                var keepVertical = Math.Abs(previous.X - current.X) < 0.5f || Math.Abs(next.X - current.X) < 0.5f;

                if (keepHorizontal && !keepVertical)
                {
                    point = new PointF(point.X, current.Y);
                }
                else if (keepVertical && !keepHorizontal)
                {
                    point = new PointF(current.X, point.Y);
                }
                else
                {
                    var deltaX = Math.Abs(point.X - current.X);
                    var deltaY = Math.Abs(point.Y - current.Y);
                    point = deltaX >= deltaY ? new PointF(point.X, current.Y) : new PointF(current.X, point.Y);
                }
            }

            wire.Points[index] = FloatPoint.FromPointF(point);
        }

        private void DetachWirePoint(DiagramItem wire, int index)
        {
            if (wire == null || wire.Type != "wire")
            {
                return;
            }
            if (index == 0)
            {
                wire.StartConnection = null;
            }
            else if (index == wire.Points.Count - 1)
            {
                wire.EndConnection = null;
            }
        }

        private void TryReconnectDraggedWireTip(PointF point)
        {
            if (selectedItem == null || selectedItem.Type != "wire")
            {
                return;
            }
            if (activeWirePointIndex != 0 && activeWirePointIndex != selectedItem.Points.Count - 1)
            {
                return;
            }

            var connection = CreateConnectionAt(point);
            if (connection == null)
            {
                return;
            }

            var connectionPoint = GetConnectionPoint(connection);
            selectedItem.Points[activeWirePointIndex] = FloatPoint.FromPointF(connectionPoint);
            if (activeWirePointIndex == 0)
            {
                selectedItem.StartConnection = connection;
            }
            else
            {
                selectedItem.EndConnection = connection;
            }
            SetStatus("Wire reconnected");
        }

        private bool InsertWireTurnAt(DiagramItem wire, PointF point)
        {
            if (wire == null || wire.Type != "wire" || wire.Points.Count < 2)
            {
                return false;
            }

            var bestIndex = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < wire.Points.Count - 1; i++)
            {
                var segmentPoints = IsOrthogonalWire(wire)
                    ? BuildOrthogonalDisplayPoints(
                        new List<PointF> { wire.Points[i].ToPointF(), wire.Points[i + 1].ToPointF() },
                        null,
                        null)
                    : new List<PointF> { wire.Points[i].ToPointF(), wire.Points[i + 1].ToPointF() };

                var distance = float.MaxValue;
                for (var segmentIndex = 0; segmentIndex < segmentPoints.Count - 1; segmentIndex++)
                {
                    distance = Math.Min(distance, DistanceToSegment(point, segmentPoints[segmentIndex], segmentPoints[segmentIndex + 1]));
                }
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0 && bestDistance <= 12f)
            {
                wire.Points.Insert(bestIndex + 1, FloatPoint.FromPointF(point));
                return true;
            }
            return false;
        }

        private void UpdateConnectedWires(DiagramItem movedItem)
        {
            if (movedItem == null)
            {
                return;
            }

            foreach (var wire in items)
            {
                if (wire.Type != "wire")
                {
                    continue;
                }
                if (wire.StartConnection != null && wire.StartConnection.ElementId == movedItem.Id && wire.Points.Count > 0)
                {
                    wire.Points[0] = FloatPoint.FromPointF(GetConnectionPoint(wire.StartConnection));
                }
                if (wire.EndConnection != null && wire.EndConnection.ElementId == movedItem.Id && wire.Points.Count > 0)
                {
                    wire.Points[wire.Points.Count - 1] = FloatPoint.FromPointF(GetConnectionPoint(wire.EndConnection));
                }
            }
        }

        private void UpdateWireConnectionPoints(DiagramItem wire)
        {
            if (wire == null || wire.Type != "wire" || wire.Points.Count == 0)
            {
                return;
            }
            if (wire.StartConnection != null)
            {
                wire.Points[0] = FloatPoint.FromPointF(GetConnectionPoint(wire.StartConnection));
            }
            if (wire.EndConnection != null)
            {
                wire.Points[wire.Points.Count - 1] = FloatPoint.FromPointF(GetConnectionPoint(wire.EndConnection));
            }
        }

        private WireConnection CreateConnectionAt(PointF point)
        {
            var item = FindConnectableAt(point);
            if (item == null)
            {
                return null;
            }

            var rect = item.Bounds.ToRectangleF();
            if (rect.Width <= 0f || rect.Height <= 0f)
            {
                return null;
            }

            if (item.ConnectionPoints != null && item.ConnectionPoints.Count > 0)
            {
                var bestIndex = -1;
                var bestDistance = float.MaxValue;
                for (var i = 0; i < item.ConnectionPoints.Count; i++)
                {
                    var hotspot = new PointF(rect.X + rect.Width * item.ConnectionPoints[i].X, rect.Y + rect.Height * item.ConnectionPoints[i].Y);
                    var distance = Distance(point, hotspot);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0 && bestDistance <= 14f)
                {
                    var hotspot = item.ConnectionPoints[bestIndex];
                    var connection = new WireConnection(item.Id, Clamp(hotspot.X, 0f, 1f), Clamp(hotspot.Y, 0f, 1f), "custom");
                    connection.AnchorIndex = bestIndex;
                    return connection;
                }
            }

            string side;
            var anchorPoint = ClosestPointOnRect(rect, point, out side);
            var anchorX = rect.Width <= 0f ? 0.5f : Clamp((anchorPoint.X - rect.X) / rect.Width, 0f, 1f);
            var anchorY = rect.Height <= 0f ? 0.5f : Clamp((anchorPoint.Y - rect.Y) / rect.Height, 0f, 1f);
            return new WireConnection(item.Id, anchorX, anchorY, side);
        }

        private PointF GetConnectionPoint(WireConnection connection)
        {
            var item = FindById(connection.ElementId);
            if (item == null || !HasResizableBounds(item))
            {
                return PointF.Empty;
            }
            var rect = item.Bounds.ToRectangleF();
            var side = NormalizeSide(connection.Side);
            if (side == "custom")
            {
                if (item.ConnectionPoints != null && connection.AnchorIndex >= 0 && connection.AnchorIndex < item.ConnectionPoints.Count)
                {
                    var hotspot = item.ConnectionPoints[connection.AnchorIndex];
                    return new PointF(rect.X + rect.Width * Clamp(hotspot.X, 0f, 1f), rect.Y + rect.Height * Clamp(hotspot.Y, 0f, 1f));
                }

                return new PointF(rect.X + rect.Width * Clamp(connection.AnchorX, 0f, 1f), rect.Y + rect.Height * Clamp(connection.AnchorY, 0f, 1f));
            }
            if (string.IsNullOrWhiteSpace(side))
            {
                if (Math.Abs(connection.AnchorX) < 0.001f) side = "left";
                else if (Math.Abs(connection.AnchorX - 1f) < 0.001f) side = "right";
                else if (Math.Abs(connection.AnchorY) < 0.001f) side = "top";
                else if (Math.Abs(connection.AnchorY - 1f) < 0.001f) side = "bottom";
            }
            if (side == "left")
            {
                return new PointF(rect.Left, rect.Top + rect.Height * Clamp(connection.AnchorY, 0f, 1f));
            }
            if (side == "right")
            {
                return new PointF(rect.Right, rect.Top + rect.Height * Clamp(connection.AnchorY, 0f, 1f));
            }
            if (side == "top")
            {
                return new PointF(rect.Left + rect.Width * Clamp(connection.AnchorX, 0f, 1f), rect.Top);
            }
            if (side == "bottom")
            {
                return new PointF(rect.Left + rect.Width * Clamp(connection.AnchorX, 0f, 1f), rect.Bottom);
            }
            return new PointF(rect.X + rect.Width * Clamp(connection.AnchorX, 0f, 1f), rect.Y + rect.Height * Clamp(connection.AnchorY, 0f, 1f));
        }

        private string NormalizeSide(string side)
        {
            var normalized = string.IsNullOrWhiteSpace(side) ? string.Empty : side.Trim().ToLowerInvariant();
            if (normalized == "l")
            {
                return "left";
            }
            if (normalized == "r")
            {
                return "right";
            }
            if (normalized == "t")
            {
                return "top";
            }
            if (normalized == "b")
            {
                return "bottom";
            }
            if (normalized == "left" || normalized == "right" || normalized == "top" || normalized == "bottom")
            {
                return normalized;
            }
            return string.Empty;
        }

        private DiagramItem FindConnectableAt(PointF point)
        {
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item.Type == "rect" || item.Type == "ellipse" || item.Type == "image")
                {
                    var rect = item.Bounds.ToRectangleF();
                    rect.Inflate(10f, 10f);
                    if (rect.Contains(point))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        private PointF ClosestPointOnRect(RectangleF rect, PointF point, out string side)
        {
            var left = Math.Abs(point.X - rect.Left);
            var right = Math.Abs(point.X - rect.Right);
            var top = Math.Abs(point.Y - rect.Top);
            var bottom = Math.Abs(point.Y - rect.Bottom);
            var min = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
            var x = Clamp(point.X, rect.Left, rect.Right);
            var y = Clamp(point.Y, rect.Top, rect.Bottom);
            side = "top";

            if (min == left)
            {
                x = rect.Left;
                side = "left";
            }
            else if (min == right)
            {
                x = rect.Right;
                side = "right";
            }
            else if (min == top)
            {
                y = rect.Top;
                side = "top";
            }
            else
            {
                y = rect.Bottom;
                side = "bottom";
            }

            return new PointF(x, y);
        }

        private void ApplyNumericProperties()
        {
            if (selectedItem == null)
            {
                lineWidth = (float)lineWidthBox.Value;
                return;
            }

            if (selectedItem.Type == "rect" || selectedItem.Type == "ellipse" || selectedItem.Type == "image")
            {
                selectedItem.Bounds.X = (float)xBox.Value;
                selectedItem.Bounds.Y = (float)yBox.Value;
                selectedItem.Bounds.Width = Math.Max(20f, (float)widthBox.Value);
                selectedItem.Bounds.Height = Math.Max(20f, (float)heightBox.Value);
                selectedItem.LineWidth = Math.Max(1f, (float)lineWidthBox.Value);
                UpdateConnectedWires(selectedItem);
            }
            else if (selectedItem.Type == "text" && selectedItem.Points.Count > 0)
            {
                selectedItem.Points[0].X = (float)xBox.Value;
                selectedItem.Points[0].Y = (float)yBox.Value;
            }
            else if (selectedItem.Type == "wire")
            {
                selectedItem.LineWidth = Math.Max(1f, (float)lineWidthBox.Value);
            }

            lineWidth = Math.Max(1f, (float)lineWidthBox.Value);
            canvas.Invalidate();
            RecordHistory();
        }

        private void UpdatePropertiesPanel()
        {
            if (xBox == null)
            {
                return;
            }

            updatingProperties = true;

            var hasSelection = selectedItem != null;
            xBox.Enabled = hasSelection;
            yBox.Enabled = hasSelection;
            widthBox.Enabled = hasSelection && HasResizableBounds(selectedItem);
            heightBox.Enabled = hasSelection && HasResizableBounds(selectedItem);
            lineWidthBox.Enabled = true;
            lineStyleCombo.Enabled = true;
            fontCombo.Enabled = hasSelection && selectedItem.Type != "wire" && selectedItem.Type != "image";

            if (!hasSelection)
            {
                lineWidthBox.Value = ToNumeric(lineWidth);
                lineStyleCombo.SelectedItem = StyleDisplayName(lineStyle);
                fontCombo.SelectedItem = fontFamily;
                updatingProperties = false;
                return;
            }

            if (selectedItem.Type == "rect" || selectedItem.Type == "ellipse" || selectedItem.Type == "image")
            {
                xBox.Value = ToNumeric(selectedItem.Bounds.X);
                yBox.Value = ToNumeric(selectedItem.Bounds.Y);
                widthBox.Value = ToNumeric(selectedItem.Bounds.Width);
                heightBox.Value = ToNumeric(selectedItem.Bounds.Height);
                lineWidthBox.Value = ToNumeric(Math.Max(1f, selectedItem.LineWidth));
                lineStyleCombo.SelectedItem = StyleDisplayName(selectedItem.LineStyle);
            }
            else if (selectedItem.Type == "text" && selectedItem.Points.Count > 0)
            {
                xBox.Value = ToNumeric(selectedItem.Points[0].X);
                yBox.Value = ToNumeric(selectedItem.Points[0].Y);
                widthBox.Value = 0;
                heightBox.Value = 0;
                lineStyleCombo.SelectedItem = StyleDisplayName(selectedItem.LineStyle);
            }
            else if (selectedItem.Type == "wire")
            {
                var bounds = GetBounds(selectedItem);
                xBox.Value = ToNumeric(bounds.X);
                yBox.Value = ToNumeric(bounds.Y);
                widthBox.Value = ToNumeric(bounds.Width);
                heightBox.Value = ToNumeric(bounds.Height);
                lineWidthBox.Value = ToNumeric(Math.Max(1f, selectedItem.LineWidth));
                lineStyleCombo.SelectedItem = StyleDisplayName(selectedItem.LineStyle);
            }
            else
            {
                lineStyleCombo.SelectedItem = StyleDisplayName(selectedItem.LineStyle);
            }

            if (!string.IsNullOrWhiteSpace(selectedItem.FontFamily) && fontCombo.Items.Contains(selectedItem.FontFamily))
            {
                fontCombo.SelectedItem = selectedItem.FontFamily;
            }

            updatingProperties = false;
        }

        private decimal ToNumeric(float value)
        {
            var clamped = Clamp(value, -10000f, 10000f);
            return Convert.ToDecimal(Math.Round(clamped));
        }

        private string StyleDisplayName(string style)
        {
            var normalized = NormalizeLineStyle(style);
            if (normalized == "dash")
            {
                return "Dashed";
            }
            if (normalized == "dot")
            {
                return "Dotted";
            }
            return "Solid";
        }

        private string NormalizeLineStyle(string style)
        {
            var normalized = string.IsNullOrWhiteSpace(style) ? "solid" : style.Trim().ToLowerInvariant();
            if (normalized == "dashed")
            {
                return "dash";
            }
            if (normalized == "dotted")
            {
                return "dot";
            }
            if (normalized == "dash" || normalized == "dot" || normalized == "solid")
            {
                return normalized;
            }
            return "solid";
        }

        private string GetSelectedLineStyle()
        {
            if (lineStyleCombo == null || lineStyleCombo.SelectedItem == null)
            {
                return NormalizeLineStyle(lineStyle);
            }

            var selected = Convert.ToString(lineStyleCombo.SelectedItem);
            if (string.Equals(selected, "Dashed", StringComparison.OrdinalIgnoreCase))
            {
                return "dash";
            }
            if (string.Equals(selected, "Dotted", StringComparison.OrdinalIgnoreCase))
            {
                return "dot";
            }
            return "solid";
        }

        private void PickColor(string target)
        {
            var current = fillColor;
            if (target == "outline") current = outlineColor;
            else if (target == "line") current = lineColor;
            else if (target == "text") current = textColor;
            else if (target == "background") current = backgroundColor;

            using (var dialog = new ColorDialog())
            {
                dialog.FullOpen = true;
                dialog.Color = current;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                SetColor(target, dialog.Color, true);
            }
        }

        private void ShowColorMenu(string target, ToolStripItem item)
        {
            var menu = BuildColorMenu(target);
            menu.Show(toolStrip, item.Bounds.Left, item.Bounds.Bottom);
        }

        private void ShowColorMenu(string target, Control control)
        {
            var menu = BuildColorMenu(target);
            menu.Show(control, new Point(0, control.Height));
        }

        private ContextMenuStrip BuildColorMenu(string target)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Choose color...", null, delegate { PickColor(target); });
            menu.Items.Add("Pick from canvas", null, delegate { BeginCanvasColorPick(target); });
            return menu;
        }

        private void BeginCanvasColorPick(string target)
        {
            activeColorPickTarget = target;
            toolBeforeColorPick = currentTool == ToolKind.ColorPicker ? ToolKind.Select : currentTool;
            SetTool(ToolKind.ColorPicker);
            SetStatus("Click the canvas to pick " + target + " color");
        }

        private void SetColor(string target, Color color, bool applyToSelection)
        {
            if (target == "fill") fillColor = color;
            else if (target == "outline") outlineColor = color;
            else if (target == "line") lineColor = color;
            else if (target == "text") textColor = color;
            else if (target == "background") backgroundColor = color;

            if (applyToSelection && selectedItem != null)
            {
                var html = ColorTranslator.ToHtml(color);
                if (target == "fill" && (selectedItem.Type == "rect" || selectedItem.Type == "ellipse"))
                {
                    selectedItem.FillColor = html;
                }
                else if (target == "outline" && (selectedItem.Type == "rect" || selectedItem.Type == "ellipse"))
                {
                    selectedItem.OutlineColor = html;
                }
                else if (target == "line" && (selectedItem.Type == "rect" || selectedItem.Type == "ellipse"))
                {
                    selectedItem.OutlineColor = html;
                }
                else if (target == "line" && selectedItem.Type == "wire")
                {
                    selectedItem.LineColor = html;
                }
                else if (target == "line" && selectedItem.Type == "image")
                {
                    selectedItem.LineColor = html;
                }
                else if (target == "text" && selectedItem.Type != "wire" && selectedItem.Type != "image")
                {
                    selectedItem.TextColor = html;
                }
            }

            UpdateColorButtons();
            canvas.Invalidate();
            RecordHistory();
        }

        private void UpdateColorButtons()
        {
            SetButtonColor(fillColorButton, fillColor);
            SetButtonColor(outlineColorButton, outlineColor);
            SetButtonColor(lineColorButton, lineColor);
            SetButtonColor(textColorButton, textColor);
            SetButtonColor(backgroundColorButton, backgroundColor);

            if (fillPanelButton != null)
            {
                fillPanelButton.BackColor = fillColor;
                outlinePanelButton.BackColor = outlineColor;
                linePanelButton.BackColor = lineColor;
                textPanelButton.BackColor = textColor;
                backgroundPanelButton.BackColor = backgroundColor;
            }
        }

        private void SetButtonColor(ToolStripButton button, Color color)
        {
            button.BackColor = color;
            button.ForeColor = color.GetBrightness() < 0.45f ? Color.White : Color.Black;
            button.DisplayStyle = ToolStripItemDisplayStyle.Text;
        }

        private Color PickCanvasColor(PointF point)
        {
            var imageItem = FindImageAt(point);
            if (imageItem != null)
            {
                var image = GetImage(imageItem);
                if (image != null)
                {
                    using (var bitmap = new Bitmap(image))
                    {
                        var rect = imageItem.Bounds.ToRectangleF();
                        var px = (int)Math.Round((point.X - rect.X) / rect.Width * (bitmap.Width - 1));
                        var py = (int)Math.Round((point.Y - rect.Y) / rect.Height * (bitmap.Height - 1));
                        px = (int)Clamp(px, 0, bitmap.Width - 1);
                        py = (int)Clamp(py, 0, bitmap.Height - 1);
                        return bitmap.GetPixel(px, py);
                    }
                }
            }

            using (var bitmap = RenderToBitmap(1f))
            {
                var px = (int)Clamp(point.X, 0, bitmap.Width - 1);
                var py = (int)Clamp(point.Y, 0, bitmap.Height - 1);
                return bitmap.GetPixel(px, py);
            }
        }

        private DiagramItem FindImageAt(PointF point)
        {
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item.Type == "image" && item.Bounds.ToRectangleF().Contains(point))
                {
                    return item;
                }
            }
            return null;
        }

        private void ApplyPickedColor(Color color)
        {
            var target = activeColorPickTarget;
            SetColor(target, color, true);
            SetStatus("Picked " + ColorTranslator.ToHtml(color) + " for " + target);
            SetTool(toolBeforeColorPick);
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
            if (item.Type == "rect" || item.Type == "image")
            {
                return item.Bounds.ToRectangleF().Contains(point);
            }
            if (item.Type == "ellipse")
            {
                var rect = item.Bounds.ToRectangleF();
                var cx = rect.Left + rect.Width / 2f;
                var cy = rect.Top + rect.Height / 2f;
                var rx = rect.Width / 2f;
                var ry = rect.Height / 2f;
                if (rx <= 0f || ry <= 0f) return false;
                var dx = (point.X - cx) / rx;
                var dy = (point.Y - cy) / ry;
                return dx * dx + dy * dy <= 1f;
            }
            if (item.Type == "wire")
            {
                var points = GetWireDisplayPoints(item);
                for (var i = 0; i < points.Count - 1; i++)
                {
                    if (DistanceToSegment(point, points[i], points[i + 1]) <= Math.Max(7f, item.LineWidth + 3f))
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

        private RectangleF GetBounds(DiagramItem item)
        {
            if (item.Type == "rect" || item.Type == "ellipse" || item.Type == "image")
            {
                return item.Bounds.ToRectangleF();
            }
            if (item.Type == "wire")
            {
                var points = GetWireDisplayPoints(item);
                if (points.Count == 0)
                {
                    return RectangleF.Empty;
                }
                var minX = points.Min(p => p.X);
                var minY = points.Min(p => p.Y);
                var maxX = points.Max(p => p.X);
                var maxY = points.Max(p => p.Y);
                return new RectangleF(minX, minY, Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY));
            }
            if (item.Type == "text" && item.Points.Count > 0)
            {
                var text = item.Label ?? string.Empty;
                var width = Math.Max(40f, text.Length * 9f);
                var point = item.Points[0].ToPointF();
                return new RectangleF(point.X - width / 2f, point.Y - 14f, width, 28f);
            }
            return RectangleF.Empty;
        }

        private bool HasResizableBounds(DiagramItem item)
        {
            return item != null && (item.Type == "rect" || item.Type == "ellipse" || item.Type == "image");
        }

        private PointF Snap(PointF point)
        {
            if (!snapToGrid)
            {
                return point;
            }
            return new PointF((float)Math.Round(point.X / gridSize) * gridSize, (float)Math.Round(point.Y / gridSize) * gridSize);
        }

        private RectangleF NormalizeRect(PointF first, PointF second)
        {
            var left = Math.Min(first.X, second.X);
            var top = Math.Min(first.Y, second.Y);
            var right = Math.Max(first.X, second.X);
            var bottom = Math.Max(first.Y, second.Y);
            return new RectangleF(left, top, right - left, bottom - top);
        }

        private float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
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
            t = Clamp(t, 0f, 1f);
            var projection = new PointF(a.X + t * dx, a.Y + t * dy);
            return Distance(point, projection);
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private Color ParseColor(string value, Color fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return fallback;
                }
                return ColorTranslator.FromHtml(value);
            }
            catch
            {
                return fallback;
            }
        }

        private void RenameSelected()
        {
            if (selectedItem == null || selectedItem.Type == "wire" || selectedItem.Type == "image")
            {
                SetStatus("No editable label selected");
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
            RecordHistory();
        }

        private void DeleteSelected()
        {
            if (selectedItem == null)
            {
                return;
            }
            RemoveConnectionsTo(selectedItem.Id);
            if (imageCache.ContainsKey(selectedItem.Id))
            {
                imageCache[selectedItem.Id].Dispose();
                imageCache.Remove(selectedItem.Id);
            }
            items.Remove(selectedItem);
            selectedItem = null;
            UpdatePropertiesPanel();
            SetStatus("Deleted selection");
            canvas.Invalidate();
            RecordHistory();
        }

        private void RemoveConnectionsTo(string itemId)
        {
            foreach (var item in items)
            {
                if (item.Type != "wire")
                {
                    continue;
                }
                if (item.StartConnection != null && item.StartConnection.ElementId == itemId)
                {
                    item.StartConnection = null;
                }
                if (item.EndConnection != null && item.EndConnection.ElementId == itemId)
                {
                    item.EndConnection = null;
                }
            }
        }

        private void CancelActiveGesture()
        {
            drawStart = null;
            dragKind = DragKind.None;
            resizeHandle = string.Empty;
            activeWirePointIndex = -1;
            CancelPendingWire();
        }

        private void NewFile()
        {
            if (items.Count > 0)
            {
                var result = MessageBox.Show(this, "Start a new diagram? Unsaved changes may be lost.", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            ClearImages();
            items.Clear();
            selectedItem = null;
            drawStart = null;
            pendingWirePoints.Clear();
            pendingStartConnection = null;
            currentPath = string.Empty;
            nextItemNumber = 1;
            backgroundColor = Color.FromArgb(248, 248, 248);
            UpdateColorButtons();
            UpdatePropertiesPanel();
            ClearHistory();
            SetStatus("New diagram");
            canvas.Invalidate();
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
                        ClearImages();
                        items.Clear();
                        items.AddRange(document.Items ?? new List<DiagramItem>());
                        backgroundColor = ParseColor(document.BackgroundColor, Color.FromArgb(248, 248, 248));
                        selectedItem = null;
                        currentPath = dialog.FileName;
                        nextItemNumber = GetNextItemNumber();
                        UpdateColorButtons();
                        UpdatePropertiesPanel();
                        ClearHistory();
                        SetStatus("Opened " + Path.GetFileName(dialog.FileName));
                        canvas.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not open file:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var document = new DiagramDocument();
                document.App = AppInfo.Name;
                document.Version = AppInfo.Version;
                document.BackgroundColor = ColorTranslator.ToHtml(backgroundColor);
                document.Items = items;
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
                MessageBox.Show(this, "Could not save file:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetNextItemNumber()
        {
            var max = 0;
            foreach (var item in items)
            {
                if (item.Id != null && item.Id.StartsWith("item-"))
                {
                    int number;
                    if (int.TryParse(item.Id.Substring(5), out number))
                    {
                        max = Math.Max(max, number);
                    }
                }
            }
            return max + 1;
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
                    MessageBox.Show(this, "Could not export SVG:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportPng()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG (*.png)|*.png";
                dialog.Title = "Export PNG";
                dialog.DefaultExt = "png";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    using (var bitmap = RenderToBitmap(2f))
                    {
                        bitmap.Save(dialog.FileName, ImageFormat.Png);
                    }
                    SetStatus("Exported " + Path.GetFileName(dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not export PNG:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportPdf()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PDF (*.pdf)|*.pdf";
                dialog.Title = "Export PDF";
                dialog.DefaultExt = "pdf";
                ConfigureDialogInitialDirectory(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    using (var bitmap = RenderToBitmap(2f))
                    {
                        WriteImagePdf(dialog.FileName, bitmap, canvas.Width, canvas.Height);
                    }
                    SetStatus("Exported " + Path.GetFileName(dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not export PDF:\n" + ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private Bitmap RenderToBitmap(float scale)
        {
            var width = Math.Max(1, (int)(canvas.Width * scale));
            var height = Math.Max(1, (int)(canvas.Height * scale));
            var bitmap = new Bitmap(width, height);
            bitmap.SetResolution(96f * scale, 96f * scale);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                RenderDiagram(graphics, canvas.ClientSize, false, scale);
            }
            return bitmap;
        }

        private void WriteImagePdf(string path, Bitmap bitmap, int pageWidth, int pageHeight)
        {
            byte[] imageBytes;
            using (var imageStream = new MemoryStream())
            {
                var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                var parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                bitmap.Save(imageStream, encoder, parameters);
                imageBytes = imageStream.ToArray();
            }

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var offsets = new List<long>();
                WriteAscii(stream, "%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");

                offsets.Add(stream.Position);
                WriteAscii(stream, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

                offsets.Add(stream.Position);
                WriteAscii(stream, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

                offsets.Add(stream.Position);
                WriteAscii(stream, string.Format("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0} {1}] /Resources << /XObject << /Im0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n", pageWidth, pageHeight));

                var content = string.Format("q\n{0} 0 0 {1} 0 0 cm\n/Im0 Do\nQ\n", pageWidth, pageHeight);
                var contentBytes = Encoding.ASCII.GetBytes(content);
                offsets.Add(stream.Position);
                WriteAscii(stream, string.Format("4 0 obj\n<< /Length {0} >>\nstream\n", contentBytes.Length));
                stream.Write(contentBytes, 0, contentBytes.Length);
                WriteAscii(stream, "endstream\nendobj\n");

                offsets.Add(stream.Position);
                WriteAscii(stream, string.Format("5 0 obj\n<< /Type /XObject /Subtype /Image /Width {0} /Height {1} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {2} >>\nstream\n", bitmap.Width, bitmap.Height, imageBytes.Length));
                stream.Write(imageBytes, 0, imageBytes.Length);
                WriteAscii(stream, "\nendstream\nendobj\n");

                var xref = stream.Position;
                WriteAscii(stream, "xref\n0 6\n0000000000 65535 f \n");
                foreach (var offset in offsets)
                {
                    WriteAscii(stream, offset.ToString("0000000000") + " 00000 n \n");
                }
                WriteAscii(stream, "trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
            }
        }

        private void WriteAscii(Stream stream, string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        private string BuildSvg()
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.AppendLine(string.Format("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0}\" height=\"{1}\" viewBox=\"0 0 {0} {1}\">", canvas.Width, canvas.Height));
            builder.AppendLine("<defs>");
            builder.AppendLine("<marker id=\"ak-arrow\" markerWidth=\"10\" markerHeight=\"8\" refX=\"9\" refY=\"4\" orient=\"auto\" markerUnits=\"strokeWidth\">");
            builder.AppendLine("<path d=\"M0,0 L10,4 L0,8 Z\" fill=\"context-stroke\"/>");
            builder.AppendLine("</marker>");
            builder.AppendLine("</defs>");
            builder.AppendLine(string.Format("<rect x=\"0\" y=\"0\" width=\"{0}\" height=\"{1}\" fill=\"{2}\"/>", canvas.Width, canvas.Height, ColorTranslator.ToHtml(backgroundColor)));

            foreach (var item in items)
            {
                if (item.Type == "rect")
                {
                    var rect = item.Bounds.ToRectangleF();
                    builder.AppendLine(string.Format("<rect x=\"{0:F2}\" y=\"{1:F2}\" width=\"{2:F2}\" height=\"{3:F2}\" fill=\"{4}\" stroke=\"{5}\" stroke-width=\"{6:F2}\"{7}/>", rect.X, rect.Y, rect.Width, rect.Height, item.FillColor, item.OutlineColor, item.LineWidth, GetSvgStrokeDashArray(item.LineStyle)));
                    AppendSvgText(builder, rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, item);
                }
                else if (item.Type == "ellipse")
                {
                    var rect = item.Bounds.ToRectangleF();
                    builder.AppendLine(string.Format("<ellipse cx=\"{0:F2}\" cy=\"{1:F2}\" rx=\"{2:F2}\" ry=\"{3:F2}\" fill=\"{4}\" stroke=\"{5}\" stroke-width=\"{6:F2}\"{7}/>", rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, rect.Width / 2f, rect.Height / 2f, item.FillColor, item.OutlineColor, item.LineWidth, GetSvgStrokeDashArray(item.LineStyle)));
                    AppendSvgText(builder, rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, item);
                }
                else if (item.Type == "wire" && item.Points.Count >= 2)
                {
                    UpdateWireConnectionPoints(item);
                    var marker = item.Arrow ? " marker-end=\"url(#ak-arrow)\"" : string.Empty;
                    var displayPoints = GetWireDisplayPoints(item);
                    if (displayPoints.Count < 2)
                    {
                        continue;
                    }
                    if (IsCurvedWire(item) && displayPoints.Count >= 3)
                    {
                        builder.AppendLine(string.Format("<path d=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"{2:F2}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{3}{4}/>", BuildCurvedSvgPath(displayPoints), item.LineColor, item.LineWidth, GetSvgStrokeDashArray(item.LineStyle), marker));
                    }
                    else
                    {
                        var points = string.Join(" ", displayPoints.Select(p => string.Format("{0:F2},{1:F2}", p.X, p.Y)));
                        builder.AppendLine(string.Format("<polyline points=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"{2:F2}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{3}{4}/>", points, item.LineColor, item.LineWidth, GetSvgStrokeDashArray(item.LineStyle), marker));
                    }
                }
                else if (item.Type == "text" && item.Points.Count >= 1)
                {
                    AppendSvgText(builder, item.Points[0].X, item.Points[0].Y, item);
                }
                else if (item.Type == "image" && !string.IsNullOrWhiteSpace(item.ImageDataBase64))
                {
                    var rect = item.Bounds.ToRectangleF();
                    var mime = item.ImageExtension == "jpeg" ? "image/jpeg" : item.ImageExtension == "bmp" ? "image/bmp" : "image/png";
                    builder.AppendLine(string.Format("<image x=\"{0:F2}\" y=\"{1:F2}\" width=\"{2:F2}\" height=\"{3:F2}\" href=\"data:{4};base64,{5}\"/>", rect.X, rect.Y, rect.Width, rect.Height, mime, item.ImageDataBase64));
                }
            }

            builder.AppendLine("</svg>");
            return builder.ToString();
        }

        private void AppendSvgText(StringBuilder builder, float x, float y, DiagramItem item)
        {
            builder.AppendLine(string.Format("<text x=\"{0:F2}\" y=\"{1:F2}\" fill=\"{2}\" font-family=\"{3}\" font-size=\"18\" text-anchor=\"middle\" dominant-baseline=\"middle\">{4}</text>", x, y, item.TextColor, EscapeXml(item.FontFamily), EscapeXml(item.Label ?? string.Empty)));
        }

        private string GetSvgStrokeDashArray(string style)
        {
            var normalized = NormalizeLineStyle(style);
            if (normalized == "dash")
            {
                return " stroke-dasharray=\"12 7\"";
            }
            if (normalized == "dot")
            {
                return " stroke-dasharray=\"2 6\"";
            }
            return string.Empty;
        }

        private string BuildCurvedSvgPath(List<PointF> points)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("M {0:F2},{1:F2}", points[0].X, points[0].Y);
            for (var i = 1; i < points.Count; i++)
            {
                var previous = points[i - 1];
                var current = points[i];
                var midX = (previous.X + current.X) / 2f;
                var midY = (previous.Y + current.Y) / 2f;
                builder.AppendFormat(" Q {0:F2},{1:F2} {2:F2},{3:F2}", previous.X, previous.Y, midX, midY);
                if (i == points.Count - 1)
                {
                    builder.AppendFormat(" T {0:F2},{1:F2}", current.X, current.Y);
                }
            }
            return builder.ToString();
        }

        private string EscapeXml(string value)
        {
            return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
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
                var resolvedPath = Path.IsPathRooted(configuredPath)
                    ? Path.GetFullPath(configuredPath)
                    : Path.GetFullPath(Path.Combine(appDirectory, configuredPath));

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

        private void ClearImages()
        {
            foreach (var image in imageCache.Values)
            {
                image.Dispose();
            }
            imageCache.Clear();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ClearImages();
            base.OnFormClosed(e);
        }
    }

    public class HandleHit
    {
        public string Name { get; private set; }
        public int Index { get; private set; }

        public HandleHit(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }

    public static class PromptDialog
    {
        public static string Show(string text, string caption, string defaultValue)
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

        public static string Show(string text, string caption)
        {
            return Show(text, caption, string.Empty);
        }
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                NativeMethods.SetProcessDPIAware();
            }
            catch
            {
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DiagramForm());
        }
    }
}
