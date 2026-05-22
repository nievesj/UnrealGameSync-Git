#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

using UGSGit.PluginAbstractions;

namespace UGSGit.Views
{
    /// <summary>
    /// Renders annotation badges (e.g., "Editor" build-available) alongside
    /// branch/tag labels in the commit graph row.
    /// </summary>
    public class CommitAnnotationPresenter : Control
    {
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
            var x = 0.0;

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
                x += item.Width + 4;
            }

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

            return annotation.AnnotationType switch
            {
                "build-available" => Brushes.Green,
                "game-available" => Brushes.Orange,
                _ => Brushes.Gray // Default style for unknown types
            };
        }

        private readonly List<RenderItem> _items = new();
        private Models.Commit? _watchedCommit;
    }
}