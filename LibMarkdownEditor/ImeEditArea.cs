﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LibMarkdownEditor.Internal;

namespace LibMarkdownEditor
{
    public abstract class ImeEditArea : FrameworkElement
    {
        static ImeEditArea()
        {
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(ImeEditArea), new FrameworkPropertyMetadata(true));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ImeEditArea), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
            InputMethod.IsInputMethodSuspendedProperty.OverrideMetadata(typeof(ImeEditArea), new PropertyMetadata(true));

            FocusableProperty.OverrideMetadata(typeof(ImeEditArea), new UIPropertyMetadata(true));
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(typeof(ImeEditArea));

        public static readonly DependencyProperty FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner(typeof(ImeEditArea));

        public static readonly DependencyProperty FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner(typeof(ImeEditArea));

        public static readonly DependencyProperty FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner(typeof(ImeEditArea));

        public static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(typeof(ImeEditArea));

        protected abstract Point GetEditorLeftTop();
        protected abstract Point GetCaretLeftTop();
        protected virtual double GetCaretHeight()
        {
            return FontSize;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            Log($"GotKeyboardFocus");

            if (IsKeyboardFocused)
            {
                if (_hwndSource != null)
                    return;
                _isUpdatingCompositionWindow = true;
                CreateContext();
            }
            else
            {
                ClearContext();
                return;
            }

            UpdateCompositionWindow();
            _isUpdatingCompositionWindow = false;

            base.OnGotKeyboardFocus(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            Log($"LostKeyboardFocus");
            if (_isUpdatingCompositionWindow)
                return;
            if (Equals(e.OldFocus, this) && _currentContext != IntPtr.Zero)
            {
                IMENative.ImmNotifyIME(_currentContext, IMENative.NI_COMPOSITIONSTR, IMENative.CPS_CANCEL);
            }

            ClearContext();

            base.OnLostKeyboardFocus(e);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();

            base.OnMouseDown(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var caretStart = GetCaretLeftTop();
            var caretHeight = GetCaretHeight() * 1.333;

            var pen = new Pen(Foreground, SizeFromDevice(1.0));
            drawingContext.DrawLine(pen, caretStart, caretStart + new Vector(0, caretHeight));

            base.OnRender(drawingContext);
        }

        [MemberNotNull(nameof(_hwndSource))]
        private void EnsureHwndSource()
        {
            if (_hwndSource != null)
            {
                return;
            }

            _hwndSource = (HwndSource)(PresentationSource.FromVisual(this));
            if (_hwndSource == null)
            {
                throw new InvalidOperationException("Window handle is not initialized");
            }
        }

        private double SizeFromDevice(double size)
        {
            EnsureHwndSource();

            var transformHeightVector = _hwndSource.CompositionTarget.TransformFromDevice.Transform(new Point(size, 0));

            if (transformHeightVector.Y == 0)
            {
                return transformHeightVector.X;
            }
            else
            {
                return Math.Sqrt(
                    transformHeightVector.X * transformHeightVector.X + transformHeightVector.Y * transformHeightVector.Y);
            }
        }

        private void CreateContext()
        {
            ClearContext();
            EnsureHwndSource();

            _defaultImeWnd = IMENative.ImmGetDefaultIMEWnd(IntPtr.Zero);
            Log($"_defaultImeWnd={_defaultImeWnd}");
            if (_defaultImeWnd == IntPtr.Zero)
            {
                // 如果拿到了空的默认 IME 窗口了，那么此时也许是作为嵌套窗口放入到另一个进程的窗口
                // 拿不到就需要刷新一下。否则微软拼音输入法将在屏幕的左上角上
                RefreshInputMethodEditors();

                // 尝试通过 _hwndSource 也就是文本所在的窗口去获取
                _defaultImeWnd = IMENative.ImmGetDefaultIMEWnd(_hwndSource.Handle);
                Log($"_defaultImeWnd2={_defaultImeWnd}");

                if (_defaultImeWnd == IntPtr.Zero)
                {
                    // 如果依然获取不到，那么使用当前激活的窗口，在准备输入的时候
                    // 当前的窗口大部分都是对的
                    // 进入这里，是尽可能恢复输入法，拿到的 GetForegroundWindow 虽然预计是不对的
                    // 也好过没有输入法
                    _defaultImeWnd = IMENative.ImmGetDefaultIMEWnd(Win32.User32.GetForegroundWindow());
                    Log($"_defaultImeWnd3={_defaultImeWnd}");
                }
            }

            // 使用 DefaultIMEWnd 可以比较好解决微软拼音的输入法到屏幕左上角的问题
            _currentContext = IMENative.ImmGetContext(_defaultImeWnd);
            Log($"_currentContext={_currentContext}");
            if (_currentContext == IntPtr.Zero)
            {
                _currentContext = IMENative.ImmGetContext(_hwndSource.Handle);
                Log($"_currentContext2={_currentContext}");
            }

            // 对 Win32 使用第二套输入法框架的输入法，可以采用 ImmAssociateContext 关联
            // 但是对实现 TSF 第三套输入法框架的输入法，在应用程序对接第三套输入法框架
            // 就需要调用 ITfThreadMgr 的 SetFocus 方法。刚好 WPF 对接了
            _previousContext = IMENative.ImmAssociateContext(_hwndSource.Handle, _currentContext);
            _hwndSource.AddHook(WndProc);

            // 尽管文档说传递null是无效的，但这似乎有助于在与WPF共享的默认输入上下文中激活IME输入法
            // 这里需要了解的是，在 WPF 的逻辑，是需要传入 DefaultTextStore.Current.DocumentManager 才符合预期
            var threadMgr = IMENative.GetTextFrameworkThreadManager();
            threadMgr?.SetFocus(IntPtr.Zero);
        }

        private void ClearContext()
        {
            if (_hwndSource == null)
                return;

            IMENative.ImmAssociateContext(_hwndSource.Handle, _previousContext);
            IMENative.ImmReleaseContext(_defaultImeWnd, _currentContext);
            _currentContext = IntPtr.Zero;
            _defaultImeWnd = IntPtr.Zero;
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        /// <summary>
        /// 更新CompositionWindow位置，用于跟随输入光标。调用此方法时，将从 <see cref="IIMETextEditor"/> 获取参数
        /// </summary>
        public void UpdateCompositionWindow()
        {
            if (_currentContext == IntPtr.Zero)
            {
                CreateContext();
                if (_currentContext == IntPtr.Zero)
                {
                    return;
                }
            }

            // 这是判断在系统版本大于 Win7 的系统，如 Win10 系统上，使用微软拼音输入法的逻辑
            // 微软拼音输入法在几个版本，需要修改 Y 坐标，加上输入的行高才可以。但是在一些 Win10 版本，通过补丁又修了这个问题
            //_isSoftwarePinYinOverWin7 = IsMsInputMethodOverWin7();
            //上面判断微软拼音的方法，会导致方法被切片，从而在快速得到焦点和失去焦点时，失去焦点清理的代码会先于此函数执行，导致引发错误
            if (_hwndSource == null)
                return;
            SetCompositionFont();
            SetCompositionWindow();
        }

        private void SetCompositionFont()
        {
            var faceName = FontFamily.Source;
            var fontSize = FontSize;

            var lf = new IMENative.LOGFONT();
            lf.lfFaceName = faceName;
            lf.lfHeight = (int)(fontSize * 96 / 72);
            lf.lfWeight = FontWeight.ToOpenTypeWeight();
            lf.lfItalic = FontStyle == FontStyles.Italic ? (byte)1 : (byte)0;

            if (_hwndSource is not null)
            {
                var transformHeightVector = _hwndSource.CompositionTarget.TransformToDevice.Transform(new Point(fontSize * 96 / 72, 0));

                if (transformHeightVector.Y == 0)
                {
                    lf.lfHeight = (int)transformHeightVector.X;
                }
                else
                {
                    lf.lfHeight = (int)Math.Sqrt(
                        transformHeightVector.X * transformHeightVector.X + transformHeightVector.Y * transformHeightVector.Y);
                }
            }

            var hIMC = _currentContext;

            var GCS_COMPSTR = 8;

            var length = IMENative.ImmGetCompositionString(hIMC, GCS_COMPSTR, null, 0);
            if (length > 0)
            {
                var target = new byte[length];
                var count = IMENative.ImmGetCompositionString(hIMC, GCS_COMPSTR, target, length);
                if (count > 0)
                {
                    var inputString = Encoding.Default.GetString(target);
                    if (string.IsNullOrWhiteSpace(inputString))
                    {
                        lf.lfWidth = 1;
                    }
                }
            }

            Log($"ImmSetCompositionFont");
            IMENative.ImmSetCompositionFont(hIMC, ref lf);
        }

        private void SetCompositionWindow()
        {
            var hIMC = _currentContext;
            HwndSource source = _hwndSource ?? throw new ArgumentNullException(nameof(_hwndSource));

            var editorLeftTop = this.GetEditorLeftTop();
            var caretLeftTop = this.GetCaretLeftTop();

            var transformToAncestor = this.TransformToAncestor(source.RootVisual);

            var editorLeftTopForRootVisual = transformToAncestor.Transform(editorLeftTop);
            var caretLeftTopForRootVisual = transformToAncestor.Transform(caretLeftTop);

            var deviceEditorLeftTopForRootVisual = source.CompositionTarget.TransformToDevice.Transform(editorLeftTopForRootVisual);
            var deviceCaretLeftTopForRootVisual = source.CompositionTarget.TransformToDevice.Transform(caretLeftTopForRootVisual);

            //解决surface上输入法光标位置不正确
            //现象是surface上光标的位置需要乘以2才能正确，普通电脑上没有这个问题
            //且此问题与DPI无关，目前用CaretWidth可以有效判断
            deviceCaretLeftTopForRootVisual = new Point(
                deviceCaretLeftTopForRootVisual.X / SystemParameters.CaretWidth,
                deviceCaretLeftTopForRootVisual.Y / SystemParameters.CaretWidth);

            //const int CFS_DEFAULT = 0x0000;
            //const int CFS_RECT = 0x0001;
            const int CFS_POINT = 0x0002;
            //const int CFS_FORCE_POSITION = 0x0020;
            //const int CFS_EXCLUDE = 0x0080;
            //const int CFS_CANDIDATEPOS = 0x0040;

            var form = new IMENative.CompositionForm();
            form.dwStyle = CFS_POINT;
            form.ptCurrentPos.x = (int)Math.Max(deviceCaretLeftTopForRootVisual.X, deviceEditorLeftTopForRootVisual.X);
            form.ptCurrentPos.y = (int)Math.Max(deviceCaretLeftTopForRootVisual.Y, deviceEditorLeftTopForRootVisual.Y);
            //if (_isSoftwarePinYinOverWin7)
            //{
            //    form.ptCurrentPos.y += (int) characterBounds.Height;
            //}

            Log($"ImmSetCompositionWindow x={form.ptCurrentPos.x} y={form.ptCurrentPos.y}");
            IMENative.ImmSetCompositionWindow(hIMC, ref form);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (msg)
            {
                case IMENative.WM_INPUTLANGCHANGE:
                    Log($"WM_INPUTLANGCHANGE");
                    if (_hwndSource != null)
                    {
                        CreateContext();
                    }

                    break;
                case IMENative.WM_IME_COMPOSITION:
                    Log($"WM_IME_COMPOSITION");
                    UpdateCompositionWindow();
                    break;

                    //case (int) Win32.WM.IME_NOTIFY:
                    // 根据 WPF 的源代码，是需要在此消息里，调用 ImmSetCandidateWindow 进行更新的
                    // 但是似乎不写也没啥锅，于是就先不写了
                    // 下次遇到，可以了解到这里还没有完全抄代码
                    //    {
                    //        Debug.WriteLine("IME_NOTIFY");
                    //        break;
                    //    }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 刷新 IME 的 ITfThreadMgr 状态，用于修复打开 Win32Dialog 之后关闭，输入法无法输入中文问题
        /// </summary>
        /// 原因是在打开 Win32Dialog 之后，将会让 ITfThreadMgr 失去焦点。因此需要使用本方法刷新，通过 InputMethod 的 IsInputMethodEnabledProperty 属性调用到 InputMethod 的 EnableOrDisableInputMethod 方法，在这里面调用到 TextServicesContext.DispatcherCurrent.SetFocusOnDefaultTextStore 方法，从而调用到 SetFocusOnDim(DefaultTextStore.Current.DocumentManager) 的代码，将 DefaultTextStore.Current.DocumentManager 设置为 ITfThreadMgr 的焦点，重新绑定 IME 输入法
        /// 但是即使如此，依然拿不到 <see cref="_defaultImeWnd"/> 的初始值。依然需要重新打开和关闭 WPF 窗口才能拿到
        /// [Can we public the `DefaultTextStore.Current.DocumentManager` property to create custom TextEditor with IME · Issue #6139 · dotnet/wpf](https://github.com/dotnet/wpf/issues/6139 )
        private void RefreshInputMethodEditors()
        {
            if (InputMethod.GetIsInputMethodEnabled(this))
            {
                InputMethod.SetIsInputMethodEnabled(this, false);
            }

            if (InputMethod.GetIsInputMethodSuspended(this))
            {
                InputMethod.SetIsInputMethodSuspended(this, false);
            }

            InputMethod.SetIsInputMethodEnabled(this, true);
            InputMethod.SetIsInputMethodSuspended(this, true);
        }

        private IntPtr _defaultImeWnd;
        private IntPtr _currentContext;
        private IntPtr _previousContext;
        private HwndSource? _hwndSource;

        private bool _isUpdatingCompositionWindow;

        [Conditional("DEBUG")]
        private static void Log(string message)
        {
            Debug.WriteLine($"[IMESupport] {message}");
        }
    }
}
