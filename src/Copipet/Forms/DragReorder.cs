using System;
using System.Drawing;
using System.Windows.Forms;

namespace Copipet.Forms
{
    /// <summary>
    /// ボタンをドラッグして並べ替える仕組み（タブとコピーボタンで共用）。
    /// 一定距離（Windows のドラッグ開始距離）動かしたらドラッグ開始とみなし、
    /// カーソルの下の位置へ項目をその場で移動させる。
    /// 並べ替えの間はスクロール可能な入れ物の端でカーソルを止めると自動でスクロールする。
    /// </summary>
    internal sealed class DragReorder : IDisposable
    {
        private readonly Control _container;
        private readonly Func<Control, int> _indexOf;
        private readonly Func<Point, int> _hitTest;
        private readonly Action<int, int> _move;
        private readonly Action<bool> _stateChanged;
        private readonly Timer _scrollTimer;
        private Control _pressed;
        private Point _pressPoint;
        private bool _suppressClick;

        /// <param name="container">ボタンが並んでいる入れ物</param>
        /// <param name="indexOf">ボタン → 項目の位置</param>
        /// <param name="hitTest">入れ物内の座標 → 移動先の位置（-1 で移動しない）</param>
        /// <param name="move">項目を from から to へ移動して表示を更新する</param>
        /// <param name="stateChanged">ドラッグの開始・終了時に呼ぶ（引数は終了したら true）</param>
        public DragReorder(Control container, Func<Control, int> indexOf, Func<Point, int> hitTest,
            Action<int, int> move, Action<bool> stateChanged)
        {
            _container = container;
            _indexOf = indexOf;
            _hitTest = hitTest;
            _move = move;
            _stateChanged = stateChanged;
            DraggingIndex = -1;
            ScrollEdge = 24;
            ScrollStep = 12;

            var scrollable = container as ScrollableControl;
            if (scrollable != null && scrollable.AutoScroll)
            {
                _scrollTimer = new Timer { Interval = 40 };
                _scrollTimer.Tick += OnScrollTick;
            }
        }

        /// <summary>false の間はドラッグを受け付けない（通常モード）。</summary>
        public bool Enabled { get; set; }

        public bool IsDragging { get; private set; }

        /// <summary>ドラッグ中の項目の現在位置（ドラッグしていなければ -1）。</summary>
        public int DraggingIndex { get; private set; }

        /// <summary>自動スクロールを始める端からの距離（px）。</summary>
        public int ScrollEdge { get; set; }

        /// <summary>自動スクロール1回ぶんの量（px）。</summary>
        public int ScrollStep { get; set; }

        public void Attach(Control control)
        {
            control.MouseDown += OnMouseDown;
            control.MouseMove += OnMouseMove;
            control.MouseUp += OnMouseUp;
            control.MouseCaptureChanged += OnCaptureChanged;
        }

        public void Detach(Control control)
        {
            control.MouseDown -= OnMouseDown;
            control.MouseMove -= OnMouseMove;
            control.MouseUp -= OnMouseUp;
            control.MouseCaptureChanged -= OnCaptureChanged;
            if (control == _pressed) End();
        }

        /// <summary>
        /// Click の処理の最初に呼ぶ。ドラッグの直後に発生した Click なら true を返す（その Click は無視する）。
        /// </summary>
        public bool ConsumeClick()
        {
            if (IsDragging)
            {
                End();
            }
            if (_suppressClick)
            {
                _suppressClick = false;
                return true;
            }
            return false;
        }

        /// <summary>ドラッグを中止する（編集モードを抜けたときなど）。</summary>
        public void Cancel()
        {
            End();
            _suppressClick = false;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _suppressClick = false;
            if (!Enabled || e.Button != MouseButtons.Left) return;
            _pressed = (Control)sender;
            _pressPoint = Cursor.Position;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_pressed == null || sender != _pressed) return;

            if (!IsDragging)
            {
                Size threshold = SystemInformation.DragSize;
                Point now = Cursor.Position;
                if (Math.Abs(now.X - _pressPoint.X) < threshold.Width && Math.Abs(now.Y - _pressPoint.Y) < threshold.Height) return;

                int index = _indexOf(_pressed);
                if (index < 0)
                {
                    _pressed = null;
                    return;
                }
                IsDragging = true;
                DraggingIndex = index;
                _pressed.Cursor = Cursors.SizeAll;
                if (_scrollTimer != null) _scrollTimer.Start();
                _stateChanged(false);
            }

            DragTo(Cursor.Position);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (sender != _pressed) return;
            End();
        }

        private void OnCaptureChanged(object sender, EventArgs e)
        {
            // ボタンを離したときや、別のウィンドウに奪われたときに来る
            if (sender == _pressed && !_pressed.Capture) End();
        }

        private void DragTo(Point screenPoint)
        {
            Point p = _container.PointToClient(screenPoint);
            int target = _hitTest(p);
            if (target < 0 || target == DraggingIndex) return;
            int from = DraggingIndex;
            DraggingIndex = target;
            _move(from, target);
        }

        private void OnScrollTick(object sender, EventArgs e)
        {
            if (!IsDragging) return;
            var scrollable = (ScrollableControl)_container;
            Point p = _container.PointToClient(Cursor.Position);
            int delta = 0;
            if (p.Y < ScrollEdge) delta = -ScrollStep;
            else if (p.Y > _container.ClientSize.Height - ScrollEdge) delta = ScrollStep;

            if (delta != 0)
            {
                int y = Math.Max(0, -scrollable.AutoScrollPosition.Y + delta);
                scrollable.AutoScrollPosition = new Point(0, y);
            }
            DragTo(Cursor.Position);
        }

        private void End()
        {
            Control pressed = _pressed;
            _pressed = null;
            if (!IsDragging) return;

            IsDragging = false;
            DraggingIndex = -1;
            if (pressed != null) pressed.Cursor = Cursors.Default;
            if (_scrollTimer != null) _scrollTimer.Stop();
            _suppressClick = true;
            _stateChanged(true);
        }

        public void Dispose()
        {
            if (_scrollTimer != null) _scrollTimer.Dispose();
        }
    }
}
