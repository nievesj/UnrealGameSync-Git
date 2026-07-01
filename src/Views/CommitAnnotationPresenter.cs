#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

using UGSGit.PluginAbstractions;

namespace SourceGit.Views
{
    /// <summary>
    /// Renders annotation badges (e.g., "Editor" build-available) alongside
    /// branch/tag labels in the commit graph row.
    /// </summary>
    public class CommitAnnotationPresenter : Control
    {
        /// <summary>
        /// Badge display order — lower values appear first (leftmost).
        /// Unknown types sort last, then alphabetically.
        /// </summary>
        private static readonly Dictionary<string, int> s_badgeOrder = new()
        {
            ["build-available"] = 0,
            ["game-available"] = 1,
            ["commit-code"] = 2,
            ["commit-content"] = 3,
        };

        // Cached brushes to avoid allocation on every render (M6)
        private static readonly IBrush s_codeDark = new SolidColorBrush(Color.FromRgb(0x00, 0x45, 0x8A));
        private static readonly IBrush s_codeLight = new SolidColorBrush(Color.FromRgb(0x74, 0xB9, 0xFF));
        private static readonly IBrush s_contentDark = new SolidColorBrush(Color.FromRgb(0x53, 0x36, 0x96));
        private static readonly IBrush s_contentLight = new SolidColorBrush(Color.FromRgb(0xA2, 0x9B, 0xFF));

        public class RenderItem
        {
            public FormattedText Label { get; set; } = null!;
            public IBrush Brush { get; set; } = null!;
            public string AnnotationType { get; set; } = string.Empty;
            public double Width { get; set; }
        }

        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<CommitAnnotationPresenter>();

        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
           TextBlock.FontSizeProperty.AddOwner<CommitAnnotationPresenter>();

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<CommitAnnotationPresenter, IBrush>(nameof(Background), Brushes.Transparent);

        public IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<CommitAnnotationPresenter, IBrush>(nameof(Foreground), Brushes.White);

        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        static CommitAnnotationPresenter()
        {
            AffectsMeasure<CommitAnnotationPresenter>(
                FontFamilyProperty,
                FontSizeProperty,
                ForegroundProperty,
                BackgroundProperty);
        }

        public override void Render(DrawingContext context)
        {
            if (_items.Count == 0)
                return;

            var y = 0.5;
            var x = 1.5;

            context.FillRectangle(Brushes.Transparent, Bounds);

            foreach (var item in _items)
            {
                var entireRect = new RoundedRect(new Rect(x, y, item.Width, 16), new CornerRadius(4));

                // Background
                using (context.PushOpacity(0.2))
                    context.DrawRectangle(item.Brush, null, entireRect);

                // Border
                context.DrawRectangle(null, new Pen(item.Brush), entireRect);

                // Label text
                context.DrawText(item.Label, new Point(x + 4, y + 8.0 - item.Label.Height * 0.5));

                x += item.Width + 4;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // Unsubscribe from previous commit's property changes
            if (_watchedCommit != null)
                _watchedCommit.PropertyChanged -= OnCommitPropertyChanged;

            _watchedCommit = DataContext as Models.Commit;

            // Subscribe to Annotations property changes so we re-render when async annotations arrive
            if (_watchedCommit != null)
                _watchedCommit.PropertyChanged += OnCommitPropertyChanged;

            InvalidateMeasure();
        }

        private void OnCommitPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Models.Commit.Annotations))
            {
                InvalidateMeasure();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (_watchedCommit != null)
            {
                _watchedCommit.PropertyChanged -= OnCommitPropertyChanged;
                _watchedCommit = null;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _items.Clear();

            if (DataContext is not Models.Commit commit || commit.Annotations == null || commit.Annotations.Count == 0)
                return new Size(0, 0);

            var typeface = new Typeface(FontFamily);
            var fg = Foreground;
            var labelSize = FontSize;

            foreach (var annotation in commit.Annotations)
            {
                var label = new FormattedText(
                    annotation.Label,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    labelSize,
                    fg);

                var brush = GetBrushForAnnotation(annotation);

                var item = new RenderItem
                {
                    Label = label,
                    Brush = brush,
                    AnnotationType = annotation.AnnotationType,
                    Width = 8 + label.Width + 4
                };

                _items.Add(item);
            }

            // Sort badges by defined order (build-available, game-available, commit-code, commit-content)
            _items.Sort((a, b) =>
            {
                var orderA = s_badgeOrder.TryGetValue(a.AnnotationType, out var oa) ? oa : int.MaxValue;
                var orderB = s_badgeOrder.TryGetValue(b.AnnotationType, out var ob) ? ob : int.MaxValue;
                if (orderA != orderB) return orderA.CompareTo(orderB);
                return string.Compare(a.AnnotationType, b.AnnotationType, StringComparison.Ordinal);
            });

            var x = 0.0;
            foreach (var item in _items)
                x += item.Width + 4;

            InvalidateVisual();
            return new Size(x + 2, 16);
        }

        private static IBrush GetBrushForAnnotation(CommitAnnotation annotation)
        {
            // Plugin-provided color override takes priority
            if (!string.IsNullOrEmpty(annotation.Color))
            {
                try
                {
                    return new SolidColorBrush(Color.Parse(annotation.Color));
                }
                catch
                {
                    // Invalid color — fall through to type-based mapping
                }
            }

            var isDarkTheme = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;

            return annotation.AnnotationType switch
            {
                "build-available" => Brushes.Green,
                "game-available" => Brushes.Orange,
                "commit-code" => isDarkTheme ? s_codeDark : s_codeLight,
                "commit-content" => isDarkTheme ? s_contentDark : s_contentLight,
                _ => Brushes.Gray // Default style for unknown types
            };
        }

        private readonly List<RenderItem> _items = new();
        private Models.Commit? _watchedCommit;
    }
}