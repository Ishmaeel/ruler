using System;
using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;

namespace Ruler
{
    sealed public class MainForm : Form, IRulerInfo
    {
        private ToolTip _toolTip = new ToolTip();
        private Point _offset;
        private Rectangle _mouseDownRect;
        private int _resizeBorderWidth = 5;
        private Point _mouseDownPoint;
        private ResizeRegion _resizeRegion = ResizeRegion.None;
        private ContextMenu _menu = new ContextMenu();
        private MenuItem _verticalMenuItem;
        private MenuItem _toolTipMenuItem;
        private MenuItem _lockedMenuItem;
        private DragMode _dragMode = DragMode.None;

        public MainForm()
        {
            RulerInfo rulerInfo = RulerInfo.GetDefaultRulerInfo();

            this.Init(rulerInfo);
        }

        public MainForm(RulerInfo rulerInfo)
        {
            this.Init(rulerInfo);
        }

        public bool IsVertical
        {
            get { return this._verticalMenuItem.Checked; }
            set { this._verticalMenuItem.Checked = value; }
        }

        public bool IsLocked
        {
            get;
            set;
        }

        public bool ShowToolTip
        {
            get
            {
                return this._toolTipMenuItem.Checked;
            }
            set
            {
                this._toolTipMenuItem.Checked = value;

                if (value)
                {
                    this.SetToolTip();
                }
            }
        }

        private void Init(RulerInfo rulerInfo)
        {
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            ResourceManager resources = new ResourceManager(typeof(MainForm));
            this.Icon = ((Icon)(resources.GetObject("$this.Icon")));

            this.SetUpMenu(rulerInfo);

            this.Text = "Ruler";
            this.BackColor = Color.White;

            rulerInfo.CopyInto(this);

            this.FormBorderStyle = FormBorderStyle.None;

            this.ContextMenu = _menu;
            this.Font = new Font("Tahoma", 10);

            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        private RulerInfo GetRulerInfo()
        {
            RulerInfo rulerInfo = new RulerInfo();

            this.CopyInto(rulerInfo);

            return rulerInfo;
        }

        private void SetUpMenu(RulerInfo rulerInfo)
        {
            MenuItem stayOnTopMenuItem = this.AddMenuItem("Stay On Top");
            this._verticalMenuItem = this.AddMenuItem("Vertical");
            this._toolTipMenuItem = this.AddMenuItem("Tool Tip");
            MenuItem opacityMenuItem = this.AddMenuItem("Opacity");
            this._lockedMenuItem = this.AddMenuItem("Lock resizing", Shortcut.None, this.LockHandler);
            this.AddMenuItem("Set size...", Shortcut.None, this.SetWidthHeightHandler);
            this.AddMenuItem("Duplicate", Shortcut.None, this.DuplicateHandler);
            this.AddMenuItem("-");
            this.AddMenuItem("About...");
            this.AddMenuItem("-");
            this.AddMenuItem("Exit");

            stayOnTopMenuItem.Checked = rulerInfo.TopMost;
            _lockedMenuItem.Checked = rulerInfo.IsLocked;

            for (int i = 10; i <= 100; i += 10)
            {
                MenuItem subMenu = new MenuItem(i + "%");
                subMenu.Checked = i == (rulerInfo.Opacity * 100);
                subMenu.Click += new EventHandler(OpacityMenuHandler);
                opacityMenuItem.MenuItems.Add(subMenu);
            }
        }

        private void SetWidthHeightHandler(object sender, EventArgs e)
        {
            SetSizeForm form = new SetSizeForm(this.Width, this.Height);

            if (this.TopMost)
            {
                form.TopMost = true;
            }

            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Size size = form.GetNewSize();

                this.Width = size.Width;
                this.Height = size.Height;
            }
        }

        private void LockHandler(object sender, EventArgs e)
        {
            this.IsLocked = !this.IsLocked;
            this._lockedMenuItem.Checked = this.IsLocked;
        }

        private void DuplicateHandler(object sender, EventArgs e)
        {
            string exe = System.Reflection.Assembly.GetExecutingAssembly().Location;

            RulerInfo rulerInfo = this.GetRulerInfo();

            ProcessStartInfo startInfo = new ProcessStartInfo(exe, rulerInfo.ConvertToParameters());

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
        }

        private MenuItem AddMenuItem(string text)
        {
            return AddMenuItem(text, Shortcut.None, this.MenuHandler);
        }

        private MenuItem AddMenuItem(string text, Shortcut shortcut, EventHandler handler)
        {
            MenuItem mi = new MenuItem(text);
            mi.Click += new EventHandler(handler);
            mi.Shortcut = shortcut;
            _menu.MenuItems.Add(mi);

            return mi;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _offset = new Point(MousePosition.X - Location.X, MousePosition.Y - Location.Y);
            _mouseDownPoint = MousePosition;
            _mouseDownRect = ClientRectangle;

            if (!IsLocked && IsInResizableArea())
                _dragMode = DragMode.Resize;
            else
                _dragMode = DragMode.Move;

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _resizeRegion = ResizeRegion.None;
            _dragMode = DragMode.None;

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            switch (_dragMode)
            {
                case DragMode.Move:

                    Location = new Point(MousePosition.X - _offset.X, MousePosition.Y - _offset.Y);

                    break;

                case DragMode.Resize:

                    HandleResize();

                    break;

                default:

                    if (IsInResizableArea())
                    {
                        Point clientCursorPos = PointToClient(MousePosition);
                        _resizeRegion = GetResizeRegion(clientCursorPos);
                        SetResizeCursor(_resizeRegion);
                    }
                    else
                    {
                        Cursor = Cursors.Default;
                    }

                    break;
            }

            base.OnMouseMove(e);
        }

        private bool IsInResizableArea()
        {
            Point clientCursorPos = PointToClient(MousePosition);
            Rectangle resizeInnerRect = ClientRectangle;
            resizeInnerRect.Inflate(-_resizeBorderWidth, -_resizeBorderWidth);

            return ClientRectangle.Contains(clientCursorPos) && !resizeInnerRect.Contains(clientCursorPos);
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.ShowToolTip)
            {
                this.SetToolTip();
            }

            base.OnResize(e);
        }

        private void SetToolTip()
        {
            _toolTip.SetToolTip(this, string.Format("Width: {0} pixels\nHeight: {1} pixels", Width, Height));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    HandleMoveResizeKeystroke(e);
                    break;

                case Keys.Space:
                    ChangeOrientation();
                    break;
            }

            base.OnKeyDown(e);
        }

        private void HandleMoveResizeKeystroke(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            {
                if (e.Control)
                {
                    if (e.Shift)
                    {
                        Width += 1;
                    }
                    else
                    {
                        Left += 1;
                    }
                }
                else
                {
                    Left += 5;
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (e.Control)
                {
                    if (e.Shift)
                    {
                        Width -= 1;
                    }
                    else
                    {
                        Left -= 1;
                    }
                }
                else
                {
                    Left -= 5;
                }
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (e.Control)
                {
                    if (e.Shift)
                    {
                        Height -= 1;
                    }
                    else
                    {
                        Top -= 1;
                    }
                }
                else
                {
                    Top -= 5;
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (e.Control)
                {
                    if (e.Shift)
                    {
                        Height += 1;
                    }
                    else
                    {
                        Top += 1;
                    }
                }
                else
                {
                    Top += 5;
                }
            }
        }

        private void HandleResize()
        {
            if (this.IsLocked)
            {
                return;
            }

            int diffX = MousePosition.X - _mouseDownPoint.X;
            int diffY = MousePosition.Y - _mouseDownPoint.Y;

            if ((_resizeRegion & ResizeRegion.N) != 0)
            {
                Top = MousePosition.Y;
                diffY = -diffY;
            }

            if ((_resizeRegion & ResizeRegion.W) != 0)
            {
                Left = MousePosition.X;
                diffX = -diffX;
            }

            if ((_resizeRegion & ResizeRegion.N) != 0 || (_resizeRegion & ResizeRegion.S) != 0)
            {
                Height = _mouseDownRect.Height + diffY;
            }

            if ((_resizeRegion & ResizeRegion.W) != 0 || (_resizeRegion & ResizeRegion.E) != 0)
            {
                Width = _mouseDownRect.Width + diffX;
            }
        }

        private void SetResizeCursor(ResizeRegion region)
        {
            if (IsLocked)
            {
                Cursor = Cursors.Default;
                return;
            }

            switch (region)
            {
                case ResizeRegion.N:
                case ResizeRegion.S:
                    Cursor = Cursors.SizeNS;
                    break;

                case ResizeRegion.E:
                case ResizeRegion.W:
                    Cursor = Cursors.SizeWE;
                    break;

                case ResizeRegion.NW:
                case ResizeRegion.SE:
                    Cursor = Cursors.SizeNWSE;
                    break;

                default:
                    Cursor = Cursors.SizeNESW;
                    break;
            }
        }

        private ResizeRegion GetResizeRegion(Point clientCursorPos)
        {
            if (clientCursorPos.Y <= _resizeBorderWidth)
            {
                if (clientCursorPos.X <= _resizeBorderWidth) return ResizeRegion.NW;
                else if (clientCursorPos.X >= Width - _resizeBorderWidth) return ResizeRegion.NE;
                else return ResizeRegion.N;
            }
            else if (clientCursorPos.Y >= Height - _resizeBorderWidth)
            {
                if (clientCursorPos.X <= _resizeBorderWidth) return ResizeRegion.SW;
                else if (clientCursorPos.X >= Width - _resizeBorderWidth) return ResizeRegion.SE;
                else return ResizeRegion.S;
            }
            else
            {
                if (clientCursorPos.X <= _resizeBorderWidth) return ResizeRegion.W;
                else return ResizeRegion.E;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            int height = Height;
            int width = Width;

            if (IsVertical)
            {
                graphics.RotateTransform(90);
                graphics.TranslateTransform(0, -Width + 1);
                height = Width;
                width = Height;
            }

            DrawRuler(graphics, width, height);

            base.OnPaint(e);
        }

        private void DrawRuler(Graphics g, int formWidth, int formHeight)
        {
            // Border
            g.DrawRectangle(Pens.Black, 0, 0, formWidth - 1, formHeight - 1);

            // Width
            g.DrawString(formWidth + " pixels", Font, Brushes.Black, 20, (formHeight / 2) - (Font.Height / 2));

            // Ticks
            for (int i = 0; i < formWidth; i++)
            {
                if (i % 2 == 0)
                {
                    int tickHeight;
                    if (i % 100 == 0)
                    {
                        tickHeight = 15;
                        DrawTickLabel(g, i.ToString(), i, formHeight, tickHeight);
                    }
                    else if (i % 10 == 0)
                    {
                        tickHeight = 10;
                    }
                    else
                    {
                        tickHeight = 5;
                    }

                    DrawTick(g, i, formHeight, tickHeight);
                }
            }
        }

        private static void DrawTick(Graphics g, int xPos, int formHeight, int tickHeight)
        {
            // Top
            g.DrawLine(Pens.Black, xPos, 0, xPos, tickHeight);

            // Bottom
            g.DrawLine(Pens.Black, xPos, formHeight, xPos, formHeight - tickHeight);
        }

        private void DrawTickLabel(Graphics g, string text, int xPos, int formHeight, int height)
        {
            if (formHeight < 60)
            {
                // Middle
                g.DrawString(text, Font, Brushes.Black, xPos, (formHeight / 2) - (Font.Height / 2));
            }
            else
            {
                // Top
                g.DrawString(text, Font, Brushes.Black, xPos, height);

                // Bottom
                g.DrawString(text, Font, Brushes.Black, xPos, formHeight - height - Font.Height);
            }
        }

        private static void Main(params string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm mainForm;

            if (args.Length == 0)
            {
                mainForm = new MainForm();
            }
            else
            {
                mainForm = new MainForm(RulerInfo.CovertToRulerInfo(args));
            }

            Application.Run(mainForm);
        }

        private void OpacityMenuHandler(object sender, EventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            UncheckMenuItem(mi.Parent);
            mi.Checked = true;
            Opacity = double.Parse(mi.Text.Replace("%", "")) / 100;
        }

        private void UncheckMenuItem(Menu parent)
        {
            if (parent == null || parent.MenuItems == null)
            {
                return;
            }

            for (int i = 0; i < parent.MenuItems.Count; i++)
            {
                if (parent.MenuItems[i].Checked)
                {
                    parent.MenuItems[i].Checked = false;
                }
            }
        }

        private void MenuHandler(object sender, EventArgs e)
        {
            MenuItem mi = (MenuItem)sender;

            switch (mi.Text)
            {
                case "Exit":
                    Close();
                    break;

                case "Tool Tip":
                    ShowToolTip = !ShowToolTip;
                    break;

                case "Vertical":
                    ChangeOrientation();
                    break;

                case "Stay On Top":
                    mi.Checked = !mi.Checked;
                    TopMost = mi.Checked;
                    break;

                case "About...":
                    using (AboutForm form = new Ruler.AboutForm())
                    {
                        form.ShowDialog(this);
                    }
                    break;

                default:
                    MessageBox.Show("Unknown menu item.");
                    break;
            }
        }

        private void ChangeOrientation()
        {
            this.IsVertical = !IsVertical;
            int width = Width;
            this.Width = Height;
            this.Height = width;

            Invalidate();
        }
    }
}